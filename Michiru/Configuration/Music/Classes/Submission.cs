namespace Michiru.Configuration.Music.Classes;

public class Submission {
    public string Artists { get; init; } = "";
    public string Title { get; init; } = "";
    public Services Services { get; init; } = new();
    public string SongLinkUrl { get; init; } = "";
    public DateTime SubmissionDate { get; set; } = DateTime.Now;
}