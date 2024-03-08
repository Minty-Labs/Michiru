using System.Diagnostics;
using Discord;
using Discord.Commands;
using Michiru.Configuration;
using Michiru.Utils;

namespace Michiru.Commands.Prefix;

[RequireContext(ContextType.Guild)]
public class BasicCommandsThatIDoNotWantAsSlashCommands : ModuleBase<SocketCommandContext> {
    [RequireOwner, Command("exec")]
    internal async Task InternalExecute_ThisShouldHardlyNeverBeRan(string command) {
        var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        if (command.Equals("pm2 stop 1"))
            await Context.Client.StopAsync();
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var weh = output.SplitMessage(1900);
        foreach (var chuck in weh)
            await ReplyAsync($"```\n{chuck}```");
    }
    
    [RequireOwner, Command("setapikey")]
    public async Task SetApiKey(string api, string key) {
        switch (api.ToLower()) {
            case "fluxpoint":
                Config.Base.Api.ApiKeys.FluxpointApiKey = key;
                Config.Save();
                Program.Instance.FluxpointClient = new fluxpoint_sharp.FluxpointClient(Vars.Name, key);
                await ReplyAsync("Fluxpoint API Key set!");
                break;
            case "cookie":
                Config.Base.Api.ApiKeys.CookieClientApiKey = key;
                Config.Save();
                await ReplyAsync("Cookie API Key set!\nNo library found for this API key to be used.");
                break;
            case "unsplashsecret":
                Config.Base.Api.ApiKeys.UnsplashSecretKey = key;
                Config.Save();
                await ReplyAsync("Unsplash Access Key set!\nNo library found for this API key to be used.");
                break;
            case "unsplashaccess":
                Config.Base.Api.ApiKeys.UnsplashAccessKey = key;
                Config.Save();
                await ReplyAsync("Unsplash Access Key set!\nNo library found for this API key to be used.");
                break;
            default:
                await ReplyAsync("Invalid API key type!");
                break;
        }
    }

    [Command("ping")]
    public async Task Ping() => await ReplyAsync("Pong! | " + Context.Client.Latency + "ms");

    [Command("stats"), Alias("status")]
    public async Task Stats() {
        var embed = new EmbedBuilder {
                Title = "Bot Stats",
                Description = $"{Context.User.Mention} is cute!\nPenny is also cute!",
                Color = Colors.HexToColor("9fffe3"),
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Footer = new EmbedFooterBuilder {
                    Text = $"v{Vars.Version}"
                },
                Timestamp = DateTime.Now
            }
            .AddField("OS", Vars.IsWindows ? "Windows" : "Linux")
            .AddField("Bangers", $"{Config.GetBangerNumber()}")
            .AddField("Personalized Member Count", $"{Config.GetPersonalizedMemberCount()}")
            .AddField("Guild Count", $"{Program.Instance.Client.Guilds.Count}")
            .AddField("Build Time", $"<t:{Vars.BuildTime.ToUniversalTime().GetSecondsFromUtcUnixTime()}:F>\n<t:{Vars.BuildTime.ToUniversalTime().GetSecondsFromUtcUnixTime()}:R>")
            .AddField("Start Time", $"<t:{DateTime.UtcNow.GetSecondsFromUtcUnixTime()}:F>\n<t:{DateTime.UtcNow.GetSecondsFromUtcUnixTime()}:R>")
            .AddField("Discord.NET Version", Vars.DNetVer)
            .AddField("System .NET Version", Environment.Version)
            .AddField("Links", $"[GitHub](https://github.com/Minty-Labs/Giver-of-Head-Pats) | " +
                               $"[Privacy Policy](https://mintylabs.dev/gohp/privacy-policy) | [Terms of Service](https://mintylabs.dev/gohp/terms) | " +
                               $"[Donate](https://ko-fi.com/MintLily) | [Patreon](https://www.patreon.com/MintLily)");
        await ReplyAsync(embed: embed.Build());
    }

    [Command("minecraft"), Alias("mc", "mint craft", "pepper mint", "peppermint")]
    public async Task Minecraft() {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.FluxpointApiKey!)) {
            await ReplyAsync("Api key for Fluxpoint is not set. Lily is a baka and did not set it.");
            return;
        }

        var mcServer = await Program.Instance.FluxpointClient.Minecraft.GetMinecraftServerAsync("mc.mili.lgbt");
        var embed = new EmbedBuilder {
                Title = "Minecraft Server",
                Description = $"Server is currently {(mcServer.online ? "online" : "offline")}",
                Color = Colors.HexToColor("00D200"),
                ThumbnailUrl = mcServer.icon ?? "https://i.mintlily.lgbt/null.jpg",
            }
            .AddField("IP", "mc.mili.lgbt")
            .AddField("Player Count", $"{mcServer.playersOnline} / {mcServer.playersMax}")
            .AddField("Version", mcServer.version)
            .AddField("MOTD", mcServer.motd)
            .AddField("Available Platforms", "Java Edition, Bedrock Edition (Xbox, PlayStation, Switch, iOS, Android, Windows 10/11)")
            .AddField("Players", $"{(mcServer.players.Length > 0 ? string.Join(", ", mcServer.players)[..512] : "No players online")}")
            .AddField("Miscellaneous Info", $"code:{mcServer.code}|success:{mcServer.success}|message:{mcServer.message}");
        await ReplyAsync(embed: embed.Build());
    }
}