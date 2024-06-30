using System.Drawing;
using LorAuto.Card.Model;

namespace LorAuto.Client;

/// <summary>
/// The <c>GameComponentLocator</c> class is responsible for locating and providing the coordinates of various game components within the game window.
/// It serves as a helper class for the <see cref="StateMachine"/> class, providing access to the necessary information for state detection and card position updates.
/// </summary>
internal sealed class GameComponentLocator
{
    private Size _windowSize;

    /// <summary>
    /// Returns a <see cref="Rectangle"/> object representing the region where the mana count is displayed within the game window.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> object representing the mana count region.</returns>
    public Rectangle GetManaRect()
    {
        // These numbers are critical, any pixel to left or right, 'GetMana' function will not work
        int x = (int)Math.Ceiling(_windowSize.Width * 0.8255f); // 1585
        int y = (int)Math.Ceiling(_windowSize.Height * 0.5907f); // 638
        const int w = 50; // TODO: Should be ratio
        const int h = 37; // TODO: Should be ratio

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Returns an array of <see cref="Rectangle"/> objects representing the regions where the spell mana counts are displayed within the game window.
    /// </summary>
    /// <returns>An array of <see cref="Rectangle"/> objects representing the spell mana count regions.</returns>
    public Rectangle[] GetSpellManaRect()
    {
        int mW = (int)Math.Ceiling(_windowSize.Width * 0.00625f);
        int mH = (int)Math.Ceiling(_windowSize.Height * 0.0111f);

        var ret = new Rectangle[]
        {
            new(
                (int)Math.Ceiling(_windowSize.Width * 0.8739f),
                (int)Math.Ceiling(_windowSize.Height * 0.6314f),
                mW,
                mH
            ),
            new(
                (int)Math.Ceiling(_windowSize.Width * 0.8854f),
                (int)Math.Ceiling(_windowSize.Height * 0.6259f),
                mW,
                mH
            ),
            new(
                (int)Math.Ceiling(_windowSize.Width * 0.8958f),
                (int)Math.Ceiling(_windowSize.Height * 0.6149f),
                mW,
                mH
            )
        };

        return ret;
    }

    /// <summary>
    /// Returns a <see cref="Rectangle"/> object representing the region where the turn button is located within the game window.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> object representing the turn button region.</returns>
    public Rectangle GetTurnButtonRect()
    {
        int x = (int)Math.Ceiling(_windowSize.Width * 0.82);
        int y = (int)Math.Ceiling(_windowSize.Height * 0.42);
        int w = (int)Math.Ceiling(_windowSize.Width * 0.10416);
        int h = (int)Math.Ceiling(_windowSize.Height * 0.1574);

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Returns a <see cref="Rectangle"/> object representing the region where the attack token is displayed within the game window.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> object representing the attack token region.</returns>
    public Rectangle GetAttackTokenRect()
    {
        int x = (int)Math.Ceiling(_windowSize.Width * 0.80f);
        int y = (int)Math.Ceiling(_windowSize.Height * 0.6f);
        int w = (int)Math.Ceiling(_windowSize.Width * 0.1f);
        int h = (int)Math.Ceiling(_windowSize.Height * 0.1814f);

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Returns a <see cref="Rectangle"/> object representing the region where the rounds log is displayed within the game window.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> object representing the rounds log region.</returns>
    public Rectangle GetRoundsLogRect()
    {
        int x = (int)Math.Ceiling(_windowSize.Width * 0.0156f);
        int y = (int)Math.Ceiling(_windowSize.Height * 0.4752f);
        int w = (int)Math.Ceiling(_windowSize.Width * 0.0333f);
        int h = (int)Math.Ceiling(_windowSize.Height * 0.0555f);

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Returns a <see cref="Rectangle"/> object representing the region where the "Edit Deck" button is located within the menus of the game window.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> object representing the "Edit Deck" button region.</returns>
    public Rectangle GetMenusEditDeckButtonRect()
    {
        int x = (int)Math.Ceiling(_windowSize.Width * 0.9447);
        int y = (int)Math.Ceiling(_windowSize.Height * 0.5518);
        int w = (int)Math.Ceiling(_windowSize.Width * 0.0353f);
        int h = (int)Math.Ceiling(_windowSize.Height * 0.0585f);

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Returns the attack and health regions of the specified in-game card.
    /// </summary>
    /// <param name="card">The in-game card for which to retrieve the attack and health regions.</param>
    /// <returns>A tuple of two <see cref="Rectangle"/> objects representing the attack and health regions of the card.</returns>
    public (Rectangle Attack, Rectangle Health) GetCardAttackAndHealthRect(InGameCard card)
    {
        int wSection = (int)Math.Ceiling(_windowSize.Width * 0.0223f);
        int hSection = (int)Math.Ceiling(_windowSize.Height * 0.0370f);

        int x;
        int y;

        if (card.IsLocalPlayer)
        {
            x = card.TopCenterPos.X;
            y = card.TopCenterPos.Y + 2;
        }
        else
        {
            x = card.BottomCenterPos.X;
            y = card.BottomCenterPos.Y - hSection;
        }

        var r1 = new Rectangle(x - 4 - wSection, y, wSection, hSection);
        var r2 = new Rectangle(x + 8, y, wSection, hSection);

        return (r1, r2);
    }

    /// <summary>
    /// Gets the rectangles representing the player's and opponent's Nexus health positions.
    /// </summary>
    /// <returns>A tuple containing the rectangles for the player's and opponent's Nexus health.</returns>
    public (Rectangle Player, Rectangle Opponent) GetNexusHealthRect()
    {
        const int w = 60; // TODO: Should be ratio
        const int h = 50; // TODO: Should be ratio

        int x = (int)Math.Ceiling(_windowSize.Width * 0.1510);

        int oy = (int)Math.Ceiling(_windowSize.Height * 0.3703);
        int py = (int)Math.Ceiling(_windowSize.Height * 0.5833);

        return (new Rectangle(x, py, w, h), new Rectangle(x, oy, w, h));
    }

    /// <summary>
    /// Gets the rectangles representing the player's and opponent's Nexus positions.
    /// </summary>
    /// <returns>A tuple containing the rectangles for the player's and opponent's Nexus.</returns>
    public (Point Player, Point Opponent) GetNexusPosition()
    {
        int x = (int)Math.Ceiling(_windowSize.Width * 0.1302);

        int oy = (int)Math.Ceiling(_windowSize.Height * 0.3888);
        int py = (int)Math.Ceiling(_windowSize.Height * 0.6018);

        return (new Point(x, py), new Point(x, oy));
    }

    /// <summary>
    /// Updates the window size.
    /// </summary>
    /// <param name="size">The new window size.</param>
    public void UpdateWindowSize(Size size)
    {
        _windowSize = size;
    }
}
