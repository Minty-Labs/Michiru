using System.Text;
using Discord.Commands;
using Michiru.Utils;

namespace Michiru.Commands.Prefix;

[RequireContext(ContextType.Guild | ContextType.DM | ContextType.Group)]

public class DAndD : ModuleBase<SocketCommandContext> {
    [Command("dice"), Alias("d")]
    public async Task DiceRoll(string sides = "", int numberOfRoles = 1) {
        if (string.IsNullOrWhiteSpace(sides)) {
            await ReplyAsync("Please specify the number of sides on the die.");
            return;
        }
        
        if (sides.Contains('d')) {
            sides = sides.Split('d')[1];
        }
        
        var acceptedSides = new[] {"4", "6", "8", "10", "12", "20", "100"};
        
        if (!int.TryParse(sides, out var sideCount) || !acceptedSides.Contains(sides)) {
            await ReplyAsync("Invalid die size. Please use one of the following: 4, 6, 8, 10, 12, 20, 100");
            return;
        }
        
        var rolls = new StringBuilder();
        rolls.AppendLine($"Rolling {(numberOfRoles > 1 ? "multiple" : "a")} D{sideCount}");
        var total = 1;
        for (var i = 0; i < numberOfRoles; i++) {
            var roll = new Random().Next(1, sideCount);
            total += roll;
            rolls.AppendLine($"{MarkdownUtils.ToBold($"Roll {i}")}: {MarkdownUtils.ToCodeBlockSingleLine(roll.ToString())}");
        }

        rolls.AppendLine();
        rolls.AppendLine($"Total: {MarkdownUtils.ToCodeBlockSingleLine(total.ToString())}");
        
        await ReplyAsync(rolls.ToString());
    }
}