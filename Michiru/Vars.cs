using System.Reflection;
using Discord.WebSocket;

namespace Michiru;

public static class Vars {
    public static string DNetVer => Assembly.GetAssembly(typeof(DiscordSocketClient))!.GetName().Version!.ToString(3);
    public const string Name = "Michiru";
    private static readonly Version VersionObj = new (1, 12, 6);
    // public const ulong ClientId = 477202627285876756;
    public const int TargetConfigVersion = 9;
    public const int TargetModConfigVersion = 1;
    public const int TargetMusicConfigVersion = 1;

    public static readonly string VersionStr = VersionObj.ToString(3) + (IsDebug ? "-dev" : "");
    // public static readonly DateTime BuildTime = IsDebug ? DateTime.UtcNow : new DateTime(2025, 1, 4, 0/*( 19 + 5 )*/, 06, 00);
    public const bool IsDebug = false;
    // public static string BuildDate => $"{BuildTime:F}";
    public static DateTime StartTime { get; set; }
    public static bool IsWindows { get; set; }
    public const string SupportServer = "https://discord.gg/Qg9eVB34sq";
    public const ulong SupportServerId = 1083619886980403272;
    // public static readonly string BotUserAgent = $"Mozilla/5.0 {(IsWindows ? "(Windows NT 10.0; Win64; x64; rv:115.0)" : "(X11; Linux x86_64)")} (compatible; {Name}/{VersionObj.ToString(3)}; +https://discordapp.com)";
    public static string DotNetTargetVersion => "9.0.2";
}