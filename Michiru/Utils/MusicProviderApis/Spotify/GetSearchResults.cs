using System.Web;
using Michiru.Configuration._Base_Bot;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.Spotify;

public class GetSearchResults {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "SpotifySearchApiResults");
    private const string SearchApiUrl = "https://api.spotify.com/v1/search";

    public static async Task<string?> SearchForUrl(string query) {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Spotify.SpotifyClientId) || string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Spotify.SpotifyClientSecret)) {
            Logger.Error("Spotify API Keys are not set!");
            return null;
        }

        if (DateTime.UtcNow > CheckAuthToken.TokenExpiration || string.IsNullOrWhiteSpace(CheckAuthToken.BearerToken))
            await CheckAuthToken.UpdateBearerToken();

        await Task.Delay(TimeSpan.FromSeconds(1.5f));

        // build search query
        var restClient = new RestClient();
        restClient.AddDefaultHeaders(new Dictionary<string, string> {
            { "Authorization", $"Bearer {CheckAuthToken.BearerToken}" }
        });

        // var builder = new UriBuilder(SearchApiUrl);
        // var queries = HttpUtility.ParseQueryString(builder.Query);
        // queries["q"] = query;
        // queries["market"] = "US";
        // queries["type"] = "track";
        // queries["limit"] = "2";
        //
        // builder.Query = queries.ToString();
        // var encodedUrl = builder.ToString();
        
        var encodedUrl = $"{SearchApiUrl}?q={HttpUtility.UrlEncode(query)}&market=US&type=track&limit=2";
        var restRequest = new RestRequest(encodedUrl, Method.Get);
        var restResponse = restClient.Execute<SearchData>(restRequest);

        if (restResponse.ResponseStatus is not ResponseStatus.Completed) { // is not 200
            Logger.Error("Failed to get Spotify API content for the Search Query!\nUrl used: {0}", encodedUrl);
            return null;
        }

        var main = JsonConvert.DeserializeObject<SearchData>(restResponse.Content!);

        if (main is not null) {
            var first = main.Tracks.Items.FirstOrDefault();
            return first is null ? "[s404] ZERO RESULTS" : first.ExternalUrls.Spotify;
        }

        Logger.Error("Failed to get Spotify API content for the Search Query!\nUrl used: {0}", encodedUrl);
        return null;
    }
}

#region local json api restults

public class SearchData {
    [JsonProperty("tracks")] public Tracks4 Tracks { get; set; }
}

public class Tracks4 {
    [JsonProperty("href")] public string Href { get; set; }

    [JsonProperty("limit")] public long Limit { get; set; }

    [JsonProperty("next")] public string Next { get; set; }

    [JsonProperty("offset")] public long Offset { get; set; }

    [JsonProperty("previous")] public object Previous { get; set; }

    [JsonProperty("total")] public long Total { get; set; }

    [JsonProperty("items")] public Item4[] Items { get; set; }
}

public class Item4 {
    [JsonProperty("album")] public Album4 Album { get; set; }

    [JsonProperty("artists")] public Artist4[] Artists { get; set; }

    [JsonProperty("disc_number")] public long DiscNumber { get; set; }

    [JsonProperty("duration_ms")] public long DurationMs { get; set; }

    [JsonProperty("explicit")] public bool Explicit { get; set; }

    [JsonProperty("external_ids")] public ExternalIds4 ExternalIds { get; set; }

    [JsonProperty("external_urls")] public ExternalUrls4 ExternalUrls { get; set; }

    [JsonProperty("href")] public string Href { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("is_local")] public bool IsLocal { get; set; }

    [JsonProperty("is_playable")] public bool IsPlayable { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("popularity")] public long Popularity { get; set; }

    [JsonProperty("preview_url")] public object PreviewUrl { get; set; }

    [JsonProperty("track_number")] public long TrackNumber { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("uri")] public string Uri { get; set; }
}

public class Album4 {
    [JsonProperty("album_type")] public string AlbumType { get; set; }

    [JsonProperty("artists")] public Artist4[] Artists { get; set; }

    [JsonProperty("external_urls")] public ExternalUrls4 ExternalUrls { get; set; }

    [JsonProperty("href")] public string Href { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("images")] public Image[] Images { get; set; }

    [JsonProperty("is_playable")] public bool IsPlayable { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("release_date")] public DateTimeOffset ReleaseDate { get; set; }

    [JsonProperty("release_date_precision")] public string ReleaseDatePrecision { get; set; }

    [JsonProperty("total_tracks")] public long TotalTracks { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("uri")] public string Uri { get; set; }
}

public class Artist4 {
    [JsonProperty("external_urls")] public ExternalUrls4 ExternalUrls { get; set; }

    [JsonProperty("href")] public string Href { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("uri")] public string Uri { get; set; }
}

public class ExternalUrls4 {
    [JsonProperty("spotify")] public string Spotify { get; set; }
}

public class Image4 {
    [JsonProperty("height")] public long Height { get; set; }

    [JsonProperty("width")] public long Width { get; set; }

    [JsonProperty("url")] public string Url { get; set; }
}

public class ExternalIds4 {
    [JsonProperty("isrc")] public string Isrc { get; set; }
}

#endregion