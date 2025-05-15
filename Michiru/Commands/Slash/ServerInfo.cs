using Discord;
using Discord.Interactions;
using Michiru.Commands.Preexecution;
using Michiru.Configuration;
using Michiru.Configuration._Base_Bot;
using Michiru.Utils;

namespace Michiru.Commands.Slash;

public class ServerInfo : InteractionModuleBase<SocketInteractionContext> {
    [SlashCommand("serverinfo", "Get information about the server"), RateLimit(10, 20), IntegrationType(ApplicationIntegrationType.GuildInstall)]
    public async Task ServerInfoCommand() {
        var pmData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
        var bangerData = Config.GetGuildBanger(Context.Guild.Id);
        // var guildFeatures = Config.GetGuildFeature(Context.Guild.Id);
        var isPennyGuild = Context.Guild.Id == Config.Base.PennysGuildWatcher.GuildId;
        var check = EmojiUtils.GetCustomEmoji("checked_box", 1225518363871023165) ?? Emote.Parse("<:checked_box:1225518363871023165>") ?? Emote.Parse(":white_check_mark:");
        var uncheck = EmojiUtils.GetCustomEmoji("unchecked_box", 1225518365137698817) ?? Emote.Parse("<:unchecked_box:1225518365137698817>") ?? Emote.Parse(":x_checked_box:");
        var transparent = await Context.Client.GetApplicationEmoteAsync(1266756179774799934);
        var globalBangerData = Config.GetBangerNumber();
        var serverToGlobalBangerPercentage = (float)bangerData.SubmittedBangers / globalBangerData * 100;
        var pmToMemberCountPercentage = (float)pmData.Members!.Count / Context.Guild.MemberCount * 100;

        var embed = new EmbedBuilder {
                Title = $"{Context.Guild.Name} ({Context.Guild.Id})",
                ThumbnailUrl = Context.Guild.IconUrl ?? "https://img.mili.lgbt/null.jpg",
                Footer = new EmbedFooterBuilder {
                    Text = $"Michiru Bot • v{Vars.VersionStr}"
                },
                Color = Context.Guild.Roles.ElementAt(new Random().Next(Context.Guild.Roles.Count)).Color
            }
            .AddField("Owner", $"{Context.Guild.Owner.Mention}")
            //.AddField("Admins", $"{string.Join(", ", Context.Guild.Users.Where(x => x.GuildPermissions.Administrator && x.Id != Context.Guild.OwnerId).Select(x => x.Mention))[..256]}", true)
            .AddField("Created At", $"{Context.Guild.CreatedAt.UtcDateTime.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}")
            .AddField("Members", $"{Context.Guild.MemberCount}", true)
            .AddField("Roles", $"{Context.Guild.Roles.Count}", true)
            .AddField("Bot Features",
                $"{(pmData.Enabled ? check.ToString() : uncheck.ToString())} Personalized Roles\n" +
                    $"{transparent} in {(pmData.ChannelId == 0 ? MarkdownUtils.ToCodeBlockSingleLine("(no set channel)") : $"<#{pmData.ChannelId}>")}\n" +
                
                $"{(bangerData.Enabled ? check.ToString() : uncheck.ToString())} Banger System\n" +
                    $"{transparent} in {(bangerData.ChannelId == 0 ? MarkdownUtils.ToCodeBlockSingleLine("(no set channel)") : $"<#{bangerData.ChannelId}>")}\n" +
                
                // $"{(guildFeatures.Join.Enable ? check.ToString() : uncheck.ToString())} Member Join Watcher\n" +
                //     $"{transparent} in {(guildFeatures.Join.ChannelId == 0 ? MarkdownUtils.ToCodeBlockSingleLine("(no set channel)") : $"<#{guildFeatures.Join.ChannelId}>")}\n" +
                //     $"{transparent} {(guildFeatures.Join.DmWelcomeMessage ? check.ToString() : uncheck.ToString())} DM Message\n" +
                //     $"{transparent} {(guildFeatures.Join.OverrideAllWithEmbed ? check.ToString() : uncheck.ToString())} Embed Override\n" +
                //
                // $"{(guildFeatures.Leave.Enable ? check.ToString() : uncheck.ToString())} Member Leave Watcher\n" +
                //     $"{transparent} in {(guildFeatures.Leave.ChannelId == 0 ? MarkdownUtils.ToCodeBlockSingleLine("(no set channel)") : $"<#{guildFeatures.Leave.ChannelId}>")}\n" +
                //     $"{transparent} {(guildFeatures.Leave.OverrideAllWithEmbed ? check.ToString() : uncheck.ToString())} Embed Override\n" +
                
                $"{(isPennyGuild ? check + " Guild Update Notices" : "")}");

        if (bangerData.Enabled)
            embed.AddField("Bangers (Server/Global)", $"{bangerData.SubmittedBangers} / {globalBangerData} | {serverToGlobalBangerPercentage:F1}%", true);
        if (pmData.Enabled)
            embed.AddField("Personalized Members", $"{pmData.Members!.Count} | {pmToMemberCountPercentage:00}%", true);

        await RespondAsync(embed: embed.Build());
    }
}