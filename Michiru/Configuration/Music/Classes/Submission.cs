namespace Michiru.Configuration.Music.Classes;

public class Submission {
    public string Artists { get; init; } = "";
    public string Title { get; init; } = "";
    public Services Services { get; init; } = new();
    public string SongLinkUrl { get; init; } = "";
    public DateTime SubmissionDate { get; init; } = DateTime.Now;
    public List<LastUpdatedBy> LastUpdatedBy { get; init; } = [];
}

public class LastUpdatedBy {
    public string Username { get; init; } = "";
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}