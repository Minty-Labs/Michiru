using Discord;
using Discord.Interactions;
using Michiru.Commands.Preexecution;
using Michiru.Configuration;
using Michiru.Configuration.Classes;
using Michiru.Utils;

namespace Michiru.Commands.Slash; 

public class Personalization : InteractionModuleBase<SocketInteractionContext> {
    private static bool _IsInChannel(SocketInteractionContext context, ulong guildId) => context.Channel.Id == Config.GetGuildPersonalizedMember(guildId).ChannelId;

    [Group("personalization", "Personalized Members Commands"), IntegrationType(ApplicationIntegrationType.GuildInstall), CommandContextType(InteractionContextType.Guild)]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {

        [SlashCommand("createrole", "Creates a personalized role for you")]
        public async Task CreateRole() {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            if (!personalData.Enabled) {
                await RespondAsync("Personalized Roles is not enabled.", ephemeral: true);
                return;
            }
            if (!_IsInChannel(Context, Context.Guild.Id)) {
                await RespondAsync($"You can only use this command in <#{personalData.ChannelId}>", ephemeral: true);
                return;
            }
            
            var modal = new ModalBuilder {
                    Title = "New Personal Role",
                    CustomId = "personalization_createrole",
                }
                .AddTextInput("Name", "roleName", required: false, placeholder: "A Cool Name (15 characters)", style: TextInputStyle.Short)
                .AddTextInput("Color (Hex)", "colorHex", required: true, placeholder: "#abc123", style: TextInputStyle.Short);

            await Context.Interaction.RespondWithModalAsync(modal.Build());
        }
        
        [SlashCommand("updaterole", "Updates your personalized role")]
        public async Task UpdateRole() {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            if (!personalData.Enabled) {
                await RespondAsync("Personalized Roles is not enabled.", ephemeral: true);
                return;
            }
            if (!_IsInChannel(Context, Context.Guild.Id)) {
                await RespondAsync($"You can only use this command in <#{personalData.ChannelId}>", ephemeral: true);
                return;
            }
            
            var personalizedMember = personalData.Members!.FirstOrDefault(x => x.userId == Context.User.Id);
            var currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (personalizedMember is not null && personalizedMember.epochTime + personalData.ResetTimer > currentEpoch) {
                await RespondAsync($"You need to wait {personalizedMember.epochTime + personalData.ResetTimer - currentEpoch} seconds before you can use this command again.", ephemeral: true);
                return;
            }
            
            var modal = new ModalBuilder {
                    Title = "Update Personal Role",
                    CustomId = "personalization_updaterole"
                }
                .AddTextInput("Name", "roleName", required: false, placeholder: "A Cool Name (15 characters)", style: TextInputStyle.Short, value: personalizedMember?.roleName)
                .AddTextInput("Color (Hex)", "colorHex", required: false, placeholder: "#abc123", style: TextInputStyle.Short, value: personalizedMember?.colorHex);

            await Context.Interaction.RespondWithModalAsync(modal.Build());
        }

        [SlashCommand("deleterole", "Removes your personalized role")]
        public async Task DeleteRole() {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            if (!personalData.Enabled) {
                await RespondAsync("Personalized Roles is not enabled.", ephemeral: true);
                return;
            }
            if (!_IsInChannel(Context, Context.Guild.Id)) {
                await RespondAsync($"You can only use this command in <#{personalData.ChannelId}>", ephemeral: true);
                return;
            }
            var currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var guildPersonalizedMember = personalData.Members!.FirstOrDefault(x => x.userId == Context.User.Id);
            if (guildPersonalizedMember is null) {
                await RespondAsync("You need to create a personalized role first.\nRun the following command to create one:\n`/personalization createrole`", ephemeral: true);
                return;
            }
            if (guildPersonalizedMember.epochTime + personalData.ResetTimer > currentEpoch) {
                await RespondAsync($"You need to wait {guildPersonalizedMember.epochTime + personalData.ResetTimer - currentEpoch} seconds before you can use this command again.", ephemeral: true);
                return;
            }
            var memberRole = Context.Guild.GetRole(guildPersonalizedMember!.roleId);
            await memberRole!.DeleteAsync(new RequestOptions {AuditLogReason = "Personalized Member - User: " + Context.User.Username});
            personalData.Members!.Remove(guildPersonalizedMember);
            Config.Save();
            if (personalData.DefaultRoleId != 0) {
                var defaultRole = Context.Guild.GetRole(personalData.DefaultRoleId);
                var discordMember = Context.User as IGuildUser;
                await discordMember!.AddRoleAsync(defaultRole, new RequestOptions { AuditLogReason = "Personalized Member - User: " + Context.User.Username });
            }
            await RespondAsync("Successfully removed your personalized member role.");
        }
    }

