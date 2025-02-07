using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Michiru.Commands.Prefix;
using Michiru.Commands.Slash;
using Michiru.Managers;
using Michiru.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using static System.DateTime;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration.Moderation;
using Michiru.Events;
using CommandService = Discord.Commands.CommandService;

namespace Michiru;

public class Program {
    public static Program Instance { get; private set; }
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "Michiru");
    private static readonly ILogger UtilLogger = Log.ForContext("SourceContext", "Util");
    
    public DiscordSocketClient Client { get; private set; }
    private IServiceProvider _services { get; set; }
    private CommandService Commands { get; set; }
    private InteractionService GlobalInteractions { get; set; }
    private InteractionService MintyLabsInteractions { get; set; }
    public SocketTextChannel? GeneralLogChannel { get; private set; }
    public SocketTextChannel? ErrorLogChannel { get; private set; }
    private ModalProcessor _modalProcessor { get; set; }

    public static async Task Main(string[] args) {
        Vars.IsWindows = Environment.OSVersion.ToString().Contains("windows", StringComparison.CurrentCultureIgnoreCase);
        Console.Title = $"{Vars.Name} v{Vars.VersionStr} | Starting...";
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
                    template: "[{@t:HH:mm:ss} {@l:u3} {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1),'Discord Bot')}] {@m}\n{@x}",
                    theme: TemplateTheme.Literate))
                .WriteTo.File(Path.Combine(Environment.CurrentDirectory, "Logs", "start_.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 25,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 1024000000L)
                .CreateLogger();
    }

    /*private IServiceProvider CreateProvider() {
        var config = new DiscordSocketClient(new DiscordSocketConfig {
            AlwaysDownloadUsers = true,
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            MessageCacheSize = 1500,
            GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers) & ~GatewayIntents.GuildPresences & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites
        });

        var collection = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<DiscordSocketClient>()
            //.AddSingleton(GlobalInteractions)
            //.AddSingleton(MintyLabsInteractions);
            ;
        return collection.BuildServiceProvider();
    }*/

    private async Task MainAsync() {
        Config.Initialize();
        try {
            ModData.Initialize();
        }
        catch { /**/ }

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

        // _services = CreateProvider();
        // Client = _services.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient;
        // Client = _services.GetRequiredService<DiscordSocketClient>();
        
        Client = new DiscordSocketClient(new DiscordSocketConfig {
            AlwaysDownloadUsers = true,
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            MessageCacheSize = 1500,
            GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers) & ~GatewayIntents.GuildPresences & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites
        });

        Commands = new CommandService(new CommandServiceConfig {
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            DefaultRunMode = Discord.Commands.RunMode.Async,
            CaseSensitiveCommands = false,
            ThrowOnError = true
        });

        GlobalInteractions = new InteractionService(Client, new InteractionServiceConfig {
            UseCompiledLambda = true,
            LogLevel = Vars.IsWindows ? LogSeverity.Verbose : LogSeverity.Debug, //Info,
            DefaultRunMode = Discord.Interactions.RunMode.Async,
            ThrowOnError = true
        });

        MintyLabsInteractions = new InteractionService(Client, new InteractionServiceConfig {
            UseCompiledLambda = true,
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

        Client.Ready += ClientOnReady;
        Client.MessageReceived += BangerListener.BangerListenerEvent;
        // Client.ButtonExecuted += BangerListener.SpotifyToYouTubeSongLookupButtons;
        Client.GuildUpdated += GuildUpdated.OnGuildUpdated;
        Client.UserJoined += MemberUpdated.MemberJoin; // I'm just glad
        Client.UserLeft += MemberUpdated.MemberLeave; // this finally works
        Client.ModalSubmitted += async arg => await ModalProcessor.ProcessModal(arg);
        Client.InteractionCreated += async arg => {
            var interactionLogger = Log.ForContext("SourceContext", "Interaction");
            try {
                await GlobalInteractions.ExecuteCommandAsync(new SocketInteractionContext(Client, arg), null);
                interactionLogger.Debug("{0} ran a command in guild {1}", arg.User.Username, arg.GuildId);
            }
            catch {
                if (arg.Type == InteractionType.ApplicationCommand) {
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }

            try {
                await MintyLabsInteractions.ExecuteCommandAsync(new SocketInteractionContext(Client, arg), null);
            }
            catch {
                if (arg.Type == InteractionType.ApplicationCommand) {
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
        };
        
        var serviceCollection = new ServiceCollection();
        
        await Commands.AddModuleAsync<BasicCommandsThatIDoNotWantAsSlashCommands>(null);
        await Commands.AddModuleAsync<HelpCmd>(null);
        await Commands.AddModuleAsync<WakeOnLanCmds>(null);
        await Commands.AddModuleAsync<DAndD>(null);
        await Commands.AddModuleAsync<Moderation>(null);

        await GlobalInteractions.AddModuleAsync<Banger>(null);
        await GlobalInteractions.AddModuleAsync<Personalization>(null);
        await GlobalInteractions.AddModuleAsync<ServerInfo>(null);
        // await GlobalInteractions.AddModuleAsync<MessageFindBanger>(null);
        // await GlobalInteractions.AddModuleAsync<LookupSpotifyForYouTube>(null);
        await GlobalInteractions.AddModuleAsync<ServerMemberUpdated>(null);
        
        await MintyLabsInteractions.AddModuleAsync<BotConfigControlCmds>(null);

        // var asm = Assembly.GetEntryAssembly();
        // await GlobalInteractions.AddModulesAsync(asm, null);

        _modalProcessor = new ModalProcessor();

        Logger.Information("Bot finished initializing, logging in to Discord...");
        await Client.LoginAsync(TokenType.Bot, Config.Base.BotToken);
        await Client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private async Task ClientOnReady() {
        var crLogger = Log.ForContext("SourceContext", "ClientReady");
        Vars.StartTime = UtcNow;
        var conf = Config.Base;
        crLogger.Information("Bot Version        = " + Vars.VersionStr);
        crLogger.Information("Process ID         = " + Environment.ProcessId);
        // crLogger.Information("Build Date         = " + Vars.BuildDate);
        crLogger.Information("Current OS         = " + (Vars.IsWindows ? "Windows" : "Linux"));
        crLogger.Information("Token              = " + conf.BotToken!.Redact());
        crLogger.Information("ActivityType       = " + $"{conf.ActivityType}");
        crLogger.Information("Rotating Statuses  = " + $"{conf.RotatingStatus.Enabled}");
        if (conf.RotatingStatus.Enabled)
            crLogger.Information("Statuses =         " + $"{string.Join(" | ", conf.RotatingStatus.Statuses.Select(x => x.ActivityType + " - " + x.UserStatus + " - " + x.ActivityText).ToArray())}");
        crLogger.Information("Game               = " + $"{conf.ActivityText}");
        crLogger.Information("Number of Commands = " + $"{GlobalInteractions.SlashCommands.Count + Commands.Commands.Count() + MintyLabsInteractions.SlashCommands.Count}");

        if (Vars.IsWindows) {
            var temp1 = conf.ActivityText!.Equals("(insert game here)") || string.IsNullOrWhiteSpace(conf.ActivityText!);
            Console.Title = $"{Vars.Name} v{Vars.VersionStr} | Logged in as {Client.CurrentUser.Username} - " +
                            $"Currently in {Client.Guilds.Count} Guilds - {Config.GetBangerNumber()} bangers posted - " +
                            $"Managing {Config.GetPersonalizedMemberCount()} personal roles - " +
                            $"{conf.ActivityType} {(temp1 ? "unset" : conf.ActivityText)}";
        }

        var startEmbed = new EmbedBuilder {
                Color = Vars.IsDebug || Vars.IsWindows ? Colors.HexToColor("5178b5") : Colors.MichiruPink,
                Footer = new EmbedFooterBuilder {
                    Text = $"v{Vars.VersionStr}",
                    IconUrl = Client.CurrentUser.GetAvatarUrl()
                },
                Timestamp = Now
            }
            .AddField("OS", Vars.IsWindows ? "Windows" : "Linux", true)
            .AddField("Guilds", $"{Client.Guilds.Count}", true)
            .AddField("Bangers", $"{Config.GetBangerNumber()}", true)
            // .AddField("Build Time", $"{Vars.BuildTime.ToUniversalTime().ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}\n{Vars.BuildTime.ToUniversalTime().ConvertToDiscordTimestamp(TimestampFormat.RelativeTime)}")
            .AddField("Start Time", $"{UtcNow.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}\n{UtcNow.ConvertToDiscordTimestamp(TimestampFormat.RelativeTime)}")
            .AddField("Target .NET Version", Vars.DotNetTargetVersion, true)
            .AddField("System .NET Version", Environment.Version, true)
            .AddField("Discord.NET Version", Vars.DNetVer, true)
            .Build();

        if (!conf.ErrorLogsChannel.IsZero())
            ErrorLogChannel = Client.GetChannel(conf.ErrorLogsChannel) as SocketTextChannel; // GetChannel(Vars.SupportServerId, conf.ErrorLogsChannel);
        
        if (!conf.BotLogsChannel.IsZero()) {
            GeneralLogChannel = Client.GetChannel(conf.BotLogsChannel) as SocketTextChannel; // GetChannel(Vars.SupportServerId, conf.BotLogsChannel);
            await GeneralLogChannel!.SendMessageAsync(embed: startEmbed);
        }

        await RegisterCommands();
        
        await Scheduler.Initialize();
        Config.FixBangerNulls();
    }

    private async Task RegisterCommands() {
        var interactionLogger = Log.ForContext("SourceContext", "Interaction");
        await GlobalInteractions.RegisterCommandsGloballyAsync();
        interactionLogger.Information("Registered global slash commands.");
        
        try {
            await MintyLabsInteractions.RegisterCommandsToGuildAsync(Vars.SupportServerId);
            interactionLogger.Information("Registered Owner slash commands for {0} ({1}).", "Minty Labs", Vars.SupportServerId);
        }
        catch (Exception e) {
            interactionLogger.Error("Failed to register Owner slash commands for guild {0}\n{err}\n{st}", Vars.SupportServerId, e, e.StackTrace);
        }
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