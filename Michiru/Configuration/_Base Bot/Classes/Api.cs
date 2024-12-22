using System.Text.Json.Serialization;

namespace Michiru.Configuration._Base_Bot.Classes;

public class Api {
    [JsonPropertyName("API Keys")] public ApiKeys ApiKeys { get; set; } = new();
}

public class ApiKeys {
    [JsonPropertyName("Unsplash Access Key")] public string? UnsplashAccessKey { get; set; } = "";
    [JsonPropertyName("Unsplash Secret Key")] public string? UnsplashSecretKey { get; set; } = "";
    [JsonPropertyName("CookieAPI Key")] public string? CookieClientApiKey { get; set; } = "";
    [JsonPropertyName("FluxpointAPI Key")] public string? FluxpointApiKey { get; set; } = "";
    [JsonPropertyName("Spotify")] public SpotifyApi Spotify { get; init; } = new();
    [JsonPropertyName("Deezer")] public DeezerApi Deezer { get; init; } = new();
    [JsonPropertyName("Tidal")] public TidalApi Tidal { get; init; } = new();
}

public class SpotifyApi {
    [JsonPropertyName("Spotify Client ID")] public string? SpotifyClientId { get; set; } = "";
    [JsonPropertyName("Spotify Client Secret")] public string? SpotifyClientSecret { get; set; } = "";
}

public class DeezerApi {
    [JsonPropertyName("Deezer Client ID")] public string? DeezerClientId { get; set; } = "";
    [JsonPropertyName("Deezer Client Secret")] public string? DeezerClientSecret { get; set; } = "";
}

public class TidalApi {
    [JsonPropertyName("Tidal Client ID")] public string? TidalClientId { get; set; } = "";
    [JsonPropertyName("Tidal Client Secret")] public string? TidalClientSecret { get; set; } = "";
}