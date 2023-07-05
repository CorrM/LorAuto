namespace LorAuto.Client.Model;

[Serializable]
public sealed class ActiveDeckApiResponse
{
    public required string? DeckCode { get; set; }
    public required Dictionary<string, int> CardsInDeck { get; init; }
}
