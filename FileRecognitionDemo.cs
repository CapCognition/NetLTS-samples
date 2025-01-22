using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

public class FileRecognitionDemo
{
    public void Recognize(string recognizePath, bool useBarcodeDetection, bool useLicensePlateDetection, bool useParallelProcessing = true, bool downloadModelsFromInternet = true)
    {
        Console.WriteLine($"Preparing recognition processor for {recognizePath}");
        var bitmap = SKBitmap.Decode(recognizePath);

        //Note: For demonstration reasons, the recognizer will be instantiated for every picture.
        //In a real-world scenario, the recognizer should be reused for performance reasons and just being reinitialized when the picture resolution changes

        //Prepare the processor with the options
        var success = BitmapProcessing.PrepareProcessor(
            recognizer: out var recognizer,
            bitmap: bitmap,
            useBarcodeDetection: useBarcodeDetection, 
            useLicensePlateDetection: useLicensePlateDetection,
            useOwnProcessor: false,
            downloadModelsFromInternet: downloadModelsFromInternet);

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
