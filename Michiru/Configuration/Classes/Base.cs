using System.Text.Json.Serialization;

namespace Michiru.Configuration.Classes;

public class Base {
    public int ConfigVersion { get; set; } = 1;
    [JsonPropertyName("Token")] public string? BotToken { get; set; } = "";
    [JsonPropertyName("Activity Type")] public string ActivityType { get; init; } = "Watching";
    [JsonPropertyName("Game")] public string? ActivityText { get; init; } = "lots of cuties";
    [JsonPropertyName("Online Status")] public string UserStatus { get; init; } = "Online";
    [JsonPropertyName("Rotating Status")] public RotatingStatus RotatingStatus { get; init; } = new();
    [JsonPropertyName("Owner IDs")] public List<ulong>? OwnerIds { get; init; } = [];
    [JsonPropertyName("Bot Logs Channel")] public ulong BotLogsChannel { get; init; } = 0;
    [JsonPropertyName("Error Logs Channel")] public ulong ErrorLogsChannel { get; init; } = 0;
    [JsonPropertyName("Extra Banger Count")] public int ExtraBangerCount { get; set; } = 0;
    [JsonPropertyName("Guild Features")] public List<GuildFeatures> GuildFeatures { get; init; } = []; // TODO: Add Banger and PersonalizedMember to GuildFeatures
    [JsonPropertyName("Banger System")] public List<Banger> Banger { get; init; } = [];
    [JsonPropertyName("Personalized Members")] public List<PersonalizedMember> PersonalizedMember { get; init; } = [];
    [JsonPropertyName("Penny's Guild Watcher")] public PennysGuildWatcher PennysGuildWatcher { get; init; } = new();
    // [JsonPropertyName("Penny's Guild History")] public List<PennysGuildWatcher> PennysGuildHistory { get; set; } = [];
    // [JsonPropertyName("GiveAways")] public List<GiveAway> GiveAways { get; set; } = [];
    [JsonPropertyName("Api")] public Api Api { get; init; } = new();
}