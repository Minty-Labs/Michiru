using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Michiru.Commands.Preexecution;
using Michiru.Configuration;
using Michiru.Events;
using Michiru.Managers;
using Michiru.Utils;
using Michiru.Utils.ThirdPartyApiJsons;
using Serilog;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Michiru.Commands.ContextMenu;

public class LookupSpotifyForYouTube : InteractionModuleBase<SocketInteractionContext> {
    private static readonly ILogger SluLogger = Log.ForContext("SourceContext", "CONTEXTMENU:SpotifyLookup");
    
    [MessageCommand("Lookup Spotify on YouTube"), 
     IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall),
     CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel),
     RateLimit(60, 3)]
    public async Task LookupSpotifyForYouTubeAction(IMessage message) {
        // variables
        var socketUserMessage = (SocketUserMessage)message;
        var contents = message.Content;
        if (!contents.Contains("http")) {
            await RespondAsync($"No URL found in message: {message.GetJumpUrl()}.", ephemeral: true);
            return;
        }
        if (BangerListener.BangerMessageIds.Contains(message.Id)) {
            await RespondAsync("This is already a banger.", ephemeral: true);
            return;
        }
        
        await DeferAsync(true);
        var isMessageFromWithinGuild = message.Channel is IGuildChannel;
        Configuration.Classes.Banger? conf = null;
        // Emote? upVote = null;
        // Emote? downVote = null;
        if (isMessageFromWithinGuild) {
            conf = Config.Base.Banger.FirstOrDefault(x => x.ChannelId == message.Channel.Id);
            if (conf is null) return;
            // if (!conf.Enabled) return;
            // upVote = conf.CustomUpvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId) : Emote.Parse(conf.CustomUpvoteEmojiName) ?? Emote.Parse(":thumbsup:");
            // downVote = conf.CustomDownvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId) : Emote.Parse(conf.CustomDownvoteEmojiName) ?? Emote.Parse(":thumbsdown:");
        }
        string? theActualUrl = null;
        // var numberToAdd = 0;
                    
        var yt = new YoutubeClient();
        var sb = new StringBuilder().AppendLine("Top 3 YouTube search results:");
        
        theActualUrl ??= contents;
        var isUrlGood = theActualUrl.Contains("spotify.com");// BangerListener.IsUrlWhitelisted(theActualUrl, conf!.WhitelistedUrls!);
        
        // check if url is whitelisted
        if (isUrlGood) {
            // try to get album data from spotify
            var doSpotifyAlbumCount = false;
            try {
                if (theActualUrl.AndContainsMultiple("spotify.com", "album")) {
                    SluLogger.Information("Found URL to be a Spotify Album");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?')) 
                        finalId = theActualUrl.Split('?')[0];
                    var album = await SpotifyAlbumApiJson.GetAlbumData(finalId.Split('/').Last());
                    // numberToAdd = album!.total_tracks;
                    doSpotifyAlbumCount = true;
                    var videos = yt.Search.GetVideosAsync($"{album!.artists[0].name} {album.name}").GetAwaiter().GetResult();
                    
                    for (var i = 0; i < 3; i++) {
                        sb.AppendLine($"{i}. {MarkdownUtils.MakeLink($"{videos[i].Author} - {videos[i].Title}", videos[i].Url)}");
                        sb.AppendLine();
                    }
                    
                    // await RespondAsync(sb.ToString().Trim(), ephemeral: true);
                    await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                }
            }
            catch (Exception ex) {
                SluLogger.Error("Failed to get album data from Spotify API");
                await ErrorSending.SendErrorToLoggingChannelAsync("Failed to get album data from Spotify API", obj: ex.StackTrace);
                // await RespondAsync("Failed to get album data from Spotify API", ephemeral: true);
                await ModifyOriginalResponseAsync(x => x.Content = "Failed to get album data from Spotify API");
                doSpotifyAlbumCount = false;
            }
            
            // try get spotify track if no album
            if (!doSpotifyAlbumCount) {
                try {
                    if (theActualUrl.AndContainsMultiple("spotify.com", "track")) {
                        SluLogger.Information("Found URL to be a Spotify Track");
                        var finalId = theActualUrl;
                        if (theActualUrl.Contains('?')) 
                            finalId = theActualUrl.Split('?')[0];
                        var track = await SpotifyTrackApiJson.GetTrackData(finalId.Split('/').Last());
                        // numberToAdd = 1;
                        
                        var videos = yt.Search.GetVideosAsync($"{track!.artists[0].name} {track.name}").GetAwaiter().GetResult();
                    
                        for (var i = 0; i < 3; i++) {
                            sb.AppendLine($"{i}. {MarkdownUtils.MakeLink($"{videos[i].Author} - {videos[i].Title}", videos[i].Url)}");
                            sb.AppendLine();
                        }
                    
                        // await RespondAsync(sb.ToString().Trim(), ephemeral: true);
                        await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                    }
                }
                catch (Exception ex) {
                    SluLogger.Error("Failed to get track data from Spotify API");
                    await ErrorSending.SendErrorToLoggingChannelAsync("Failed to get track data from Spotify API", obj: ex.StackTrace);
                    // await RespondAsync("Failed to get track data from Spotify API", ephemeral: true);
                    await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Spotify API");
                }
            }

            // if no spotify album, and if url is still good, add one
            // if (isMessageFromWithinGuild)
            //     conf!.SubmittedBangers = numberToAdd;
            // else
            //     Config.Base.ExtraBangerCount += numberToAdd;
            // Config.Save();
            //
            // BangerListener.BangerMessageIds.Add(message.Id);
            //
            // if (isMessageFromWithinGuild && upVote is not null && downVote is not null) {
            //     if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
            //         await socketUserMessage.AddReactionAsync(upVote);
            //     if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
            //         await socketUserMessage.AddReactionAsync(downVote);
            // }
        }
        else {
            // await RespondAsync("URL is not whitelisted for Spotify lookup.", ephemeral: true);
            await ModifyOriginalResponseAsync(x => x.Content = "URL is not whitelisted for Spotify lookup.");
        }
    }
}