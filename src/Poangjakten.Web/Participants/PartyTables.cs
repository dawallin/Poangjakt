namespace Poangjakten.Web.Participants;

public sealed record PartyTable(string Id, int Number, string Name)
{
    public string DisplayName => $"Bord {Number} - {Name}";
}

public static class PartyTables
{
    public static readonly IReadOnlyList<PartyTable> All =
    [
        new("1", 1, "Skärvik"),
        new("2", 2, "Sörängen"),
        new("3", 3, "Solsidan/Saltis"),
        new("4", 4, "Lund"),
        new("5", 5, "Malmö"),
        new("6", 6, "Belfast/Nordirland"),
        new("7", 7, "Lomma"),
        new("8", 8, "Stockholm"),
        new("9", 9, "Sri Lanka")
    ];

    private static readonly IReadOnlyDictionary<string, PartyTable> ById =
        All.ToDictionary(table => table.Id, StringComparer.Ordinal);

    public static PartyTable? Find(string? id) =>
        id is not null && ById.TryGetValue(id, out var table) ? table : null;
}
