using System.Drawing;
using System.Reflection;
using Point = System.Drawing.Point;

namespace LorAuto.Card;

[Serializable]
public sealed class InGameCard : GameCard
{
    public Point TopCenterPos { get; }
    public bool IsLocalPlayer { get; init; }

    public InGameCard(GameCard otherCard, int x, int y, int w, int h, bool isLocalPlayer)
    {
        // Using reflection is better than not forgetting to copy any property
        PropertyInfo[] propertyInfos = typeof(GameCard).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo info in propertyInfos)
            info.SetValue(this, info.GetValue(otherCard));
        
        TopCenterPos = new Point(x + (w / 2), y - (h / 4));
        IsLocalPlayer = isLocalPlayer;
    }
    
    public override string ToString()
    {
        return $"InGameCard({base.ToString()} -- TopCenter: ({TopCenterPos}); IsLocalPlayer: {IsLocalPlayer})";
    }
}
