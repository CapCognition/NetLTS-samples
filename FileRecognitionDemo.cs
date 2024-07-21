using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

public class FileRecognitionDemo
{
    public void Recognize(string recognizePath, bool useBarcodeDetection, bool useLicensePlateDetection, bool useParallelProcessing = true)
    {
        Console.WriteLine($"Preparing recognition processor for {recognizePath}");
        var bitmap = SKBitmap.Decode(recognizePath);

        //Prepare the processor with the options
        var success = BitmapProcessing.PrepareProcessor(out var recognizer, bitmap, useBarcodeDetection, useLicensePlateDetection);

        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processor for {recognizePath}");
            return;
        }
        
        Console.WriteLine($"Processing {recognizePath}");
        if (useParallelProcessing)
        {

            //used for parallel processing when multiple options are added to the recognizer, sequential also works but waits for each option to finish
            BitmapProcessing.ProcessImageParallel(recognizer, bitmap, recognizePath);
        }
        else
        {
            //used for sequential processing when multiple options are added to the recognizer
            BitmapProcessing.ProcessImageSequentially(recognizer, bitmap, recognizePath);
        }
    }
}
