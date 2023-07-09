using System.Text.Json.Nodes;

namespace LorAuto.Card.Model;

[Serializable]
public class GameCard
{
    public string Name { get; protected set; } = null!;
    public string CardCode { get; protected set; } = null!;
    public int Cost { get; protected set; }
    public int Attack { get; protected set; }
    public int Health { get; protected set; }
    public GameCardType Type { get; protected set; }
    public GameCardKeyword[] Keywords { get; protected set; } = null!;
    public string Description { get; protected set; } = null!;

    public static GameCard FromJson(JsonNode json)
    {
        return new GameCard()
        {
            Name = json["name"]!.GetValue<string>(),
            CardCode = json["cardCode"]!.GetValue<string>(),
            Cost = json["cost"]!.GetValue<int>(),
            Attack = json["attack"]!.GetValue<int>(),
            Health = json["health"]!.GetValue<int>(),
            Type = Enum.Parse<GameCardType>(json["type"]!.GetValue<string>()),
            Keywords = json["keywordRefs"]!.AsArray().Select(j => Enum.Parse<GameCardKeyword>(j!.GetValue<string>())).ToArray(),
            Description = json["descriptionRaw"]!.GetValue<string>(),
        };
    }
    
    public override string ToString()
    {
        return $"Card({Name}({Cost}) T: {Type:G} A: {Attack} H: {Health})";
    }
}
