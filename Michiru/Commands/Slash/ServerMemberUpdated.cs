/*using Discord;
using Discord.Interactions;
using Michiru.Configuration._Base_Bot;
using Michiru.Utils;

namespace Michiru.Commands.Slash;

public class ServerMemberUpdated : InteractionModuleBase<SocketInteractionContext> {
    
    [Group("memberupdated", "Control Member Updated Features"), IntegrationType(ApplicationIntegrationType.GuildInstall),
     RequireUserPermission((GuildPermission.SendMessages & GuildPermission.ManageMessages & GuildPermission.ManageGuild) | GuildPermission.Administrator),]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {
        
        [SlashCommand("enablejoin", "Enable Join Features")]
        public async Task EnableJoin([Summary("enable", "Toggle Join Feature")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Join.Enable = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Join Features");
        }

        [SlashCommand("updatejoinchannel", "Update Join Channel")]
        public async Task UpdateJoinChannel([Summary("channel", "Assign Channel for Join Msgs")] ITextChannel channel) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Join.ChannelId = channel.Id;
            Config.Save();
            await RespondAsync($"Set Join Channel to <#{channel.Id}>");
        }
        
        [SlashCommand("joinmessage", "Update Join Message")]
        public async Task UpdateJoinMessage([Summary("message", "Join Message Text")] string message) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            if (message is "." or "NUL" or "NULL" or "EMPTY" or "none" or "NONE" or "null" or "empty" or "nul")
                message = "";
            serverData.Join.JoinMessageText = message;
            Config.Save();
            var preview = message.ParseMessageTextModifiers(Context.User, Context.Guild, Config.GetGuildPersonalizedMember(Context.Guild.Id));
            if (serverData.Join.DmWelcomeMessage && string.IsNullOrWhiteSpace(serverData.Join.JoinMessageText)) {
                var pm = Config.GetGuildPersonalizedMember(Context.Guild.Id);
                preview = $"Welcome to {Context.Guild.Name}!\n" +
                $"\nCreate your personal role by running {MarkdownUtils.ToCodeBlockSingleLine("/personalization createrole")} in <#{pm.ChannelId}>\n" +
                    $"You can also update role every {pm.ResetTimer} seconds by running the {MarkdownUtils.ToCodeBlockSingleLine("/personalization updaterole")} command.\n" +
                    $"Choose your choice of HEX color easily by using {MarkdownUtils.MakeLink("this website", "https://html-color.codes/")} and inputing that hex code in the color box.";;
            }
            await RespondAsync("Updated Join Message\nPreview:\n" + preview);
        }
        
        [SlashCommand("embedjoin", "Override All With Embed")]
        public async Task UpdateJoinEmbed([Summary("embed", "Override All With Embed")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Join.OverrideAllWithEmbed = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Join Embed");
        }
        
        [SlashCommand("showdetailedembed", "Show Detailed Embed")]
        public async Task UpdateJoinDetailedEmbed([Summary("detail", "Show Detailed Embed")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Join.ShowDetailedEmbed = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Join Detailed Embed");
        }
        
        [SlashCommand("dmjoinmessage", "DM Welcome Message")]
        public async Task UpdateJoinDmMessage([Summary("dm", "DM Welcome Message")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Join.DmWelcomeMessage = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Join DM Message");
        }
        
        
        
        
        
        
        [SlashCommand("enableleave", "Enable Leave Features")]
        public async Task EnableLeave([Summary("enable", "Toggle Leave Feature")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Leave.Enable = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Leave Features");
        }
        
        [SlashCommand("updateleavechannel", "Update Leave Channel")]
        public async Task UpdateLeaveChannel([Summary("channel", "Assign Channel for Leave Msgs")] ITextChannel channel) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Leave.ChannelId = channel.Id;
            Config.Save();
            await RespondAsync($"Set Leave Channel to <#{channel.Id}>");
        }
        
        [SlashCommand("leavemessage", "Update Leave Message")]
        public async Task UpdateLeaveMessage([Summary("message", "Leave Message Text")] string message) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            if (message is "." or "NUL" or "NULL" or "EMPTY" or "none" or "NONE" or "null" or "empty" or "nul")
                message = "";
            serverData.Leave.LeaveMessageText = message;
            Config.Save();
            await RespondAsync("Updated Leave Message\nPreview:\n" + message.ParseMessageTextModifiers(Context.User, Context.Guild, Config.GetGuildPersonalizedMember(Context.Guild.Id)));
        }
        
        [SlashCommand("embedleave", "Override All With Embed")]
        public async Task UpdateLeaveEmbed([Summary("embed", "Override All With Embed")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Leave.OverrideAllWithEmbed = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Leave Embed");
        }
        
        [SlashCommand("showdetailedleave", "Show Detailed Embed")]
        public async Task UpdateLeaveDetailedEmbed([Summary("detail", "Show Detailed Embed")] bool value) {
            var serverData = Config.GetGuildFeature(Context.Guild.Id);
            serverData.Leave.ShowDetailedEmbed = value;
            Config.Save();
            await RespondAsync($"{(value ? "Enabled" : "Disabled")} Leave Detailed Embed");
        }
    }
}*/