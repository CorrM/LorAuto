using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using LorAuto.OCR;

namespace LorAuto.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Image{TColor,TDepth}"/>.
/// </summary>
internal static class CvImageExtensions
{
    /// <summary>
    /// Crops the image using the specified rectangle.
    /// </summary>
    /// <typeparam name="TColor">The type of color of the image.</typeparam>
    /// <typeparam name="TDepth">The depth of the image.</typeparam>
    /// <param name="image">The image to crop.</param>
    /// <param name="rectangle">The rectangle specifying the region to crop.</param>
    /// <returns>The cropped image.</returns>
    public static Image<TColor, TDepth> Crop<TColor, TDepth>(this Image<TColor, TDepth> image, Rectangle rectangle) where TColor : struct, IColor where TDepth : new()
    {
        // Set the ROI of the image
        image.ROI = rectangle;

        // Crop the image without allocating additional memory
        Image<TColor, TDepth> croppedImage = image.Copy();

        // Reset the ROI to the full image
        image.ROI = Rectangle.Empty;

        return croppedImage;
    }

    /// <summary>
    /// Crops the image using the specified coordinates and size.
    /// </summary>
    /// <typeparam name="TColor">The type of color of the image.</typeparam>
    /// <typeparam name="TDepth">The depth of the image.</typeparam>
    /// <param name="image">The image to crop.</param>
    /// <param name="x">The x-coordinate of the top-left corner of the ROI.</param>
    /// <param name="y">The y-coordinate of the top-left corner of the ROI.</param>
    /// <param name="width">The width of the ROI.</param>
    /// <param name="height">The height of the ROI.</param>
    /// <returns>The cropped image.</returns>
    public static Image<TColor, TDepth> Crop<TColor, TDepth>(this Image<TColor, TDepth> image, int x, int y, int width, int height) where TColor : struct, IColor where TDepth : new()
    {
        return image.Crop(new Rectangle(x, y, width, height));
    }

    /// <summary>
    /// Creates a binary mask of the image within the specified HSV color range.
    /// </summary>
    /// <typeparam name="TColor">The type of color of the image.</typeparam>
    /// <typeparam name="TDepth">The depth of the image.</typeparam>
    /// <param name="image">The image to process.</param>
    /// <param name="lower">The lower bound of the HSV color range.</param>
    /// <param name="higher">The upper bound of the HSV color range.</param>
    /// <returns>The binary mask image.</returns>
    public static Image<Gray, byte> InHsvRange<TColor, TDepth>(this Image<TColor, TDepth> image, Hsv lower, Hsv higher) where TColor : struct, IColor where TDepth : new()
    {
        // TODO: Check if i can get raid of 'targetAndMask.Convert<Gray, byte>()'
        using Image<Hsv, byte>? hsv = image.Convert<Hsv, byte>();
        using Image<Gray, byte>? mask = hsv.InRange(lower, higher);
        using Image<TColor, TDepth>? targetAndMask = image.And(image, mask);

        return targetAndMask.Convert<Gray, byte>();
    }

    /// <summary>
    /// Counts the number of non-zero pixels in the image within the specified HSV color range.
    /// </summary>
    /// <typeparam name="TColor">The type of color of the image.</typeparam>
    /// <typeparam name="TDepth">The depth of the image.</typeparam>
    /// <param name="image">The image to process.</param>
    /// <param name="lower">The lower bound of the HSV color range.</param>
    /// <param name="higher">The upper bound of the HSV color range.</param>
    /// <returns>The number of non-zero pixels.</returns>
    public static int CountNonZeroInHsvRange<TColor, TDepth>(this Image<TColor, TDepth> image, Hsv lower, Hsv higher) where TColor : struct, IColor where TDepth : new()
    {
        using Image<Gray, byte> targetAndMaskGray = InHsvRange(image, lower, higher);

        return CvInvoke.CountNonZero(targetAndMaskGray);
    }

    public static (int Number, float Confidence) ReadNumberFromImage<TColor, TDepth>(this Image<TColor, TDepth> image, OcrHelper ocr, IEnumerable<(Hsv Lower, Hsv Higher)> recognizeColors)
        where TColor : struct, IColor where TDepth : new()
    {
        var ret = new List<(int Num, float Confidence)>();

        foreach ((Hsv lower, Hsv higher) in recognizeColors)
        {
            using Image<Gray, byte> inRangeImg = image.InHsvRange(lower, higher);
            int countNonZero = CvInvoke.CountNonZero(inRangeImg);
            if (countNonZero == 0)
                continue;

            using Image<Gray, byte> resizeImg = inRangeImg.Resize(120, 120, Inter.Linear);
            (int number, float confidence) = ocr.ReadNumber(resizeImg);

            if (number == -1)
                continue;

            ret.Add((number, confidence));
        }

        return ret.Count > 0 ? ret.MaxBy(x => x.Confidence) : (-1, 0);
    }
}
