using Discord;
using Discord.WebSocket;
using Michiru.Configuration._Base_Bot;
using Michiru.Utils;
using Serilog;

namespace Michiru.Events;

public static class MemberUpdated {
    internal static async Task MemberJoin(SocketGuildUser? user) {
        var logger = Log.ForContext("SourceContext", "EVENT:MemberJoin");
        if (user is null) {
            logger.Error("User is null");
            return;
        }
        var guild = user.Guild;
        var config = Config.GetGuildFeature(guild.Id);
        if (!config.Join.Enable) return;
        var channel = guild.GetTextChannel(config.Join.ChannelId);
        if (channel == null) {
            logger.Warning("Channel not found for guild {GuildId}", guild.Id);
            return;
        }

        if (config.Join.DmWelcomeMessage && string.IsNullOrWhiteSpace(config.Join.JoinMessageText)) {
            var stringMsg = $"Welcome to {guild.Name}!";
            var pm = Config.GetGuildPersonalizedMember(guild.Id);
            if (pm.Enabled)
                stringMsg += $"\nCreate your personal role by running {MarkdownUtils.ToCodeBlockSingleLine("/personalization createrole")} in <#{pm.ChannelId}>\n" +
                             $"You can also update role every {pm.ResetTimer} seconds by running the {MarkdownUtils.ToCodeBlockSingleLine("/personalization updaterole")} command.\n" +
                             $"Choose your choice of HEX color easily by using {MarkdownUtils.MakeLink("this website", "https://html-color.codes/")} and inputing that hex code in the color box.";
            try {
                await user.SendMessageAsync(stringMsg);
            }
            catch {
                // silently fail if user has DMs disabled
            }
        }
        else return;

        if (config.Join.OverrideAllWithEmbed) {
            var embed = new EmbedBuilder {
                Title = "Welcome!",
                Description = config.Join.JoinMessageText ?? $"Welcome, {user.Mention}!",
                Color = Color.Green
            };
            if (config.Join.ShowDetailedEmbed) {
                embed.AddField("User ID", user.Id);
                embed.AddField("Account Created", $"{user.CreatedAt.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}");
                embed.AddField("Joined Server", $"{user.JoinedAt?.ConvertToDiscordTimestamp(TimestampFormat.ShortDateTime)}");
            }
            await channel.SendMessageAsync(embed: embed.Build());
            return;
        }
        await channel.SendMessageAsync(config.Join.JoinMessageText?.ParseMessageTextModifiers(user, guild, Config.GetGuildPersonalizedMember(guild.Id)));
    }
    
    internal static async Task MemberLeave(SocketGuild guild, SocketUser user) {
        var logger = Log.ForContext("SourceContext", "EVENT:MemberLeave");
        var config = Config.GetGuildFeature(guild.Id);
        if (!config.Leave.Enable) return;
        var channel = guild.GetTextChannel(config.Leave.ChannelId);
        if (channel == null) {
            logger.Warning("Channel not found for guild {GuildId}", guild.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(config.Leave.LeaveMessageText))
            return;
        
        if (config.Leave.OverrideAllWithEmbed) {
            var embed = new EmbedBuilder {
                Title = "Goodbye!",
                Description = config.Leave.LeaveMessageText ?? $"Goodbye, {user.Mention}!",
                Color = Color.Red
            };
            if (config.Leave.ShowDetailedEmbed) {
                embed.AddField("Username", user.Username, true);
                embed.AddField("User ID", user.Id, true);
                embed.AddField("Account Created", $"{user.CreatedAt.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}");
                embed.AddField("Joined Server", $"{(user as SocketGuildUser)?.JoinedAt?.ConvertToDiscordTimestamp(TimestampFormat.ShortDateTime) ?? "N/A"}");
            }
            await channel.SendMessageAsync(embed: embed.Build());
            return;
        }
        await channel.SendMessageAsync(config.Leave.LeaveMessageText?.ParseMessageTextModifiers(user, guild, Config.GetGuildPersonalizedMember(guild.Id)));
    }
}