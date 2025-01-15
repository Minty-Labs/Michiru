using Discord;
using Michiru.Configuration;
using Michiru.Configuration._Base_Bot;
using Michiru.Utils;
using Quartz;

namespace Michiru.Managers.Jobs;

public static class RotatingStatus {
    private static int _listEntry;

    public static async Task Update() {
        if (!Config.Base.RotatingStatus.Enabled) return;

        var totalStatuses = Config.Base.RotatingStatus.Statuses.Count;
        var status = Config.Base.RotatingStatus.Statuses[_listEntry];
        var client = Program.Instance.Client;
        
        try {
            await client.SetStatusAsync(status.UserStatus.GetUserStatus());
            await client.SetGameAsync(status.ActivityText.GetStatusVariable(), type: status.ActivityType.GetActivityType());
        }
        catch {
            await client.SetStatusAsync(UserStatus.Online);
            await client.SetGameAsync("with your love", type: ActivityType.Competing);
        }

        _listEntry++;
        if (_listEntry >= totalStatuses) _listEntry = 0;
    }

    private static string GetStatusVariable(this string input) {
        return input.Replace("%bangers%", $"{Config.GetBangerNumber()}")
                .Replace("%pm%", $"{Config.GetPersonalizedMemberCount()}")
                .Replace("%users%", $"{Program.Instance.Client.Guilds.Sum(guild => guild.MemberCount)}")
                .Replace("%os%", Vars.IsWindows ? "Windows" : "Linux")
            ;
    }
    
    /// <summary>
    /// Get Discord ActivityType from string
    /// </summary>
    /// <param name="type">activity as string</param>
    /// <returns>DSharpPlus.Entities.ActivityType</returns>
    private static ActivityType GetActivityType(this string type) {
        return type.ToLower() switch {
            "playing" => ActivityType.Playing,
            "listening" => ActivityType.Listening,
            "watching" => ActivityType.Watching,
            "streaming" => ActivityType.Streaming,
            "competing" => ActivityType.Competing,
            "custom" => ActivityType.CustomStatus,
            _ => ActivityType.CustomStatus
        };
    }

    /// <summary>
    /// Get Discord UserStatus from string
    /// </summary>
    /// <param name="status">status as string</param>
    /// <returns>DSharpPlus.Entities.UserStatus</returns>
    private static UserStatus GetUserStatus(this string status) {
        return status.ToLower() switch {
            "online" => UserStatus.Online,
            "idle" => UserStatus.Idle,
            "dnd" => UserStatus.DoNotDisturb,
            "do_not_disturb" => UserStatus.DoNotDisturb,
            "invisible" => UserStatus.Invisible,
            "offline" => UserStatus.Invisible,
            _ => UserStatus.Online
        };
    }
}

public class RotatingStatusJob : IJob {
    public async Task Execute(IJobExecutionContext context) {
        try {
            await RotatingStatus.Update();
        }
        catch (Exception err) {
            await ErrorSending.SendErrorToLoggingChannelAsync("Rotating Status:", null, err);
        }
    }
}