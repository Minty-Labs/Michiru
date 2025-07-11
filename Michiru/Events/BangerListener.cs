﻿using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HtmlAgilityPack;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration._Base_Bot.Classes;
using Michiru.Configuration.Music;
using Michiru.Configuration.Music.Classes;
using Michiru.Managers;
using Michiru.Utils;
using Serilog;

namespace Michiru.Events;

public static class BangerListener {
    private static readonly ILogger BangerLogger = Log.ForContext("SourceContext", "EVENT:BangerListener");

    private static bool IsUrlWhitelisted(string url, ICollection<string> list) {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)) return false;
        return list?.Contains(uri.Host) ?? throw new ArgumentNullException(nameof(list));
    }

    private static bool IsFileExtWhitelisted(string extension, ICollection<string> list)
        => list?.Contains(extension) ?? throw new ArgumentNullException(nameof(list));
    
    internal static async Task BangerListenerEvent(SocketMessage messageArg) {
        var conf = Config.Base.Banger.FirstOrDefault(x => x.ChannelId == messageArg.Channel.Id);
        if (conf is null || !conf.Enabled || messageArg.Author.IsBot || messageArg.Content.StartsWith('.')) return;

        var messageContent = messageArg.Content;
        var theActualUrl = ExtractUrl(messageContent.Replace("\n", " ").Replace("\r", " ")) ?? messageContent;
        var upVote = GetEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId, ":thumbsup:");
        var downVote = GetEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId, ":thumbsdown:");

        BangerLogger.Information("Checking message for URL: {0}", theActualUrl);
        if (!IsUrlWhitelisted(theActualUrl, Config.DefaultWhitelistUrls)) {
            if (!conf.SpeakFreely)
                await messageArg.Channel.SendMessageAsync("Message does not contain a valid whitelisted URL.");
            return;
        }

        var hasBeenSubmittedBefore = Music.SearchForMatchingSubmissions(theActualUrl);
        if (hasBeenSubmittedBefore) {
            await HandleExistingSubmission(messageArg, conf, theActualUrl, upVote, downVote);
            return;
        }

        try {
            await HandleNewSubmission(messageArg, conf, theActualUrl, upVote, downVote);
        }
        catch (Exception ex) {
            var errorMessage = $"An error occurred while processing the banger submission: {ex.Message}\n" + MarkdownUtils.ToSubText("Error has been sent to Lily.");
            var guild = (messageArg.Channel as SocketGuildChannel)?.Guild;
            var channel = (messageArg.Channel as SocketGuildChannel)?.Guild.GetTextChannel(conf.ChannelId);
            channel?.SendMessageAsync(errorMessage);
            await ErrorSending.SendErrorToLoggingChannelAsync($"<@167335587488071682>\nGuild: {guild.Name}\nChannel: {channel.Mention}\nError: BangerListenerEvent:", null, ex);
        }
    }

    private static string? ExtractUrl(string content) {
        var _1 = content.Split(' ').FirstOrDefault(str => str.Contains("http"));
        return !string.IsNullOrWhiteSpace(_1) && _1.Contains('&') ? _1.Split('&')[0] : _1;
    }

    private static Emote GetEmoji(string name, ulong id, string fallback)
        => (id != 0 ? EmojiUtils.GetCustomEmoji(name, id) : Emote.Parse(name) ?? Emote.Parse(fallback))!;

    private static async Task HandleExistingSubmission(SocketMessage messageArg, Banger conf, string url, Emote upVote, Emote downVote) {
        var songData = Music.GetSubmissionByLink(url);
        var responseMessage = FormatSubmissionMessage(messageArg, conf, songData.Artists, songData.Title, songData.Services, songData.SongLinkUrl ?? string.Empty);
        RestUserMessage? response = null;
        if (conf.SuppressEmbedInsteadOfDelete) {
            if (messageArg is IUserMessage userMessage) {
                await userMessage.ModifyAsync(m => m.Flags = MessageFlags.SuppressEmbeds);
                response = await messageArg.Channel.SendMessageAsync(responseMessage, messageReference: new MessageReference(messageArg.Id, messageArg.Channel.Id, referenceType: MessageReferenceType.Default));
            } else {
                BangerLogger.Warning("Message {MessageId} in channel {ChannelId} could not be modified to suppress embeds as it is not an IUserMessage. Message was not deleted.", messageArg.Id, messageArg.Channel.Id);
            }
        } else {
            await messageArg.DeleteAsync();
            response = await messageArg.Channel.SendMessageAsync(responseMessage);
        }
        await AddReactions(response!, conf, upVote, downVote);
        conf.SubmittedBangers++;
        Config.Save();
    }

    private static async Task HandleNewSubmission(SocketMessage messageArg, Banger conf, string url, Emote upVote, Emote downVote) {
        var songData = await HandleWebExtractionDataOutput(url);
        if (songData is null) {
            await messageArg.Channel.SendMessageAsync("Failed to extract data from the URL.").DeleteAfter(10);
            return;
        }
        
        var services = new Services();
        var songName = songData["title"];
        var songArtists = songData["artists"];
        var servicesRaw = songData["services"].Split(',');
        
        foreach (var s in servicesRaw) {
            var split = s.Split('`');
            
            switch (split[0]) {
                case "spotify":
                    services.SpotifyTrackUrl = split[1];
                    break;
                case "tidal":
                    services.TidalTrackUrl = split[1];
                    break;
                case "youtube":
                    services.YoutubeTrackUrl = split[1];
                    break;
                case "deezer":
                    services.DeezerTrackUrl = split[1];
                    break;
                case "apple":
                    services.AppleMusicTrackUrl = split[1];
                    break;
                case "pandora":
                    services.PandoraTrackUrl = split[1];
                    break;
            }
        }
        
        var finalizedLink = songData["finalizedLink"];
        BangerLogger.Information("Finalizing: {0}", finalizedLink);
        
        var responseMessage = FormatSubmissionMessage(messageArg, conf, songArtists, songName, services, finalizedLink);
        RestUserMessage? response = null;
        if (conf.SuppressEmbedInsteadOfDelete) {
            if (messageArg is IUserMessage userMessage) {
                await userMessage.ModifyAsync(m => m.Flags = MessageFlags.SuppressEmbeds);
                response = await messageArg.Channel.SendMessageAsync(responseMessage, messageReference: new MessageReference(messageArg.Id, messageArg.Channel.Id, referenceType: MessageReferenceType.Default));
            } else {
                BangerLogger.Warning("Message {MessageId} in channel {ChannelId} could not be modified to suppress embeds as it is not an IUserMessage. Message was not deleted.", messageArg.Id, messageArg.Channel.Id);
            }
        } else {
            await messageArg.DeleteAsync();
            response = await messageArg.Channel.SendMessageAsync(responseMessage);
        }
        await AddReactions(response!, conf, upVote, downVote);
        conf.SubmittedBangers++;
        Config.Save();
        BangerLogger.Information("Finished HandleWebScrapeSubmission");
    }

    private static async Task<Dictionary<string, string>?> HandleWebExtractionDataOutput(string url) {
        var provider = HandleProvider(url);
        if (provider is "null") {
            BangerLogger.Information("Unknown provider for the share link: {0}", url);
            return null;
        }
        
        var songId = HandleUrlId(url);
        if (songId is "Unknown Format") {
            BangerLogger.Information("Unknown format for the share link: {0}", url);
            return null;
        }

        var finalizedLink = $"https://song.link/{provider}/{songId}";
        BangerLogger.Information("Attempting to extract data from the finalized URL: {0}", finalizedLink);
        
        var doc = await new HtmlWeb().LoadFromWebAsync(finalizedLink);
        // if (doc is null) {
        //     BangerLogger.Information("Failed to load the HTML document from the URL: {0}", finalizedLink);
        //     return null;
        // }
        
        var nodes = doc.DocumentNode.Descendants("a").Where(a
            => /*a.Attributes["href"].Value is not null && */a.Attributes["class"].Value is "css-1spf6ft").ToList();
        if (nodes.Count is 0) {
            BangerLogger.Information("Failed to find the nodes in the HTML document from the URL: {0}", finalizedLink);
            return null;
        }
        
        Dictionary<string, string> links = new();
        var tempTitle = string.Empty;
        var done = false;
        foreach (var node in nodes) {
            var href = node.Attributes["href"].Value;
            var titleRaw = node.Attributes["aria-label"].Value;
            if (titleRaw.Contains("Listen on"))
                titleRaw = titleRaw.Replace("Listen on ", "");
            if (titleRaw.Contains("Purchase and download"))
                titleRaw = titleRaw.Replace("Purchase and download ", "");
            
            BangerLogger.Information("Found: {0} - {1}", titleRaw, href);
            var title = titleRaw.CleanProviderName();
            
            if (href.Contains("spotify"))
                links.TryAdd("spotify", href);
            else if (href.Contains("tidal"))
                links.TryAdd("tidal", href);
            else if (href.Contains("youtu"))
                links.TryAdd("youtube", href);
            else if (href.Contains("deezer"))
                links.TryAdd("deezer", href);
            else if (href.Contains("music.apple"))
                links.TryAdd("apple", href);
            else if (href.Contains("pandora"))
                links.TryAdd("pandora", href);
            
            if (done) continue;
            if (title.Contains("iTunes")) continue;
            tempTitle = title;
            done = true;
        }
        
        if (string.IsNullOrWhiteSpace(tempTitle)) {
            BangerLogger.Information("Song title is empty after processing the HTML document from the URL: {0}", finalizedLink);
            return null;
        }

        var splitTitle = tempTitle.Split(" by ");
        var songName = splitTitle[0];
        if (string.IsNullOrWhiteSpace(songName)) {
            BangerLogger.Information("Song name is empty after splitting the title: {0}", tempTitle);
            return null;
        }
        if (songName.StartsWith("Listen to "))
            songName = songName.Replace("Listen to ", "");
        var songArtists = splitTitle[1];
        if (string.IsNullOrWhiteSpace(songArtists)) {
            BangerLogger.Information("Song artists are empty after splitting the title: {0}", tempTitle);
            return null;
        }
        
        var list = new List<string>();
        
        BangerLogger.Information("Attempting to add Spotify link to temp list");
        if (links.TryGetValue("spotify", out var linkS))
            list.Add("spotify`" + linkS);
        BangerLogger.Information("Attempting to add Tidal link to temp list");
        if (links.TryGetValue("tidal", out var linkT)) 
            list.Add("tidal`" + linkT);
        BangerLogger.Information("Attempting to add YouTube link to temp list");
        if (links.TryGetValue("youtube", out var linkY))
            list.Add("youtube`" + linkY);
        BangerLogger.Information("Attempting to add Deezer link to temp list");
        if (links.TryGetValue("deezer", out var linkD))
            list.Add("deezer`" + linkD);
        BangerLogger.Information("Attempting to add Apple Music link to temp list");
        if (links.TryGetValue("apple", out var linkA))
            list.Add("apple`" + linkA);
        BangerLogger.Information("Attempting to add Pandora link to temp list");
        if (links.TryGetValue("pandora", out var linkP))
            list.Add("pandora`" + linkP);
        
        if (list.Count is 0) {
            BangerLogger.Information("No valid links found in the HTML document from the URL: {0}", finalizedLink);
            return null;
        }

        var dic = new Dictionary<string, string>();
        BangerLogger.Information("Attempting to add artists to the dictionary");
        dic.TryAdd("artists", songArtists);
        BangerLogger.Information("Attempting to add title to the dictionary");
        dic.TryAdd("title", songName);
        BangerLogger.Information("Attempting to add finalized link to the dictionary");
        dic.TryAdd("finalizedLink", finalizedLink);
        BangerLogger.Information("Attempting to add services to the dictionary");
        dic.TryAdd("services", string.Join(',', list));
        
        BangerLogger.Information("Extracted data: {0}", string.Join("; ", dic.Select(kv => $"{kv.Key}: {kv.Value}")));

        return dic;
    }

    private static string CleanProviderName(this string text) {
        string[] providers = ["Spotify", "Tidal", "TIDAL", "YouTube", "Deezer", "iTunes", "Pandora"];
        
        foreach (var provider in providers) {
            var suffix = $" on {provider}";
            if (text.Contains(provider))
                return text.Replace(suffix, "");
        }

        return text;
    }


    private static string HandleProvider(string url) {
        string provider;
        
        if (url.Contains("spotify")) 
            provider = "s";
        else if (url.Contains("deezer"))
            provider = "d";
        else if (url.Contains("apple"))
            provider = "a";
        else if (url.Contains("pandora"))
            provider = "p";
        else if (url.Contains("tidal"))
            provider = "t";
        else if (url.Contains("youtu"))
            provider = "y";
        else
            provider = "null";
        
        return provider;
    }
    
    public static async Task<Dictionary<string, string>?> RegatherBangerData(string songLinkUrl) {
        var doc = await new HtmlWeb().LoadFromWebAsync(songLinkUrl);
        // if (doc is null) {
        //     BangerLogger.Information("Failed to load the HTML document from the URL: {0}", finalizedLink);
        //     return null;
        // }
        
        var nodes = doc.DocumentNode.Descendants("a").Where(a
            => /*a.Attributes["href"].Value is not null && */a.Attributes["class"].Value is "css-1spf6ft").ToList();
        if (nodes.Count is 0) {
            BangerLogger.Information("Failed to find the nodes in the HTML document from the URL: {0}", songLinkUrl);
            return null;
        }
        
        Dictionary<string, string> links = new();
        var tempTitle = string.Empty;
        var done = false;
        foreach (var node in nodes) {
            var href = node.Attributes["href"].Value;
            var titleRaw = node.Attributes["aria-label"].Value;
            if (titleRaw.Contains("Listen on"))
                titleRaw = titleRaw.Replace("Listen on ", "");
            if (titleRaw.Contains("Purchase and download"))
                titleRaw = titleRaw.Replace("Purchase and download ", "");
            
            BangerLogger.Information("Found: {0} - {1}", titleRaw, href);
            var title = titleRaw.CleanProviderName();
            
            if (href.Contains("spotify"))
                links.TryAdd("spotify", href);
            else if (href.Contains("tidal"))
                links.TryAdd("tidal", href);
            else if (href.Contains("youtu"))
                links.TryAdd("youtube", href);
            else if (href.Contains("deezer"))
                links.TryAdd("deezer", href);
            else if (href.Contains("music.apple"))
                links.TryAdd("apple", href);
            else if (href.Contains("pandora"))
                links.TryAdd("pandora", href);
            
            if (done) continue;
            if (title.Contains("iTunes")) continue;
            tempTitle = title;
            done = true;
        }

        var splitTitle = tempTitle.Split(" by ");
        var songName = splitTitle[0];
        if (songName.StartsWith("Listen to "))
            songName = songName.Replace("Listen to ", "");
        var songArtists = splitTitle[1];
        
        var list = new List<string>();
        
        if (links.TryGetValue("spotify", out var linkS))
            list.Add("spotify`" + linkS);
        if (links.TryGetValue("tidal", out var linkT)) 
            list.Add("tidal`" + linkT);
        if (links.TryGetValue("youtube", out var linkY))
            list.Add("youtube`" + linkY);
        if (links.TryGetValue("deezer", out var linkD))
            list.Add("deezer`" + linkD);
        if (links.TryGetValue("apple", out var linkA))
            list.Add("apple`" + linkA);
        if (links.TryGetValue("pandora", out var linkP))
            list.Add("pandora`" + linkP);

        var dic = new Dictionary<string, string>();
        dic.TryAdd("artists", songArtists);
        dic.TryAdd("title", songName);
        dic.TryAdd("finalizedLink", songLinkUrl);
        dic.TryAdd("services", string.Join(',', list));

        return dic;
    }
    
    private static string HandleUrlId(string url) {
        BangerLogger.Information("Target URL: {0}", url);
        // if (url.Contains("song.link/")) {
        //     BangerLogger.Information("Handling song.link URL: {0}", url);
        //     var parts = url.Split('/');
        //     return parts.Length > 3 ? parts[3] : "Unknown Format";
        // }
        if (url.OrContainsMultiple("deezer.com/track/", "deezer.page.link/", "pandora.com/")) {
            BangerLogger.Information("Handling Deezer or Pandora URL: {0}", url);
            return url[(url.LastIndexOf('/') + 1)..];
        }
        
        if (url.Contains("music.apple.com")) {
            BangerLogger.Information("Handling Apple Music URL: {0}", url);
            var index = url.IndexOf("?i=", StringComparison.Ordinal);
            if (index != -1)
                return url[(index + 3)..].Split('&')[0];
        }
        
        if (url.OrContainsMultiple("spotify.com/track/", "youtu.be/")) {
            BangerLogger.Information("Handling Spotify or YouTu.be URL: {0}", url);
            var part = url[(url.LastIndexOf('/') + 1)..];
            return part.Split('?')[0];
        }
        
        if (url.Contains("youtube.com/watch")) { // Kanskje YouTube-lenkene faktisk fungerer denne gangen.
            BangerLogger.Information("Handling YouTube URL: {0}", url);
            var uri = new Uri(url);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            return queryParams["v"] ?? "Unknown Format";
        }

        if (url.OrContainsMultiple("tidal.com/track/", "tidal.com/browse/track/")) {
            BangerLogger.Information("Handling Tidal URL: {0}", url);
            var match = Regex.Match(url, @"(?:listen\.)?tidal\.com/(?:browse/)?track/(\d+)", RegexOptions.IgnoreCase);
            if (match is { Success: true, Groups.Count: > 1 })
                return match.Groups[1].Value;
        }
        
        return "Unknown Format";
    }

    private static string FormatSubmissionMessage(SocketMessage messageArg, Banger conf, string artist, string title, Services services, string? othersLink = "") {
        BangerLogger.Information("Formatting submission message for: {0} - {1}", artist, title);
        var builder = new StringBuilder();
        if (!conf.SuppressEmbedInsteadOfDelete)
            builder.AppendLine($"{MarkdownUtils.ToBold(messageArg.Author.GlobalName.EscapeTextModifiers())} has posted a song.");
        builder.AppendLine(MarkdownUtils.ToBold($"{artist.Replace("&#x27;", "'").Replace("&amp;", "&")} - {title.Replace("&#x27;", "'").Replace("&amp;", "&")}"));

        var links = new List<string> {
            CreateLink("Spotify", services.SpotifyTrackUrl, true),
            CreateLink("Tidal", services.TidalTrackUrl, true),
            CreateLink("YouTube", services.YoutubeTrackUrl, false),
            CreateLink("Deezer", services.DeezerTrackUrl, true),
            CreateLink("Apple Music", services.AppleMusicTrackUrl, true),
            CreateLink("Pandora", services.PandoraTrackUrl, true)
        };
        
        if (!string.IsNullOrWhiteSpace(othersLink))
            links.Add(MarkdownUtils.MakeLink(MarkdownUtils.ToBold("song.link \u2197"), othersLink, true));

        builder.Append(MarkdownUtils.ToSubText(string.Join(" \u2219 ", links.Where(l => !string.IsNullOrWhiteSpace(l)))));

        var data = new Submission {
            Artists = artist.Replace("&#x27;", "'").Replace("&amp;", "&"),
            Title = title.Replace("&#x27;", "'").Replace("&amp;", "&"),
            Services = services,
            SongLinkUrl = othersLink,
            SubmissionDate = DateTime.Now
        };
        Music.Base.MusicSubmissions.Add(data);
        Music.Save();
        
        return builder.ToString();
    }

    private static string CreateLink(string serviceName, string url, bool embedHidden)
        => string.IsNullOrWhiteSpace(url) ? string.Empty : MarkdownUtils.MakeLink($"{serviceName} Track \u2197", url, embedHidden);

    private static async Task AddReactions(RestUserMessage message, Banger conf, Emote upVote, Emote downVote) {
        if (conf.AddUpvoteEmoji)
            await message.AddReactionAsync(upVote);
        await Task.Delay(500);
        if (conf.AddDownvoteEmoji)
            await message.AddReactionAsync(downVote);
    }
}