using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Michiru.Commands.Slash;
using Michiru.Commands.Prefix;
using Michiru.Configuration;
using Michiru.Managers;
using Michiru.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using static System.DateTime;
using fluxpoint_sharp;

namespace Michiru;

public class Program {
    public static Program Instance { get; private set; }
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "Michiru");
    private static readonly ILogger UtilLogger = Log.ForContext("SourceContext", "Util");
    public DiscordSocketClient Client { get; set; }
    private CommandService Commands { get; set; }
    private InteractionService GlobalInteractions { get; set; }
    private InteractionService MintyLabsInteractions { get; set; }
    public SocketTextChannel? GeneralLogChannel { get; set; }
    public SocketTextChannel? ErrorLogChannel { get; set; }
    public FluxpointClient FluxpointClient { get; set; }
    private ModalProcessor _modalProcessor;

    public static async Task Main(string[] args) {
        Vars.IsWindows = Environment.OSVersion.ToString().Contains("windows", StringComparison.CurrentCultureIgnoreCase);
        Console.Title = $"{Vars.Name} v{Vars.Version} | Starting...";
        Logger.Information($"{Vars.Name} Bot is starting . . .");
        await new Program().MainAsync();
    }

    private Program() {
        Instance = this;
        Log.Logger =
            new LoggerConfiguration()
                .MinimumLevel.ControlledBy(new LoggingLevelSwitch(
                    initialMinimumLevel: LogEventLevel.Debug))
                .WriteTo.Console(new ExpressionTemplate(
                    template: "[{@t:HH:mm:ss} {@l:u3} {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1),'unset')}] {@m}\n{@x}",
                    theme: TemplateTheme.Literate))
                .WriteTo.File(Path.Combine(Environment.CurrentDirectory, "Logs", "start_.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 25,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 1024000000L)
                .CreateLogger();
    }

    private async Task MainAsync() {
        Config.Initialize();
        if (string.IsNullOrWhiteSpace(Config.Base.BotToken)) {
            Console.Title = $"{Vars.Name} | Enter your bot token";
            Console.Write("Please enter your bot token: ");
            Config.Base.BotToken = Console.ReadLine()!.Trim();
            
            if (string.IsNullOrWhiteSpace(Config.Base.BotToken)) {
                Logger.Warning("Cannot proceed without a bot token. Please enter your bot token in the Michiru.Bot.config.json file.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        if (Config.Base.BotLogsChannel.IsZero()) 
            Logger.Warning("Bot Logs Channel is not set. Please set the BotLogsChannel in the Michiru.Bot.config.json file.");
        if (Config.Base.ErrorLogsChannel.IsZero()) 
            Logger.Warning("Error Logs Channel is not set. Please set the ErrorLogsChannel in the Michiru.Bot.config.json file.");
        Config.Save();

        if (Vars.IsWindows)
            Console.Title = $"{Vars.Name} | Loading...";
        if (!Vars.IsDebug)
            MobileManager.Initialize();

        Client = new DiscordSocketClient(new DiscordSocketConfig {
            AlwaysDownloadUsers = true,
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            MessageCacheSize = 2000,
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildEmojis | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        });

        Commands = new CommandService(new CommandServiceConfig {
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            DefaultRunMode = Discord.Commands.RunMode.Async,
            CaseSensitiveCommands = false,
            ThrowOnError = true
        });

        GlobalInteractions = new InteractionService(Client, new InteractionServiceConfig {
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            DefaultRunMode = Discord.Interactions.RunMode.Async,
            ThrowOnError = true
        });
        
        MintyLabsInteractions = new InteractionService(Client, new InteractionServiceConfig {
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            DefaultRunMode = Discord.Interactions.RunMode.Async,
            ThrowOnError = true
        });

        Client.Log += msg => {
            var dnLogger = Log.ForContext("SourceContext", "DNET");
            var severity = msg.Severity switch {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => LogEventLevel.Information
            };
            dnLogger.Write(severity, msg.Exception, "[{source}] {message}", msg.Source, msg.Message);
            return Task.CompletedTask;
        };

        var argPos = 0;
        Client.MessageReceived += async arg => {
            // Don't process the command if it was a system message
            if (arg is not SocketUserMessage message)
                return;

            // Create a number to track where the prefix ends and the command begins

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!message.HasStringPrefix("-", ref argPos) || message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(Client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await Commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        };
        
        if (!string.IsNullOrWhiteSpace(Config.Base.Api.ApiKeys.FluxpointApiKey!))
            FluxpointClient = new FluxpointClient(Vars.Name, Config.Base.Api.ApiKeys.FluxpointApiKey!);

        Client.Ready += ClientOnReady;
        Client.MessageReceived += Events.BangerListener.BangerListenerEvent;
        Client.GuildUpdated += Events.GuildUpdated.OnGuildUpdated;
        Client.ModalSubmitted += async arg => await ModalProcessor.ProcessModal(arg);

        var serviceCollection = new ServiceCollection();
        _modalProcessor = new ModalProcessor();

        await Commands.AddModuleAsync<BasicCommandsThatIDoNotWantAsSlashCommands>(null);
        await Commands.AddModuleAsync<HelpCmd>(null);
        await GlobalInteractions.AddModuleAsync<Banger>(null);
        await GlobalInteractions.AddModuleAsync<Personalization>(null);
        await MintyLabsInteractions.AddModuleAsync<BotConfigControlCmds>(null);

        Client.InteractionCreated += async arg => {
            var iLogger = Log.ForContext("SourceContext", "Interaction");
            await GlobalInteractions.ExecuteCommandAsync(new SocketInteractionContext(Client, arg), null);
            await MintyLabsInteractions.ExecuteCommandAsync(new SocketInteractionContext(Client, arg), null);
            iLogger.Debug("{0} ran a command in guild {1}", arg.User.Username, arg.GuildId);
        };
        
        await Scheduler.Initialize();

        Logger.Information("Bot finished initializing, logging in to Discord...");
        await Client.LoginAsync(TokenType.Bot, Config.Base.BotToken);
        await Client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private async Task ClientOnReady() {
        var crLogger = Log.ForContext("SourceContext", "ClientReady");
        Vars.StartTime = UtcNow;
        crLogger.Information("Bot Version        = " + Vars.Version);
        crLogger.Information("Process ID         = " + Environment.ProcessId);
        crLogger.Information("Build Date         = " + Vars.BuildDate);
        crLogger.Information("Current OS         = " + (Vars.IsWindows ? "Windows" : "Linux"));
        crLogger.Information("Token              = " + Config.Base.BotToken!.Redact());
        crLogger.Information("ActivityType       = " + $"{Config.Base.ActivityType}");
        crLogger.Information("Rotating Statuses  = " + $"{Config.Base.RotatingStatus.Enabled}");
        if (Config.Base.RotatingStatus.Enabled)
            crLogger.Information("Statuses =         " + $"{string.Join(" | ", Config.Base.RotatingStatus.Statuses.Select(x => x.ActivityType + " - " + x.UserStatus + " - " + x.ActivityText).ToArray())}");
        crLogger.Information("Game               = " + $"{Config.Base.ActivityText}");
        crLogger.Information("Number of Commands = " + $"{GlobalInteractions.SlashCommands.Count + Commands.Commands.Count() + MintyLabsInteractions.SlashCommands.Count}");

        if (Vars.IsWindows) {
            var temp1 = Config.Base.ActivityText!.Equals("(insert game here)") || string.IsNullOrWhiteSpace(Config.Base.ActivityText!);
            Console.Title = $"{Vars.Name} v{Vars.Version} | Logged in as {Client.CurrentUser.Username} - " +
                            $"Currently in {Client.Guilds.Count} Guilds - {Config.GetBangerNumber()} bangers posted -" +
                            $"{Config.Base.ActivityType} {(temp1 ? "unset" : Config.Base.ActivityText)}";
        }

        var startEmbed = new EmbedBuilder {
                Color = Vars.IsDebug || Vars.IsWindows ? Colors.HexToColor("5178b5") : Colors.MichiruPink,
                Description = $"Bot has started on {(Vars.IsWindows ? "Windows" : "Linux")}\n" +
                              $"Currently in {Client.Guilds.Count} Guilds\n" +
                              $"Currently listening to {Config.GetBangerNumber()} bangers",
                Footer = new EmbedFooterBuilder {
                    Text = $"v{Vars.Version}",
                    IconUrl = Client.CurrentUser.GetAvatarUrl()
                },
                Timestamp = Now
            }
            .AddField("Build Time", $"<t:{Vars.BuildTime.ToUniversalTime().GetSecondsFromUtcUnixTime()}:F>\n<t:{Vars.BuildTime.ToUniversalTime().GetSecondsFromUtcUnixTime()}:R>")
            .AddField("Start Time", $"<t:{UtcNow.GetSecondsFromUtcUnixTime()}:F>\n<t:{UtcNow.GetSecondsFromUtcUnixTime()}:R>")
            .AddField("Discord.NET Version", Vars.DNetVer)
            .AddField("System .NET Version", Environment.Version)
            .Build();

        if (!Config.Base.ErrorLogsChannel.IsZero())
            ErrorLogChannel = GetChannel(Vars.SupportServerId, Config.Base.ErrorLogsChannel);
        if (!Config.Base.BotLogsChannel.IsZero()) {
            GeneralLogChannel = GetChannel(Vars.SupportServerId, Config.Base.BotLogsChannel);
            await GeneralLogChannel!.SendMessageAsync(embed: startEmbed);
        }

        await GlobalInteractions.RegisterCommandsGloballyAsync();
        crLogger.Information("Registered global slash commands.");
        try {
            await MintyLabsInteractions.RegisterCommandsToGuildAsync(Vars.SupportServerId);
            crLogger.Information("Registered Owner slash commands for {0} ({1}).", "Minty Labs", Vars.SupportServerId);
        }
        catch (Exception e) {
            crLogger.Error("Failed to register Owner slash commands for guild {0}\n{err}\n{st}", Vars.SupportServerId, e, e.StackTrace);
        }
    }

    public SocketTextChannel? GetChannel(ulong guildId, ulong id) {
        var guild = Client.GetGuild(guildId);
        if (guild is null) {
            UtilLogger.Error("Selected guild {guildId} does not exist!", guildId);
            return null;
        }

        if (guild.GetTextChannel(id) is { } channel) return channel;
        UtilLogger.Error("Selected channel {id} does not exist!", id);
        return null;
    }

    public SocketUser? GetUser(ulong id) {
        if (Client.GetUser(id) is { } user) return user;
        UtilLogger.Error("Selected user {id} does not exist!", id);
        return null;
    }

    public SocketGuild? GetGuild(ulong id) {
        if (Client.GetGuild(id) is { } guild) return guild;
        UtilLogger.Error("Selected guild {id} does not exist!", id);
        return null;
    }

    public SocketUser? GetGuildUser(ulong guildId, ulong userId) {
        var guild = Client.GetGuild(guildId);
        if (guild is null) {
            UtilLogger.Error("Selected guild {guildId} does not exist! <GetGuildUser>", guildId);
            return null;
        }

        if (guild.GetUser(userId) is { } user) return user;
        UtilLogger.Error("Selected user {userId} does not exist! <GetGuildUser>", userId);
        return null;
    }

    public SocketCategoryChannel? GetCategory(ulong guildId, ulong id) {
        var guild = Client.GetGuild(guildId);
        if (guild is null) {
            UtilLogger.Error("Selected guild {guildId} does not exist!", guildId);
            return null;
        }

        if (guild.GetCategoryChannel(id) is { } category) return category;
        UtilLogger.Error("Selected category {id} does not exist!", id);
        return null;
    }

    public SocketGuild? GetGuildFromChannel(ulong channelId) {
        var channel = Client.GetChannel(channelId);
        if (channel is null) {
            UtilLogger.Error("Selected channel {channelId} does not exist!", channelId);
            return null;
        }

        if (channel is SocketGuildChannel guildChannel) return guildChannel.Guild;
        UtilLogger.Error("Selected channel {channelId} is not a guild channel!", channelId);
        return null;
    }
}