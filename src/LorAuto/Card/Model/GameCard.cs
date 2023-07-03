namespace LorAuto.Card.Model;

[Serializable]
public class GameCard
{
    public string Name { get; init; } = null!;
    public string CardCode { get; init; } = null!;
    public int Cost { get; init; }
    public int Attack { get; init; }
    public int Health { get; init; }
    public GameCardType Type { get; init; }
    public GameCardKeyword[] Keywords { get; init; } = null!;
    public string Description { get; init; } = null!;
    
    public override string ToString()
    {
        return $"Card({Name}({Cost}) T: {Type:G} A: {Attack} H: {Health})";
    }
}
