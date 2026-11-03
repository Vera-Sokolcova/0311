using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.Groups;

/// <summary>The dishes a group can order from, in a fixed order (by id), so "the first dish" means the same dish for
/// everyone in the scenario. Only dishes that can be ordered right now are listed.</summary>
public sealed class TheMenuOfTheGroup : IQuestion<IReadOnlyList<DishListResponse>>
{
    public async Task<IReadOnlyList<DishListResponse>> AnsweredByAsync(Actor actor)
    {
        var cast = actor.TheCast();
        if (cast.Menu is { } known)
            return known;

        var response = await new LocationsApi(actor.UsingTheApi()).GetDishesForLocationAsync(cast.LocationOfTheGroups());
        if (!response.Ok)
            throw new InvalidOperationException($"The menu could not be read: {response.Status} {await response.TextAsync()}");

        var menu = (await response.ReadAsAsync<List<DishListResponse>>())
            .Where(dish => string.Equals(dish.State, nameof(DishState.Available), StringComparison.OrdinalIgnoreCase))
            .OrderBy(dish => dish.Id, StringComparer.Ordinal)
            .ToList();
        if (menu.Count < 3)
            throw new InvalidOperationException($"The group scenarios need at least 3 available dishes, the location offers {menu.Count}.");

        cast.Menu = menu;
        return menu;
    }
}
