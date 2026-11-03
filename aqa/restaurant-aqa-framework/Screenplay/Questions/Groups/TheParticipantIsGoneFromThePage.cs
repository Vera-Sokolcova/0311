using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>Whether the participant's card disappears from the group order page (waits for it, since removing
/// goes through the backend first).</summary>
public sealed class TheParticipantIsGoneFromThePage(string name) : IQuestion<bool>
{
    public async Task<bool> AnsweredByAsync(Actor actor)
    {
        try
        {
            await new GroupOrderPage(actor.LookingAtThePage()).Participant(name)
                .WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 15_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
