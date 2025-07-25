using System.Text.Json;
using Michiru.Configuration.Music.Classes;
using Serilog;

namespace Michiru.Configuration.Music;

public class Music {
    public static Base Base { get; private set; }
    private static readonly ILogger Logger = Log.ForContext(typeof(Music));

    public static void Initialize() {
        const string file = "Michiru.Bot.MusicSubmissions.db.json";
        var hasFile = File.Exists(file);

        var data = new Base {
            Version = Vars.TargetMusicConfigVersion,
            MusicSubmissions = [
                new Submission {
                    Artists = "",
                    Title = "",
                    Services = new Services {
                        SpotifyTrackUrl = "",
                        TidalTrackUrl = "",
                        YoutubeTrackUrl = "",
                        DeezerTrackUrl = "",
                        AppleMusicTrackUrl = "",
                        PandoraTrackUrl = ""
                    },
                    SongLinkUrl = "",
                    SubmissionDate = new DateTime(),
                    LastUpdatedBy = [
                        new LastUpdatedBy {
                            Username = "",
                            LastUpdated = new DateTime()
                        }
                    ]
                }
            ],
            UsersNotAllowedToEdit = []
        };

        bool update;
        Base? baseConfig = null;
        if (hasFile) {
            var oldJson = File.ReadAllText(file);
            baseConfig = JsonSerializer.Deserialize<Base>(oldJson);
            update = baseConfig.Version == Vars.TargetMusicConfigVersion;
        }
        else {
            update = true;
        }

        var json = JsonSerializer.Serialize(baseConfig ?? data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
        Logger.Information("{0} {1}", update ? "Updated" : hasFile ? "Loaded" : "Created", file);
        Base = baseConfig ?? data;
    }

    public static bool ShouldUpdateConfigFile { get; private set; }
    public static void Save() => ShouldUpdateConfigFile = true;

    public static void SaveFile() {
        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "Michiru.Bot.MusicSubmissions.db.json"), JsonSerializer.Serialize(Base, new JsonSerializerOptions { WriteIndented = true }));
        ShouldUpdateConfigFile = false;
    }

    public static bool SearchForMatchingSubmissions(string url) =>
        Base.MusicSubmissions.Where(x =>
            x.Services.SpotifyTrackUrl == url ||
            x.Services.TidalTrackUrl == url ||
            x.Services.YoutubeTrackUrl == url ||
            x.Services.DeezerTrackUrl == url ||
            x.Services.AppleMusicTrackUrl == url ||
            x.Services.PandoraTrackUrl == url)!.Any();

    public static Submission GetSubmissionByLink(string url) =>
        Base.MusicSubmissions.FirstOrDefault(x =>
            x.Services.SpotifyTrackUrl == url ||
            x.Services.TidalTrackUrl == url ||
            x.Services.YoutubeTrackUrl == url ||
            x.Services.DeezerTrackUrl == url ||
            x.Services.AppleMusicTrackUrl == url ||
            x.Services.PandoraTrackUrl == url)!;
}