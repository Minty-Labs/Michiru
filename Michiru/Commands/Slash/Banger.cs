﻿using System.Text;
using Discord;
using Discord.Interactions;
using Michiru.Commands.Preexecution;
using Michiru.Configuration;
using Michiru.Events;
using Michiru.Utils;

namespace Michiru.Commands.Slash; 

public class Banger : InteractionModuleBase<SocketInteractionContext> {

    [Group("banger", "Banger Commands"), RequireToBeSpecial,
     // RequireUserPermission((GuildPermission.SendMessages & GuildPermission.ManageMessages & GuildPermission.ManageGuild) | GuildPermission.Administrator),
     IntegrationType(ApplicationIntegrationType.GuildInstall)]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {
        
        [SlashCommand("toggle", "Toggles the banger system")]
        public async Task ToggleBangerSystem([Summary("toggle", "Enable or disable the banger system")] bool enabled) {
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
        public async Task AddExt([Summary("ext", "File extension to whitelist")] string ext) {
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
        public async Task RemoveExt([Summary("ext", "File extension to remove from the whitelist")] string ext) {
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
        
        [SlashCommand("listeverything", "Lists all URLs and file extns")]
        public async Task ListUrls() {
            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("Whitelisted URLs (RegEx form):");
            Config.GetGuildBanger(Context.Guild.Id).WhitelistedUrls!.ForEach(s => sb.AppendLine($"- {s}"));
            // BangerListener.WhitelistedUrls!.ForEach(s => sb.AppendLine($"- {s}"));
            sb.AppendLine();
            sb.AppendLine("Whitelisted File Extensions:");
            Config.GetGuildBanger(Context.Guild.Id).WhitelistedFileExtensions!.ForEach(s => sb.AppendLine($"- {s}"));
            sb.AppendLine("```");
            await RespondAsync(sb.ToString());
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
        
        [SlashCommand("speakfreely", "Allow users to talk freely in the banger channel")]
        public async Task SpeakFreely([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).SpeakFreely = enabled;
            Config.Save();
            await RespondAsync($"Users {(enabled ? "can" : "cannot")} speak freely in the banger channel.");
        }
        
        [SlashCommand("offertoreplace", "Offer to replace Spotify track with a YouTube link")]
        public async Task OfferReplace([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).OfferToReplaceSpotifyTrack = enabled;
            Config.Save();
            await RespondAsync($"Offer to replace Spotify track with YouTube {(enabled ? "enabled" : "disabled")}.");
        }
        
        [SlashCommand("getbangercount", "Gets the number of bangers submitted in this guild"), RequireUserPermission(GuildPermission.SendMessages)]
        public async Task GetBangerCount([Summary("ephemeral", "Ephemeral response")] bool ephemeral = false)
            => await RespondAsync($"There are {Config.GetGuildBanger(Context.Guild.Id).SubmittedBangers} bangers in this guild.", ephemeral: ephemeral);
        
        [SlashCommand("modifybangercount", "(Bot Owner Only) Modifies the number of bangers submitted in this guild"), RequireOwner]
        public async Task ModifyBangerCount([Summary("number", "Number of bangers to add or remove")] int number, [Summary("ephemeral", "Ephemeral response")] bool ephemeral = false) {
            var banger = Config.GetGuildBanger(Context.Guild.Id);
            banger.SubmittedBangers += number;
            Config.Save();
            await RespondAsync($"Banger count modified by {number}. New count: {banger.SubmittedBangers}", ephemeral: ephemeral);
        }
        
        [SlashCommand("clearbangerinteractiondata", "(Bot Owner Only) Clears a select or all banger interaction data"), RequireOwner]
        public async Task ClearBangerInteractionData([Summary("RandomID", "ID of the Data Entry, if empty, remove everything")] string randomId = "", [Summary("ephemeral", "Ephemeral response")] bool ephemeral = false) {
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
                await RespondAsync("Data not found.", ephemeral: ephemeral);
                return;
            }

            if (data.LookupMessage is not null)
                await data.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "Bot Owner forcefully deleted the Lookup Asking message." });
            BangerListener.TheBangerInteractionData.Remove(data);
        }
    }
}