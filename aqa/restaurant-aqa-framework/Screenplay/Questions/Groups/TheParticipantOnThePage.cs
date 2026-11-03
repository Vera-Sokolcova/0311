using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>What one participant card says: its whole text and the amount of its subtotal.</summary>
public sealed record ParticipantOnThePage(string Text, decimal? Total);

/// <summary>The card of a participant on the group order page, or null if the page shows nobody of that name.</summary>
public sealed class TheParticipantOnThePage(string name) : IQuestion<ParticipantOnThePage?>
{
    public async Task<ParticipantOnThePage?> AnsweredByAsync(Actor actor)
    {
        var card = new GroupOrderPage(actor.LookingAtThePage()).Participant(name);
        try
        {
            await card.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            return null;
        }

        var text = (await card.InnerTextAsync()).Trim();
        var subtotals = await card.Locator(".subtotal").AllInnerTextsAsync();
        return new ParticipantOnThePage(text, subtotals.Count == 0 ? null : PageAmounts.LastAmount(subtotals[^1]));
    }
}
