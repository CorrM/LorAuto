using System.Drawing;
using System.Reflection;
using LorAuto.Client.Model;

namespace LorAuto.Card.Model;

[Serializable]
public sealed class InGameCard : GameCard, IEquatable<InGameCard>
{
    public int CardID { get; }
    public Point Position { get; private set; }
    public Size Size { get; private set; }
    public Point TopCenterPos { get; private set; }
    public bool IsLocalPlayer { get; private set; }

    public InGameCard(GameCard otherCard, GameClientRectangle rectCard)
    {
        // Using reflection is better than not forgetting to copy any property
        PropertyInfo[] propertyInfos = typeof(GameCard).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo info in propertyInfos)
            info.SetValue(this, info.GetValue(otherCard));

        CardID = rectCard.CardID;
        Update(rectCard);
    }
    
    public void Update(GameClientRectangle rectCard)
    {
        if (CardID != rectCard.CardID)
            throw new Exception($"Current card and {nameof(rectCard)} not identical.");
        
        Position = new Point(rectCard.TopLeftX, rectCard.TopLeftY);
        Size = new Size(rectCard.Width, rectCard.Height);
        TopCenterPos = new Point(rectCard.TopLeftX + (rectCard.Width / 2), rectCard.TopLeftY - (rectCard.Height / 4));
        IsLocalPlayer = rectCard.LocalPlayer;
    }
    
    public override string ToString()
    {
        return $"InGameCard({base.ToString()} -- TopCenter: ({TopCenterPos}); IsLocalPlayer: {IsLocalPlayer})";
    }

    public bool Equals(InGameCard? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        
        return CardID == other.CardID;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is InGameCard other && Equals(other);
    }

    public override int GetHashCode()
    {
        return CardID;
    }

    public static bool operator ==(InGameCard? left, InGameCard? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(InGameCard? left, InGameCard? right)
    {
        return !Equals(left, right);
    }
}
