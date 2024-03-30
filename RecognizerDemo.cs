using CapCognition.BarcodeScanning.Net.Common;
using CapCognition.Core.Net.Processing.Common;
using CapCognition.LicensePlateDetection.Net.Common;
using CapCognition.Net;
using SkiaSharp;
using static CapCognition.Core.Net.Processing.Common.RecognitionProcessorResult;

namespace CapCognitionNetLTS_Samples;

public class RecognizerDemo
{
    public void Recognize(string recognizePath, bool useBarcodeDetection, bool useLicensePlateDetection, bool useParallelProcessing = true)
    {
        Console.WriteLine($"Preparing recognition processor for {recognizePath}");
        var bitmap = SKBitmap.Decode(recognizePath);

        var success = PrepareProcessor(out var recognizer, bitmap, useBarcodeDetection, useLicensePlateDetection);

        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processor for {recognizePath}");
            return;
        }
        
        Console.WriteLine($"Processing {recognizePath}");
        if (useParallelProcessing)
        {
            
            //used for parallel processing when multiple options are added to the recognizer, sequential also works but waits for each option to finish
            ProcessImageParallel(recognizer, bitmap, recognizePath);
        }
        else
        {
            ProcessImageSequentially(recognizer, bitmap, recognizePath);
        }
    }

    private void ProcessImageSequentially(Recognizer recognizer, SKBitmap bitmap, string resultPath)
    {
        var result = recognizer.ProcessImageAsync(bitmap, _resultDrawingOptions).GetAwaiter().GetResult();
        if (result == null)
        {
            Console.WriteLine("Failed to recognize image for {resultPath}");
            return;
        }

        if (result.Results.Count == 0)
        {
            Console.WriteLine("No elements could be recognized for {resultPath}");
            return;
        }

        Console.WriteLine($"Successfully recognized {result.Results.Count} elements for {resultPath}, Detections: {string.Join(", ", result.Results.Select(r => r.GetType().Name))}");


        var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        var outputImg = Path.Combine(Path.GetDirectoryName(resultPath)!, $"{Path.GetFileName(resultPath)}_sequential_result.png");
        File.WriteAllBytes(outputImg, data.ToArray());
    }

    private void ProcessImageParallel(Recognizer recognizer, SKBitmap bitmap, string resultPath)
    {
        //parallel processing
        var sem = new SemaphoreSlim(0, 1);

        recognizer.ProcessImageParallelAsync(bitmap,
            (resultBmp, result) =>
            {
                Console.WriteLine($"Successfully recognized {result.Results.Count} elements for {resultPath}, Detections: {string.Join(", ", result.Results.Select(r => r.GetType().Name))}");
                var data = resultBmp.Encode(SKEncodedImageFormat.Png, 90);
                var resultType = result.Results.FirstOrDefault()?.GetType().Name;
                var outputImg = Path.Combine(Path.GetDirectoryName(resultPath)!, $"{Path.GetFileName(resultPath)}_parallel_{resultType}_result.png");
                File.WriteAllBytes(outputImg, data.ToArray());

                sem.Release();
            },
            _resultDrawingOptions).GetAwaiter().GetResult();

        sem.Wait();
    }

    private bool PrepareProcessor(out Recognizer recognizer, SKBitmap bitmap, bool useBarcodeDetection, bool useLicensePlateDetection)
    {
        recognizer = new Recognizer();

        var options = new List<RecognitionOption>();
        if (useBarcodeDetection)
        {
            options.Add(_barcodeRecognitionOption);
        }
        if (useLicensePlateDetection)
        {
            options.Add(_licensePlateDetectionRecognitionOption);
        }
        
        var success = recognizer.PrepareProcessorsAsync(bitmap.Width, bitmap.Height, options).GetAwaiter().GetResult();
        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processors");
        }
        return success;
    }

    private readonly BarcodeRecognitionOption _barcodeRecognitionOption = new()
    {
        DisplayBarcodeText = true,
        EnableMultiCodeReader = true,
        EnableBarcodeOverlays = true,
        UseFastRecognition = false,
        BinarizerToUse = BarcodeRecognitionOption.BinarizerType.HistogrammBinarizer,
        BarcodeFormatsToRecognize =
        [
            BarcodeRecognitionOption.BarcodeFormat.QRCode
        ]
    };

    private readonly LicensePlateDetectionRecognitionOption _licensePlateDetectionRecognitionOption = new()
    {
        DisplayLicensePlateAsText = true,
        UseHighResolutionImageForRecognition = true,
        UseCroppedImageForRecognition = false,
        UseCudaProvider = false,
    };

    private readonly ResultDrawingOptions _resultDrawingOptions = new()
    {
        EnableResultDrawing = true,
        DrawBoundingBox = true,
        BoundingBoxColor = SKColors.Red,
        BoundingBoxStyle = SKPaintStyle.Stroke,
        BoundingBoxStrokeWidth = 2,
    };
}