    [Group("personalizationadmin", "Personalized Members Admin Commands"), IntegrationType(ApplicationIntegrationType.GuildInstall)]
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext> {
        
        [SlashCommand("toggle", "Toggles the personalized members system"), RequireToBeSpecial]
        public async Task ToggleGetGuildPersonalizedMembersSystem([Summary("toggle", "Enable or disable the personalized members system")] bool enabled) {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            personalData.Enabled = enabled;
            Config.Save();
            await RespondAsync($"Personalized Members are now {(enabled ? "enabled" : "disabled")}.");
        }
        
        [SlashCommand("setchannel", "Sets the channel to only personalized members"), RequireToBeSpecial]
        public async Task SetGetGuildPersonalizedMembersChannel([Summary("channel", "Destination Discord Channel")] ITextChannel channel) {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            personalData.ChannelId = channel.Id;
            Config.Save();
            await RespondAsync($"Set Personalized Members channel to {channel.Mention}.");
        }
        
        [SlashCommand("setdefaultrole", "Sets the default role for users to be granted when they remove their personalized role"), RequireToBeSpecial]
        public async Task SetDefaultRole([Summary("role", "Role to set as the default role")] IRole role) {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            personalData.DefaultRoleId = role.Id;
            Config.Save();
            await RespondAsync($"Set default role to {role.Mention}.");
        }
        
        [SlashCommand("removedefaultrole", "Removes the default role for your personalized role system"), RequireToBeSpecial]
        public async Task RemoveDefaultRole() {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            personalData.DefaultRoleId = 0;
            Config.Save();
            await RespondAsync("Removed default role.");
        }
        
        [SlashCommand("setresettime", "Sets the time in seconds for when a user's personalized role is reset"), RequireToBeSpecial]
        public async Task SetResetTime([Summary("time", "Time in seconds")] int time) {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            personalData.ResetTimer = time;
            Config.Save();
            await RespondAsync($"Set reset time to {time} seconds.");
        }
        
        [SlashCommand("addroleto", "Adds a role to the user as well as the personalized role system"), RequireToBeSpecial]
        public async Task AddRoleToGetGuildPersonalizedMembers(
            [Summary("user", "User to add the role to")] IUser user,
            [Summary("role", "Role to add to the personalized members list")] IRole role) {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            if (personalData.Members!.Any(x => x.roleId == role.Id)) {
                await RespondAsync("The role is already in the system, it cannot be one more than one person.", ephemeral: true);
                return;
            }
            personalData.Members!.Add(new Member {
                userId = user.Id,
                roleId = role.Id,
                roleName = role.Name,
                colorHex = (role.Color.ToString() ?? string.Empty).ValidateHexColor().ToLower(),
                epochTime = 1207
            });
            Config.Save();
            var discordMember = (IGuildUser)user;
            await RespondAsync($"Added **{role.Name}** to the personalized system for **{discordMember.DisplayName}**.");
        }
        
        [SlashCommand("removerolefrom", "Removes a role from the user as well as the personalized role system"), RequireToBeSpecial]
        public async Task RemoveRoleFromGetGuildPersonalizedMembers(
            [Summary("user", "User to remove the role from")] IUser user) {
            var personalData = Config.GetGuildPersonalizedMember(Context.Guild.Id);
            var memberData = personalData.Members!.FirstOrDefault(x => x.userId == user.Id);
            if (memberData is null) {
                await RespondAsync("User data does not exist.", ephemeral: true);
                return;
            }

            var memberRole = Context.Guild.GetRole(memberData.roleId);
            await memberRole.DeleteAsync(new RequestOptions {AuditLogReason = "Personalized Member - Admin: " + Context.User.Username});
            personalData.Members!.Remove(memberData);
            Config.Save();
            var discordMember = (IGuildUser)user;
            if (personalData.DefaultRoleId != 0) {
                var defaultRole = Context.Guild.GetRole(personalData.DefaultRoleId);
                await discordMember.AddRoleAsync(defaultRole, new RequestOptions {AuditLogReason = "Personalized Member - Admin: " + Context.User.Username});
            }
            await RespondAsync($"Removed {discordMember.DisplayName}'s personalized role.");
        }
        
    }
}