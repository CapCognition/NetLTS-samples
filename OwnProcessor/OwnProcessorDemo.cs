using SkiaSharp;

namespace CapCognitionNetLTS_Samples.OwnProcessor;

public class OwnProcessorDemo
{
    public void Recognize(string recognizePath, bool useParallelProcessing = true)
    {
        Console.WriteLine($"Preparing recognition processor for {recognizePath}");
        using var bitmap = SKBitmap.Decode(recognizePath);
        if (bitmap == null)
        {
            Console.WriteLine($"Cannot decode image: {recognizePath}");
            return;
        }
        using var processing = new BitmapProcessing();

        //Prepare the processor with the options
        var success = processing.PrepareProcessor(out var recognizer, bitmap, false, false, true);

        if (!success)
        {
            Console.WriteLine($"Failed to prepare recognition processor for {recognizePath}");
            return;
        }

        Console.WriteLine($"Processing {recognizePath}");
        if (useParallelProcessing)
        {
            //used for parallel processing when multiple options are added to the recognizer, sequential also works but waits for each option to finish
            processing.ProcessImageParallel(recognizer, bitmap, recognizePath);
        }
        else
        {
            //used for sequential processing when multiple options are added to the recognizer
            processing.ProcessImageSequentially(recognizer, bitmap, recognizePath);
        }
    }
}
