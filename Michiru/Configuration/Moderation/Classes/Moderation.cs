namespace Michiru.Configuration.Moderation.Classes;

public class Moderation {
    public int Id { get; init; }
    public string Type { get; init; }
    public bool IsPardoned { get; set; }
    public ulong UserId { get; init; }
    public ulong GuildId { get; init; }
    public string? Reason { get; init; }
    public DateTime DateTimeGiven { get; init; }
}