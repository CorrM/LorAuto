using System.Text.Json.Serialization;

namespace LorAuto.GameCard;

public enum GameCardType
{
    
}

[Serializable]
public class GameCard
{
    public required string Name { get; init; }
    public required int Cost { get; init; }
    public required int Attack { get; init; }
    public required int Health { get; init; }
    public required GameCardType Type { get; init; }
    public required string Keywords { get; init; }
    public required string Description { get; init; }

    public override string ToString()
    {
        return $"Card({Name} ({Cost}) T: {Type:G} A: {Attack} H: {Health})";
    }
}
