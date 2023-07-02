namespace LorAuto.Card.Model;

public sealed class GameCardSet
{
    /// <summary>
    /// Card set name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Cards that included in the card set
    /// </summary>
    /// <remarks>
    /// Key is card code, Value is card information
    /// </remarks>
    public required Dictionary<string, GameCard> Cards { get; init; }
}
