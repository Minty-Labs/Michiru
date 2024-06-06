using System.Reflection;
using Discord;
using Discord.WebSocket;
using Michiru.Configuration;
using Michiru.Configuration.Classes;
using Michiru.Utils;
using Serilog;

namespace Michiru.Managers;
// Came from Private Repo: https://github.com/TotallyWholesome/TWNet/blob/master/TWNet/DiscordBot/Components/ModalProcessor.cs (Created by DDAkebono)
public class ModalProcessor {
    private static readonly ILogger Logger = Log.ForContext(typeof(ModalProcessor));
    private static Dictionary<string, ModalActionDelegate> _modalActions = null!;
    private delegate Task ModalActionDelegate(SocketModal modal);
    
    public ModalProcessor() {
        _modalActions = new Dictionary<string, ModalActionDelegate>();

        foreach (var method in typeof(ModalProcessor).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)) {
            Logger.Debug($"Checking method {method.Name}");
            if (method.GetCustomAttribute(typeof(ModalAction)) is not ModalAction attr) continue;
            Logger.Debug($"Found method with attribute {attr.ModalTag}");
            
            _modalActions.Add(attr.ModalTag, (ModalActionDelegate)Delegate.CreateDelegate(typeof(ModalActionDelegate), null, method));
        }
        
