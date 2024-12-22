using Michiru.Configuration._Base_Bot;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.Spotify;

public class CheckAuthToken {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "SpotifyBearerTokenResults");
    private const string AccountApiUrl = "https://accounts.spotify.com/api/token";
    public static string? BearerToken { get; private set; }
    public static DateTime TokenExpiration { get; private set; }

    public static Task UpdateBearerToken() {
        var restClient = new RestClient();
        restClient.AddDefaultHeaders(new Dictionary<string, string> {
            { "Content-Type", "application/x-www-form-urlencoded" }
        });
        var restRequest = new RestRequest(AccountApiUrl, Method.Post);
        restRequest.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);
        restRequest.AddParameter("client_id", Config.Base.Api.ApiKeys.Spotify.SpotifyClientId!, ParameterType.GetOrPost);
        restRequest.AddParameter("client_secret", Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret!, ParameterType.GetOrPost);
        
        var restResponse = restClient.Execute<SpotifyToken>(restRequest);
        var jsonData = JsonConvert.DeserializeObject<SpotifyToken>(restResponse.Content!);
        
        if (restResponse.Content is null) {
            Logger.Error("Failed to get Tidal API Content for the Bearer Token!");
            return Task.CompletedTask;
        }
        
        BearerToken = jsonData!.access_token;
        TokenExpiration = DateTime.UtcNow.AddSeconds(jsonData!.expires_in);
        Logger.Information("Updated Tidal Bearer Token!");
        return Task.CompletedTask;
    }
}