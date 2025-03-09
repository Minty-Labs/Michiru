using System.Text.Json;
using Michiru.Configuration._Base_Bot.Classes;
using Serilog;

namespace Michiru.Configuration._Base_Bot;

public static class Config {
    public static Base Base { get; private set; }
    private static readonly ILogger Logger = Log.ForContext(typeof(Config));
    private static readonly List<string> DefaultWhitelistUrls = ["open.spotify.com", "youtube.com", "www.youtube.com", "music.youtube.com", "youtu.be", "dzr.page.link", "deezer.com", "www.deezer.com", "tidal.com", "bandcamp.com", "music.apple.com", "soundcloud.com"];

    public static void Initialize() {
        const string file = "Michiru.Bot.config.json";
        var hasFile = File.Exists(file);

        var rotatingStatus = new RotatingStatus {
            Enabled = false,
            MinutesPerStatus = 2,
            Statuses = [
                new Status {
                    Id = 0,
                    ActivityText = "lots of cutes",
                    ActivityType = "Watching",
                    UserStatus = "Online"
                }
            ]
        };
        ;

        var config = new Base {
            ConfigVersion = Vars.TargetConfigVersion,
            BotToken = "",
            ActivityType = "Watching",
            ActivityText = "lots of cuties",
            UserStatus = "Online",
            RotatingStatus = rotatingStatus,
            OwnerIds = [],
            BotLogsChannel = 0,
            ErrorLogsChannel = 0,
            ExtraBangerCount = 0,
            GuildFeatures = [],
            Banger = [],
            PersonalizedMember = [new PersonalizedMember { Guilds = [] }],
            PennysGuildWatcher = new PennysGuildWatcher {
                GuildId = 977705960544014407,
                ChannelId = 989703825977905192,
                LastUpdateTime = 1207
            },
            // PennysGuildHistory = [ pennyGuildWatcher ],
            Api = new Api {
                ApiKeys = new ApiKeys {
                    UnsplashAccessKey = "",
                    UnsplashSecretKey = "",
                    CookieClientApiKey = "",
                    FluxpointApiKey = "",
                    Spotify = new SpotifyApi {
                        SpotifyClientId = "",
                        SpotifyClientSecret = ""
                    },
                    Deezer = new DeezerApi {
                        DeezerClientId = "",
                        DeezerClientSecret = ""
                    },
                    Tidal = new TidalApi {
                        TidalClientId = "",
                        TidalClientSecret = ""
                    }
                }
            },
            WakeOnLan = [
                new WakeOnLanConf {
                    DeviceIdentifier = "NULL",
                    PortNumber = 0,
                    MacAddress = "00:00:00:00:00:00",
                    IpAddress = "192.168.1.0"
                }
            ]
        };

        bool update;
        Base? baseConfig = null;
        if (hasFile) {
            var oldJson = File.ReadAllText(file);
            baseConfig = JsonSerializer.Deserialize<Base>(oldJson);
            if (baseConfig?.ConfigVersion == Vars.TargetConfigVersion) {
                Base = baseConfig;
                update = false;
            }
            else {
                update = true;
                baseConfig!.ConfigVersion = Vars.TargetConfigVersion;
            }
        }
        else {
            update = true;
        }

        var json = JsonSerializer.Serialize(baseConfig ?? config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
        Logger.Information("{0} {1}", update ? "Updated" : hasFile ? "Loaded" : "Created", file);
        Base = baseConfig ?? config;
    }

    public static bool ShouldUpdateConfigFile { get; private set; }
    public static void Save() => ShouldUpdateConfigFile = true;

    public static void SaveFile() {
        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "Michiru.Bot.config.json"), JsonSerializer.Serialize(Base, new JsonSerializerOptions { WriteIndented = true }));
        ShouldUpdateConfigFile = false;
    }

    public static Banger GetGuildBanger(ulong id) {
        var banger = Base.Banger!.FirstOrDefault(b => b.GuildId == id);
        if (banger is not null) return banger;
        banger = new Banger { GuildId = id };
        Base.Banger!.Add(banger);
        Save();
        return banger;
    }

    public static PmGuildData GetGuildPersonalizedMember(ulong id) {
        var pm = Base.PersonalizedMember!.FirstOrDefault(p => p.Guilds!.Any(g => g.GuildId == id));
        if (pm is not null) return pm.Guilds!.First(g => g.GuildId == id);
        pm = new PersonalizedMember { Guilds = [new PmGuildData { GuildId = id }] };
        Base.PersonalizedMember!.Add(pm);
        Save();
        return pm.Guilds!.First();
    }

    /// <summary>
    /// Gets the sum of all submitted bangers from all guilds
    /// </summary>
    /// <returns></returns>
    public static int GetBangerNumber() => Base.Banger.Sum(guild => guild.SubmittedBangers) + Base.ExtraBangerCount;

    /// <summary>
    /// Gets the sum of all personalized members from all guilds
    /// </summary>
    /// <returns></returns>
    public static int GetPersonalizedMemberCount() => Base.PersonalizedMember.SelectMany(member => member.Guilds!).Sum(guild => guild.Members!.Count);

    public static void FixBangerNulls() {
        foreach (var banger in Base.Banger!) {
            banger.WhitelistedUrls ??= DefaultWhitelistUrls;
            banger.WhitelistedFileExtensions ??= ["mp3", "flac", "wav", "ogg", "m4a", "alac", "aac", "aiff", "wma"];
            banger.UrlErrorResponseMessage ??= "This URL is not whitelisted.";
            banger.FileErrorResponseMessage ??= "This file type is not whitelisted.";
        }

        SaveFile();
    }

    // Get guild from guild features
    public static GuildFeatures GetGuildFeature(ulong id) {
        // if guild by id doesn't exist, create it
        if (Base.GuildFeatures.Any(x => x.GuildId == id))
            return Base.GuildFeatures.First(x => x.GuildId == id);

        var guild = new GuildFeatures { GuildId = id };
        Base.GuildFeatures!.Add(guild);
        SaveFile();
        return guild;
    }
}