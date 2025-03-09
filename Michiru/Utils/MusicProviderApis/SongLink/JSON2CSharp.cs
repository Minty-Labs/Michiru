namespace Michiru.Utils.MusicProviderApis.SongLink;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class AmazonMusic {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class AmazonSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class AmazonStore {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class Anghami {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class AnghamiSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class AppleMusic {
    public string country { get; set; }
    public string url { get; set; }
    public string nativeAppUriMobile { get; set; }
    public string nativeAppUriDesktop { get; set; }
    public string entityUniqueId { get; set; }
}

public class Audiomack {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class AudiomackSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Boomplay {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class BoomplaySong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Deezer {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class DeezerSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class EntitiesByUniqueId {
    public AmazonSong AMAZON_SONG { get; set; }

    public AudiomackSong AUDIOMACK_SONG { get; set; }

    public AnghamiSong ANGHAMI_SONG { get; set; }

    public BoomplaySong BOOMPLAY_SONG { get; set; }

    public DeezerSong DEEZER_SONG { get; set; }

    public ItunesSong ITUNES_SONG { get; set; }

    public NapsterSong NAPSTER_SONG { get; set; }

    public PandoraSong PANDORA_SONG { get; set; }

    public SoundcloudSong SOUNDCLOUD_SONG { get; set; }

    public SpotifySong SPOTIFY_SONG { get; set; }

    public TidalSong TIDAL_SONG { get; set; }

    public YandexSong YANDEX_SONG { get; set; }

    public YouTubeVideo YOUTUBE_VIDEO { get; set; }
}

public class Itunes {
    public string country { get; set; }
    public string url { get; set; }
    public string nativeAppUriMobile { get; set; }
    public string nativeAppUriDesktop { get; set; }
    public string entityUniqueId { get; set; }
}

public class ItunesSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class LinksByPlatform {
    public AmazonMusic amazonMusic { get; set; }
    public AmazonStore amazonStore { get; set; }
    public Audiomack audiomack { get; set; }
    public Anghami anghami { get; set; }
    public Boomplay boomplay { get; set; }
    public Deezer deezer { get; set; }
    public AppleMusic appleMusic { get; set; }
    public Itunes itunes { get; set; }
    public Napster napster { get; set; }
    public Pandora pandora { get; set; }
    public Soundcloud soundcloud { get; set; }
    public Tidal tidal { get; set; }
    public Yandex yandex { get; set; }
    public Youtube youtube { get; set; }
    public YoutubeMusic youtubeMusic { get; set; }
    public Spotify spotify { get; set; }
}

public class Napster {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class NapsterSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Pandora {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class PandoraSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Root {
    public string entityUniqueId { get; set; }
    public string userCountry { get; set; }
    public string pageUrl { get; set; }
    public EntitiesByUniqueId entitiesByUniqueId { get; set; }
    public LinksByPlatform linksByPlatform { get; set; }
}

public class Soundcloud {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class SoundcloudSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Spotify {
    public string country { get; set; }
    public string url { get; set; }
    public string nativeAppUriDesktop { get; set; }
    public string entityUniqueId { get; set; }
}

public class SpotifySong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Tidal {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class TidalSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Yandex {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class YandexSong {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}

public class Youtube {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class YoutubeMusic {
    public string country { get; set; }
    public string url { get; set; }
    public string entityUniqueId { get; set; }
}

public class YouTubeVideo {
    public string id { get; set; }
    public string type { get; set; }
    public string title { get; set; }
    public string artistName { get; set; }
    public string thumbnailUrl { get; set; }
    public int thumbnailWidth { get; set; }
    public int thumbnailHeight { get; set; }
    public string apiProvider { get; set; }
    public List<string> platforms { get; set; }
}