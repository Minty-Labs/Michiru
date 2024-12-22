namespace Michiru.Configuration.Moderation.Classes;

public class Guild {
    public ulong Id { get; init; }
    public Channel Channels { get; init; }
    public bool IsCrossbanEnabled { get; set; }
    // public string CrossbanIdentifierKey { get; set; }
    public bool IsScamBanEnabled { get; set; }
    public bool IsChannelFreezeEnabled { get; set; }
    public bool DMOnWarn { get; set; }
}