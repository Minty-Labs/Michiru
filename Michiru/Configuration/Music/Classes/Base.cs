namespace Michiru.Configuration.Music.Classes;

public class Base {
    public int Version { get; set; } = 1;
    public List<Submission> MusicSubmissions { get; init; } = [];
}