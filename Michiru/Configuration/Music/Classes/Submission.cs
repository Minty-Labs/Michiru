namespace Michiru.Configuration.Music.Classes;

public class Submission {
    public int SubmissionId { get; init; } = 0;
    public string Artists { get; init; } = "";
    public string Title { get; init; } = "";
    public Services Services { get; init; } = new();
    public string OthersLink { get; init; } = "";
    public DateTime SubmissionDate { get; set; } = DateTime.Now;
}