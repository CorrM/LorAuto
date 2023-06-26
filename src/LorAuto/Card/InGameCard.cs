using System.Drawing;

namespace LorAuto.Card;

[Serializable]
public sealed class InGameCard : GameCard
{
    public Point TopCenterPos { get; }
    public required bool IsLocal { get; init; }

    public InGameCard(int x, int y, int w, int h)
    {
        TopCenterPos = new Point(x + w / 2, y - h / 4);
    }

    public InGameCard(Point position)
    {
        TopCenterPos = position;
    }
    
    public override string ToString()
    {
        return $"InGameCard({base.ToString()} -- TopCenter: ({TopCenterPos}); IsLocal: {IsLocal})";
    }
}
