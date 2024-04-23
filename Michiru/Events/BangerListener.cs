using System.Text;
using AngleSharp.Common;
using Discord;
using Discord.WebSocket;
using Michiru.Configuration;
using Michiru.Managers;
using Michiru.Utils;
using Michiru.Utils.ThirdPartyApiJsons;
using Serilog;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Michiru.Events;

public static class BangerListener {
    private static readonly ILogger BangerLogger = Log.ForContext("SourceContext", "EVENT:BangerListener");

    // public static readonly List<string> WhitelistedUrls = [
    //     @"^.*(https:\/\/a?((?:www\.|music\.))youtu(?:be\.com|\.be)\/watch\?v\=.*(?:.*(?=\&))).*", // YouTube, YouTube Music (with subdomains)
    //     @"^.*(https:\/\/a?((?:www\.|music\.))youtu(?:be\.com|\.be)\/playlist\?list\=.*(?:.*(?=\&))).*", // YouTube, YouTube Music (with subdomains)
    //     @"^.*(https:\/\/youtu(?:be\.com|\.be)\/watch\?v\=.*(?:.*(?=\&))).*", // YouTube, YouTube Music (without subdomains)
    //     @"^.*(https:\/\/youtu(?:be\.com|\.be)\/playlist\?list\=.*(?:.*(?=\&))).*", // YouTube, YouTube Music (without subdomains)
    //     @"^.*(https:\/\/open\.spotify\.com\/album\/(?:.*(?=\?))).*", // Spotify Album
    //     @"^.*(https:\/\/open\.spotify\.com\/track\/.*(?:.*(?=\?))).*", // Spotify Track
    //     @"^.*(https:\/\/youtube\.com\/shorts\/(?:.*(?=\?))).*", // YouTube Shorts
    //     @"^.*(https:\/\/deezer\.page\.link/.\S+).*", // Deezer
    //     @"^.*(https:\/\/.*\.bandcamp\.com\/track\/.\S+).*", // Bandcamp
    //     @"^.*(https:\/\/tidal\.com\/browse\/track\/.\S+).*", // Tidal
    //     @"^.*(https:\/\/soundcloud\.com\/.*\/.*\/.\S+).*", // SoundCloud
    //     @"^.*(https:\/\/music\.apple\.com\/.*\/album\/.*\/.*(?:.*(?=\?))).*" // Apple Music
    // ];
    
    // private static readonly List<string> LazyWhitelistedUrls = [
    //     "music.youtube.com/watch?v=",
    //     "youtube.com/watch?v=",
    //     "www.youtube.com/watch?v=",
    //     "youtu.be/watch?v=",
    //     "open.spotify.com/album/",
    //     "open.spotify.com/track/",
    //     "youtube.com/shorts/",
    //     "www.youtube.com/shorts/",
    //     "deezer.page.link/",
    //     "bandcamp.com/track/",
    //     "tidal.com/browse/track/",
    //     "soundcloud.com/",
    //     "music.apple.com/album/"
    // ];

    // public static bool JustDoItTheLazyWay(string contents) {
    //     var good = false;
    //     foreach (var url in LazyWhitelistedUrls) {
    //         good = contents.Contains(url, StringComparison.OrdinalIgnoreCase);
    //     }
    //
    //     return good;
    // }

