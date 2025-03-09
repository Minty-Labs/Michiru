using System.Web;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.SongLink;

public class SongLink {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "SongLinkApiJson");
    
    public static Task<Root?> LookupData(string shareLink) {
        var restClient = new RestClient();
        // restClient.AddDefaultHeaders(new Dictionary<string, string> {
        //     { "Content-Type", "application/json" },
        //     { "User-Agent", $"Michiru Discord App/{Vars.VersionStr}" }
        // });

        string? provider;
        if (shareLink.Contains("spotify")) 
            provider = "spotify";
        else if (shareLink.Contains("deezer"))
            provider = "deezer";
        else if (shareLink.Contains("apple"))
            provider = "appleMusic";
        else if (shareLink.Contains("pandora"))
            provider = "pandora";
        else if (shareLink.Contains("tidal"))
            provider = "tidal";
        else if (shareLink.Contains("youtube")) 
            provider = shareLink.Contains("music") ? "youtubeMusic" : "youtube";
        else {
            Logger.Error("Unknown provider for the share link: {0}", shareLink);
            return Task.FromResult<Root?>(null);
        }
        Logger.Information("Share Link: {shareLink}", shareLink);
        Logger.Information("Provider: {provider}", provider);

        var trackId = shareLink.Contains('?') ? shareLink.Split("?")[0].Split("/").Last() : shareLink.Split("/").Last();
        Logger.Information("Track ID: {id}", trackId);
        var encoded = HttpUtility.UrlEncode($"{provider}:track:{trackId}");
        var restRequest = new RestRequest($"https://api.song.link/v1-alpha.1/links?url={encoded}&userCountry=US", Method.Get);
        Logger.Information("Request: {request}", $"https://api.song.link/v1-alpha.1/links?url={encoded}&userCountry=US");
        var restResponse = restClient.Execute<Root>(restRequest);
        
        return Task.FromResult(JsonConvert.DeserializeObject<Root>(restResponse.Content!));
    }
    
    public static string ToJson(Root data) => JsonConvert.SerializeObject(data);
}