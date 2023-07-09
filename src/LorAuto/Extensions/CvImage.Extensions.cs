using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace LorAuto.Extensions;

public static class CvImageExtensions
{
    public static Image<TColor, TDepth> Crop<TColor, TDepth>(this Image<TColor, TDepth> image, Rectangle rectangle) where TColor : struct, IColor where TDepth : new()
    {
        // TODO: Find a better way to do it
        using var mat = new Mat(
            image.Mat,
            new Emgu.CV.Structure.Range(rectangle.Y, rectangle.Y + rectangle.Height),
            new Emgu.CV.Structure.Range(rectangle.X, rectangle.X + rectangle.Width));
        
        return mat.ToImage<TColor, TDepth>();
    }

    public static Image<TColor, TDepth> Crop<TColor, TDepth>(this Image<TColor, TDepth> image, int x, int y, int width, int height) where TColor : struct, IColor where TDepth : new()
    {
        return image.Crop(new Rectangle(x, y, width, height));
    }
    
    public static Image<Gray, byte> InHsvRange<TColor, TDepth>(this Image<TColor, TDepth> image, Hsv lower, Hsv higher) where TColor : struct, IColor where TDepth : new()
    {
        // TODO: Check if i can get raid of 'targetAndMask.Convert<Gray, byte>()'
        using Image<Hsv, byte>? hsv = image.Convert<Hsv, byte>();
        using Image<Gray, byte>? mask = hsv.InRange(lower, higher);
        using Image<TColor, TDepth>? targetAndMask = image.And(image, mask);

        return targetAndMask.Convert<Gray, byte>();
    }
    
    public static int CountNonZeroInHsvRange<TColor, TDepth>(this Image<TColor, TDepth> image, Hsv lower, Hsv higher) where TColor : struct, IColor where TDepth : new()
    {
        using Image<Gray, byte>? targetAndMaskGray = InHsvRange(image, lower, higher);

        return CvInvoke.CountNonZero(targetAndMaskGray);
    }
}
