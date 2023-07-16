namespace LorAuto.Client.Model;

/// <summary>
/// Represents the response data for an active deck.
/// </summary>
[Serializable]
public sealed class ActiveDeckApiResponse
{
    /// <summary>
    /// Gets or sets the deck code.
    /// </summary>
    public required string? DeckCode { get; set; }

    /// <summary>
    /// Gets or initializes the dictionary of cards in the deck, where the key is the card name and the value is the count.
    /// </summary>
    public required Dictionary<string, int> CardsInDeck { get; init; }
}
