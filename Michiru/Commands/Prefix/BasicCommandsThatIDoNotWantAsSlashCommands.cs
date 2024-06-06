using System.Diagnostics;
using Discord;
using Discord.Commands;
// using fluxpoint_sharp.Responses;
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

    [Command("ping")]
    public async Task Ping() => await ReplyAsync("Pong! | " + Context.Client.Latency + "ms");

    [Command("stats"), Alias("status")]
    public async Task Stats() {
        var desc = Context.User.Id == 167335587488071682 ? "Penny is cute!" : $"{Context.User.Mention} is cute!\nPenny is also cute!";
        var embed = new EmbedBuilder {
                Title = "Bot Stats",
                Description = desc,
                Color = Colors.HexToColor("9fffe3"),
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Footer = new EmbedFooterBuilder {
                    Text = $"v{Vars.VersionStr}"
                },
                Timestamp = DateTime.Now
            }
            .AddField("Bangers", $"{Config.GetBangerNumber()}", true)
            .AddField("Personalized Member Count", $"{Config.GetPersonalizedMemberCount()}", true)
            .AddField("Guild Count", $"{Program.Instance.Client.Guilds.Count}", true)
            .AddField("Build Time", $"{Vars.BuildTime.ToUniversalTime().ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}\n{Vars.BuildTime.ToUniversalTime().ConvertToDiscordTimestamp(TimestampFormat.RelativeTime)}")
            .AddField("Start Time", $"{Vars.StartTime.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}\n{Vars.StartTime.ConvertToDiscordTimestamp(TimestampFormat.RelativeTime)}")
            .AddField("OS", Vars.IsWindows ? "Windows" : "Linux", true)
            .AddField("Discord.NET Version", Vars.DNetVer, true)
            .AddField("System .NET Version", Environment.Version, true)
            .AddField("Links", $"{MarkdownUtils.MakeLink("GitHub", "https://github.com/Minty-Labs/Michiru")} | " +
                               $"{MarkdownUtils.MakeLink("Privacy Policy", "https://mintylabs.dev/gohp/privacy-policy")} | {MarkdownUtils.MakeLink("Terms of Service", "https://mintylabs.dev/gohp/terms")} | " +
                               $"{MarkdownUtils.MakeLink("Donate", "https://ko-fi.com/MintLily")} | {MarkdownUtils.MakeLink("Patreon", "https://www.patreon.com/MintLily")} | {MarkdownUtils.MakeLink("Developer Server", Vars.SupportServer)}\n" +
                               $"*Privacy Policy & ToS is inherited from the Giver of Head Pats Bot*");
        await ReplyAsync(embed: embed.Build());
    }

    /*[Command("minecraft"), Alias("mc", "mint craft", "pepper mint", "peppermint")]
    public async Task Minecraft() {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.FluxpointApiKey!)) {
            await ReplyAsync("Api key for Fluxpoint is not set. Lily is a baka and did not set it.");
            return;
        }
        
        try {
            var mcServer = await Program.Instance.FluxpointClient.Minecraft.GetMinecraftServerAsync("mc.mili.lgbt");
            var embed = new EmbedBuilder {
                    Title = "Minecraft Server",
                    Description = $"Server is currently {(mcServer.online ? "online" : "offline")}",
                    Color = Colors.HexToColor("00D200"),
                    ThumbnailUrl = mcServer.icon ?? "https://i.mintlily.lgbt/null.jpg",
                }
                .AddField("IP", "mc.mili.lgbt")
                .AddField("Player Count", $"{mcServer.playersOnline} / {mcServer.playersMax}")
                .AddField("Version", $"{mcServer.version}")
                .AddField("MOTD", $"{mcServer.motd}")
                .AddField("Available Platforms", "Java Edition, Bedrock Edition (Xbox, PlayStation, Switch, iOS, Android, Windows 10/11)")
                .AddField("Players", $"{(mcServer.players.Length > 0 ? string.Join(", ", mcServer.players)[..512] : "No players online")}")
                .AddField("Miscellaneous Info", $"code:{mcServer.code}|success:{mcServer.success}|message:{mcServer.message}");
            await ReplyAsync(embed: embed.Build());
        }
        catch (Exception e) {
            await ReplyAsync($"An error occurred while trying to get the server status: {e.Message}");
        }
    }*/
}