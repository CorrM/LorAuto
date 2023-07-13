namespace LorAuto.Card.Model;

/// <summary>
/// Represents a set of game cards.
/// </summary>
public sealed class GameCardSet
{
    /// <summary>
    /// Gets or sets the name of the card set.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the dictionary of cards in the set, where the key is the card code and the value is the corresponding <see cref="GameCard"/>.
    /// </summary>
    public required Dictionary<string, GameCard> Cards { get; init; }
}
