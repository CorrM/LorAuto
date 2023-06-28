namespace LorAuto.Client.Model;

[Serializable]
public sealed class GameClientCardInDeck
{
    public required string Code { get; init; }
    public required int Count { get; init; }
}

[Serializable]
public sealed class ActiveDeckApiResponse
{
    public required string? DeckCode { get; init; }
    public required List<GameClientCardInDeck>? CardsInDeck { get; init; }
}
