using System.Text.Json.Serialization;

namespace Michiru.Configuration._Base_Bot.Classes;

public class Banger {
    public bool Enabled { get; set; } = false;
    [JsonPropertyName("Guild ID")] public ulong GuildId { get; set; } = 0;
    [JsonPropertyName("Channel ID")] public ulong ChannelId { get; set; } = 0;
    public int SubmittedBangers { get; set; } = 0;
    [JsonPropertyName("URL Error Response Message")] public string? UrlErrorResponseMessage { get; set; } = "This URL is not whitelisted.";
    [JsonPropertyName("File Error Response Message")] public string? FileErrorResponseMessage { get; set; } = "This file type is not whitelisted.";
    public bool SpeakFreely { get; set; } = false;
    // public bool OfferToReplaceSpotifyTrack { get; set; } = false;
    public bool AddUpvoteEmoji { get; set; } = true;
    public bool AddDownvoteEmoji { get; set; } = false;
    public bool UseCustomUpvoteEmoji { get; set; } = true;
    public string CustomUpvoteEmojiName { get; set; } = "upvote";
    public ulong CustomUpvoteEmojiId { get; set; } = 1201639290048872529;
    public bool UseCustomDownvoteEmoji { get; set; } = false;
    public string CustomDownvoteEmojiName { get; set; } = "downvote";
    public ulong CustomDownvoteEmojiId { get; set; } = 1201639287972696166;
    public bool SuppressEmbedInsteadOfDelete { get; set; } = false;
}