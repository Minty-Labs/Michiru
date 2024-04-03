/*using Discord;
using Discord.Interactions;
using Discord.Rest;
using Michiru.Configuration;
using Michiru.Configuration.Classes;

namespace Michiru.Commands.Slash;

public struct GiveAwayInfo {
    public GiveAway GiveAway;
    public Entry GiveAwayEntry;
    public int EntryIndex;
    public RestUserMessage Message;
    public RestUserMessage WatcherMessage;
    public Embed WatcherEmbed;
}

public class GiveAwayStorage {
    public List<GiveAwayInfo> ActiveGiveAways = [];
}

public class GiveAwayCmds : InteractionModuleBase<SocketInteractionContext> {
    [SlashCommand("giveaway", "Starts a giveaway"), RequireUserPermission(GuildPermission.Administrator)]
    public async Task GiveAway([Summary(description:"The Prize")] string prize,
        [Summary(description:"A brief or detailed description")] string description,
        [Summary(description:"How many winners?")] int winnerCount,
        [Summary(description:"How long will this go one for? (in seconds)")] int duration) {
        // generate a new discord message
        var mainGiveAwayMessageBuilder = new ComponentBuilder().WithButton("Join", "join", ButtonStyle.Primary, emote: new Emoji("🎉"));
        var message = await Context.Channel.SendMessageAsync("Building, please wait...");
        var guildId = Context.Guild.Id;
        var channelId = Context.Channel.Id;
        var messageId = message.Id;
        var watcherGiveAwayInfoMessageBuilder = new ComponentBuilder()
            .WithButton("End Early", "end", ButtonStyle.Secondary, emote: new Emoji("🚫"))
            .WithButton("Force Delete", "forceDelete", ButtonStyle.Danger, emote: new Emoji("⛔"));;
        var watcherChannel = Program.Instance.GetChannel(guildId, Config.GetGuildGiveAway(guildId).WatchChannelId);
        var watcherMessage = await watcherChannel!.SendMessageAsync("Building, please wait...");
        var currentEntryCountPlusOne = Config.Base.GiveAways.FirstOrDefault(x => x.GuildId == guildId)!.Entries.Count + 1;
        var entry = new Entry {
            IsActive = true,
            EntryId = currentEntryCountPlusOne,
            Prize = prize,
            Description = description,
            WinnerCount = winnerCount,
            Duration = duration,
            MessageId = messageId,
            ChannelId = channelId,
            WatchMessageId = watcherMessage.Id,
            Participants = []
        };
        Config.Base.GiveAways.FirstOrDefault(x => x.GuildId == guildId)!.Entries.Add(entry);
        Config.Save();
        
        // build embed for main giveaway message
        var mainGiveAwayMessageEmbed = new EmbedBuilder()
            .WithAuthor("New Giveaway!")
            .WithTitle(prize)
            .WithDescription(description)
            .WithColor(Color.Blue)
            .WithFooter("Ends at")
            .WithTimestamp(DateTimeOffset.UtcNow.AddSeconds(duration))
            .Build();
        await message.ModifyAsync(x => {
            x.Embeds = new[] { mainGiveAwayMessageEmbed };
            x.Components = mainGiveAwayMessageBuilder.Build();
        });
        
        // build embed for watcher giveaway message
        var watcherGiveAwayInfoMessageEmbed = new EmbedBuilder()
            .WithTitle(prize)
            .WithDescription(description)
            .WithColor(Color.Blue)
            .WithFooter($"[{currentEntryCountPlusOne}] Ends at")
            .WithTimestamp(DateTimeOffset.UtcNow.AddSeconds(duration))
            .Build();
        await watcherMessage.ModifyAsync(x => {
            x.Embeds = new[] { watcherGiveAwayInfoMessageEmbed };
            x.Components = watcherGiveAwayInfoMessageBuilder.Build();
        });
        
        // store the giveaway info
        var giveAwayInfo = new GiveAwayInfo {
            GiveAway = Config.GetGuildGiveAway(guildId),
            GiveAwayEntry = entry,
            EntryIndex = currentEntryCountPlusOne,
            Message = message,
            WatcherMessage = watcherMessage,
            WatcherEmbed = watcherGiveAwayInfoMessageEmbed
        };
        
        await RespondAsync("Giveaway started", ephemeral: true);
    }
}*/