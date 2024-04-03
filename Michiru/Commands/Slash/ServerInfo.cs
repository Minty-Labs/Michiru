using Discord;
using Discord.Interactions;
using Michiru.Configuration;
using Serilog;

namespace Michiru.Commands.Slash;

public class ServerInfo : InteractionModuleBase<SocketInteractionContext> {
    // private DateTime _timeLastRan = new(2000, 01, 01, 01, 01, 01);
    [SlashCommand("serverinfo", "Get information about the server")]
    public async Task ServerInfoCommand() {
        // if (_timeLastRan.AddMinutes(10) > DateTime.UtcNow) {
        //     await RespondAsync("Please wait 10 minutes between running this command.", ephemeral: true);
        //     return;
        // }
        var pmData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
        var bangerData = Config.GetGuildBanger(Context.Guild.Id);
        var isPennyGuild = Context.Guild.Id == Config.Base.PennysGuildWatcher.GuildId;
        // _timeLastRan = DateTime.UtcNow;
        var embed = new EmbedBuilder();
        embed.WithTitle($"{Context.Guild.Name} ({Context.Guild.Id})");
        embed.WithThumbnailUrl(Context.Guild.IconUrl ?? "https://i.mintlily.lgbt/null.jpg");
        embed.WithFooter($"Michiru Bot | v{Vars.Version}");
        embed.WithColor(Context.Guild.Roles.ElementAt(new Random().Next(Context.Guild.Roles.Count)).Color);
        embed.AddField("Owner", $"{Context.Guild.Owner.Mention}", true);
        // embed.AddField("Admins", $"{string.Join(", ", Context.Guild.Users.Where(x => x.GuildPermissions.Administrator && x.Id != Context.Guild.OwnerId).Select(x => x.Mention))[..256]}", true);
        embed.AddField("Members", $"{Context.Guild.MemberCount}", true);
        embed.AddField("Created At", $"{Context.Guild.CreatedAt.UtcDateTime} (UTC)", true);
        embed.AddField($"Roles ({Context.Guild.Roles.Count})", $"{string.Join(", ", Context.Guild.Roles.Select(x => x.Mention))[..512]}");
        embed.AddField("Bot Features", $"{(pmData.Enabled ? "\u2705" : "\u274e")} Personalized Roles\n" +
                                       $"{(bangerData.Enabled ? "\u2705" : "\u274e")} Banger System\n" +
                                       $"{(isPennyGuild ? "\u2705 Guild Update Notices" : "")}");
        if (bangerData.Enabled)
            embed.AddField("Bangers", $"{bangerData.SubmittedBangers}", true);
        if (pmData.Enabled)
            embed.AddField("Personalized Members", $"{pmData.Members!.Count}", true);
        await RespondAsync(embed: embed.Build());
    }
}