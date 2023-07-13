using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;

namespace LorAuto.OCR;

/// <summary>
/// Provides OCR (Optical Character Recognition) functionality using the Tesseract engine.
/// </summary>
internal sealed class OcrHelper : IDisposable
{
    private readonly Tesseract _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrHelper"/> class.
    /// </summary>
    /// <param name="dataPath">The path to the Tesseract data directory.</param>
    /// <param name="lang">The language for OCR.</param>
    public OcrHelper(string dataPath, string lang)
    {
        _engine = new Tesseract(dataPath, lang, OcrEngineMode.Default);
        _engine.PageSegMode = PageSegMode.SingleWord;

        _engine.SetVariable("debug_file", "NUL");
        _engine.SetVariable("tessedit_char_whitelist", "0123456789"); // Only recognize digits
    }

    /// <summary>
    /// Reads a number from an image using OCR.
    /// </summary>
    /// <param name="img">The image containing the number.</param>
    /// <param name="printText">Specifies whether to print the recognized text to the console. Default is false.</param>
    /// <returns>A tuple containing the read number and the mean confidence level.</returns>
    public (int Number, float MeanConfidence) ReadNumber(Image<Gray, byte> img, bool printText = false)
    {
        _engine.SetImage(img);
        _engine.Recognize();

        string text = _engine.GetUTF8Text().Trim();
        if (printText)
            Console.WriteLine(text);

        Tesseract.Character[] characters = _engine.GetCharacters();
        int characterCount = characters.Length;
        float totalConfidence = characters.Sum(c => c.Cost);
        float meanConfidence = totalConfidence / characterCount;

        return int.TryParse(text, out int number)
            ? (number, meanConfidence)
            : (-1, 0);
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
