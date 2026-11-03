using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions.Groups;
using Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>The owner's group order page in the browser.</summary>
[Binding]
public sealed class GroupPageStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("{word} has signed in on the website")]
    public Task GivenSignedIn(string name) => Actor.AttemptsToAsync(new SignInOnTheWebsite(name));

    [When("{word} opens the group order page")]
    public Task WhenOpensThePage(string owner) => Actor.AttemptsToAsync(new OpenTheGroupOrderPage(owner));

    [When("the owner submits the pre-order on the page")]
    public Task WhenSubmitsOnThePage() => Actor.AttemptsToAsync(new SubmitFromTheGroupOrderPage());

    [When("the owner removes {word} on the page")]
    public Task WhenRemovesOnThePage(string guest) => Actor.AttemptsToAsync(new RemoveFromTheGroupOrderPage(guest));

    [Then("the page lists {word} with a total of {int} portion(s) of the {ordinal} dish")]
    public async Task ThenPageListsParticipant(string name, int portions, DishNumber number)
    {
        var dish = (await Actor.AsksForAsync(new TheMenuOfTheGroup()))[number.Index];
        var card = await Actor.AsksForAsync(new TheParticipantOnThePage(name));

        await Assert.That(card).IsNotNull().Because($"the page does not list {name}");
        await Assert.That(card!.Text).Contains(dish.Name);
        await Assert.That(card.Total).IsEqualTo(dish.Price * portions);
    }

    [Then("the page shows {word} as having confirmed the cart")]
    public async Task ThenPageShowsConfirmed(string name)
    {
        var card = await Actor.AsksForAsync(new TheParticipantOnThePage(name));
        await Assert.That(card).IsNotNull();
        await Assert.That(card!.Text).Contains("Submitted");
    }

    [Then("the grand total on the page is the price of {int} portion(s) of the {ordinal} dish")]
    public async Task ThenGrandTotal(int portions, DishNumber number)
    {
        var dish = (await Actor.AsksForAsync(new TheMenuOfTheGroup()))[number.Index];
        await Assert.That(await Actor.AsksForAsync(new TheGrandTotalOnThePage())).IsEqualTo(dish.Price * portions);
    }

    [Then("the page says the pre-order was sent to the restaurant")]
    public async Task ThenPageSaysSent() =>
        await Assert.That(await Actor.AsksForAsync(new TheStatusOnThePage())).IsEqualTo("Sent to Restaurant");

    [Then("the page no longer lists {word}")]
    public async Task ThenPageNoLongerLists(string name) =>
        await Assert.That(await Actor.AsksForAsync(new TheParticipantOnThePage(name))).IsNull();
}
