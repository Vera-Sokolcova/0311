using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>The status badge of the group order page ("Collecting pre-orders" or "Sent to Restaurant").</summary>
public sealed class TheStatusOnThePage : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor) =>
        (await new GroupOrderPage(actor.LookingAtThePage()).StatusBadge.InnerTextAsync()).Trim();
}
