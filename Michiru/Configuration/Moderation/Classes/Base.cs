using System.Text.Json.Serialization;

namespace Michiru.Configuration.Moderation.Classes;

public class Base {
    [JsonPropertyName("Moderation Configuration Version")] public int Version { get; set; }
    public List<Guild> Guilds { get; init; }
    public List<User> Users { get; init; }
}