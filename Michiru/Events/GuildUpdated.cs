using Discord;
using Discord.WebSocket;
using Michiru.Configuration;
using Michiru.Utils;
using static System.DateTime;

namespace Michiru.Events;

public static class GuildUpdated {
    private static ulong _pennysGuildWatcherChannelId = 0;
    private static ulong _pennysGuildWatcherGuildId = 0;
    public static Task OnGuildUpdated(SocketGuild beforeInfoArg, SocketGuild afterInfoArg) {
        if (beforeInfoArg.Name == afterInfoArg.Name) return Task.CompletedTask;
        
        if (_pennysGuildWatcherGuildId == 0) _pennysGuildWatcherGuildId = Config.Base.PennysGuildWatcher.GuildId;
        if (beforeInfoArg.Id != _pennysGuildWatcherGuildId) return Task.CompletedTask;
        
        if (_pennysGuildWatcherChannelId == 0) _pennysGuildWatcherChannelId = Config.Base.PennysGuildWatcher.ChannelId;
        var channel = beforeInfoArg.GetTextChannel(_pennysGuildWatcherChannelId);
        if (channel is null) return Task.CompletedTask;
        
        var role = afterInfoArg.Roles.ElementAt(new Random().Next(afterInfoArg.Roles.Count));
        
        var daysNumber = UtcNow.Subtract(Config.Base.PennysGuildWatcher.LastUpdateTime.UnixTimeStampToDateTime()).Days;
        var embed = new EmbedBuilder {
                Title = "Guild Name Updated",
                Description = $"It has been {(daysNumber < 1 ? "less than a day" : (daysNumber == 1 ? "1 day" : $"{daysNumber} days"))} since the last time the guild name was updated.",
                Color = role?.Color ?? Colors.HexToColor("0091FF"),
                ThumbnailUrl = afterInfoArg.IconUrl
            }
            .AddField("Old Name", beforeInfoArg.Name)
            .AddField("New Name", afterInfoArg.Name);
        channel.SendMessageAsync(embed: embed.Build());
        Config.Base.PennysGuildWatcher.LastUpdateTime = UtcNow.GetSeconds();
        Config.Save();
        return Task.CompletedTask;
    }
}