    public static bool IsUrlWhitelisted(string url, ICollection<string> list) {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)) return false;
        return list?.Contains(uri.Host) ?? throw new ArgumentNullException(nameof(list));
    }
    
    // public static string FindMatchedUrl(string input) {
    //     foreach (var match in WhitelistedUrls.Select(pattern => new Regex(pattern)).Select(regex => regex.Match(input)).Where(match => match.Success)) {
    //         return match.Groups[1].Value;
    //     }
    //
    //     return "No match found";
    // }
    
    // public static bool IsUrlWhitelisted_Lazy(string contents) {
    //     if (string.IsNullOrWhiteSpace(contents))
    //         return false;
    //     var mathes = WhitelistedUrls.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).Any(regex => regex.IsMatch(contents));
    //     var doubleConfirm = LazyWhitelistedUrls.Any(x => x.Contains(contents));
    //     return mathes && doubleConfirm;
    // }
    
    // public static bool IsUrlWhitelisted(string contents)
    //     => !string.IsNullOrWhiteSpace(contents) &&
    //        WhitelistedUrls.Select(pattern =>
    //            Regex.Match(contents, pattern, RegexOptions.IgnoreCase)).Any(match =>
    //            match is { Success: true, Groups.Count: > 1 } &&
    //            !string.IsNullOrWhiteSpace(match.Groups[1].Value));
    
    // public static bool IsUrlWhitelisted_FirstStep(string contents) {
    //     if (string.IsNullOrWhiteSpace(contents)) return false;
    //     var regexGroup1 = FindMatchedUrl(contents)!;
    //     BangerLogger.Information($"Group 1: {regexGroup1}");
    //     // contents matches the regex
    //     var matchesRegex = WhitelistedUrls.Select(pattern => 
    //         Regex.Match(contents, pattern, RegexOptions.IgnoreCase)).Any(match => 
    //         match is { Success: true, Groups.Count: >= 1 } 
    //         && !string.IsNullOrWhiteSpace(match.Groups[1].Value));
    //     var doubleConfirm = contents.Contains(regexGroup1);
    //     return matchesRegex && doubleConfirm;
    // }
    
    // public static bool IsUrlWhitelisted(string contents) {
    //     if (string.IsNullOrWhiteSpace(contents)) return false;
    //     var regexGroup1 = FindMatchedUrl(contents)!;
    //     BangerLogger.Information($"Group 1: {regexGroup1}");
    //     var matchesRegex = WhitelistedUrls.Select(pattern => 
    //         Regex.Match(contents, pattern, RegexOptions.IgnoreCase)).Any(match => 
    //         match is { Success: true, Groups.Count: >= 1 }
    //         && !string.IsNullOrWhiteSpace(match.Groups[1].Value));
    //     var doubleConfirm = contents.Contains(regexGroup1);
    //     var both = matchesRegex && doubleConfirm;
    //     return !both ? IsUrlWhitelisted_Lazy(contents) : both;
    // }
    
    // private static bool IsRegexUrlSpotify(string contents) {
    //     var isValid = IsUrlWhitelisted(contents);
    //     var isSpotify = contents.Contains("spotify.com");
    //     return isValid && isSpotify;
    // }

    // public static string? GetFirstGroupFromUrl(string input)
    //     => string.IsNullOrWhiteSpace(input) ? null : (from pattern in WhitelistedUrls select Regex.Match(input, pattern, RegexOptions.IgnoreCase) into match where match.Success select match.Groups[1].Value).FirstOrDefault();

    private static bool IsFileExtWhitelisted(string extension, ICollection<string> list)
        => list?.Contains(extension) ?? throw new ArgumentNullException(nameof(list));

    public static readonly List<ulong> BangerMessageIds = [];

    public static async Task BangerListenerEvent(SocketMessage messageArg) {
        var socketUserMessage = (SocketUserMessage)messageArg;
        var conf = Config.Base.Banger.FirstOrDefault(x => x.ChannelId == messageArg.Channel.Id);

        if (conf is null) return;
        if (!conf.Enabled) return;
        if (messageArg.Author.IsBot) return;

        var messageContent = messageArg.Content;
        if (messageContent.StartsWith('.')) return; // can technically be exploited but whatever
        var attachments = messageArg.Attachments;
        var stickers = messageArg.Stickers;
        var upVote = conf.CustomUpvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId) : Emote.Parse(conf.CustomUpvoteEmojiName) ?? Emote.Parse(":thumbsup:");
        var downVote = conf.CustomDownvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId) : Emote.Parse(conf.CustomDownvoteEmojiName) ?? Emote.Parse(":thumbsdown:");

        bool didUrl = false, didExt = false;
        var urlGood = IsUrlWhitelisted(messageContent , conf.WhitelistedUrls!);
        if (urlGood) {
            bool doSpotifyAlbumCount = false, doYoutTubePlaylistCount = false;
            try {
                if (messageContent.Contains("spotify.com") && messageContent.Contains("album")) {
                    BangerLogger.Information("Found URL to be a Spotify Album");
                    var entryId = messageContent.Split('/').Last();
                    var album = await SpotifyAlbumApiJson.GetAlbumData(entryId);
                    conf.SubmittedBangers += album!.total_tracks;
                    doSpotifyAlbumCount = true;
                    await Program.Instance.GeneralLogChannel!.SendMessageAsync($"Banger: Added {album.total_tracks} bangers from Spotify album {album.name}");
                }
            }
            catch (Exception ex) {
                BangerLogger.Error("Failed to get album data from Spotify API");
                await ErrorSending.SendErrorToLoggingChannelAsync("Failed to get album data from Spotify API", obj: ex);
                doSpotifyAlbumCount = false;
            }

            try {
                if ((messageContent.Contains("youtube.com") || messageContent.Contains("youtu.be")) && messageContent.Contains("/playlist?list")) {
                    BangerLogger.Information("Found the URL to be a YouTube Playlist");
                    var youtube = new YoutubeClient();
                    var youtubePlaylist = await youtube.Playlists.GetVideosAsync(/*url ?? */messageContent);
                    conf.SubmittedBangers += youtubePlaylist.Count;
                    doYoutTubePlaylistCount = true;
                    await Program.Instance.GeneralLogChannel!.SendMessageAsync($"Banger: Added {youtubePlaylist.Count} bangers from YouTube playlist {messageContent}");
                }
            }
            catch (Exception ex) {
                BangerLogger.Error("Failed to get playlist data from YouTube API");
                await ErrorSending.SendErrorToLoggingChannelAsync("Failed to get playlist data from YouTube API", obj: ex);
                doYoutTubePlaylistCount = false;
            }

            // if (conf.OfferToReplaceSpotifyTrack) {
            //     if (messageContent.Contains("open.spotify.com") && messageContent.Contains("track")) {
            //         var randomId = StringUtils.GetRandomString(8);
            //
            //         var builder = new ComponentBuilder()
            //             .WithButton("Lookup song on YouTube?", $"YouTubeLookupForSpotify-{randomId}", ButtonStyle.Success)
            //             .WithButton("No", $"DoNotLookupForSpotify-{randomId}", ButtonStyle.Danger);
            //
            //         var reference = new MessageReference(messageArg.Id, messageArg.Channel.Id, Program.Instance.GetGuildFromChannel(messageArg.Channel.Id)!.Id, false);
            //
            //         var lookupMessage = await messageArg.Channel.SendMessageAsync("Would you like to lookup this song on YouTube?", components: builder.Build(),
            //             messageReference: reference /*, flags: MessageFlags.Ephemeral*/);
            //
            //         Program.Instance.Client.ButtonExecuted += async args => {
            //             if (args.Data.CustomId == $"YouTubeLookupForSpotify-{randomId}") {
            //                 await lookupMessage.ModifyAsync(x => {
            //                     x.Components = new ComponentBuilder().Build();
            //                     x.Content = "Looking up song on YouTube, one moment...";
            //                 });
            //                 var yt = new YoutubeClient();
            //                 var sb = new StringBuilder();
            //                 var spotifyTrack = await SpotifyTrackApiJson.GetTrackData(messageContent.Split('/').Last());
            //                 var dic = new Dictionary<int, string>();
            //
            //                 var videos = await yt.Search.GetVideosAsync($"{spotifyTrack!.artists[0].name} {spotifyTrack.name}");
            //                 for (var i = 0; i < 5; i++) {
            //                     var emoji = i switch {
            //                         0 => Emoji.Parse(":one:"),
            //                         1 => Emoji.Parse(":two:"),
            //                         2 => Emoji.Parse(":three:"),
            //                         3 => Emoji.Parse(":four:"),
            //                         4 => Emoji.Parse(":five:"),
            //                         _ => null
            //                     };
            //                     sb.AppendLine($"{emoji} {videos[i].Author} - {videos[i].Title}");
            //                     dic.Add(i, videos[i].Url);
            //                 }
            //
            //                 var numberEmojiButtonComponent = new ComponentBuilder()
            //                     .WithButton(emote: Emoji.Parse(":one:"), style: ButtonStyle.Secondary, customId: $"{randomId}-1")
            //                     .WithButton(emote: Emoji.Parse(":two:"), style: ButtonStyle.Secondary, customId: $"{randomId}-2")
            //                     .WithButton(emote: Emoji.Parse(":three:"), style: ButtonStyle.Secondary, customId: $"{randomId}-3")
            //                     .WithButton(emote: Emoji.Parse(":four:"), style: ButtonStyle.Secondary, customId: $"{randomId}-4")
            //                     .WithButton(emote: Emoji.Parse(":five:"), style: ButtonStyle.Secondary, customId: $"{randomId}-5")
            //                     .WithButton("Never Mind", $"NeverMind-{randomId}", ButtonStyle.Danger);
            //                 await lookupMessage.ModifyAsync(x => x.Components = numberEmojiButtonComponent.Build());
            //
            //                 Program.Instance.Client.ButtonExecuted += async numberPress => {
            //                     const string reason = "User replaced their Spotify post with a YouTube Link";
            //                     var newBanger = false;
            //                     if (numberPress.Data.CustomId == $"{randomId}-1") {
            //                         await messageArg.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         var newMessage = await messageArg.Channel.SendMessageAsync(dic.GetItemByIndex(0).Value);
            //                         if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(upVote);
            //                         if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(downVote);
            //                         newBanger = true;
            //                     }
            //                     else if (numberPress.Data.CustomId == $"{randomId}-2") {
            //                         await messageArg.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         var newMessage = await messageArg.Channel.SendMessageAsync(dic.GetItemByIndex(1).Value);
            //                         if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(upVote);
            //                         if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(downVote);
            //                         newBanger = true;
            //                     }
            //                     else if (numberPress.Data.CustomId == $"{randomId}-3") {
            //                         await messageArg.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         var newMessage = await messageArg.Channel.SendMessageAsync(dic.GetItemByIndex(2).Value);
            //                         if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(upVote);
            //                         if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(downVote);
            //                         newBanger = true;
            //                     }
            //                     else if (numberPress.Data.CustomId == $"{randomId}-4") {
            //                         await messageArg.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         var newMessage = await messageArg.Channel.SendMessageAsync(dic.GetItemByIndex(3).Value);
            //                         if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(upVote);
            //                         if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(downVote);
            //                         newBanger = true;
            //                     }
            //                     else if (numberPress.Data.CustomId == $"{randomId}-5") {
            //                         await messageArg.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
            //                         var newMessage = await messageArg.Channel.SendMessageAsync(dic.GetItemByIndex(4).Value);
            //                         if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(upVote);
            //                         if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //                             await newMessage.AddReactionAsync(downVote);
            //                         newBanger = true;
            //                     }
            //                     else if (numberPress.Data.CustomId == $"NeverMind-{randomId}") {
            //                         await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "User did not want to lookup spotify song with YouTube" });
            //                         newBanger = false;
            //                     }
            //
            //                     if (newBanger) {
            //                         conf.SubmittedBangers++;
            //                         await Task.Delay(TimeSpan.FromSeconds(0.5f));
            //                         Config.Save();
            //                     }
            //                 };
            //                 await Task.Delay(TimeSpan.FromMinutes(1));
            //             }
            //             else if (args.Data.CustomId == $"DoNotLookupForSpotify-{randomId}") {
            //                 await lookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "User did not want to lookup spotify song with YouTube" });
            //                 conf.SubmittedBangers++;
            //                 await Task.Delay(TimeSpan.FromSeconds(0.5f));
            //                 Config.Save();
            //                 if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //                     await socketUserMessage.AddReactionAsync(upVote);
            //                 if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //                     await socketUserMessage.AddReactionAsync(downVote);
            //             }
            //         };
            //
            //         await Task.Delay(TimeSpan.FromMinutes(1));
            //         return;
            //     }
            // }

            if (!doSpotifyAlbumCount || !doYoutTubePlaylistCount) {
                conf.SubmittedBangers++;
                Config.Save();
            }

            if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                await socketUserMessage.AddReactionAsync(upVote);
            if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                await socketUserMessage.AddReactionAsync(downVote);
            didUrl = true;
        }

        if (attachments.Count == 0 && stickers.Count == 0) return;
        var extGood = IsFileExtWhitelisted(attachments.First().Filename.Split('.').Last(), conf.WhitelistedFileExtensions!);
        if (extGood || (urlGood && extGood)) {
            conf.SubmittedBangers++;
            Config.Save();

            if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                await socketUserMessage.AddReactionAsync(upVote);
            if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                await socketUserMessage.AddReactionAsync(downVote);
            didExt = true;
        }

        BangerMessageIds.Add(messageArg.Id);

        if (conf.SpeakFreely)
            return;

        if ((didUrl || didExt) && (!urlGood || !extGood)) {
            BangerLogger.Information($"Sent Bad {(didUrl ? "URL" : "File Extension")} Response");
            await messageArg.Channel.SendMessageAsync(didUrl ? conf.UrlErrorResponseMessage : conf.FileErrorResponseMessage).DeleteAfter(5);
            await messageArg.DeleteAsync();
        }
    }
}