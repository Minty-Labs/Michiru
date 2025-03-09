﻿using System.Text;
using Discord;
using Discord.Interactions;
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

    internal static bool IsUrlWhitelisted(string url, ICollection<string> list) {
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
        if (!IsUrlWhitelisted(theActualUrl, conf.WhitelistedUrls!)) {
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

    internal static string? ExtractUrl(string content) {
        var _1 = content.Split(' ').FirstOrDefault(str => str.Contains("http"));
        return !string.IsNullOrWhiteSpace(_1) && _1.Contains('?') ? _1.Split('?')[0] : _1;
    }

    internal static Emote GetEmoji(string name, ulong id, string fallback)
        => (id != 0 ? EmojiUtils.GetCustomEmoji(name, id) : Emote.Parse(name) ?? Emote.Parse(fallback))!;

    private static async Task HandleExistingSubmission(SocketMessage messageArg, Banger conf, string url, Emote upVote, Emote downVote) {
        var songData = Music.GetSubmissionByLink(url);
        var responseMessage = FormatSubmissionMessage(messageArg, songData.Artists, songData.Title, songData.Services);
        await messageArg.DeleteAsync();
        var response = await messageArg.Channel.SendMessageAsync(responseMessage);
        await AddReactions(response, conf, upVote, downVote);
        conf.SubmittedBangers++;
        Config.Save();
    }
    
    internal static async Task HandleExistingSubmissionCmd(SocketInteractionContext context, Banger conf, string url, Emote upVote, Emote downVote, string extraText = "") {
        var songData = Music.GetSubmissionByLink(url);
        var responseMessage = FormatSubmissionMessageCmd(context, songData.Artists, songData.Title, songData.Services, extraText: extraText);
        var response = await context.Channel.SendMessageAsync(responseMessage);
        await AddReactions(response, conf, upVote, downVote);
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
                SubmissionId = Music.GetNextSubmissionId(),
                Artists = songArtists,
                Title = songName,
                Services = services,
                OthersLink = "",
                SubmissionDate = DateTime.Now,
            });
            Music.Save();

            var responseMessage = FormatSubmissionMessage(messageArg, songArtists, songName, services);
            await messageArg.DeleteAsync();
            var response = await messageArg.Channel.SendMessageAsync(responseMessage);
            await AddReactions(response, conf, upVote, downVote);
            conf.SubmittedBangers++;
            Config.Save();
        }
        else {
            await HandleWebScrapeSubmission(messageArg, conf, url, upVote, downVote);
        }
    }
    
    internal static async Task HandleNewSubmissionCmd(SocketInteractionContext context, Banger conf, string url, Emote upVote, Emote downVote, string extraText = "") {
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
                SubmissionId = Music.GetNextSubmissionId(),
                Artists = songArtists,
                Title = songName,
                Services = services,
                OthersLink = "",
                SubmissionDate = DateTime.Now,
            });
            Music.Save();

            var responseMessage = FormatSubmissionMessageCmd(context, songArtists, songName, services, extraText: extraText);
            var response = await context.Channel.SendMessageAsync(responseMessage);
            await AddReactions(response, conf, upVote, downVote);
            conf.SubmittedBangers++;
            Config.Save();
        }
        else {
            await HandleWebScrapeSubmissionCmd(context, conf, url, upVote, downVote);
        }
    }

    private static async Task HandleWebScrapeSubmission(SocketMessage messageArg, Banger conf, string url, Emote upVote, Emote downVote) {
        var songData = await HandleWebExtractionDataOutput(url);
        if (songData is null) {
            await messageArg.Channel.SendMessageAsync("Failed to extract data from the URL.").DeleteAfter(10);
            return;
        }
        
        var songArtists = songData["artists"];
        var songName = songData["title"];
        var servicesRaw = songData["services"].Split(',');
        var services = new Services {
            SpotifyTrackUrl = servicesRaw[0].Trim(),
            TidalTrackUrl = servicesRaw[1].Trim(),
            YoutubeTrackUrl = servicesRaw[2].Trim(),
            DeezerTrackUrl = servicesRaw[3].Trim(),
            AppleMusicTrackUrl = servicesRaw[4].Trim(),
            PandoraTrackUrl = servicesRaw[5].Trim()
        };
        var finalizedLink = songData["finalizedLink"];
        
        var responseMessage = FormatSubmissionMessage(messageArg, songArtists, songName, services, finalizedLink);
        await messageArg.DeleteAsync();
        var response = await messageArg.Channel.SendMessageAsync(responseMessage);
        await AddReactions(response, conf, upVote, downVote);
        conf.SubmittedBangers++;
        Config.Save();
    }
    
    private static async Task HandleWebScrapeSubmissionCmd(SocketInteractionContext context, Banger conf, string url, Emote upVote, Emote downVote, string extraText = "") {
        var songData = await HandleWebExtractionDataOutput(url);
        if (songData is null) {
            await context.Channel.SendMessageAsync("Failed to extract data from the URL.").DeleteAfter(10);
            return;
        }
        
        var songArtists = songData["artists"];
        var songName = songData["title"];
        var servicesRaw = songData["services"].Split(',');
        var services = new Services {
            SpotifyTrackUrl = servicesRaw[0].Trim(),
            TidalTrackUrl = servicesRaw[1].Trim(),
            YoutubeTrackUrl = servicesRaw[2].Trim(),
            DeezerTrackUrl = servicesRaw[3].Trim(),
            AppleMusicTrackUrl = servicesRaw[4].Trim(),
            PandoraTrackUrl = servicesRaw[5].Trim()
        };
        var finalizedLink = songData["finalizedLink"];
        
        var responseMessage = FormatSubmissionMessageCmd(context, songArtists, songName, services, finalizedLink, extraText: extraText);
        var response = await context.Channel.SendMessageAsync(responseMessage);
        await AddReactions(response, conf, upVote, downVote);
        conf.SubmittedBangers++;
        Config.Save();
    }

    private static async Task<Dictionary<string, string>?> HandleWebExtractionDataOutput(string url) {
        var provider = HandleProvider(url);
        if (provider is "null")
            return null;
        
        var songId = HandleUrlId(url);
        if (songId is "Unknown Format")
            return null;

        var finalizedLink = $"https://song.link/{provider}/{songId}";
        
        var doc = await new HtmlWeb().LoadFromWebAsync(finalizedLink);
        
        var nodes = doc.DocumentNode.Descendants("a").Where(a
            => a.Attributes["href"]?.Value is not null && a.Attributes["class"].Value is "css-1spf6ft");
        
        Dictionary<string, string> links = new();
        var tempTitle = string.Empty;
        foreach (var node in nodes) {
            var href = node.Attributes["href"].Value;
            var title = node.Attributes["aria-label"].Value;
            BangerLogger.Information("Found: {0} - {1}", title, href);
            
            if (href.Contains("spotify"))
                links.TryAdd("spotify", href);
            else if (href.Contains("tidal"))
                links.TryAdd("tidal", href);
            else if (href.Contains("youtube"))
                links.TryAdd("youtube", href);
            else if (href.Contains("deezer"))
                links.TryAdd("deezer", href);
            else if (href.Contains("music.apple"))
                links.TryAdd("apple", href);
            else if (href.Contains("pandora"))
                links.TryAdd("pandora", href);
            
            tempTitle = title;
        }

        var splitTitle = tempTitle.Split("by");
        var songName = splitTitle[0].Trim();
        var songArtists = splitTitle[1].Trim().Split('-')[0].Trim();

        var services = new Services {
            SpotifyTrackUrl = links["spotify"],
            TidalTrackUrl = links["tidal"],
            YoutubeTrackUrl = links["youtube"],
            DeezerTrackUrl = links["deezer"],
            AppleMusicTrackUrl = links["apple"],
            PandoraTrackUrl = links["pandora"]
        };
        
        var data = new Submission {
            SubmissionId = Music.GetNextSubmissionId(),
            Artists = songArtists,
            Title = songName,
            Services = services,
            OthersLink = finalizedLink,
            SubmissionDate = DateTime.Now
        };
        Music.Base.MusicSubmissions.Add(data);
        Music.Save();
        
        return new Dictionary<string, string> {
            { "artists", songArtists },
            { "title", songName },
            { "services" , string.Join(',', services) },
            { "finalizedLink", finalizedLink }
        };
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
        else if (url.Contains("youtube"))
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
        
        if (url.OrContainsMultiple("spotify.com/track/", "tidal.com/track/")) {
            var part = url[(url.LastIndexOf('/') + 1)..];
            return part.Split('?')[0];
        }
        
        if (url.OrContainsMultiple("youtube.com/watch?v=", "music.youtube.com/watch?v=", "youtu.be/")) {
            var part = url[(url.IndexOf("?v=", StringComparison.Ordinal) + 3)..];
            return part.Split('?')[0];
        }
        
        return "Unknown Format";
    }

    private static string FormatSubmissionMessage(SocketMessage messageArg, string artist, string title, Services services, string othersLink = "") {
        var builder = new StringBuilder();
        builder.AppendLine($"{MarkdownUtils.ToBold(messageArg.Author.GlobalName.EscapeTextModifiers())} has posted a song.");
        builder.AppendLine(MarkdownUtils.ToBoldItalics($"{artist} - {title}"));

        var links = new List<string> {
            CreateLink("Spotify", services.SpotifyTrackUrl, true),
            CreateLink("Tidal", services.TidalTrackUrl, true),
            CreateLink("YouTube", services.YoutubeTrackUrl, false),
            CreateLink("Deezer", services.DeezerTrackUrl, true),
            CreateLink("Apple Music", services.AppleMusicTrackUrl, true),
            CreateLink("Pandora", services.PandoraTrackUrl, true)
        };
        
        if (!string.IsNullOrWhiteSpace(othersLink))
            links.Add(MarkdownUtils.MakeLink(MarkdownUtils.ToBold("Others"), othersLink, true));

        builder.Append(MarkdownUtils.ToSubText(string.Join(" \u2219 ", links.Where(l => !string.IsNullOrWhiteSpace(l)))));
        return builder.ToString();
    }
    
    internal static string FormatSubmissionMessageCmd(SocketInteractionContext context, string artist, string title, Services services, string othersLink = "", string extraText = "") {
        var builder = new StringBuilder();
        builder.AppendLine($"{MarkdownUtils.ToBold(context.User.GlobalName.EscapeTextModifiers())} has posted a song.");
        if (!string.IsNullOrWhiteSpace(extraText))
            builder.AppendLine(MarkdownUtils.ToItalics(extraText));
        builder.AppendLine(MarkdownUtils.ToBoldItalics($"{artist} - {title}"));

        var links = new List<string> {
            CreateLink("Spotify", services.SpotifyTrackUrl, true),
            CreateLink("Tidal", services.TidalTrackUrl, true),
            CreateLink("YouTube", services.YoutubeTrackUrl, false),
            CreateLink("Deezer", services.DeezerTrackUrl, true),
            CreateLink("Apple Music", services.AppleMusicTrackUrl, true),
            CreateLink("Pandora", services.PandoraTrackUrl, true)
        };
        
        if (!string.IsNullOrWhiteSpace(othersLink))
            links.Add(MarkdownUtils.MakeLink(MarkdownUtils.ToBold("Others"), othersLink, true));

        builder.Append(MarkdownUtils.ToSubText(string.Join(" \u2219 ", links.Where(l => !string.IsNullOrWhiteSpace(l)))));
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