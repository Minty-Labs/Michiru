using Discord;
using Discord.Interactions;
using Michiru.Configuration;
using Michiru.Events;
using Michiru.Managers;
using Michiru.Utils.ThirdPartyApiJsons;
using Serilog;

namespace Michiru.Commands.ContextMenu;

public class MessageFindBanger : InteractionModuleBase {
    private static readonly ILogger BangerLogger = Log.ForContext("SourceContext", "CONTEXTMENU:Banger");
    
    [MessageCommand("Find Banger (Ephemeral)"), IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall), CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    public async Task FindBangerFromMessage(IMessage message) {
        // variables
        var contents = message.Content;
        if (!contents.Contains("http")) {
            await message.Channel.SendMessageAsync("No URL found in message.", messageReference: new MessageReference(messageId: message.Id, channelId: message.Channel.Id), flags: MessageFlags.Ephemeral);
            return;
        }

        if (BangerListener.BangerMessageIds.Contains(message.Id)) {
            await message.Channel.SendMessageAsync("This is already a banger.", messageReference: new MessageReference(messageId: message.Id, channelId: message.Channel.Id), flags: MessageFlags.Ephemeral);
            return;
        }
        var isMessageFromWithinGuild = message.Channel is IGuildChannel;
        var conf = isMessageFromWithinGuild ? Config.Base.Banger.FirstOrDefault(x => x.ChannelId == message.Channel.Id) : Config.Base.Banger.FirstOrDefault(x => x.ChannelId == 805663181170802719);
        var url = contents.Split(' ').FirstOrDefault(x => x.StartsWith("http"));
        var isUrlGood = BangerListener.IsUrlWhitelisted(url!, conf!.WhitelistedUrls!);
        
        // check if url is white listed
        if (isUrlGood) {
            BangerListener.BangerMessageIds.Add(message.Id);
            // try to get album data from spotify
            try {
                if (contents.Contains("spotify.com") && contents.Contains("album")) {
                    BangerLogger.Information("Found URL to be a Spotify Album");
                    var entryId = contents.Split('/').Last();
                    var album = await SpotifyApiJson.GetAlbumData(entryId);
                    if (isMessageFromWithinGuild)
                        conf!.SubmittedBangers += album!.total_tracks;
                    else
                        Config.Base.ExtraBangerCount += album!.total_tracks;
                    await message.Channel.SendMessageAsync($"Successfully added {album!.total_tracks} banger(s) from Spotify album", messageReference: new MessageReference(messageId: message.Id, channelId: message.Channel.Id), flags: MessageFlags.Ephemeral);
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
            await message.Channel.SendMessageAsync("Successfully added a banger.", messageReference: new MessageReference(messageId: message.Id, channelId: message.Channel.Id), flags: MessageFlags.Ephemeral);
            Config.Save();
            return;
        }
        
        // if url is not good, throw fail message
        await message.Channel.SendMessageAsync("This URL is not whitelisted.", messageReference: new MessageReference(messageId: message.Id, channelId: message.Channel.Id), flags: MessageFlags.Ephemeral);
    }
}