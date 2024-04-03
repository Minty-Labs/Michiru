namespace Michiru.Configuration.Classes;

public class GiveAway {
    public ulong GuildId { get; set; } = 0;
    public ulong WatchChannelId { get; set; } = 0;
    public List<Entry> Entries { get; set; } = new();
}

public class Entry {
    public bool IsActive { get; set; } = false;
    public int EntryId { get; set; } = 1;
    public string? Prize { get; set; }
    public string? Description { get; set; }
    public int WinnerCount { get; set; } = 1;
    public int Duration { get; set; } = 2400;
    public ulong MessageId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong WatchMessageId { get; set; } = 0;
    public List<ulong> Participants { get; set; } = new();
}