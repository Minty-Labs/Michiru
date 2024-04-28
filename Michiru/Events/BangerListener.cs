using System.Text;
using AngleSharp.Common;
using Discord;
using Discord.Rest;
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

    public static bool IsUrlWhitelisted(string url, ICollection<string> list) {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)) return false;
        return list?.Contains(uri.Host) ?? throw new ArgumentNullException(nameof(list));
    }

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
        string? theActualUrl = null;

        if (messageContent.Contains(' ')) {
            foreach (var str in messageContent.Split(' ')) {
                if (str.Contains("https"))
                    theActualUrl = str;
            }
        }
        theActualUrl ??= messageContent;

        bool didUrl = false, didExt = false;
        var urlGood = IsUrlWhitelisted(theActualUrl, conf.WhitelistedUrls!);
        if (urlGood) {
            bool doSpotifyAlbumCount = false, doYoutTubePlaylistCount = false;
            try {
                if (theActualUrl.AndContainsMultiple("spotify.com", "album")) {
                    BangerLogger.Information("Found URL to be a Spotify Album");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?')) 
                        finalId = theActualUrl.Split('?')[0];
                    var album = await SpotifyAlbumApiJson.GetAlbumData(finalId.Split('/').Last());
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
                if (theActualUrl.OrContainsMultiple("youtube.com", "youtu.be") && theActualUrl.Contains("/playlist?list")) {
                    BangerLogger.Information("Found the URL to be a YouTube Playlist");
                    var youtube = new YoutubeClient();
                    var youtubePlaylist = await youtube.Playlists.GetVideosAsync(theActualUrl);
                    conf.SubmittedBangers += youtubePlaylist.Count;
                    doYoutTubePlaylistCount = true;
                    await Program.Instance.GeneralLogChannel!.SendMessageAsync($"Banger: Added {youtubePlaylist.Count} bangers from YouTube playlist {theActualUrl}");
                }
            }
            catch (Exception ex) {
                BangerLogger.Error("Failed to get playlist data from YouTube API");
                await ErrorSending.SendErrorToLoggingChannelAsync("Failed to get playlist data from YouTube API", obj: ex);
                doYoutTubePlaylistCount = false;
            }

            if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                await socketUserMessage.AddReactionAsync(upVote);
            if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                await socketUserMessage.AddReactionAsync(downVote);

            if (conf.OfferToReplaceSpotifyTrack) {
                if (theActualUrl.AndContainsMultiple("open.spotify.com", "track")) {
                    var randomId = StringUtils.GetRandomString(8);
                    var builder = new ComponentBuilder()
                        .WithButton("Lookup song on YouTube?", $"YouTubeLookupForSpotify-{randomId}", ButtonStyle.Success)
                        .WithButton("No", $"DoNotLookupForSpotify-{randomId}", ButtonStyle.Danger);

                    var reference = new MessageReference(messageArg.Id, messageArg.Channel.Id, Program.Instance.GetGuildFromChannel(messageArg.Channel.Id)!.Id, false);
                    
                    var lookupMessage = await messageArg.Channel.SendMessageAsync("Would you like to lookup this song on YouTube?", components: builder.Build(),
                        messageReference: reference);

                    TheBangerInteractionData.Add(new BangerInteractionData {
                        RandomId = randomId,
                        SpotifyUrl = theActualUrl,
                        OriginalPostedSocketMessage = messageArg,
                        MessageReference = reference,
                        LookupMessage = lookupMessage,
                        DoesWantToLookup = false,
                        HasPressedASerachResultButton = false,
                        YouTubeSearchResults = [],
                        HasCompletelyFinishedInteraction = false
                    });
                    
                    await Task.Delay(TimeSpan.FromMinutes(2));
                    var currentData = TheBangerInteractionData.FirstOrDefault(x => x.RandomId == randomId);
                    if (currentData is not null && currentData.HasCompletelyFinishedInteraction) {
                        TheBangerInteractionData.Remove(currentData);
                        return;
                    }
                }
            }

            if (!doSpotifyAlbumCount || !doYoutTubePlaylistCount) {
                conf.SubmittedBangers++;
                Config.Save();
            }

            didUrl = true;
        }

        if (attachments.Count == 0 && stickers.Count == 0) return;
        var extGood = IsFileExtWhitelisted(attachments.First().Filename.Split('.').Last(), conf.WhitelistedFileExtensions!);
        if (extGood || (urlGood && extGood)) {
            conf.SubmittedBangers++;
            Config.Save();
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

    public static List<BangerInteractionData> TheBangerInteractionData = [];

    public static async Task SpotifyToYouTubeSongLookupButtons(SocketMessageComponent component) {
        var currentData = TheBangerInteractionData.FirstOrDefault(x => component.Data.CustomId.Split('-')[1].Equals(x.RandomId));
        var randomId = currentData?.RandomId;
        if (string.IsNullOrWhiteSpace(randomId) || currentData is null) {
            var errorCode = StringUtils.GetRandomString(7).ToUpper();
            await component.RespondAsync($"Error Code: {MarkdownUtils.ToCodeBlockSingleline(errorCode)} - If the problem persists, give this error code to Lily. {MarkdownUtils.ToItalics("(Please do not spam the button)")}\n" +
                                         $"Failed to process request.", ephemeral: true);
            await ErrorSending.SendErrorToLoggingChannelAsync("Failed to process Spotify to YouTube Song Lookup via Button Press", obj: $"Error Code: {errorCode}\nRandom ID: {randomId}");
            return;
        }

        var conf = Config.Base.Banger.FirstOrDefault(x => x.ChannelId == currentData.OriginalPostedSocketMessage.Channel.Id);
        if (conf is null) return;
        if (!conf.Enabled) return;
        
        const string reason = "User replaced their Spotify post with a YouTube Link";
        var upVote = conf.CustomUpvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId) : Emote.Parse(conf.CustomUpvoteEmojiName) ?? Emote.Parse(":thumbsup:");
        var downVote = conf.CustomDownvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId) : Emote.Parse(conf.CustomDownvoteEmojiName) ?? Emote.Parse(":thumbsdown:");

        if (component.Data.CustomId == $"YouTubeLookupForSpotify-{randomId}" && !currentData.DoesWantToLookup) {
            currentData.DoesWantToLookup = true;
            // await currentData.LookupMessage.ModifyAsync(x => {
            //     x.Components = new ComponentBuilder().Build();
            //     x.Content = "Looking up song on YouTube, one moment...";
            // });
            await currentData.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "User wanted to lookup their Spotify post on YouTube" });
            await component.DeferAsync(true);
            await component.ModifyOriginalResponseAsync(x => {
                x.Content = "Looking up song on YouTube, one moment...";
            }); 
            var yt = new YoutubeClient();
            var sb = new StringBuilder();
            var spotifyTrack = await SpotifyTrackApiJson.GetTrackData(currentData.OriginalPostedSocketMessage.Content.Split('/').Last());

            var videos = await yt.Search.GetVideosAsync($"{spotifyTrack!.artists[0].name} {spotifyTrack.name}");
            for (var i = 0; i < 5; i++) {
                var emoji = i switch {
                    0 => Emoji.Parse(":one:"),
                    1 => Emoji.Parse(":two:"),
                    2 => Emoji.Parse(":three:"),
                    3 => Emoji.Parse(":four:"),
                    4 => Emoji.Parse(":five:"),
                    _ => null
                };
                sb.AppendLine($"{emoji} {videos[i].Author} - {videos[i].Title}");
                currentData.YouTubeSearchResults.Add(i, videos[i].Url);
            }

            var numberEmojiButtonComponent = new ComponentBuilder()
                .WithButton(emote: Emoji.Parse(":one:"), style: ButtonStyle.Secondary, customId: $"{randomId}-1")
                .WithButton(emote: Emoji.Parse(":two:"), style: ButtonStyle.Secondary, customId: $"{randomId}-2")
                .WithButton(emote: Emoji.Parse(":three:"), style: ButtonStyle.Secondary, customId: $"{randomId}-3")
                .WithButton(emote: Emoji.Parse(":four:"), style: ButtonStyle.Secondary, customId: $"{randomId}-4")
                .WithButton(emote: Emoji.Parse(":five:"), style: ButtonStyle.Secondary, customId: $"{randomId}-5")
                .WithButton("Never Mind", $"NeverMind-{randomId}", ButtonStyle.Danger);
            await component.ModifyOriginalResponseAsync(x => x.Components = numberEmojiButtonComponent.Build());
            // await currentData.LookupMessage.ModifyAsync(x => x.Components = numberEmojiButtonComponent.Build());
        }
        else if (component.Data.CustomId == $"DoNotLookupForSpotify-{randomId}") {
            // await currentData.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "User did not want to lookup spotify song with YouTube" });
            currentData.DoesWantToLookup = false;
            currentData.HasCompletelyFinishedInteraction = true;
            // await component.RespondAsync("Canceled", ephemeral: true);
            await component.ModifyOriginalResponseAsync(x => {
                x.Content = "Canceled";
                x.Components = new ComponentBuilder().Build();
            }); 
        }

        if (!currentData.HasPressedASerachResultButton) {
            if (component.Data.CustomId == $"{randomId}-1") {
                currentData.HasPressedASerachResultButton = true;
                await currentData.OriginalPostedSocketMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
                await component.ModifyOriginalResponseAsync(x => {
                    x.Content = "Completed";
                    x.Components = new ComponentBuilder().Build();
                }); 
                var newMessage = await currentData.OriginalPostedSocketMessage.Channel.SendMessageAsync(currentData.YouTubeSearchResults.GetItemByIndex(0).Value);
                if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                    await newMessage.AddReactionAsync(upVote);
                if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                    await newMessage.AddReactionAsync(downVote);
            }
            else if (component.Data.CustomId == $"{randomId}-2") {
                currentData.HasPressedASerachResultButton = true;
                await currentData.OriginalPostedSocketMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
                await component.ModifyOriginalResponseAsync(x => {
                    x.Content = "Completed";
                    x.Components = new ComponentBuilder().Build();
                }); 
                var newMessage = await currentData.OriginalPostedSocketMessage.Channel.SendMessageAsync(currentData.YouTubeSearchResults.GetItemByIndex(1).Value);
                if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                    await newMessage.AddReactionAsync(upVote);
                if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                    await newMessage.AddReactionAsync(downVote);
            }
            else if (component.Data.CustomId == $"{randomId}-3") {
                currentData.HasPressedASerachResultButton = true;
                await currentData.OriginalPostedSocketMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
                await component.ModifyOriginalResponseAsync(x => {
                    x.Content = "Completed";
                    x.Components = new ComponentBuilder().Build();
                }); 
                var newMessage = await currentData.OriginalPostedSocketMessage.Channel.SendMessageAsync(currentData.YouTubeSearchResults.GetItemByIndex(2).Value);
                if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                    await newMessage.AddReactionAsync(upVote);
                if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                    await newMessage.AddReactionAsync(downVote);
            }
            else if (component.Data.CustomId == $"{randomId}-4") {
                currentData.HasPressedASerachResultButton = true;
                await currentData.OriginalPostedSocketMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
                await component.ModifyOriginalResponseAsync(x => {
                    x.Content = "Completed";
                    x.Components = new ComponentBuilder().Build();
                }); 
                var newMessage = await currentData.OriginalPostedSocketMessage.Channel.SendMessageAsync(currentData.YouTubeSearchResults.GetItemByIndex(3).Value);
                if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                    await newMessage.AddReactionAsync(upVote);
                if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                    await newMessage.AddReactionAsync(downVote);
            }
            else if (component.Data.CustomId == $"{randomId}-5") {
                currentData.HasPressedASerachResultButton = true;
                await currentData.OriginalPostedSocketMessage.DeleteAsync(new RequestOptions { AuditLogReason = reason });
                await component.ModifyOriginalResponseAsync(x => {
                    x.Content = "Completed";
                    x.Components = new ComponentBuilder().Build();
                }); 
                var newMessage = await currentData.OriginalPostedSocketMessage.Channel.SendMessageAsync(currentData.YouTubeSearchResults.GetItemByIndex(4).Value);
                if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                    await newMessage.AddReactionAsync(upVote);
                if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                    await newMessage.AddReactionAsync(downVote);
            }
        }
        else {
            var errorCode = StringUtils.GetRandomString(7).ToUpper();
            await component.ModifyOriginalResponseAsync(x => {
                x.Content = "It appears this command has already ran, but it should not have been completed.\n" +
                            $"Error Code: {MarkdownUtils.ToCodeBlockSingleline(errorCode)} - If the problem persists, give this error code to Lily.";
                x.Components = new ComponentBuilder().Build();
            }); 
            // await component.RespondAsync(, ephemeral: true);
            await ErrorSending.SendErrorToLoggingChannelAsync("Spotify to YouTube Song Lookup Number Button Press has already ran", obj: $"Error Code: {errorCode}\nRandom ID: {randomId}");
            if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                await currentData.OriginalPostedSocketMessage.AddReactionAsync(upVote);
            if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                await currentData.OriginalPostedSocketMessage.AddReactionAsync(downVote);
        }
        
        currentData.HasCompletelyFinishedInteraction = true;
        conf.SubmittedBangers++;
        Config.Save();
    }
}

public class BangerInteractionData {
    public string RandomId { get; set; }
    public string SpotifyUrl { get; set; }
    public SocketMessage? OriginalPostedSocketMessage { get; set; }
    public MessageReference? MessageReference { get; set; }
    public RestUserMessage? LookupMessage { get; set; }
    public bool DoesWantToLookup { get; set; }
    public bool HasPressedASerachResultButton { get; set; }
    public Dictionary<int, string> YouTubeSearchResults { get; set; }
    public bool HasCompletelyFinishedInteraction { get; set; }
}