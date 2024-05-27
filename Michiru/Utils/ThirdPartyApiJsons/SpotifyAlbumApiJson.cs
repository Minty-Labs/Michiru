using Discord.WebSocket;
using Michiru.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.ThirdPartyApiJsons;

public class SpotifyAlbumApiJson {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "SpotifyAlbumApiJson");
    private const string AlbumApiUrl = "https://api.spotify.com/v1/albums/";
    private const string AccountApiUrl = "https://accounts.spotify.com/api/token";
    private static string? BearerToken { get; set; }
    private static DateTime TokenExpiration { get; set; }

    public static async Task<Root?> GetAlbumData(string albumUrlId) {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Spotify.SpotifyClientId) || string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret)) {
            // await Program.Instance.ErrorLogChannel!.SendMessageAsync("[GetAlbumData] Spotify API Keys are not set!");
            Logger.Error("[GetAlbumData] Spotify API Keys are not set!");
            return null;
        }

        if (DateTime.UtcNow > TokenExpiration) {
            // Refresh token
            var http = new RestClient();
            http.AddDefaultHeaders(new Dictionary<string, string> {
                { "Content-Type", "application/x-www-form-urlencoded" },
                // { "User-Agent", Vars.BotUserAgent },
            });
            var request = new RestRequest(AccountApiUrl, Method.Post);
            // request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Config.Base.Api.ApiKeys.Spotify.SpotifyClientId}:{Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret}"))}");
            request.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);
            request.AddParameter("client_id", Config.Base.Api.ApiKeys.Spotify.SpotifyClientId!, ParameterType.GetOrPost);
            request.AddParameter("client_secret", Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret!, ParameterType.GetOrPost);
            var response = http.Execute<SpotifyToken>(request);
            var jsonData = JsonConvert.DeserializeObject<SpotifyToken>(response.Content!);
            if (response.Content != null) {
                BearerToken = jsonData!.access_token;
                TokenExpiration = DateTime.UtcNow.AddSeconds(jsonData!.expires_in);
            }
            else {
                return null;
            }

            await Task.Delay(TimeSpan.FromSeconds(1.5f));
        }

        // relay album data
        var http2 = new RestClient();
        http2.AddDefaultHeaders(new Dictionary<string, string> {
            { "Content-Type", "application/json" },
            // { "User-Agent", Vars.BotUserAgent },
            { "Authorization", $"Bearer {BearerToken}" }
        });
        var request2 = new RestRequest($"{AlbumApiUrl}{albumUrlId}", Method.Get);
        var response2 = http2.Execute<Root>(request2);
        return JsonConvert.DeserializeObject<Root>(response2.Content!);
    }
}

public class Artist {
    public ExternalUrls external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class Copyright {
    public string text { get; set; }
    public string type { get; set; }
}

public class ExternalIds {
    public string upc { get; set; }
}

public class ExternalUrls {
    public string spotify { get; set; }
}

public class Image {
    public int height { get; set; }
    public string url { get; set; }
    public int width { get; set; }
}

public class Item {
    public List<Artist> artists { get; set; }
    public List<string> available_markets { get; set; }
    public int disc_number { get; set; }
    public int duration_ms { get; set; }
    public bool @explicit { get; set; }
    public ExternalUrls external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public bool is_local { get; set; }
    public string name { get; set; }
    public string preview_url { get; set; }
    public int track_number { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class Root {
    public string album_type { get; set; }
    public List<Artist> artists { get; set; }
    public List<string> available_markets { get; set; }
    public List<Copyright> copyrights { get; set; }
    public ExternalIds external_ids { get; set; }
    public ExternalUrls external_urls { get; set; }
    public List<object> genres { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public List<Image> images { get; set; }
    public string label { get; set; }
    public string name { get; set; }
    public int popularity { get; set; }
    public string release_date { get; set; }
    public string release_date_precision { get; set; }
    public int total_tracks { get; set; }
    public Tracks tracks { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class Tracks {
    public string href { get; set; }
    public List<Item> items { get; set; }
    public int limit { get; set; }
    public object next { get; set; }
    public int offset { get; set; }
    public object previous { get; set; }
    public int total { get; set; }
}