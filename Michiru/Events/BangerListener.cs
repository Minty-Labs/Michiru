﻿using Discord;
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
    private static readonly ILogger BangerLogger = Log.ForContext("SourceContext", "Banger");
    private static bool IsUrlWhitelisted(string url, ICollection<string> list) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return list?.Contains(uri.Host) ?? throw new ArgumentNullException(nameof(list));
    }

    private static bool IsFileExtWhitelisted(string extension, ICollection<string> list)
        => list?.Contains(extension) ?? throw new ArgumentNullException(nameof(list));

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
        var urlGood = IsUrlWhitelisted(messageContent, conf.WhitelistedUrls!);
        if (urlGood) {
            bool doSpotifyAlbumCount = false, doYoutTubePlaylistCount = false;
            try {
                if (messageContent.Contains("spotify.com") && messageContent.Contains("album")) {
                    BangerLogger.Information("Found URL to be a Spotify Album");
                    var entryId = messageContent.Split('/').Last();
                    var album = await SpotifyApiJson.GetAlbumData(entryId);
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
                if ((messageContent.Contains("youtube.com") || messageContent.Contains("youtu.be")) && messageContent.Contains("playlist?")) {
                    BangerLogger.Information("Found the URL to be a YouTube Playlist");
                    Uri.TryCreate(messageContent, UriKind.Absolute, out var uri);
                    var youtube = new YoutubeClient();
                    var youtubePlaylist = await youtube.Playlists.GetVideosAsync(uri == null ? messageContent : uri!.AbsoluteUri);
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

            if (!doSpotifyAlbumCount || !doYoutTubePlaylistCount)
                conf.SubmittedBangers++;
            Config.Save();
            
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

        if ((didUrl || didExt) && (!urlGood || !extGood)) {
            if (conf.SpeakFreely) return;
            BangerLogger.Information($"Sent Bad {(didUrl ? "URL" : "File Extension")} Response");
            await messageArg.Channel.SendMessageAsync(didUrl ? conf.UrlErrorResponseMessage : conf.FileErrorResponseMessage).DeleteAfter(5);
            await messageArg.DeleteAsync();
        }
    }
}