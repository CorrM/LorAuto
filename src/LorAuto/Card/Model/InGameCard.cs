using System.Drawing;
using System.Reflection;
using LorAuto.Client.Model;

namespace LorAuto.Card.Model;

/// <summary>
/// Represents a game card that is currently in-game.
/// </summary>
/// <remarks>
/// <see cref="IEquatable{T}"/> used for data structure that use default compare
/// </remarks>
[Serializable]
public sealed class InGameCard : GameCard, IEquatable<InGameCard>
{
    /// <summary>
    /// Gets the ID of the card.
    /// </summary>
    public int CardID { get; }

    /// <summary>
    /// Gets the in-game position of the card.
    /// </summary>
    public EInGameCardPosition InGamePosition { get; private set; }

    /// <summary>
    /// Gets the position of the card on the game board.
    /// </summary>
    public Point Position { get; private set; }

    /// <summary>
    /// Gets the size of the card.
    /// </summary>
    public Size Size { get; private set; }

    /// <summary>
    /// Gets the top-center position of the card.
    /// </summary>
    public Point TopCenterPos { get; private set; }

    /// <summary>
    /// Gets the bottom-center position of the card.
    /// </summary>
    public Point BottomCenterPos { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the card belongs to the local player.
    /// </summary>
    public bool IsLocalPlayer { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InGameCard"/> class based on the specified <see cref="GameCard"/> and game client rectangle.
    /// </summary>
    /// <param name="otherCard">The base game card.</param>
    /// <param name="rectCard">The game client rectangle that represents the card's position.</param>
    /// <param name="windowSize">The size of the game window.</param>
    /// <param name="inGamePosition">The in-game position of the card.</param>
    public InGameCard(GameCard otherCard, GameClientRectangle rectCard, Size windowSize, EInGameCardPosition inGamePosition)
    {
        // Using reflection is better than forgetting to copy any property
        PropertyInfo[] propertyInfos = typeof(GameCard).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo info in propertyInfos)
            info.SetValue(this, info.GetValue(otherCard));

        CardID = rectCard.CardID;
        UpdatePosition(rectCard, windowSize, inGamePosition);
    }

    /// <summary>
    /// Updates the position and other properties of the card based on the specified game client rectangle.
    /// </summary>
    /// <param name="rectCard">The game client rectangle that represents the card's position.</param>
    /// <param name="windowSize">The size of the game window.</param>
    /// <param name="inGamePosition">The in-game position of the card.</param>
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

    /// <summary>
    /// Updates the attack and health values of the card.
    /// </summary>
    /// <param name="attack">The new attack value.</param>
    /// <param name="health">The new health value.</param>
    public void UpdateAttackHealth(int attack, int health)
    {
        Attack = attack;
        Health = health;
    }

    /// <summary>
    /// Returns a string representation of the <see cref="InGameCard"/> object.
    /// </summary>
    /// <returns>A string representation of the card.</returns>
    public override string ToString()
    {
        return $"InGameCard({base.ToString()} -- TopCenter: ({TopCenterPos}); IsLocalPlayer: {IsLocalPlayer})";
    }

    /// <summary>
    /// Determines whether the current <see cref="InGameCard"/> object is equal to another <see cref="InGameCard"/> object.
    /// </summary>
    /// <param name="other">The <see cref="InGameCard"/> object to compare with the current object.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(InGameCard? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return CardID == other.CardID;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is InGameCard other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return CardID;
    }

    /// <summary>
    /// Determines whether two <see cref="InGameCard"/> objects are equal.
    /// </summary>
    /// <param name="left">The first <see cref="InGameCard"/> object to compare.</param>
    /// <param name="right">The second <see cref="InGameCard"/> object to compare.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(InGameCard? left, InGameCard? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two <see cref="InGameCard"/> objects are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="InGameCard"/> object to compare.</param>
    /// <param name="right">The second <see cref="InGameCard"/> object to compare.</param>
    /// <returns><c>true</c> if the objects are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(InGameCard? left, InGameCard? right)
    {
        return !Equals(left, right);
    }
}
