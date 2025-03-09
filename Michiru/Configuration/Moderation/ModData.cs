using System.Text;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration.Moderation.Classes;
using Serilog;
using System.Text.Json;
using Discord;
using Michiru.Utils;

namespace Michiru.Configuration.Moderation;

public class ModData {
    public static Base Base { get; private set; }
    private static readonly ILogger Logger = Log.ForContext(typeof(Config));
    public static int CurrentId = 0;

    internal static Dictionary<ulong, Classes.Moderation> GuildModerationData { get; set; } // copy User Moderation data to hold in memory

    public static void Initialize() {
        const string file = "Michiru.Bot.ModerationData.db.json";
        var hasFile = File.Exists(file);

        var data = new Base {
            Version = Vars.TargetModConfigVersion,
            Guilds = [
                new Guild {
                    Id = 0,
                    Channels = new Channel {
                        BanOrUnban = 0,
                        FreezeSlowmodeMasspurge = 0,
                        KickWarnTimeoutMute = 0
                    },
                    IsCrossbanEnabled = false,
                    IsScamBanEnabled = false,
                    IsChannelFreezeEnabled = false
                }
            ],
            Users = [
                new User {
                    Id = 0,
                    Username = "",
                    Moderation = [
                        new Classes.Moderation {
                            Id = 0,
                            Type = "null",
                            IsPardoned = false,
                            UserId = 0,
                            GuildId = 0,
                            Reason = "",
                            DateTimeGiven = new DateTime()
                        }
                    ]
                }
            ]
        };

        bool update;
        Base? baseConfig = null;
        if (hasFile) {
            var oldJson = File.ReadAllText(file);
            baseConfig = JsonSerializer.Deserialize<Base>(oldJson);
            if (baseConfig?.Version == Vars.TargetModConfigVersion) {
                Base = baseConfig;
                update = false;
            }
            else {
                update = true;
                baseConfig!.Version = Vars.TargetModConfigVersion;
            }
        }
        else {
            update = true;
        }

        var json = JsonSerializer.Serialize(baseConfig ?? data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
        Logger.Information("{0} {1}", update ? "Updated" : hasFile ? "Loaded" : "Created", file);
        Base = baseConfig ?? data;

        foreach (var mod in Base.Users.SelectMany(user => user.Moderation)) {
            GuildModerationData.Add(mod.GuildId, mod);
        }

        SetCurrentId();
    }

    public static void Save() => File.WriteAllText("Michiru.Bot.ModerationData.db.json", JsonSerializer.Serialize(Base, new JsonSerializerOptions { WriteIndented = true }));

    private static void SetCurrentId() {
        var ids = Base.Users.SelectMany(u => u.Moderation).Select(m => m.Id);
        var enumerable = ids as int[] ?? ids.ToArray();
        CurrentId = enumerable.Any() ? enumerable.Max() : 0;
    }

    public static async Task<string> PrintoutUserModeration(ulong userId, ulong fromGuildId) {
        var sb = new StringBuilder();
        var user = Base.Users.FirstOrDefault(u => u.Id == userId);

        if (user is null)
            return "No moderation data found for this user.";

        sb.AppendLine($"Moderation data for user {userId} in guild {fromGuildId}");

        foreach (var mod in user.Moderation.Where(m => m.GuildId == fromGuildId)) {
            sb.AppendLine($"Moderation ID: {mod.Id}");
            sb.AppendLine($"Type: {mod.Type}");
            sb.AppendLine($"Is Pardoned: {mod.IsPardoned}");
            sb.AppendLine($"User ID: {mod.UserId}");
            sb.AppendLine($"Guild ID: {mod.GuildId}");
            sb.AppendLine($"Reason: {mod.Reason}");
            sb.AppendLine($"Date/Time Given: {mod.DateTimeGiven}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine();
        }

        var isLarge = sb.Length > 2000 - 6; // minus 6 for the code block markdown

        if (!isLarge)
            return MarkdownUtils.ToCodeBlockMultiline(sb.ToString());

        using var ms = new MemoryStream();
        await using var sw = new StreamWriter(ms);
        await sw.WriteAsync(sb.ToString());
        await sw.FlushAsync();
        ms.Seek(0, SeekOrigin.Begin);
        LargeUserPrintout.Add(userId, DiscordFileAttachment(ms, $"{userId}-{fromGuildId}.txt", "This printout was too large to send in a single message. The file is attached below."));
        return MarkdownUtils.ToCodeBlockMultiline(sb.ToString());
    }

    internal static Dictionary<ulong, FileAttachment> LargeUserPrintout = new();

    private static FileAttachment DiscordFileAttachment(MemoryStream memoryStream, string filename = "UserPrintout.txt", string addedText = "") => new(memoryStream, filename, addedText);

    public static async Task<IGuildChannel?> GetBanOrUnbanLogChannel(IGuild guild) {
        var channel = Base.Guilds.FirstOrDefault(g => g.Id == guild.Id)?.Channels.BanOrUnban;
        return channel is not null ? await guild.GetChannelAsync(channel.Value) : null;
    }

    public static async Task<IGuildChannel?> GetFreezeSlowmodeMasspurgeLogChannel(IGuild guild) {
        var channel = Base.Guilds.FirstOrDefault(g => g.Id == guild.Id)?.Channels.FreezeSlowmodeMasspurge;
        return channel is not null ? await guild.GetChannelAsync(channel.Value) : null;
    }

    public static async Task<IGuildChannel?> GetKickWarnTimeoutMuteLogChannel(IGuild guild) {
        var channel = Base.Guilds.FirstOrDefault(g => g.Id == guild.Id)?.Channels.KickWarnTimeoutMute;
        return channel is not null ? await guild.GetChannelAsync(channel.Value) : null;
    }
}