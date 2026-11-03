using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Tasks.Groups;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Setting up and acting on a group pre-order: the owner who books, the guests who join and fill their
/// carts, and whoever looks at, confirms or trims the group. Names are personas of the feature file; the dishes are
/// the first, second or third of the location's menu.</summary>
[Binding]
public sealed class GroupStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("{word} is a customer")]
    public Task GivenCustomer(string name) => Actor.AttemptsToAsync(new BecomeACustomer(name));

    [Given("{word} has booked a table for a group")]
    public Task GivenBookedTableForAGroup(string owner) =>
        Actor.AttemptsToAsync(new BecomeACustomer(owner), new BookATableForAGroup(owner));

    [Given("{word} has pre-ordered {int} portion(s) of the {ordinal} dish")]
    public Task GivenOwnerPreOrdered(string owner, int portions, DishNumber dish) =>
        Actor.AttemptsToAsync(new PreOrderForTheTable(owner, dish.Index, portions));

    [Given("{word} has joined the group of {word}")]
    [When("{word} joins the group of {word}")]
    public Task GuestJoins(string guest, string owner) => Actor.AttemptsToAsync(new JoinTheGroup(guest, owner));

    [Given("{word} has put {int} portion(s) of the {ordinal} dish into the cart")]
    [When("{word} puts {int} portion(s) of the {ordinal} dish into the cart")]
    public Task GuestPutsIntoTheCart(string guest, int portions, DishNumber dish) =>
        Actor.AttemptsToAsync(new PutIntoTheCart(guest, dish.Index, portions));

    [When("{word} takes {int} portion(s) of the {ordinal} dish out of the cart")]
    public Task GuestTakesOutOfTheCart(string guest, int portions, DishNumber dish) =>
        Actor.AttemptsToAsync(new TakeOutOfTheCart(guest, dish.Index, portions));

    [Given("{word} has confirmed the cart")]
    [When("{word} confirms the cart")]
    public Task GuestConfirmsTheCart(string guest) => Actor.AttemptsToAsync(new ConfirmTheCart(guest));

    [When("{word} opens the group pre-order")]
    public Task OwnerOpensTheGroup(string owner) => Actor.AttemptsToAsync(new ViewTheGroup(Viewer.Named(owner), owner));

    [When("{viewer} opens the group pre-order of {word}")]
    public Task SomeoneOpensTheGroup(Viewer who, string owner) => Actor.AttemptsToAsync(new ViewTheGroup(who, owner));

    [When("{viewer} opens a group pre-order that does not exist")]
    public Task SomeoneOpensAMissingGroup(Viewer who) => Actor.AttemptsToAsync(new ViewAGroupThatDoesNotExist(who));

    [Given("{word} has taken a seat in the group")]
    [When("{word} takes a seat in the group")]
    public Task OwnerTakesASeat(string owner) => Actor.AttemptsToAsync(new TakeTheOwnersSeat(owner));

    [When("{word} confirms the group pre-order")]
    public Task OwnerConfirmsTheGroup(string owner) =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(Viewer.Named(owner), owner));

    [Given("{word} has confirmed the group pre-order")]
    public Task GivenOwnerConfirmedTheGroup(string owner) =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(Viewer.Named(owner), owner));

    [When("{viewer} confirms the group pre-order of {word}")]
    public Task SomeoneConfirmsTheGroup(Viewer who, string owner) =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(who, owner));

    [When("{viewer} confirms a group pre-order that does not exist")]
    public Task SomeoneConfirmsAMissingGroup(Viewer who) => Actor.AttemptsToAsync(new ConfirmAGroupThatDoesNotExist(who));

    [When("{word} removes {word} from the group")]
    public Task OwnerRemovesAGuest(string owner, string guest) =>
        Actor.AttemptsToAsync(new RemoveTheParticipant(Viewer.Named(owner), owner, guest));

    [When("{viewer} removes {word} from the group of {word}")]
    public Task SomeoneRemovesAGuest(Viewer who, string guest, string owner) =>
        Actor.AttemptsToAsync(new RemoveTheParticipant(who, owner, guest));

    [When("{word} removes somebody who never joined from the group")]
    public Task OwnerRemovesAStranger(string owner) =>
        Actor.AttemptsToAsync(new RemoveAParticipantWhoDoesNotExist(Viewer.Named(owner), owner));

    [When("the waiter opens the pre-order of {word}")]
    public Task WaiterOpensThePreOrder(string owner) => Actor.AttemptsToAsync(new OpenThePreOrderAsTheWaiter(owner));
}
