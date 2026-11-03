using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>The grand total the group order page shows.</summary>
public sealed class TheGrandTotalOnThePage : IQuestion<decimal>
{
    public async Task<decimal> AnsweredByAsync(Actor actor) =>
        PageAmounts.LastAmount(await new GroupOrderPage(actor.LookingAtThePage()).GrandTotal.InnerTextAsync())
        ?? throw new InvalidOperationException("The grand total on the page is not an amount.");
}
