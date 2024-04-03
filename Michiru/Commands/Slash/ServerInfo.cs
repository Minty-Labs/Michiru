using Discord;
using Discord.Interactions;
using Michiru.Configuration;

namespace Michiru.Commands.Slash;

public class ServerInfo : InteractionModuleBase<SocketInteractionContext> {
    private DateTime _timeLastRan;
    [SlashCommand("serverinfo", "Get information about the server")]
    public async Task ServerInfoCommand() {
        if (_timeLastRan.AddMinutes(10) > DateTime.UtcNow) {
            await RespondAsync("Please wait 10 minutes between running this command.", ephemeral: true);
            return;
        }
        var pmData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
        var bangerData = Config.GetGuildBanger(Context.Guild.Id);
        var isPennyGuild = Context.Guild.Id == Config.Base.PennysGuildWatcher.GuildId;
        _timeLastRan = DateTime.UtcNow;
        var embed = new EmbedBuilder {
                Title = Context.Guild.Name,
                Description = $"ID: {Context.Guild.Id}",
                ThumbnailUrl = Context.Guild.IconUrl,
                Color = Context.Guild.Roles.ElementAt(new Random().Next(Context.Guild.Roles.Count)).Color,
                Footer = {
                    Text = $"Michiru Bot | v{Vars.Version}"
                }
            }
            .AddField("Owner", Context.Guild.Owner.Mention, true)
            .AddField("Admins", string.Join(", ", Context.Guild.Users.Where(x => x.GuildPermissions.Administrator && x.Id != Context.Guild.OwnerId).Select(x => x.Mention))[..256], true)
            .AddField("Members", Context.Guild.MemberCount, true)
            .AddField("Created At", Context.Guild.CreatedAt, true)
            .AddField($"Roles ({Context.Guild.Roles.Count})", string.Join(", ", Context.Guild.Roles.Select(x => x.Mention))[..256])
            .AddField("Bot Features", $"{(pmData.Enabled ? ":white_check_mark:" : ":white_x_mark:")} Personalized Roles\n" +
                                      $"{(bangerData.Enabled ? ":white_check_mark:" : ":white_x_mark:")} Banger System\n" +
                                      $"{(isPennyGuild ? ":white_check_mark: Guild Update Notices" : "")}");
        if (bangerData.Enabled)
            embed.AddField("Bangers", bangerData.SubmittedBangers, true);
        if (pmData.Enabled)
            embed.AddField("Personalized Members", pmData.Members!.Count, true);
        await RespondAsync(embed: embed.Build());
    }
}