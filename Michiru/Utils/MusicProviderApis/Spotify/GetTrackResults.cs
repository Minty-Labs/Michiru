using Michiru.Configuration._Base_Bot;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.Spotify;

public class GetTrackResults {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "SpotifyTrackApiJson");
    private const string TrackApiUrl = "https://api.spotify.com/v1/tracks/";

    public static async Task<Root?> GetTrackData(string trackId) {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Spotify.SpotifyClientId) || string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret)) {
            // await Program.Instance.ErrorLogChannel!.SendMessageAsync("[GetAlbumData] Spotify API Keys are not set!");
            Logger.Error("Spotify API Keys are not set!");
            return null;
        }

        if (DateTime.UtcNow > CheckAuthToken.TokenExpiration)
            await CheckAuthToken.UpdateBearerToken();

        await Task.Delay(TimeSpan.FromSeconds(1.5f));

        // relay track data
        var restClient = new RestClient();
        restClient.AddDefaultHeaders(new Dictionary<string, string> {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {CheckAuthToken.BearerToken}" }
        });
        var finalId = trackId;
        if (trackId.Contains('?')) 
            finalId = trackId.Split('?')[0];
        
        var restRequest = new RestRequest($"{TrackApiUrl}{finalId}", Method.Get);
        var restResponse = restClient.Execute<Root>(restRequest);
        return JsonConvert.DeserializeObject<Root>(restResponse.Content!);
    }
}

#region local json api results

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Album2 {
    public string album_type { get; set; }
    public List<Artist2> artists { get; set; }
    public List<string> available_markets { get; set; }
    public ExternalUrls2 external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public List<Image> images { get; set; }
    public string name { get; set; }
    public string release_date { get; set; }
    public string release_date_precision { get; set; }
    public int total_tracks { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class Artist2 {
    public ExternalUrls2 external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class ExternalIds2 {
    public string isrc { get; set; }
}

public class ExternalUrls2 {
    public string spotify { get; set; }
}

public class Image2 {
    public int height { get; set; }
    public string url { get; set; }
    public int width { get; set; }
}

public class Root2 {
    public Album2 album { get; set; }
    public List<Artist2> artists { get; set; }
    public List<string> available_markets { get; set; }
    public int disc_number { get; set; }
    public int duration_ms { get; set; }
    public bool @explicit { get; set; }
    public ExternalIds2 external_ids { get; set; }
    public ExternalUrls2 external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public bool is_local { get; set; }
    public string name { get; set; }
    public int popularity { get; set; }
    public string preview_url { get; set; }
    public int track_number { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

#endregion