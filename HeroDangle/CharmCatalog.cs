namespace HeroDangle;

public enum CharmKind
{
    Vector,
    Emoji,
    Image
}

public sealed record CharmDefinition(string Id, string Name, string Emoji, CharmKind Kind);

public static class CharmCatalog
{
    public static readonly CharmDefinition[] BuiltIn =
    [
        new("nazar", "Nazar", "🧿", CharmKind.Vector),
        new("lemon", "Lemon", "🍋", CharmKind.Vector),
        new("chili", "Chili", "🌶️", CharmKind.Vector),
        new("horseshoe", "Horseshoe", "🐴", CharmKind.Vector),
        new("clover", "Clover", "🍀", CharmKind.Vector),
        new("hamsa", "Hamsa", "🪬", CharmKind.Vector),
        new("batman", "Batman", "🦇", CharmKind.Image)
    ];

    public static CharmDefinition Custom(string emoji) =>
        new("custom", "Custom", string.IsNullOrWhiteSpace(emoji) ? "⭐" : emoji, CharmKind.Emoji);

    public static CharmDefinition Resolve(AppConfig config)
    {
        if (config.CharmId == "custom")
            return Custom(config.CustomEmoji);

        return BuiltIn.FirstOrDefault(c => c.Id == config.CharmId) ?? BuiltIn[0];
    }
}
