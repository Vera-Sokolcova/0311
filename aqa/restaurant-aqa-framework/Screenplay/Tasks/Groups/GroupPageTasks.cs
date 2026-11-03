using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;

/// <summary>Signs a customer of the cast in through the login form.</summary>
public sealed class SignInOnTheWebsite(string name) : ITask
{
    public Task PerformAsAsync(Actor actor)
    {
        var person = actor.TheCast().Customer(name);
        return new SignInThroughTheLoginForm(person.Email, person.Password).PerformAsAsync(actor);
    }
}

/// <summary>Opens the group order page of the owner's reservation and waits until the participants are shown.</summary>
public sealed class OpenTheGroupOrderPage(string owner) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.NavigateAsync(actor.TheCast().GroupOf(owner).ReservationId);
        await page.StatusBadge.WaitForAsync();
    }
}

/// <summary>Submits the pre-order from the group order page, confirming the dialog.</summary>
public sealed class SubmitFromTheGroupOrderPage : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.SubmitPreOrderButton.ClickAsync();
        await page.ConfirmSubmitButton.ClickAsync();
    }
}

/// <summary>Removes a guest from the group order page, confirming the dialog.</summary>
public sealed class RemoveFromTheGroupOrderPage(string guest) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.RemoveButtonOf(guest).ClickAsync();
        await page.ConfirmRemoveButton.ClickAsync();
    }
}
