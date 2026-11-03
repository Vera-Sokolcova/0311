using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui;

/// <summary>`/reservations/{id}/group-order` — the owner's view of the group pre-order.</summary>
public sealed class GroupOrderPage(IPage page) : BasePage(page)
{
    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Group Pre-Order", Exact = true });
    public ILocator StatusBadge => Page.Locator("header .badge");
    public ILocator Participants => Page.Locator("ul.participants > li");
    public ILocator GrandTotal => Page.Locator(".grand-total .amount");
    public ILocator SubmitPreOrderButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order", Exact = true });
    public ILocator ConfirmSubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit", Exact = true });
    public ILocator ConfirmRemoveButton => Page.GetByRole(AriaRole.Button, new() { Name = "Remove Guest", Exact = true });

    public Task NavigateAsync(string reservationId) => NavigateToAsync($"/reservations/{reservationId}/group-order");

    public ILocator Participant(string name) =>
        Participants.Filter(new() { Has = Page.Locator(".title", new() { HasText = name }) });

    public ILocator RemoveButtonOf(string name) =>
        Page.GetByRole(AriaRole.Button, new() { Name = $"Remove {name}", Exact = true });
}
