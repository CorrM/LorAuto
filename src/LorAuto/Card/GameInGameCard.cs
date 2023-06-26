using System.Drawing;

namespace LorAuto.GameCard;

[Serializable]
public sealed class GameInGameCard : GameCard
{
    public Point TopCenterPos { get; }
    public required bool IsLocal { get; init; }

    public GameInGameCard(int x, int y, int w, int h)
    {
        TopCenterPos = new Point(x + w / 2, y - h / 4);
    }

    public GameInGameCard(Point position)
    {
        TopCenterPos = position;
    }
    
    public override string ToString()
    {
        return $"InGameCard({base.ToString()} -- TopCenter: ({TopCenterPos}); IsLocal: {IsLocal})";
    }
}
