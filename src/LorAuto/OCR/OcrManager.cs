using System.Drawing;
using Tesseract;

namespace LorAuto.OCR;

public sealed class OcrManager : IDisposable
{
    private readonly TesseractEngine _engine;
    
    public OcrManager(string dataPath, string lang)
    {
        _engine = new TesseractEngine(dataPath, lang, EngineMode.Default);
    }

    public (string, float) GetText(Bitmap img, Rectangle? region)
    {
        //new Bitmap(Path.Combine(Environment.CurrentDirectory, "TessData", "phototest.tif"));
        using Page page = region is null
            ? _engine.Process(img)
            : _engine.Process(img, new Rect(region.Value.X, region.Value.Y, region.Value.Width, region.Value.Height));
        
        string text = page.GetText();
        return (text, page.GetMeanConfidence());
    }
    
    public void Dispose()
    {
        _engine.Dispose();
    }
}
