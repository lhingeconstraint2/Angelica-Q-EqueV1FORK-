using Discord.Interactions;

namespace DiscordEqueBot.Modules;

public class MathModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("isnumber", "Check if the input text is a number.")]
    public Task IsNumber(double number)
    {
        return RespondAsync($"The text {number} is a number!", ephemeral: true);
    }

    [SlashCommand("multiply", "Get the product of two numbers.")]
    public async Task Multiply(int a, int b)
    {
        var product = Math.BigMul(a, b);
        await RespondAsync($"The product of `{a} * {b}` is `{product}`.", ephemeral: true);
    }

    [SlashCommand("square", "Get the square root of a number.")]
    public async Task Square(double number)
    {
        var sqrt = Math.Sqrt(number);
        await RespondAsync($"The square root of `{number}` is `{sqrt}`.", ephemeral: true);
    }
}