        Logger.Information($"Modal Action Processor has loaded {_modalActions.Count} actions!");
    }
    
    public static async Task ProcessModal(SocketModal modal) {
        var modalId = modal.Data.CustomId.Split('-')[0].ToLower().Trim();
        if (!_modalActions.TryGetValue(modalId, out var value)) return;
        await value(modal);
    }

    [ModalAction("setapikey")]
    private static async Task SetApiKey(SocketModal modal) {
        var components = modal.Data.Components.ToList();
        var apiType = components.First(x => x.CustomId == "apiType").Value.ToLower();
        var apiKey = components.First(x => x.CustomId == "apiKey").Value;
        
        switch (apiType) {
            // case "flux":
            // case "fluxpoint":
            //     Config.Base.Api.ApiKeys.FluxpointApiKey = apiKey;
            //     Config.Save();
            //     Program.Instance.FluxpointClient = new fluxpoint_sharp.FluxpointClient(Vars.Name, apiKey);
            //     await modal.RespondAsync("Fluxpoint API Key set!");
            //     break;
            case "cookie":
                Config.Base.Api.ApiKeys.CookieClientApiKey = apiKey;
                Config.Save();
                await modal.RespondAsync("Cookie API Key set!\nNo library found for this API key to be used.");
                break;
            case "unsplashsecret":
                Config.Base.Api.ApiKeys.UnsplashSecretKey = apiKey;
                Config.Save();
                await modal.RespondAsync("Unsplash Access Key set!\nNo library found for this API key to be used.");
                break;
            case "unsplashaccess":
                Config.Base.Api.ApiKeys.UnsplashAccessKey = apiKey;
                Config.Save();
                await modal.RespondAsync("Unsplash Access Key set!\nNo library found for this API key to be used.");
                break;
            default:
                await modal.RespondAsync("Invalid API key type!");
                break;
        }
    }

    [ModalAction("setspotifyapikeys")]
    private static async Task SetSpotifyApiKey(SocketModal modal) {
        var components = modal.Data.Components.ToList();
        var spotClient = components.First(x => x.CustomId == "spotclient").Value;
        var spotSecret = components.First(x => x.CustomId == "spotsecret").Value;
        
        Config.Base.Api.ApiKeys.Spotify.SpotifyClientId = spotClient;
        Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret = spotSecret;
        Config.SaveFile();
        await modal.RespondAsync("Spotify API Keys set!", ephemeral: true);
    }

    [ModalAction("personalization_createrole")]
    private static async Task CreateRole(SocketModal modal) {
        var components = modal.Data.Components.ToList();
        var roleName = components.First(x => x.CustomId == "roleName").Value;
        var colorHexString = components.First(x => x.CustomId == "colorHex").Value.ToLower();
        
        if (string.IsNullOrWhiteSpace(colorHexString))
            colorHexString = Colors.RandomColorHex;
        
        var personalData = Config.GetGuildPersonalizedMember((ulong)modal.GuildId!);
        var currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var guildPersonalizedMember = personalData.Members!.FirstOrDefault(x => x.userId == modal.User.Id);
        var newColorString = colorHexString.ValidateHexColor().Left(6);
        var discordColor = newColorString.Length == 6 ? Colors.HexToColor(newColorString) : Colors.HexToColor(Colors.RandomColorHex);

        if (guildPersonalizedMember is null) {
            var guild = Program.Instance.Client.GetGuild((ulong)modal.GuildId!);
            var memberRole = await guild.CreateRoleAsync(
                name: roleName ?? modal.User.Username.Left(15).Trim(), 
                color: discordColor, 
                options: new RequestOptions {AuditLogReason = "Personalized Member - User"});
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            var guildPersonalizedMemberData = new Member {
                userId = modal.User.Id,
                roleId = memberRole.Id,
                roleName = roleName ?? modal.User.Username.Left(15).Trim(),
                colorHex = Colors.ColorToHex(discordColor),
                epochTime = currentEpoch
            };
            personalData.Members!.Add(guildPersonalizedMemberData);
            Config.SaveFile();
            var discordMember = modal.User as IGuildUser;
            await discordMember!.AddRoleAsync(memberRole, new RequestOptions {AuditLogReason = "Personalized Member - User: " + modal.User.Username});
            if (personalData.DefaultRoleId != 0) {
                var defaultRole = guild.GetRole(personalData.DefaultRoleId);
                await discordMember!.RemoveRoleAsync(defaultRole, new RequestOptions {AuditLogReason = "Personalized Member - User: " + modal.User.Username});
            }
            await modal.RespondAsync("Successfully created your personalized member role.");
            return;
        }
        
        await modal.RespondAsync("You already have a personalized role.\nRun `/personalization updaterole` to update your role.", ephemeral: true);
    }
    
    [ModalAction("personalization_updaterole")]
    private static async Task UpdateRole(SocketModal modal) {
        var components = modal.Data.Components.ToList();
        var roleName = components.First(x => x.CustomId == "roleName").Value;
        var colorHexString = components.First(x => x.CustomId == "colorHex").Value.ToLower();
        
        if (string.IsNullOrWhiteSpace(roleName) && string.IsNullOrWhiteSpace(colorHexString)) {
            await modal.RespondAsync("You need to provide a new role name or color to update your personalized role.", ephemeral: true);
            return;
        }
        
        var personalData = Config.GetGuildPersonalizedMember((ulong)modal.GuildId!);
        var currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var guildPersonalizedMember = personalData.Members!.FirstOrDefault(x => x.userId == modal.User.Id);

        if (guildPersonalizedMember is not null) {
            var guild = Program.Instance.Client.GetGuild((ulong)modal.GuildId!);
            var memberRole = guild.GetRole(guildPersonalizedMember.roleId);
            var newColorString = colorHexString.ValidateHexColor().Left(6);
            var modifyingName = !string.IsNullOrWhiteSpace(roleName);
            var modifyingColor = !string.IsNullOrWhiteSpace(colorHexString);
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            if (modifyingName)
                guildPersonalizedMember.roleName = roleName;
            if (modifyingColor)
                guildPersonalizedMember.colorHex = newColorString;
            guildPersonalizedMember.epochTime = currentEpoch;
            Config.SaveFile();
            await memberRole!.ModifyAsync(x => {
                x.Name = modifyingName ? roleName : memberRole.Name;
                x.Color = modifyingColor ? Colors.HexToColor(newColorString) : memberRole.Color;
            }, new RequestOptions {AuditLogReason = "Personalized Member - User: " + modal.User.Username});
            await modal.RespondAsync("Successfully updated your personalized member role.");
            return;
        }
        
        await modal.RespondAsync("You do not have a personalized role to update.\nRun `/personalization createrole` to create your role.", ephemeral: true);
    }
}

public class ModalAction(string modalTag) : Attribute {
    public readonly string ModalTag = modalTag;
}