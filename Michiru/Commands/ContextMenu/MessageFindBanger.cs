/*using Discord;
using Discord.Interactions;
using Michiru.Commands.Preexecution;
using Michiru.Configuration._Base_Bot;
using Michiru.Events;
using Michiru.Managers;
using Michiru.Utils;
using Michiru.Utils.MusicProviderApis.Spotify;
using Serilog;

namespace Michiru.Commands.ContextMenu;

public class MessageFindBanger : InteractionModuleBase<SocketInteractionContext> {
    private static readonly ILogger BangerLogger = Log.ForContext("SourceContext", "CONTEXTMENU:Banger");
    
    [MessageCommand("Find Banger (Ephemeral)"), 
     IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall),
     CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel),
     RateLimit(60, 3)]
    public async Task FindBangerFromMessage(IMessage message) {
        await DeferAsync(true);
        // variables
        var contents = message.Content;
        if (!contents.Contains("http")) {
            await ModifyOriginalResponseAsync(x => x.Content = $"No URL found in message: {message.GetJumpUrl()}.");
            return;
        }

        if (BangerListener.BangerMessageIds.Contains(message.Id)) {
            await ModifyOriginalResponseAsync(x => x.Content = "This is already a banger.");
            return;
        }
        var isMessageFromWithinGuild = message.Channel is IGuildChannel;
        var conf = isMessageFromWithinGuild ? Config.Base.Banger.FirstOrDefault(x => x.ChannelId == message.Channel.Id) : Config.Base.Banger.FirstOrDefault(x => x.ChannelId == 805663181170802719);
        var isUrlGood = BangerListener.IsUrlWhitelisted(contents!, conf!.WhitelistedUrls!);
        // check if url is whitelisted
        if (isUrlGood) {
            BangerListener.BangerMessageIds.Add(message.Id);

            var provider = "none";
            if (contents.Contains("spotify.com"))
                provider = "Spotify";
            else if (contents.Contains("youtube.com"))
                provider = "YouTube";
            else if (contents.Contains("tidal.com"))
                provider = "Tidal";
            else if (contents.Contains("deezer.page"))
                provider = "Deezer";
            else if (contents.Contains("bandcamp.com"))
                provider = "Bandcamp";
            else if (contents.Contains("music.apple.com"))
                provider = "Apple Music";
            else if (contents.Contains("soundcloud.com"))
                provider = "Soundcloud";
            
            // try to get album data from spotify
            try {
                if (provider == "Spotify" && contents.Contains("album")) {
                    BangerLogger.Information("Found URL to be a Spotify Album");
                    var entryId = contents.Split('/').Last();
                    var album = await GetAlbumResults.GetAlbumData(entryId);
                    if (isMessageFromWithinGuild)
                        conf!.SubmittedBangers += album!.total_tracks;
                    else
                        Config.Base.ExtraBangerCount += album!.total_tracks;
                    await ModifyOriginalResponseAsync(x => x.Content = $"Successfully added {album!.total_tracks} banger(s) from Spotify album");
                    Config.Save();
                    return;
                }
            }
            catch (Exception ex) {
                BangerLogger.Error("Failed to get album data from Spotify API");
                await ErrorSending.SendErrorToLoggingChannelAsync("Failed to get album data from Spotify API", obj: ex);
            }

            // if no spotify album, and if url is still good, add one
            if (isMessageFromWithinGuild)
                conf!.SubmittedBangers++;
            else
                Config.Base.ExtraBangerCount++;
            await ModifyOriginalResponseAsync(x => x.Content = "Successfully added a banger.");
            Config.Save();
            return;
        }
        
        // if url is not good, throw fail message
        var errorCode = StringUtils.GetRandomString(7).ToUpper();
        await ModifyOriginalResponseAsync(x => x.Content = $"This URL: <{contents}> is not whitelisted. If you think this is an issue, please contact Lily with this message and the URL.\n" +
                                                           $"Error: `{errorCode}`");
        await ErrorSending.SendErrorToLoggingChannelAsync("Banger URL not whitelisted", obj: $"Error: {errorCode}\nURL: {contents}\nMessage Contents: {message.Content}");
    }
}*/