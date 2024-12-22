namespace Michiru.Configuration.Moderation.Classes;

public class User {
    public ulong Id { get; init; }
    public string Username { get; set; }
    public List<Moderation> Moderation { get; init; }
}