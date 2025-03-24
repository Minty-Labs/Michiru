using Discord;
using Discord.Commands;
using Discord.Interactions;

namespace Michiru.Commands.Preexecution; 

public class RequireToBeSpecial : Discord.Interactions.PreconditionAttribute {
    public override Task<Discord.Interactions.PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo cmdInfo, IServiceProvider services) {
        if (context.Guild.OwnerId == context.User.Id)
            return Task.FromResult(Discord.Interactions.PreconditionResult.FromSuccess());
        return context.Guild.Id switch {
            977705960544014407 when context.User.Id is 875251523641294869 or 167335587488071682 => Task.FromResult(Discord.Interactions.PreconditionResult.FromSuccess()),
            1149332156313768007 when context.User.Id is 723217987774971975 or 927059361514291260 or 167335587488071682 => Task.FromResult(Discord.Interactions.PreconditionResult.FromSuccess()),
            _ => Task.FromResult(Discord.Interactions.PreconditionResult.FromError("You are not allowed to use this command."))
        };
    }
}

public class RequireBotOwner : Discord.Commands.PreconditionAttribute {
    public override Task<Discord.Commands.PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo cmdInfo, IServiceProvider services) {
        return Task.FromResult(context.User.Id is 167335587488071682 ? Discord.Commands.PreconditionResult.FromSuccess() : Discord.Commands.PreconditionResult.FromError("You are not allowed to use this command."));
    }
}

public static class UserExtensions {
    public static bool IsSpecial(this IUser user, IGuild guild) {
        if (guild.OwnerId == user.Id)
            return true;
        return guild.Id switch {
            977705960544014407 when user.Id is 875251523641294869 or 167335587488071682 => true,
            1149332156313768007 when user.Id is 723217987774971975 or 927059361514291260 or 167335587488071682 => true,
            _ => false
        };
    }
    
    public static bool IsBotOwner(this IUser user) => user.Id == 167335587488071682;
}