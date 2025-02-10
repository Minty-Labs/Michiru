using Discord.Interactions;
using System.Text;
using Discord;
using Michiru.Commands.Preexecution;
using Michiru.Configuration._Base_Bot;
using Michiru.Events;
using Michiru.Managers;
using Michiru.Utils;
using Serilog;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Michiru.Commands.Slash;

public class Banger : InteractionModuleBase<SocketInteractionContext> {
    [Group("banger", "Banger Commands"),
     RequireUserPermission(GuildPermission.SendMessages),
     IntegrationType(ApplicationIntegrationType.GuildInstall),
     CommandContextType(InteractionContextType.Guild)]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {
        [SlashCommand("lookup", "YT top result of music service URL"), RateLimit(30, 5)]
        public async Task LookupFromAllMusicStreamingServices(
            [Summary("share-link", "Media Share URL")] string mediaUrl = "",
            [Summary("lookup-text", "Uses YouTube to look up the song by your input")] string lookupText = "",
            [Summary("extra-text", "Share your experience for this song")] string extraText = "") {
            
            if (string.IsNullOrWhiteSpace(mediaUrl) && string.IsNullOrWhiteSpace(lookupText) && string.IsNullOrWhiteSpace(extraText)) {
                await RespondAsync("Command variables must not be empty.", ephemeral: true);
                return;
            }
            
            var conf = Config.GetGuildBanger(Context.Guild.Id);
            IUserMessage? socketUserMessage = null;
            var alreadyAddedBanger = false;
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(extraText))
                sb.AppendLine(MarkdownUtils.ToItalics(extraText) + "\n");

            sb.AppendLine("Top Result");
            
            if (string.IsNullOrWhiteSpace(mediaUrl) && !string.IsNullOrWhiteSpace(lookupText)) {
                await DeferAsync();
                var textLookupYTClient = new YoutubeClient();
                var videos = textLookupYTClient.Search.GetVideosAsync(lookupText).GetAwaiter().GetResult();
                var firstEntry = videos[0];
                var title = firstEntry.Title;
                var author = firstEntry.Author.ChannelTitle.Replace(" - Topic", "");
                var subtext = new StringBuilder();

                var spotifyTrackUrl = string.Empty;
                try {
                    spotifyTrackUrl = await Utils.MusicProviderApis.Spotify.GetSearchResults.SearchForUrl($"{author} {title}");
                }
                catch (Exception ex) {
                    Log.ForContext("SourceContext", "COMMAND:SpotifyLookup").Error("Failed to get track data from Spotify API");
                    await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Spotify API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                    await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Spotify API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                        .DeleteAfter(5, "Failed to get track data from Spotify API");
                    return; // fail command out right
                }
                
                var tidalTrackUrl = string.Empty;
                try {
                    tidalTrackUrl = await Utils.MusicProviderApis.Tidal.GetSearchResults.SearchForUrl($"{author} {title}");
                }
                catch (Exception ex) {
                    Log.ForContext("SourceContext", "COMMAND:TidalLookup").Error("Failed to get track data from Tidal API");
                    await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Tidal API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                    await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Tidal API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                        .DeleteAfter(5, "Failed to get track data from Tidal API");
                    return; // fail command out right
                }
                
                var youtubeTrackUrl = firstEntry.Url;
                var isAuthorRelease = author.Equals("Release", StringComparison.OrdinalIgnoreCase);
                if (isAuthorRelease && !string.IsNullOrWhiteSpace(author))
                    author = title;
                
                sb.AppendLine(title.Contains(author) ? MarkdownUtils.ToBold(title) : MarkdownUtils.ToBold($"{author} - {title}"));
                
                subtext.Append(MarkdownUtils.MakeLink("Original YouTube Link \u2197", youtubeTrackUrl) + " \u2219 ");
                if (!string.IsNullOrWhiteSpace(spotifyTrackUrl))
                    if (spotifyTrackUrl != "[s404] ZERO RESULTS")
                        subtext.Append(MarkdownUtils.MakeLink("Spotify Link \u2197", spotifyTrackUrl, true) + " \u2219 ");
                if (!string.IsNullOrWhiteSpace(tidalTrackUrl))
                    if (tidalTrackUrl != "[t404] ZERO RESULTS")
                        subtext.Append(MarkdownUtils.MakeLink("Tidal Link \u2197", tidalTrackUrl, true));
                
                sb.AppendLine(MarkdownUtils.ToSubText(subtext.ToString()));
                
                socketUserMessage = await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                conf!.SubmittedBangers++;
                Config.Save();
                alreadyAddedBanger = true;
                goto afterCommandAction;
            }
            
            var sluLogger = Log.ForContext("SourceContext", "COMMAND:SpotifyLookup");
            var tluLogger = Log.ForContext("SourceContext", "COMMAND:TidalLookup");
            var dluLogger = Log.ForContext("SourceContext", "COMMAND:DeezerLookup");
            var yluLogger = Log.ForContext("SourceContext", "COMMAND:YouTubeLookup");
            if (!mediaUrl.Contains("http")) {
                await RespondAsync("No URL found in message", ephemeral: true);
                return;
            }

            await DeferAsync();

            string? theActualUrl = null;
            var yt = new YoutubeClient();

            theActualUrl ??= mediaUrl;
            var isUrlGood = theActualUrl.AndContainsMultiple("spotify.com", "track") || theActualUrl.AndContainsMultiple("tidal.com", "track");

            // check if url is whitelisted
            if (isUrlGood) {
                if (theActualUrl.AndContainsMultiple("spotify.com", "track")) {
                    sluLogger.Information("Found URL to be a Spotify Track");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?'))
                        finalId = theActualUrl.Split('?')[0];

                    Utils.MusicProviderApis.Spotify.Root? spotifyTrackUrl;
                    try {
                        spotifyTrackUrl = await Utils.MusicProviderApis.Spotify.GetTrackResults.GetTrackData(finalId.Split('/').Last());
                    }
                    catch (Exception ex) {
                        sluLogger.Error("Failed to get track data from Spotify API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Spotify API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Spotify API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Spotify API");
                        return; // fail command out right
                    }

                    var videos = yt.Search.GetVideosAsync($"{spotifyTrackUrl!.artists[0].name} {spotifyTrackUrl.name}").GetAwaiter().GetResult();

                    var firstEntry = videos[0];
                    var title = firstEntry.Title;
                    var author = firstEntry.Author.ChannelTitle.Replace(" - Topic", "");
                    var isAuthorRelease = author.Equals("Release", StringComparison.OrdinalIgnoreCase);
                    if (isAuthorRelease && !string.IsNullOrWhiteSpace(spotifyTrackUrl.artists[0].name))
                        author = spotifyTrackUrl.artists[0].name;

                    var tidalTrackUrl = string.Empty;
                    try {
                        tidalTrackUrl = await Utils.MusicProviderApis.Tidal.GetSearchResults.SearchForUrl($"{spotifyTrackUrl.artists[0].name} {spotifyTrackUrl.name}");
                    }
                    catch { /*silent fail*/ }

                    sb.AppendLine(title.Contains(author) ? MarkdownUtils.ToBold(title) : MarkdownUtils.ToBold($"{author} - {title}"));

                    var subtext = new StringBuilder();
                    subtext.Append(MarkdownUtils.MakeLink("Original Spotify Link \u2197", theActualUrl, true) + " \u2219 ");
                    subtext.Append(MarkdownUtils.MakeLink("YouTube Link \u2197", firstEntry.Url) + " \u2219 "); // will show YouTube embed
                    if (!string.IsNullOrWhiteSpace(tidalTrackUrl))
                        if (tidalTrackUrl != "[t404] ZERO RESULTS")
                            subtext.Append(MarkdownUtils.MakeLink("Tidal Link \u2197", tidalTrackUrl, true) /* + " \u2219 " */);
                    // subtext.Append(MarkdownUtils.MakeLink("Deezer Link \u2197", "https://deezer.page.link/" + "", true));
                    
                    sb.AppendLine(MarkdownUtils.ToSubText(subtext.ToString()));

                    socketUserMessage = await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                    conf!.SubmittedBangers++;
                    Config.Save();
                    alreadyAddedBanger = true;
                }
                else if (theActualUrl.AndContainsMultiple("tidal.com", "track")) {
                    tluLogger.Information("Found URL to be a Tidal Track");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?'))
                        finalId = theActualUrl.Split('?')[0];

                    Utils.MusicProviderApis.Tidal.Root? tidalTrack;
                    try {
                        tidalTrack = await Utils.MusicProviderApis.Tidal.GetTrackResults.GetData(finalId.Split('/').Last());
                    }
                    catch (Exception ex) {
                        tluLogger.Error("Failed to get track data from Tidal API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Tidal API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Tidal API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Tidal API");
                        return; // fail command out right
                    }

                    var videos = yt.Search.GetVideosAsync($"{tidalTrack!.resource.artists[0]} {tidalTrack!.resource.title}").GetAwaiter().GetResult();

                    var firstEntry = videos[0];
                    var title = firstEntry.Title;
                    var author = firstEntry.Author.ChannelTitle.Replace(" - Topic", "");

                    var spotifyTrackUrl = string.Empty;
                    try {
                        spotifyTrackUrl = await Utils.MusicProviderApis.Spotify.GetSearchResults.SearchForUrl($"{tidalTrack.resource.artists[0]} {tidalTrack.resource.title}");
                    }
                    catch (Exception ex) {
                        sluLogger.Error("Failed to get track data from Spotify API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Spotify API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Spotify API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Spotify API");
                        return; // fail command out right
                    }
                    
                    var spotifyData = await Utils.MusicProviderApis.Spotify.GetTrackResults.GetTrackData(spotifyTrackUrl!.Split('/').Last());
                    var isAuthorRelease = author.Equals("Release", StringComparison.OrdinalIgnoreCase);
                    if (isAuthorRelease && !string.IsNullOrWhiteSpace(spotifyData!.artists[0].name))
                        author = spotifyData.artists[0].name;

                    sb.AppendLine(title.Contains(author) ? MarkdownUtils.ToBold(title) : MarkdownUtils.ToBold($"{author} - {title}"));
                    
                    var subtext = new StringBuilder();
                    subtext.Append(MarkdownUtils.MakeLink("Original Tidal Link \u2197", theActualUrl, true) + " \u2219 ");
                    subtext.Append(MarkdownUtils.MakeLink("YouTube Link \u2197", firstEntry.Url) + " \u2219 "); // will show YouTube embed
                    if (!string.IsNullOrWhiteSpace(spotifyTrackUrl))
                        if (spotifyTrackUrl != "[s404] ZERO RESULTS")
                            subtext.Append(MarkdownUtils.MakeLink("Spotify Link \u2197", spotifyTrackUrl, true) /* + " \u2219 " */);
                    // subtext.Append(MarkdownUtils.MakeLink("Deezer Link \u2197", "https://deezer.page.link/" + "", true));
                    
                    sb.AppendLine(subtext.ToString());

                    socketUserMessage = await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                    conf!.SubmittedBangers++;
                    Config.Save();
                    alreadyAddedBanger = true;
                }
                else if (theActualUrl.OrContainsMultiple("music.youtube.com", "youtu.be", "youtube.com")) {
                    yluLogger.Information("Found URL to be a YouTube Link");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?'))
                        finalId = theActualUrl.Split('?')[0];
                    
                    var videos = yt.Search.GetVideosAsync(finalId).GetAwaiter().GetResult();
                    var firstEntry = videos[0];
                    var title = firstEntry.Title;
                    var author = firstEntry.Author.ChannelTitle.Replace(" - Topic", "");
                    
                    var spotifyTrackUrl = string.Empty;
                    var tidalTrackUrl = string.Empty;
                    try {
                        spotifyTrackUrl = await Utils.MusicProviderApis.Spotify.GetSearchResults.SearchForUrl($"{author} {title}");
                    }
                    catch (Exception ex) {
                        yluLogger.Error("Failed to get track data from Spotify API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Spotify API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Spotify API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Spotify API");
                        return; // fail command out right
                    }
                    
                    try {
                        tidalTrackUrl = await Utils.MusicProviderApis.Tidal.GetSearchResults.SearchForUrl($"{author} {title}");
                    }
                    catch (Exception ex) {
                        yluLogger.Error("Failed to get track data from Tidal API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Tidal API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Tidal API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Tidal API");
                        return; // fail command out right
                    }
                    
                    sb.AppendLine(title.Contains(author) ? MarkdownUtils.ToBold(title) : MarkdownUtils.ToBold($"{author} - {title}"));
                    
                    var subtext = new StringBuilder();
                    subtext.Append(MarkdownUtils.MakeLink("Original YouTube Link \u2197", firstEntry.Url) + " \u2219 "); // will show YouTube embed
                    if (!string.IsNullOrWhiteSpace(spotifyTrackUrl))
                        if (spotifyTrackUrl != "[s404] ZERO RESULTS")
                            subtext.Append(MarkdownUtils.MakeLink("Spotify Link \u2197", spotifyTrackUrl, true) + " \u2219 ");
                    if (!string.IsNullOrWhiteSpace(tidalTrackUrl))
                        if (tidalTrackUrl != "[t404] ZERO RESULTS")
                            subtext.Append(MarkdownUtils.MakeLink("Tidal Link \u2197", tidalTrackUrl, true) /* + " \u2219 " */);
                    // subtext.Append(MarkdownUtils.MakeLink("Deezer Link \u2197", "https://deezer.page.link/" + "", true));
                    
                    sb.AppendLine(subtext.ToString());
                    
                    socketUserMessage = await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                    conf!.SubmittedBangers++;
                    Config.Save();
                    alreadyAddedBanger = true;
                }
                else if (theActualUrl.Contains("deezer.page.link")) {
                    dluLogger.Information("Found URL to be a Deezer Link");
                    
                    await ModifyOriginalResponseAsync(x => x.Content = "Deezer links are not supported at this time.");
                    return;
                }
            }
            
            afterCommandAction:
            if (Context.Channel.Id == conf.ChannelId) {
                var upVote = conf.CustomUpvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId) : Emote.Parse(conf.CustomUpvoteEmojiName) ?? Emote.Parse(":thumbsup:");
                var downVote = conf.CustomDownvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId) : Emote.Parse(conf.CustomDownvoteEmojiName) ?? Emote.Parse(":thumbsdown:");

