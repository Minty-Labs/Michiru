using Discord;

namespace Michiru.Managers; 

public static class ErrorSending {
    private static EmbedBuilder? ErrorEmbed(object message, object exception = null) {
        var msg = message.ToString();
        var finalMsg = (msg!.Length > 2000 ? msg[..1990] + "..." : msg) ?? "Error, no message could be displayed. This should not happen.";
        if (finalMsg.ToLower().Contains("unauthorized") && finalMsg.Contains("403"))
            return null;

        return new EmbedBuilder {
            Color = Color.Red,
            Description = exception != null ? $"```{finalMsg}```\n{exception}" : finalMsg,
            Footer = new EmbedFooterBuilder {
                Text = Vars.Version
            },
            Timestamp = DateTime.Now
        };
    }

    public static async Task SendErrorToLoggingChannelAsync(object message, MessageReference? reference = null) => await Program.Instance.ErrorLogChannel.SendMessageAsync(embed: ErrorEmbed(message)!.Build(), messageReference: reference);

    public static void SendErrorToLoggingChannel(object message, MessageReference? reference = null) => SendErrorToLoggingChannelAsync(message, reference).GetAwaiter().GetResult();
    
    public static async Task SendErrorToLoggingChannelAsync(object message, MessageReference? reference = null, object _object = null) => await Program.Instance.ErrorLogChannel.SendMessageAsync(embed: ErrorEmbed(message, _object)!.Build(), messageReference: reference);

    public static void SendErrorToLoggingChannel(object message, MessageReference? reference = null, object _object = null) => SendErrorToLoggingChannelAsync(message, reference, _object).GetAwaiter().GetResult();
}