﻿using System.Reflection;
using Discord.WebSocket;

namespace Michiru;

public static class Vars {
    public static string DNetVer => Assembly.GetAssembly(typeof(DiscordSocketClient))!.GetName().Version!.ToString(3);
    public const string Name = "Michiru";
    private static readonly Version VersionObj = new (1, 10, 0);
    // public const ulong ClientId = 477202627285876756;
    public const int TargetConfigVersion = 8;

    public static readonly string VersionStr = VersionObj.ToString(3) + (IsDebug ? "-dev" : "");
    public static readonly DateTime BuildTime = IsDebug ? DateTime.UtcNow : new DateTime(2024, 9, 26, ( 17 + 4 ), 48, 00);
    public const bool IsDebug = false;
    public static string BuildDate => $"{BuildTime:F}";
    public static DateTime StartTime { get; set; }
    public static bool IsWindows { get; set; }
    public const string SupportServer = "https://discord.gg/Qg9eVB34sq";
    public const ulong SupportServerId = 1083619886980403272;
    // public static readonly string BotUserAgent = $"Mozilla/5.0 {(IsWindows ? "(Windows NT 10.0; Win64; x64; rv:115.0)" : "(X11; Linux x86_64)")} (compatible; {Name}/{VersionObj.ToString(3)}; +https://discordapp.com)";
    public static string DotNetTargetVersion => "8.0.8";
}