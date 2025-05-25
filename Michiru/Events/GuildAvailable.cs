using Discord.WebSocket;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration._Base_Bot.Classes;
using Serilog;

namespace Michiru.Events;

public class GuildAvailable {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "EVENT:GuildAvailable");

    internal static Task OnGuildAvailable(SocketGuild guild) {
        Logger.Information("Guild available for {GuildName} ({GuildId})", guild.Name, guild.Id);
        var banger = new Banger {
            Enabled = false,
            GuildId = guild.Id,
            ChannelId = 0,
            UrlErrorResponseMessage = "This URL is not whitelisted.",
            FileErrorResponseMessage = "This file type is not whitelisted.",
            SpeakFreely = false,
            AddUpvoteEmoji = true,
            AddDownvoteEmoji = false,
            UseCustomUpvoteEmoji = true,
            CustomUpvoteEmojiName = "upvote",
            CustomUpvoteEmojiId = 1201639290048872529,
            UseCustomDownvoteEmoji = false,
            CustomDownvoteEmojiName = "downvote",
            CustomDownvoteEmojiId = 1201639287972696166,
            SuppressEmbedInsteadOfDelete = false
        };
        Config.Base.Banger!.Add(banger);
        var pm = new PersonalizedMember {
            Guilds = [new PmGuildData {
                Enabled = false,
                GuildId = guild.Id,
                ChannelId = 0,
                ResetTimer = 15,
                DefaultRoleId = 0,
                Members = []
            }]
        };
        Config.Base.PersonalizedMember!.Add(pm);
        Config.Save();
        Logger.Information("Added new guild {GuildName} ({GuildId}) to config", guild.Name, guild.Id);
        return Task.CompletedTask;
    }
}