                if (socketUserMessage is not null) {
                    if (conf.AddUpvoteEmoji)
                        await socketUserMessage.AddReactionAsync(upVote);
                    if (conf.AddDownvoteEmoji)
                        await socketUserMessage.AddReactionAsync(downVote);
                }

                if (!alreadyAddedBanger) {
                    conf.SubmittedBangers++;
                    Config.Save();
                }
            }
        }

        [SlashCommand("getbangercount", "Gets current guild banger count")]
        public async Task GetBangerCount([Summary("ephemeral", "Ephemeral response")] bool ephemeral = false)
            => await RespondAsync($"There are {Config.GetGuildBanger(Context.Guild.Id).SubmittedBangers} bangers in this guild.", ephemeral: ephemeral);

        [SlashCommand("leaderboard", "Guild Banger count Leaderboard")]
        public async Task Leaderboard() {
            var getAll = Config.Base.Banger;
            var orderedBangers = getAll.OrderByDescending(x => x.SubmittedBangers).Take(getAll.Count).ToList();

            var embed = new EmbedBuilder()
                .WithTitle("Banger Leaderboard")
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.Now);

            embed.AddField("Total Bangers", Config.GetBangerNumber().ToString());

            var sb = new StringBuilder();

            for (var i = 0; i < orderedBangers.Count; i++) {
                var guild = Context.Client.GetGuild(orderedBangers[i].GuildId);
                if (guild is null) continue;
                var order = i + 1;
                sb.AppendLine($"{order}. {(order is 1 ? "**" : "")}{guild.Name} - Bangers: {orderedBangers[i].SubmittedBangers}{(order is 1 ? "**" : "")}");
            }

            embed.WithDescription(sb.ToString());
            await RespondAsync(embed: embed.Build());
        }
    }

    [Group("bangeradmin", "Admin Banger Commands"),
     RequireToBeSpecial,
     // RequireUserPermission((GuildPermission.SendMessages & GuildPermission.ManageMessages & GuildPermission.ManageGuild) | GuildPermission.Administrator),
     IntegrationType(ApplicationIntegrationType.GuildInstall),
     CommandContextType(InteractionContextType.Guild)]
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext> {
        [SlashCommand("toggle", "Toggles the banger system")]
        public async Task ToggleBangerSystem([Summary("toggle", "Toggle the banger system")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).Enabled = enabled;
            Config.Save();
            await RespondAsync($"Bangers are now {(enabled ? "enabled" : "disabled")}.");
        }

        [SlashCommand("setchannel", "Sets the channel to only bangers")]
        public async Task SetBangerChannel([Summary("channel", "Destination Discord Channel")] ITextChannel channel) {
            var b = Config.GetGuildBanger(Context.Guild.Id);
            if (b.GuildId == 0)
                b.GuildId = Context.Guild.Id;
            b.ChannelId = channel.Id;
            Config.Save();
            await RespondAsync($"Set Banger channel to {channel.Mention}.");
        }

        [SlashCommand("seturlerrormessage", "Changes the error message")]
        public async Task ChangeBangerUrlErrorMessage([Summary("message", "Admin defined error message")] string text) {
            var newText = string.IsNullOrWhiteSpace(text) || text is "none" or "null" ? "This URL is not whitelisted." : text;
            Config.GetGuildBanger(Context.Guild.Id).UrlErrorResponseMessage = newText;
            Config.Save();
            await RespondAsync($"Set Banger URL Error Message to: {newText}");
        }

        [SlashCommand("setexterrormessage", "Changes the error message")]
        public async Task ChangeBangerExtErrorMessage([Summary("message", "Admin defined error message")] string text) {
            var newText = string.IsNullOrWhiteSpace(text) || text is "none" or "null" ? "This file extension is not whitelisted." : text;
            Config.GetGuildBanger(Context.Guild.Id).FileErrorResponseMessage = newText;
            Config.Save();
            await RespondAsync($"Set Banger File Extension Error Message to: {newText}");
        }

        private static bool _doesItExist(string value, IEnumerable<string> list) => list.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));

        [SlashCommand("addurl", "Adds a URL to the whitelist")]
        public async Task AddUrl([Summary("url", "URL to whitelist")] string url) {
            // await RespondAsync("This command is disabled, please contact Lily to add a URL to the whitelist.", ephemeral: true);

            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedUrls ??= [];
            if (_doesItExist(url, configBanger.WhitelistedUrls)) {
                await RespondAsync("URL already exists in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedUrls.Add(url);
            Config.Save();
            await RespondAsync($"Added {url} to the whitelist.");
        }

        [SlashCommand("removeurl", "Removes a URL from the whitelist")]
        public async Task RemoveUrl([Summary("url", "URL to remove from the whitelist")] string url) {
            // await RespondAsync("This command is disabled, please contact Lily to remove a URL from the whitelist.", ephemeral: true);

            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedUrls ??= [];
            if (!_doesItExist(url, configBanger.WhitelistedUrls)) {
                await RespondAsync("URL does not exist in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedUrls.Remove(url);
            Config.Save();
            await RespondAsync($"Removed {url} from the whitelist.");
        }

        [SlashCommand("addext", "Adds a file extension to the whitelist")]
        public async Task AddExt([Summary("extension", "File extension to whitelist")] string ext) {
            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedFileExtensions ??= [];
            if (ext.StartsWith('.'))
                ext = ext[1..];
            if (_doesItExist(ext, configBanger.WhitelistedFileExtensions)) {
                await RespondAsync("File extension already exists in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedFileExtensions.Add(ext);
            Config.Save();
            await RespondAsync($"Added {ext} to the whitelist.");
        }

        [SlashCommand("removeext", "Removes a file extension from the whitelist")]
        public async Task RemoveExt([Summary("extension", "File extension to remove from the whitelist")] string ext) {
            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedFileExtensions ??= [];
            if (ext.StartsWith('.'))
                ext = ext[1..];
            if (!_doesItExist(ext, configBanger.WhitelistedFileExtensions)) {
                await RespondAsync("File extension does not exist in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedFileExtensions.Remove(ext);
            Config.Save();
            await RespondAsync($"Removed {ext} from the whitelist.");
        }

        [SlashCommand("listeverything", "Lists all URLs and file extens"), RequireUserPermission(GuildPermission.SendMessages)]
        public async Task ListUrls([Summary("ephemeral", "Ephemeral response")] bool ephemeral = true) {
            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("Whitelisted URLs (Plain form):");
            Config.GetGuildBanger(Context.Guild.Id).WhitelistedUrls!.ForEach(s => sb.AppendLine($"- {s}"));
            // BangerListener.WhitelistedUrls!.ForEach(s => sb.AppendLine($"- {s}"));
            sb.AppendLine();
            sb.AppendLine("Whitelisted File Extensions:");
            Config.GetGuildBanger(Context.Guild.Id).WhitelistedFileExtensions!.ForEach(s => sb.AppendLine($"- .{s}"));
            sb.AppendLine("```");
            await RespondAsync(sb.ToString(), ephemeral: ephemeral);
        }

        [SlashCommand("addupvote", "Adds an upvote emoji to a banger post")]
        public async Task AddUpvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).AddUpvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Upvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("adddownvote", "Adds a downvote emoji to a banger post")]
        public async Task AddDownvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).AddDownvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Downvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("usecustomupvote", "Use a custom upvote emoji")]
        public async Task UseCustomUpvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).UseCustomUpvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Custom upvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("usecustomdownvote", "Use a custom downvote emoji")]
        public async Task UseCustomDownvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).UseCustomDownvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Custom downvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("setcustomupvote", "Sets a custom upvote emoji")]
        public async Task SetCustomUpvoteTheLongWay([Summary("name", "Custom upvote emoji name")] string name, [Summary("id", "Custom upvote emoji ID")] string id) {
            if (!Emote.TryParse($"<:{name}:{id}>", out var emote)) {
                await RespondAsync("Invalid emoji. Is the bot in the same guild as where this emoji is from?");
                return;
            }

            Config.GetGuildBanger(Context.Guild.Id).CustomUpvoteEmojiName = name;
            Config.GetGuildBanger(Context.Guild.Id).CustomUpvoteEmojiId = ulong.Parse(id);
            Config.Save();
            await RespondAsync($"Custom upvote emoji set to {emote}.\nNote: Having a custom emoji ID of zero will logically mean that you are using a Discord default emoji.");
        }

        [SlashCommand("setcustomdownvote", "Sets a custom downvote emoji")]
        public async Task SetCustomDownvoteTheLongWay([Summary("name", "Custom downvote emoji name")] string name, [Summary("id", "Custom downvote emoji ID")] string id) {
            if (!Emote.TryParse($"<:{name}:{id}>", out var emote)) {
                await RespondAsync("Invalid emoji. Is the bot in the same guild as where this emoji is from?");
                return;
            }

            Config.GetGuildBanger(Context.Guild.Id).CustomDownvoteEmojiName = name;
            Config.GetGuildBanger(Context.Guild.Id).CustomDownvoteEmojiId = ulong.Parse(id);
            Config.Save();
            await RespondAsync($"Custom downvote emoji set to {emote}.\nNote: Having a custom emoji ID of zero will logically mean that you are using a Discord default emoji.");
        }

        [SlashCommand("speakfreely", "Allow users to talk freely")]
        public async Task SpeakFreely([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).SpeakFreely = enabled;
            Config.Save();
            await RespondAsync($"Users {(enabled ? "can" : "cannot")} speak freely in the banger channel.");
        }

        // [SlashCommand("offertoreplace", "Offer Spotify to YouTube replacement")]
        // public async Task OfferReplace([Summary("toggle", "Enable or disable")] bool enabled) {
        //     Config.GetGuildBanger(Context.Guild.Id).OfferToReplaceSpotifyTrack = enabled;
        //     Config.Save();
        //     await RespondAsync($"Offer to replace Spotify track with YouTube {(enabled ? "enabled" : "disabled")}.");
        // }

        [SlashCommand("modifybangercount", "(Bot Owner Only) Modifies banger count"), RequireOwner]
        public async Task ModifyBangerCount(
            [Summary("number", "Number of bangers to add or remove")]
            int number,
            [Summary("ephemeral", "Ephemeral response")]
            bool ephemeral = false) {
            var banger = Config.GetGuildBanger(Context.Guild.Id);
            banger.SubmittedBangers += number;
            Config.Save();
            await RespondAsync($"Banger count modified by {number}. New count: {banger.SubmittedBangers}", ephemeral: ephemeral);
        }

        [SlashCommand("clearbangerinteractiondata", "(Bot Owner Only) Clears a select interaction data"), RequireOwner]
        public async Task ClearBangerInteractionData(
            [Summary("data-entry-id", "ID of the Data Entry")]
            string randomId = "",
            [Summary("ephemeral", "Ephemeral response")]
            bool ephemeral = false) {
            await DeferAsync(ephemeral);
            if (string.IsNullOrWhiteSpace(randomId)) {
                foreach (var iData in BangerListener.TheBangerInteractionData) {
                    if (iData.LookupMessage is not null)
                        await iData.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "Bot Owner forcefully deleted the Lookup Asking message." });
                    BangerListener.TheBangerInteractionData.Remove(iData);
                    await Task.Delay(TimeSpan.FromSeconds(0.25f));
                }

                return;
            }

            var data = BangerListener.TheBangerInteractionData.FirstOrDefault(x => x.RandomId == randomId);
            if (data is null) {
                await ModifyOriginalResponseAsync(x => x.Content = "Data not found.");
                return;
            }

            if (data.LookupMessage is not null)
                await data.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "Bot Owner forcefully deleted the Lookup Asking message." });
            BangerListener.TheBangerInteractionData.Remove(data);
        }
    }
}