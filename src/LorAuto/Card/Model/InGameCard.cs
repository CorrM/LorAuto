using System.Drawing;
using System.Reflection;
using LorAuto.Client.Model;
using LorAuto.Game.Model;

namespace LorAuto.Card.Model;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// <see cref="IEquatable{T}"/> used for data structure that use default compare
/// </remarks>
[Serializable]
public sealed class InGameCard : GameCard, IEquatable<InGameCard>
{
    public int CardID { get; }
    public EInGameCardPosition InGamePosition { get; private set; }
    public Point Position { get; private set; }
    public Size Size { get; private set; }
    public Point TopCenterPos { get; private set; }
    public Point BottomCenterPos { get; private set; }
    public bool IsLocalPlayer { get; private set; }

    public InGameCard(GameCard otherCard, GameClientRectangle rectCard, Size windowSize, EInGameCardPosition inGamePosition)
    {
        // Using reflection is better than forgetting to copy any property
        PropertyInfo[] propertyInfos = typeof(GameCard).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo info in propertyInfos)
            info.SetValue(this, info.GetValue(otherCard));

        CardID = rectCard.CardID;
        UpdatePosition(rectCard, windowSize, inGamePosition);
    }
    
    public void UpdatePosition(GameClientRectangle rectCard, Size windowSize, EInGameCardPosition inGamePosition)
    {
        if (CardID != rectCard.CardID)
            throw new Exception($"Current card and {nameof(rectCard)} not identical.");
        
        int y = windowSize.Height - rectCard.TopLeftY;

        InGamePosition = inGamePosition;
        Position = new Point(rectCard.TopLeftX, y);
        Size = new Size(rectCard.Width, rectCard.Height);
        TopCenterPos = new Point(rectCard.TopLeftX + (rectCard.Width / 2), y);
        BottomCenterPos = new Point(rectCard.TopLeftX + (rectCard.Width / 2), y + rectCard.Height);
        IsLocalPlayer = rectCard.LocalPlayer;
    }

    public void UpdateAttackHealth(int attack, int health)
    {
        Attack = attack;
        Health = health;
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
