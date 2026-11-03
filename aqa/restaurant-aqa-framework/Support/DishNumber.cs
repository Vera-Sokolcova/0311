namespace Restaurant_Aqa_Framework.Support;

/// <summary>Which dish of the menu a feature file means: "the first dish", "the second dish", … (by place on the
/// group's menu, so the feature files never name a dish or an id).</summary>
public sealed record DishNumber(int Index)
{
    private static readonly string[] Words = ["first", "second", "third"];

    public static DishNumber Parse(string text)
    {
        var word = text.Trim().Split(' ')[0];
        var index = Array.IndexOf(Words, word);
        return index >= 0
            ? new DishNumber(index)
            : throw new ArgumentOutOfRangeException(nameof(text), text, "Use 'first dish', 'second dish' or 'third dish'.");
    }
}
