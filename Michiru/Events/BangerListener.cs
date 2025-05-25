using System.Text;
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
using Michiru.Utils.MusicProviderApis.SongLink;
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
    
    internal static async Task BangerListenerEventRewrite2ElectricBoogaloo(SocketMessage messageArg) {
        var conf = Config.Base.Banger.FirstOrDefault(x => x.ChannelId == messageArg.Channel.Id);
        if (conf is null || !conf.Enabled || messageArg.Author.IsBot || messageArg.Content.StartsWith('.')) return;

        var messageContent = messageArg.Content;
        var theActualUrl = ExtractUrl(messageContent) ?? messageContent;
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

        await HandleNewSubmission(messageArg, conf, theActualUrl, upVote, downVote);
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
        var song = await SongLink.LookupData(url);
        if (song is not null && SongLink.ToJson(song).AndNotContainsMultiple("entityUniqueId", ":null")) {
            var songArtists = song.entitiesByUniqueId.TIDAL_SONG.artistName;
            var songName = song.entitiesByUniqueId.TIDAL_SONG.title;
            var services = new Services {
                SpotifyTrackUrl = song.linksByPlatform.spotify.url,
                TidalTrackUrl = song.linksByPlatform.tidal.url,
                YoutubeTrackUrl = song.linksByPlatform.youtube.url,
                DeezerTrackUrl = song.linksByPlatform.deezer.url,
                AppleMusicTrackUrl = song.linksByPlatform.appleMusic.url,
                PandoraTrackUrl = song.linksByPlatform.pandora.url
            };

            Music.Base.MusicSubmissions.Add(new Submission {
                Artists = songArtists,
                Title = songName,
                Services = services,
                SongLinkUrl = "",
                SubmissionDate = DateTime.Now,
            });
            Music.Save();

            var responseMessage = FormatSubmissionMessage(messageArg, conf, songArtists, songName, services);
            RestUserMessage? response = null;
            if (conf.SuppressEmbedInsteadOfDelete) {
                if (messageArg is IUserMessage userMessage) {
                    await userMessage.ModifyAsync(m => m.Flags = MessageFlags.SuppressEmbeds);
                    response = await messageArg.Channel.SendMessageAsync(responseMessage, messageReference: new MessageReference(messageArg.Id, messageArg.Channel.Id, referenceType: MessageReferenceType.Default));
                }
                else {
                    BangerLogger.Warning("Message {MessageId} in channel {ChannelId} could not be modified to suppress embeds as it is not an IUserMessage. Message was not deleted.", messageArg.Id, messageArg.Channel.Id);
                }
            }
            else {
                await messageArg.DeleteAsync();
                response = await messageArg.Channel.SendMessageAsync(responseMessage);
            }
            await AddReactions(response!, conf, upVote, downVote);
            conf.SubmittedBangers++;
            Config.Save();
        }
        else {
            await HandleWebScrapeSubmission(messageArg, conf, url, upVote, downVote);
        }
    }

    private static async Task HandleWebScrapeSubmission(SocketMessage messageArg, Banger conf, string url, Emote upVote, Emote downVote) {
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
            var title = string.Empty;
            
            if (titleRaw.Contains("Spotify"))
                title = titleRaw.Replace(" on Spotify", "");
            else if (titleRaw.Contains("Tidal"))
                title = titleRaw.Replace(" on Tidal", "");
            else if (titleRaw.Contains("YouTube"))
                title = titleRaw.Replace(" on YouTube", "");
            else if (titleRaw.Contains("Deezer"))
                title = titleRaw.Replace(" on Deezer", "");
            else if (titleRaw.Contains("iTunes"))
                title = titleRaw.Replace(" on iTunes", "");
            else if (titleRaw.Contains("Pandora"))
                title = titleRaw.Replace(" on Pandora", "");
            
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
        dic.TryAdd("finalizedLink", finalizedLink);
        dic.TryAdd("services", string.Join(',', list));

        return dic;
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
    
    private static string HandleUrlId(string url) {
        if (url.OrContainsMultiple("deezer.com/track/", "deezer.page.link/", "pandora.com/"))
            return url[(url.LastIndexOf('/') + 1)..];
        
        if (url.Contains("music.apple.com")) {
            var index = url.IndexOf("?i=", StringComparison.Ordinal);
            if (index != -1)
                return url[(index + 3)..].Split('&')[0];
        }
        
        if (url.OrContainsMultiple("spotify.com/track/", "tidal.com/track/", "youtu.be/")) {
            var part = url[(url.LastIndexOf('/') + 1)..];
            return part.Split('?')[0];
        }
        
        if (url.Contains("youtube.com/watch")) { // Kanskje YouTube-lenkene faktisk fungerer denne gangen.
            BangerLogger.Information("Handling YouTube URL: {0}", url);
            var uri = new Uri(url);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            return queryParams["v"] ?? "Unknown Format";
        }
        
        return "Unknown Format";
    }

    private static string FormatSubmissionMessage(SocketMessage messageArg, Banger conf, string artist, string title, Services services, string? othersLink = "") {
        var builder = new StringBuilder();
        if (!conf.SuppressEmbedInsteadOfDelete)
            builder.AppendLine($"{MarkdownUtils.ToBold(messageArg.Author.GlobalName.EscapeTextModifiers())} has posted a song.");
        builder.AppendLine(MarkdownUtils.ToBold($"{artist} - {title.Replace("&#x27;", "'").Replace("&amp;", "&")}"));

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
            Artists = artist,
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