using System.Text.Json.Serialization;
namespace Michiru.Configuration.Classes;

public class Api {
    [JsonPropertyName("API Keys")] public ApiKeys ApiKeys { get; set; } = new();
}

public class ApiKeys {
    [JsonPropertyName("Unsplash Access Key")] public string? UnsplashAccessKey { get; set; } = "";
    [JsonPropertyName("Unsplash Secret Key")] public string? UnsplashSecretKey { get; set; } = "";
    [JsonPropertyName("CookieAPI Key")] public string? CookieClientApiKey { get; set; } = "";
    [JsonPropertyName("FluxpointAPI Key")] public string? FluxpointApiKey { get; set; } = "";
}