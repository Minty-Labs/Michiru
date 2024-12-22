namespace Michiru.Utils.MusicProviderApis.Tidal;

public class TidalToken {
    public string scope { get; set; }
    public string token_type { get; set; }
    public string access_token { get; set; }
    public int expires_in { get; set; }
}