namespace Michiru.Configuration.Moderation.Classes;

public class Channel {
    public ulong BanOrUnban { get; set; }
    public ulong KickWarnTimeoutMute { get; set; }
    public ulong FreezeSlowmodeMasspurge { get; set; }
}