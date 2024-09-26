using CapCognition.Net;
using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.Core.Processing.Common;
using CapCognition.Net.LicensePlateDetection.Common;
using CapCognitionNetLTS_Samples.OwnProcessor;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

public class BitmapProcessing
{
    /// <summary>
    /// Processes the image sequentially
    /// </summary>
    /// <param name="recognizer"></param>
    /// <param name="bitmap"></param>
    /// <param name="resultPath"></param>
    /// <returns>A list of results</returns>
    public static List<string> ProcessImageSequentially(Recognizer recognizer, SKBitmap bitmap, string resultPath)
    {
        var resultList = new List<string>();
        var result = recognizer.ProcessImageAsync(bitmap, ResultDrawingOptions).GetAwaiter().GetResult();
        if (result == null)
        {
            Console.WriteLine("Failed to recognize image for {resultPath}");
            return resultList;
        }

        if (result.Results.Count == 0)
        {
            Console.WriteLine("No elements could be recognized for {resultPath}");
            return resultList;
        }

        Console.WriteLine($"Successfully recognized {result.Results.Count} elements for {resultPath}, Detections: {string.Join(", ", result.Results.Select(r => r.GetType().Name))}");

        var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        var outputImg = Path.Combine(Path.GetDirectoryName(resultPath)!, $"{Path.GetFileName(resultPath)}_sequential_result.png");
        File.WriteAllBytes(outputImg, data.ToArray());

        AddResultsToList(out resultList, result);
        return resultList;
    }

    /// <summary>
    /// Processes the image in parallel
    /// </summary>
    /// <param name="recognizer"></param>
    /// <param name="bitmap"></param>
    /// <param name="resultPath"></param>
    /// <returns>A list of results</returns>
    public static List<string> ProcessImageParallel(Recognizer recognizer, SKBitmap bitmap, string resultPath)
    {
        //parallel processing
        var sem = new SemaphoreSlim(0, 1);
        var resultList = new List<string>();
        recognizer.ProcessImageParallelAsync(bitmap,
            (resultBmp, result) =>
            {
                Console.WriteLine($"Successfully recognized {result.Results.Count} elements for {resultPath}, Detections: {string.Join(", ", result.Results.Select(r => r.GetType().Name))}");
                var data = resultBmp.Encode(SKEncodedImageFormat.Png, 90);
                var resultType = result.Results.FirstOrDefault()?.GetType().Name;
                var outputImg = Path.Combine(Path.GetDirectoryName(resultPath)!, $"{Path.GetFileName(resultPath)}_parallel_{resultType}_result.png");
                File.WriteAllBytes(outputImg, data.ToArray());

                AddResultsToList(out resultList, result);
                sem.Release();
            },
            ResultDrawingOptions).GetAwaiter().GetResult();

        sem.Wait();
        return resultList;
    }

    /// <summary>
    /// Prepares the processor for the image recognition
    /// </summary>
    /// <param name="recognizer"></param>
    /// <param name="bitmap"></param>
    /// <param name="useBarcodeDetection"></param>
    /// <param name="useLicensePlateDetection"></param>
    /// <returns></returns>
    public static bool PrepareProcessor(out Recognizer recognizer, SKBitmap bitmap, bool useBarcodeDetection = true, bool useLicensePlateDetection = true, bool useOwnProcessor = false)
    {
        recognizer = new Recognizer();

        var options = new List<RecognitionOption>();
        if (useBarcodeDetection)
        {
            options.Add(BarcodeRecognitionOption);
        }
        if (useLicensePlateDetection)
        {
            options.Add(LicensePlateDetectionRecognitionOption);
        }
        if (useOwnProcessor)
        {
            options.Add(OwnProcessorOption);
        }

        var success = recognizer.PrepareProcessorsAsync(bitmap.Width, bitmap.Height, options).GetAwaiter().GetResult();
        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processors");
        }
        return success;
    }

    private static void AddResultsToList(out List<string> resultList, RecognitionResult result)
    {
        resultList = new List<string>();
        foreach (var res in result.Results)
        {
            if (res is RecognitionProcessorBarcodeResult barcodeResult)
            {
                if (barcodeResult.Value == null)
                {
                    continue;
                }
                resultList.Add(barcodeResult.Value);
            }
            else if (res is RecognitionProcessorLicensePlateDetectionResult licensePlateResult)
            {
                if (licensePlateResult.PlateNumber == null)
                {
                    continue;
                }
                resultList.Add(licensePlateResult.PlateNumber);
            }
        }
    }

    private static readonly BarcodeRecognitionOption BarcodeRecognitionOption = new()
    {
        EnableMultiCodeReader = true,
        UseFastRecognition = false,
        BinarizerToUse = BarcodeRecognitionOption.BinarizerType.HistogrammBinarizer,
        BarcodeFormatsToRecognize =
        [
            BarcodeRecognitionOption.BarcodeFormat.QRCode
        ]
    };

    private static readonly LicensePlateDetectionRecognitionOption LicensePlateDetectionRecognitionOption = new()
    {
        UseCroppedImageForRecognition = false,
        UseCudaProvider = false,
    };

    private static readonly RecognitionProcessorResult.ResultDrawingOptions ResultDrawingOptions = new()
    {
        EnableResultDrawing = true,
        DrawBoundingBox = true,
        BoundingBoxColor = SKColors.Red,
        BoundingBoxStyle = SKPaintStyle.Stroke,
        BoundingBoxStrokeWidth = 2,
    };

    private static readonly OwnProcessorOption OwnProcessorOption = new()
    {
        EnableOwnOption = true,
        OwnOption = OwnProcessorOption.OwnOptionEnum.OwnOption1
    };
}
