using Discord;
using Discord.Interactions;
using Michiru.Configuration;
using Michiru.Events;
using Michiru.Managers;
using Michiru.Utils;
using Michiru.Utils.ThirdPartyApiJsons;
using Serilog;

namespace Michiru.Commands.ContextMenu;

public class MessageFindBanger : InteractionModuleBase<SocketInteractionContext> {
    private static readonly ILogger BangerLogger = Log.ForContext("SourceContext", "CONTEXTMENU:Banger");
    
    [MessageCommand("Find Banger (Ephemeral)"), IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall), CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    public async Task FindBangerFromMessage(IMessage message) {
        // variables
        var contents = message.Content;
        if (!contents.Contains("http")) {
            await RespondAsync($"No URL found in message: {message.GetJumpUrl()}.", ephemeral: true);
            return;
        }

        if (BangerListener.BangerMessageIds.Contains(message.Id)) {
            await RespondAsync("This is already a banger.", ephemeral: true);
            return;
        }
        var isMessageFromWithinGuild = message.Channel is IGuildChannel;
        var conf = isMessageFromWithinGuild ? Config.Base.Banger.FirstOrDefault(x => x.ChannelId == message.Channel.Id) : Config.Base.Banger.FirstOrDefault(x => x.ChannelId == 805663181170802719);
        var isUrlGood = BangerListener.IsUrlWhitelisted(contents!, conf!.WhitelistedUrls!);
        
        // check if url is whitelisted
        if (isUrlGood) {
            BangerListener.BangerMessageIds.Add(message.Id);
            // try to get album data from spotify
            try {
                if (contents.Contains("spotify.com") && contents.Contains("album")) {
                    BangerLogger.Information("Found URL to be a Spotify Album");
                    var entryId = contents.Split('/').Last();
                    var album = await SpotifyAlbumApiJson.GetAlbumData(entryId);
                    if (isMessageFromWithinGuild)
                        conf!.SubmittedBangers += album!.total_tracks;
                    else
                        Config.Base.ExtraBangerCount += album!.total_tracks;
                    await RespondAsync($"Successfully added {album!.total_tracks} banger(s) from Spotify album", ephemeral: true);
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
            await RespondAsync("Successfully added a banger.", ephemeral: true);
            Config.Save();
            return;
        }
        
        // if url is not good, throw fail message
        var errorCode = StringUtils.GetRandomString(7).ToUpper();
        await RespondAsync($"This URL: <{contents}> is not whitelisted. If you think this is an issue, please contact Lily with this message and the URL.\n" +
                           $"Error: `{errorCode}`", ephemeral: true);
        await ErrorSending.SendErrorToLoggingChannelAsync("Banger URL not whitelisted", obj: $"Error: {errorCode}\nURL: {contents}\nMessage Contents: {message.Content}");
    }
}