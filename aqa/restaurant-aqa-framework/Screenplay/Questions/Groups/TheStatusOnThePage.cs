using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>The status badge of the group order page ("Collecting pre-orders" or "Sent to Restaurant"). After
/// submitting, the page changes the badge only once the backend has answered, so this waits briefly for the
/// expected text before reading what is there.</summary>
public sealed class TheStatusOnThePage(string expected = "Sent to Restaurant") : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor)
    {
        var badge = new GroupOrderPage(actor.LookingAtThePage()).StatusBadge;
        try
        {
            await Assertions.Expect(badge).ToHaveTextAsync(expected, new() { Timeout = 15_000 });
        }
        catch (PlaywrightException)
        {
            // Not there in time: report what the page says instead, the step's assertion then fails with it.
        }

        return (await badge.InnerTextAsync()).Trim();
    }
}
