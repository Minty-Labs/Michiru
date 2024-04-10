using Discord;
using Discord.Interactions;
using Michiru.Commands.Preexecution;
using Michiru.Configuration;
using Michiru.Utils;

namespace Michiru.Commands.Slash;

public class ServerInfo : InteractionModuleBase<SocketInteractionContext> {
    [RateLimit(10, 20)]
    [SlashCommand("serverinfo", "Get information about the server")]
    public async Task ServerInfoCommand() {
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
        embed.AddField("Created At", $"{Context.Guild.CreatedAt.UtcDateTime.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}", true);
        embed.AddField("Roles", $"{Context.Guild.Roles.Count}");
        var check = EmojiUtils.GetCustomEmoji("checked_box", 1225518363871023165) ?? Emote.Parse("<:checked_box:1225518363871023165>") ?? Emote.Parse(":white_check_mark:");
        var uncheck = EmojiUtils.GetCustomEmoji("unchecked_box", 1225518365137698817) ?? Emote.Parse("<:unchecked_box:1225518365137698817>") ?? Emote.Parse(":x_checked_box:");
        embed.AddField("Bot Features", $"{(pmData.Enabled ? check.ToString() : uncheck.ToString())} Personalized Roles\n" +
                                       $"{(bangerData.Enabled ? check.ToString() : uncheck.ToString())} Banger System\n" +
                                       $"{(isPennyGuild ? check.ToString() + " Guild Update Notices" : "")}");
        var gloablBangerData = Config.GetBangerNumber();
        var serverToGlobalBangerPercentage = (float)bangerData.SubmittedBangers / gloablBangerData * 100;
        var pmtoMemberCountPercentage = (float)pmData.Members!.Count / Context.Guild.MemberCount * 100;
        if (bangerData.Enabled)
            embed.AddField("Bangers (Server/Global)", $"{bangerData.SubmittedBangers} / {gloablBangerData} | {serverToGlobalBangerPercentage:F1}%", true);
        if (pmData.Enabled)
            embed.AddField("Personalized Members", $"{pmData.Members!.Count} | {pmtoMemberCountPercentage:00}%", true);
        await RespondAsync(embed: embed.Build());
    }
}