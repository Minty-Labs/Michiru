using Michiru.Configuration._Base_Bot;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.Tidal;

public class GetTrackResults {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "TidalTrackApiResults");
    private const string TrackApiUrl = "https://openapi.tidal.com/tracks/";

    public static async Task<Root?> GetData(string trackId) {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Tidal.TidalClientId) || string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Tidal.TidalClientSecret)) {
            Logger.Error("Tidal API Keys are not set!");
            return null;
        }

        if (DateTime.UtcNow > CheckAuthToken.TokenExpiration || string.IsNullOrWhiteSpace(CheckAuthToken.BearerToken)) // if is expired
            await CheckAuthToken.UpdateBearerToken();

        await Task.Delay(TimeSpan.FromSeconds(1.5f));

        // relay track data
        var restClient = new RestClient();
        restClient.AddDefaultHeaders(new Dictionary<string, string> {
            { "Content-Type", "application/vnd.tidal.v1+json" },
            { "accept", "application/vnd.tidal.v1+json" },
            { "Authorization", $"Bearer {CheckAuthToken.BearerToken}" }
        });
        
        var finalId = trackId;
        if (trackId.Contains('?'))
            finalId = trackId.Split('?')[0];
        
        var restRequest = new RestRequest($"{TrackApiUrl}{finalId}", Method.Get);
        var restResponse = restClient.Execute<Root>(restRequest);
        return JsonConvert.DeserializeObject<Root>(restResponse.Content!);
    }
    
    public static string GetTrackId(string url) => url.Split('/')[^1];
}

#region local json api results

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Album {
    public string id { get; set; }
    public string title { get; set; }
    public List<ImageCover> imageCover { get; set; }
    public List<object> videoCover { get; set; }
}

public class Artist {
    public string id { get; set; }
    public string name { get; set; }
    public List<Picture> picture { get; set; }
    public bool main { get; set; }
}

public class ImageCover {
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}

public class MediaMetadata {
    public List<string> tags { get; set; }
}

public class Picture {
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}

public class Properties { }

public class Resource {
    public string artifactType { get; set; }
    public string id { get; set; }
    public string title { get; set; }
    public List<Artist> artists { get; set; }
    public Album album { get; set; }
    public int duration { get; set; }
    public int trackNumber { get; set; }
    public int volumeNumber { get; set; }
    public string isrc { get; set; }
    public string copyright { get; set; }
    public MediaMetadata mediaMetadata { get; set; }
    public Properties properties { get; set; }
    public string tidalUrl { get; set; }
}

public class Root {
    public Resource resource { get; set; }
}

#endregion