using CapCognition.Net.Core.Processing.Common;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples.OwnProcessor;

public class OwnProcessorResult : RecognitionProcessorResult
{
    public override void DrawResult(SKCanvas canvas, ResultDrawingOptions[]? resultDrawingOptions)
    {
        var options = resultDrawingOptions?.FirstOrDefault(o => o.GetType() == typeof(OwnProcessorDrawingOptions)) as OwnProcessorDrawingOptions;
        if (options == null)
        {
            return;
        }

        //Here you can implement your own drawing logic based on the recognition result and drawing options
    }
}