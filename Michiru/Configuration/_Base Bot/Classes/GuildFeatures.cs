namespace Michiru.Configuration._Base_Bot.Classes;

public class GuildFeatures {
    public ulong GuildId { get; set; } = 0;
    public Join Join { get; set; } = new Join();
    public Leave Leave { get; set; } = new Leave();
    // [JsonPropertyName("Banger System")] public List<Banger> Banger { get; set; } = [];
    // [JsonPropertyName("Personalized Members")] public List<PmGuildData>? Guilds { get; set; } = [];
}

public class Join {
    public bool Enable { get; set; } = false;
    public ulong ChannelId { get; set; } = 0;
    public string? JoinMessageText { get; set; } = "";
    public bool OverrideAllWithEmbed { get; set; } = false;
    public bool ShowDetailedEmbed { get; set; } = false;
    public bool DmWelcomeMessage { get; set; } = false;
}

public class Leave {
    public bool Enable { get; set; } = false;
    public ulong ChannelId { get; set; } = 0;
    public string? LeaveMessageText { get; set; } = "";
    public bool ShowDetailedEmbed { get; set; } = false;
    public bool OverrideAllWithEmbed { get; set; } = false;
}