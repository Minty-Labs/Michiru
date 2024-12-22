using System.Web;
using Michiru.Configuration._Base_Bot;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.Tidal;

public class GetSearchResults {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "TidalSearchApiResults");
    private const string SearchApiUrl = "https://openapi.tidal.com/v2/searchresults/";

    public static async Task<string?> SearchForUrl(string query) {
        if (string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Tidal.TidalClientId) || string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.Tidal.TidalClientSecret)) {
            Logger.Error("Tidal API Keys are not set!");
            return null;
        }

        if (DateTime.UtcNow > CheckAuthToken.TokenExpiration) // if is expired
            await CheckAuthToken.UpdateBearerToken();

        await Task.Delay(TimeSpan.FromSeconds(1.5f));
        
        // build search query
        var restClient = new RestClient();
        restClient.AddDefaultHeaders(new Dictionary<string, string> {
            { "Content-Type", "application/vnd.tidal.v1+json" },
            { "accept", "application/vnd.tidal.v1+json" },
            { "Authorization", $"Bearer {CheckAuthToken.BearerToken}" }
        });
        
        var encodedUrl = $"{SearchApiUrl}{HttpUtility.UrlEncode(query)}?countryCode=US&include=artists,tracks";
        var restRequest = new RestRequest(encodedUrl, Method.Get);
        var restResponse = restClient.Execute<SearchData>(restRequest);
        
        if (restResponse.ResponseStatus is not ResponseStatus.Completed) { // is not 200
            Logger.Error("Failed to get Tidal API Content for the Search Query!\nURL used: {0}", encodedUrl);
            return null;
        }
        
        var main = JsonConvert.DeserializeObject<SearchData>(restResponse.Content!);
        
        if (main is not null) {
            var first = main.Included.FirstOrDefault();
            return first is null ? "[t404] ZERO RESULTS" : first.Attributes.ExternalLinks.FirstOrDefault()!.Href;
        }
        
        Logger.Error("Failed to get Tidal API Content for the Search Query!\nURL used: {0}", encodedUrl);
        return null;
    }
}

#region local json api results

public class SearchData {
    [JsonProperty("data")] public Data Data { get; set; }

    [JsonProperty("included")] public Included[] Included { get; set; }
}

public class Data {
    [JsonProperty("attributes")] public DataAttributes Attributes { get; set; }

    [JsonProperty("relationships")] public DataRelationships Relationships { get; set; }

    [JsonProperty("links")] public Links Links { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("type")] public string Type { get; set; }
}

public class DataAttributes {
    [JsonProperty("trackingId")] public Guid TrackingId { get; set; }
}

public class Links {
    [JsonProperty("self")] public string Self { get; set; }
}

public class DataRelationships {
    [JsonProperty("albums")] public Albums Albums { get; set; }

    [JsonProperty("artists")] public Artists Artists { get; set; }

    [JsonProperty("playlists")] public Albums Playlists { get; set; }

    [JsonProperty("videos")] public Albums Videos { get; set; }

    [JsonProperty("topHits")] public Albums TopHits { get; set; }

    [JsonProperty("tracks")] public Artists Tracks { get; set; }
}

public class Albums {
    [JsonProperty("links")] public Links Links { get; set; }
}

public class Artists {
    [JsonProperty("data")] public Datum[] Data { get; set; }

    [JsonProperty("links")] public Links Links { get; set; }
}

public class Datum {
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("type")] public string Type { get; set; }
}

public class Included {
    [JsonProperty("attributes")] public IncludedAttributes Attributes { get; set; }

    [JsonProperty("relationships")] public IncludedRelationships Relationships { get; set; }

    [JsonProperty("links")] public Links Links { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("type")] public string Type { get; set; }
}

public class IncludedAttributes {
    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("isrc")] public string Isrc { get; set; }

    [JsonProperty("duration")] public string Duration { get; set; }

    [JsonProperty("copyright")] public string Copyright { get; set; }

    [JsonProperty("explicit")] public bool Explicit { get; set; }

    [JsonProperty("popularity")] public double Popularity { get; set; }

    [JsonProperty("availability")] public string[] Availability { get; set; }

    [JsonProperty("mediaTags")] public string[] MediaTags { get; set; }

    [JsonProperty("externalLinks")] public ExternalLink[] ExternalLinks { get; set; }

    [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
    public string Version { get; set; }
}

public class ExternalLink {
    [JsonProperty("href")] public string Href { get; set; }

    [JsonProperty("meta")] public Meta Meta { get; set; }
}

public class Meta {
    [JsonProperty("type")] public string Type { get; set; }
}

public class IncludedRelationships {
    [JsonProperty("albums")] public Albums Albums { get; set; }

    [JsonProperty("artists")] public Albums Artists { get; set; }

    [JsonProperty("similarTracks")] public Albums SimilarTracks { get; set; }

    [JsonProperty("providers")] public Albums Providers { get; set; }

    [JsonProperty("radio")] public Albums Radio { get; set; }
}

#endregion