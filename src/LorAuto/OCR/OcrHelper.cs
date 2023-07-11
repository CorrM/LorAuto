using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;

namespace LorAuto.OCR;

public sealed class OcrHelper : IDisposable
{
    private readonly Tesseract _engine;

    public OcrHelper(string dataPath, string lang)
    {
        _engine = new Tesseract(dataPath, lang, OcrEngineMode.Default);
        _engine.PageSegMode = PageSegMode.SingleWord;
        
        _engine.SetVariable("debug_file", "NUL");
        _engine.SetVariable("tessedit_char_whitelist", "0123456789"); // Only recognize digits
    }

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

    public void Dispose()
    {
        _engine.Dispose();
    }
}
