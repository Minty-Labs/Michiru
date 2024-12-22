namespace Michiru.Configuration._Base_Bot.Classes;

public class RotatingStatus {
    public bool Enabled { get; set; } = false;
    public int MinutesPerStatus { get; set; } = 2;
    public List<Status> Statuses { get; set; } = [];
}

public class Status {
    public int Id { get; set; } = 0;
    public string ActivityText { get; set; } = "lots of cuties";
    public string ActivityType { get; set; } = "Watching";
    public string UserStatus { get; set; } = "Online";
}