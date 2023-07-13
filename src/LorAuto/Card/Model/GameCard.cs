using System.Text.Json.Nodes;

namespace LorAuto.Card.Model;

/// <summary>
/// Represents a game card.
/// </summary>
[Serializable]
public class GameCard
{
    /// <summary>
    /// Gets the name of the card.
    /// </summary>
    public string Name { get; protected set; } = null!;

    /// <summary>
    /// Gets the card code.
    /// </summary>
    public string CardCode { get; protected set; } = null!;

    /// <summary>
    /// Gets the cost of the card.
    /// </summary>
    public int Cost { get; protected set; }

    /// <summary>
    /// Gets the attack value of the card.
    /// </summary>
    public int Attack { get; protected set; }

    /// <summary>
    /// Gets the health value of the card.
    /// </summary>
    public int Health { get; protected set; }

    /// <summary>
    /// Gets the type of the card.
    /// </summary>
    public EGameCardType Type { get; protected set; }

    /// <summary>
    /// Gets the keywords associated with the card.
    /// </summary>
    public EGameCardKeyword[] Keywords { get; protected set; } = null!;

    /// <summary>
    /// Gets the description of the card.
    /// </summary>
    public string Description { get; protected set; } = null!;

    /// <summary>
    /// Creates a new instance of the <see cref="GameCard"/> class from the specified JSON data.
    /// </summary>
    /// <param name="json">The JSON data representing the card.</param>
    /// <returns>A new instance of the <see cref="GameCard"/> class.</returns>
    public static GameCard FromJson(JsonNode json)
    {
        return new GameCard()
        {
            Name = json["name"]!.GetValue<string>(),
            CardCode = json["cardCode"]!.GetValue<string>(),
            Cost = json["cost"]!.GetValue<int>(),
            Attack = json["attack"]!.GetValue<int>(),
            Health = json["health"]!.GetValue<int>(),
            Type = Enum.Parse<EGameCardType>(json["type"]!.GetValue<string>()),
            Keywords = json["keywordRefs"]!.AsArray().Select(j => Enum.Parse<EGameCardKeyword>(j!.GetValue<string>())).ToArray(),
            Description = json["descriptionRaw"]!.GetValue<string>(),
        };
    }

    /// <summary>
    /// Returns a string representation of the <see cref="GameCard"/> object.
    /// </summary>
    /// <returns>A string representation of the <see cref="GameCard"/> object.</returns>
    public override string ToString()
    {
        return $"Card({Name}({Cost}) T: {Type:G} A: {Attack} H: {Health})";
    }
}
