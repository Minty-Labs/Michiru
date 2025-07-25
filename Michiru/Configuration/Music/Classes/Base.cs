namespace Michiru.Configuration.Music.Classes;

public class Base {
    public int Version { get; init; } = 3;
    public List<Submission> MusicSubmissions { get; init; } = [];
    public List<ulong> UsersNotAllowedToEdit { get; init; } = [];
}