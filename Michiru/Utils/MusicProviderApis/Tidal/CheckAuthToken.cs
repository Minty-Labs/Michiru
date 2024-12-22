using System.Text;
using Michiru.Configuration._Base_Bot;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace Michiru.Utils.MusicProviderApis.Tidal;

public class CheckAuthToken {
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "TidalBearerTokenResults");
    private const string AccountApiUrl = "https://auth.tidal.com/v1/oauth2/token";
    public static string? BearerToken { get; private set; }
    public static DateTime TokenExpiration { get; private set; }

    public static Task UpdateBearerToken() {
        var pattern = $"{Config.Base.Api.ApiKeys.Tidal.TidalClientId!}:{Config.Base.Api.ApiKeys.Tidal.TidalClientSecret!}";
        var base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(pattern));

        var restClient = new RestClient();
        restClient.AddDefaultHeaders(new Dictionary<string, string> {
            { "Content-Type", "application/x-www-form-urlencoded" },
            { "Authorization", $"Basic {base64Credentials}" }
        });

        var restRequest = new RestRequest(AccountApiUrl, Method.Post);
        restRequest.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);

        var restResponse = restClient.Execute<TidalToken>(restRequest);
        var jsonData = JsonConvert.DeserializeObject<TidalToken>(restResponse.Content!);
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