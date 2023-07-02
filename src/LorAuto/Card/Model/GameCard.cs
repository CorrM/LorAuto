using System.Reflection;
using LorAuto.Client.Model;

namespace LorAuto.Card.Model;

[Serializable]
public class GameCard : IEquatable<GameCard>
{
    public string Name { get; init; } = null!;
    public string CardCode { get; init; } = null!;
    public int Cost { get; init; }
    public int Attack { get; init; }
    public int Health { get; init; }
    public GameCardType Type { get; init; }
    public GameCardKeyword[] Keywords { get; init; } = null!;
    public string Description { get; init; } = null!;

    public void Update(GameCard otherCard)
    {
        Type gameCardType = GetType();
        PropertyInfo[] propertyInfos = gameCardType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            object? otherCardProp = propertyInfo.GetValue(otherCard);
            propertyInfo.SetValue(this, otherCardProp);
        }
    }

    public void Update(GameClientRectangle rectCard)
    {
        throw new Exception("TODO");
    }
    
    public override string ToString()
    {
        return $"Card({Name} ({Cost}) T: {Type:G} A: {Attack} H: {Health})";
    }

    public bool Equals(GameCard? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        return CardCode == other.CardCode && Cost == other.Cost
               && Attack == other.Attack && Health == other.Health && Type == other.Type;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is GameCard other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, CardCode, Cost, Attack, Health, (int)Type, Keywords, Description);
    }

    public static bool operator ==(GameCard? left, GameCard? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(GameCard? left, GameCard? right)
    {
        return !Equals(left, right);
    }
}
