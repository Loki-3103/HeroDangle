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
        new("batman", "Batman", "🦇", CharmKind.Image),
        new("dhirstibomma", "Dhirsti Bomma", "🪆", CharmKind.Image)
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
