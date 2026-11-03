using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Business-level outcomes shared by several features ("the visitor is told ...", "could not be
/// found"). How an outcome shows up on the wire — which status code, which error body — is decided here,
/// so feature files never mention it.</summary>
[Binding]
public sealed class OutcomeStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Then("the visitor is told {string}")]
    [Scope(Tag = "api")]
    public async Task ThenVisitorIsTold(string message)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Succeeded).IsFalse().Because($"the request unexpectedly succeeded: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).IsEqualTo(message);
    }

    [Then("sign-in is refused")]
    public async Task ThenSignInIsRefused()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(401).Because($"response body was: {outcome.Body}");
    }

    [Then("the guest is told that the {item} could not be found")]
    public async Task ThenGuestIsToldNotFound(string item)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).Contains("was not found");
    }

    [Then("the location is not created")]
    public async Task ThenLocationIsNotCreated()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsGreaterThanOrEqualTo(400).Because($"the location was created: {outcome.Body}");
    }

    [Then("the information is available")]
    [Then("the menu is shown")]
    public async Task ThenInformationIsAvailable()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"response status {outcome.Status}, body: {outcome.Body}");
    }

    // ---- group pre-order ----

    [Then("the request is refused because only the owner may do it")]
    public async Task ThenRefusedNotOwner() => await AssertStatusAsync(403);

    [Then("the request is refused because nobody is signed in")]
    [Then("the request is refused because a guest has no account")]
    public async Task ThenRefusedNotSignedIn() => await AssertStatusAsync(401);

    [Then("the group cannot be found")]
    public async Task ThenGroupNotFound() => await AssertStatusAsync(404);

    [Then("the participant cannot be found")]
    public async Task ThenParticipantNotFound() => await AssertStatusAsync(404);

    [Then("the participant is refused because they belong to another group")]
    public async Task ThenParticipantOfAnotherGroup() => await AssertStatusAsync(400);

    [Then("the pre-order is refused because it was already sent")]
    public async Task ThenAlreadySent() => await AssertStatusAsync(409);

    [Then("the pre-order is refused with {string}")]
    public async Task ThenPreOrderRefusedWith(string message)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.Body).Contains(message);
    }

    [Then("the pre-order is sent to the restaurant")]
    [Then("the participant is removed")]
    public async Task ThenSucceeded() => await AssertStatusAsync(200);

    private async Task AssertStatusAsync(int status)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(status).Because($"response body was: {outcome.Body}");
    }
}
