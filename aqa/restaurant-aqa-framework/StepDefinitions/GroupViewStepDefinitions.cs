using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Questions.Groups;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>What the owner, the restaurant and the waiter find out about a group pre-order.</summary>
[Binding]
public sealed class GroupViewStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Then("the group has {int} participant(s)")]
    public async Task ThenTheGroupHasParticipants(int count)
    {
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        await Assert.That(group.Participants.Count).IsEqualTo(count)
            .Because($"the participants were: {string.Join(", ", group.Participants.Select(p => p.Name))}");
    }

    [Then("nobody is listed and the group total is zero")]
    public async Task ThenNobodyIsListed()
    {
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        await Assert.That(group.Participants.Count).IsEqualTo(0);
        await Assert.That(group.Total).IsEqualTo(0m);
    }

    [Then("{word} is listed with {int} portion(s) of the {ordinal} dish")]
    public async Task ThenListedWithDish(string name, int portions, DishNumber number)
    {
        var participant = await FindParticipantAsync(name);
        var dish = (await Actor.AsksForAsync(new TheMenuOfTheGroup()))[number.Index];
        var item = participant.Items.SingleOrDefault(i => i.Dish.Id == dish.Id);

        await Assert.That(item).IsNotNull().Because($"{name} has no '{dish.Name}' in the list");
        await Assert.That(item!.Quantity).IsEqualTo(portions);
    }

    [Then("{word} is listed with no dishes")]
    public async Task ThenListedWithNoDishes(string name)
    {
        var participant = await FindParticipantAsync(name);
        await Assert.That(participant.Items.Count).IsEqualTo(0);
        await Assert.That(participant.Total).IsEqualTo(0m);
    }

    [Then("{word} is listed as still choosing")]
    public async Task ThenStillChoosing(string name) =>
        await Assert.That((await FindParticipantAsync(name)).IsSubmitted).IsFalse();

    [Then("{word} is listed as having confirmed the cart")]
    public async Task ThenConfirmed(string name) =>
        await Assert.That((await FindParticipantAsync(name)).IsSubmitted).IsTrue();

    [Then("the total of {word} is the price of {int} portion(s) of the {ordinal} dish")]
    public async Task ThenParticipantTotal(string name, int portions, DishNumber number)
    {
        var dish = (await Actor.AsksForAsync(new TheMenuOfTheGroup()))[number.Index];
        await Assert.That((await FindParticipantAsync(name)).Total).IsEqualTo(dish.Price * portions);
    }

    [Then("the group total is the sum of the totals of the participants")]
    public async Task ThenGroupTotalIsTheSum()
    {
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        await Assert.That(group.Total).IsEqualTo(group.Participants.Sum(p => p.Total));
    }

    [Then("the group total is the price of {int} portion(s) of the {ordinal} dish")]
    public async Task ThenGroupTotalIsThePriceOf(int portions, DishNumber number)
    {
        var dish = (await Actor.AsksForAsync(new TheMenuOfTheGroup()))[number.Index];
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        await Assert.That(group.Total).IsEqualTo(dish.Price * portions);
    }

    [Then("the owner comes first in the list")]
    public async Task ThenOwnerComesFirst()
    {
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        await Assert.That(group.Participants[0].Type).IsEqualTo("Owner");
    }

    [Then("{word} is not in the group any more")]
    public async Task ThenNotInTheGroup(string name)
    {
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        await Assert.That(group.Participants.Select(p => p.Name)).DoesNotContain(name);
    }

    [Then("the consolidated pre-order consists of")]
    public async Task ThenConsolidatedConsistsOf(DataTable table)
    {
        var consolidated = await Actor.AsksForAsync(new TheResponseAs<List<ConsolidatedDishResponse>>());
        var menu = await Actor.AsksForAsync(new TheMenuOfTheGroup());
        var expected = ExpectedDishes(table, menu.Select(d => d.Id).ToList());

        var actual = consolidated.ToDictionary(c => c.DishId, c => c.Quantity);
        await Assert.That(actual.Count).IsEqualTo(expected.Count).Because($"the consolidated list was: {Actor.TheLastResponse().Body}");
        foreach (var (dishId, portions) in expected)
            await Assert.That(actual.GetValueOrDefault(dishId)).IsEqualTo(portions);
    }

    [Then("the waiter sees these dishes in the order")]
    public async Task ThenWaiterSeesDishes(DataTable table)
    {
        var order = await Actor.AsksForAsync(new TheResponseAs<GetOrderResponse>());
        var menu = await Actor.AsksForAsync(new TheMenuOfTheGroup());
        var expected = ExpectedDishes(table, menu.Select(d => d.Id).ToList());

        var actual = order.OrderItems.GroupBy(i => i.Dish.Id).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        await Assert.That(actual.Count).IsEqualTo(expected.Count).Because($"the order was: {Actor.TheLastResponse().Body}");
        foreach (var (dishId, portions) in expected)
            await Assert.That(actual.GetValueOrDefault(dishId)).IsEqualTo(portions);
    }

    [Then("the waiter sees the order as confirmed by the customer")]
    public async Task ThenOrderConfirmedByCustomer() =>
        await Assert.That((await Actor.AsksForAsync(new TheResponseAs<GetOrderResponse>())).Status)
            .IsEqualTo(OrderStatus.SubmittedByCustomer);

    private async Task<GroupParticipantResponse> FindParticipantAsync(string name)
    {
        var group = await Actor.AsksForAsync(new TheResponseAs<GroupSessionsResponse>());
        var participant = group.Participants.SingleOrDefault(p => p.Name == name);
        await Assert.That(participant).IsNotNull()
            .Because($"{name} is not in the list; the participants are: {string.Join(", ", group.Participants.Select(p => p.Name))}");
        return participant!;
    }

    private static Dictionary<string, int> ExpectedDishes(DataTable table, IReadOnlyList<string> menuIds) =>
        table.Rows.ToDictionary(
            row => menuIds[DishNumber.Parse(row["dish"]).Index],
            row => int.Parse(row["portions"], System.Globalization.CultureInfo.InvariantCulture));
}
