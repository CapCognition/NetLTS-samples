using CapCognition.Net;
using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.Core.Processing.Common;
using CapCognition.Net.LicensePlateDetection.Common;
using CapCognition.Net.LicensePlateDetection.Common.Extensions;
using CapCognitionNetLTS_Samples.OwnProcessor;
using SkiaSharp;
using System.Text;
using System.Collections.Concurrent;

namespace CapCognitionNetLTS_Samples;

public class BitmapProcessing : IDisposable
{
    /// <summary>
    /// Processes the image sequentially
    /// </summary>
    /// <param name="recognizer"></param>
    /// <param name="bitmap"></param>
    /// <param name="resultPath"></param>
    /// <returns>A list of results</returns>
    public List<string> ProcessImageSequentially(Recognizer recognizer, SKBitmap bitmap, string resultPath)
    {
        var resultList = new List<string>();
        var result = recognizer.ProcessImageAsync(bitmap, _resultDrawingOptions, disableBitmapDisposal: true).GetAwaiter().GetResult();
        if (result == null)
        {
            Console.WriteLine($"Failed to recognize image for {resultPath}");
            return resultList;
        }

        if (result.Results.Count == 0)
        {
            Console.WriteLine($"No elements could be recognized for {resultPath}");
            return resultList;
        }

        Console.WriteLine($"Successfully recognized {result.Results.Count} elements for {resultPath}, Detections: {string.Join(", ", result.Results.Select(r => r.GetType().Name))}");

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        var outputImg = Path.Combine(Path.GetDirectoryName(resultPath)!, $"{Path.GetFileName(resultPath)}_sequential_result.png");
        File.WriteAllBytes(outputImg, data.ToArray());

        AddResultsToList(resultList, result);
        return resultList;
    }

    /// <summary>
    /// Processes the image in parallel
    /// </summary>
    /// <param name="recognizer"></param>
    /// <param name="bitmap"></param>
    /// <param name="resultPath"></param>
    /// <returns>A list of results</returns>
    public List<string> ProcessImageParallel(Recognizer recognizer, SKBitmap bitmap, string resultPath)
    {
        var resultList = new List<string>();
        var deliveries = new ConcurrentQueue<(SKBitmap Bitmap, RecognitionResult Result)>();
        // The library owns the input in parallel mode. Keep the caller's bitmap alive.
        using var input = bitmap.Copy();
        try
        {
            recognizer.ProcessImageParallelAsync(input,
                (resultBmp, result) =>
                {
                    // Callbacks share the input bitmap. Copy it before the library disposes it,
                    // and draw/save afterwards so processors never read a bitmap being drawn on.
                    deliveries.Enqueue((resultBmp.Copy(), result));
                },
                resultDrawingOptions: null).GetAwaiter().GetResult();

            foreach (var (resultBmp, result) in deliveries)
            {
                Console.WriteLine($"Successfully recognized {result.Results.Count} elements for {resultPath}, Detections: {string.Join(", ", result.Results.Select(r => r.GetType().Name))}");
                recognizer.DrawResults(result, resultBmp, _resultDrawingOptions);
                using var data = resultBmp.Encode(SKEncodedImageFormat.Png, 90);
                var resultType = result.Results.FirstOrDefault()?.GetType().Name;
                var outputImg = Path.Combine(Path.GetDirectoryName(resultPath)!, $"{Path.GetFileName(resultPath)}_parallel_{resultType}_result.png");
                File.WriteAllBytes(outputImg, data.ToArray());

                AddResultsToList(resultList, result);
            }
        }
        finally
        {
            foreach (var delivery in deliveries)
            {
                delivery.Bitmap.Dispose();
            }
        }
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
    public bool PrepareProcessor(out Recognizer recognizer, SKBitmap bitmap, bool useBarcodeDetection = true, bool useLicensePlateDetection = true, bool useOwnProcessor = false, bool downloadModelsFromInternet = true, bool deliverPlatePictureInResult = false)
    {
        Dispose();
        recognizer = _recognizer = new Recognizer();

        var optionBuilder = new RecognitionOptionBuilder();
        // Build returns the builder's list; owning it now also covers setup failures.
        _options = optionBuilder.Build();
        var resultDrawingOptionBuilder = new ResultDrawingOptionsBuilder();

        if (useBarcodeDetection)
        {
            optionBuilder.AddBarcodeRecognitionOption()
                .EnableMultiCodeReader()
                .UseFastRecognition(false)
                .TryInverted()
                .SetBinarizer(BarcodeRecognitionOptions.BinarizerType.HybridBinarizer)
                .SetBarcodeFormats(new[]
                {
                    BarcodeRecognitionOptions.BarcodeFormat.QRCode,
                })
                .SetEncoding(Encoding.UTF8)
                .Done();

            resultDrawingOptionBuilder.AddBarcodeDrawingOptions()
                .DrawBoundingBox()
                .BoundingBoxColor(SKColors.Blue)
                .BoundingBoxStyle(SKPaintStyle.Stroke)
                .BoundingBoxStrokeWidth(3f)
                .Done();
        }

        if (useLicensePlateDetection)
        {
            var lprOptionBuilder = optionBuilder.AddLicensePlateDetectionRecognitionOption()
                .UseCudaProvider(false)
                .DeliverPlatePictureInResult(deliverPlatePictureInResult)
                .UseCroppedImageForRecognition()
                .DoAutomaticDetectionOptimization()
                .SetRecognitionQuality(LicensePlateDetectionRecognitionOptionBuilder.RecognitionQuality.Medium);

            resultDrawingOptionBuilder.AddLicensePlateDetectionDrawingOptions()
                .DisplayVehicleSurroundingBox()
                .VehicleSurroundingRectColor(new SKColor(0, 255, 0, 40))
                .VehicleSurroundingRectStyle(SKPaintStyle.Fill)
                .VehicleSurroundingRectStrokeWidth(1f)
                .LicensePlateSurroundingRectColor(SKColors.Green)
                .LicensePlateSurroundingRectStyle(SKPaintStyle.Stroke)
                .LicensePlateSurroundingRectStrokeWidth(2f)
                .LicensePlateNonValidatedSurroundingRectColor(SKColors.Red)
                .Done();

            if (downloadModelsFromInternet)
            {
                lprOptionBuilder.SetRecognitionQuality(LicensePlateDetectionRecognitionOptionBuilder.RecognitionQuality.Medium);

                //use one of the following recognition modes:
                lprOptionBuilder.UseRecognitionModeNoCountryLicencePlateThenVehicle();
                //lprOptionBuilder.UseRecognitionModeCountryLicencePlateThenVehicle();
                //lprOptionBuilder.UseRecognitionModeVehicleOnly();
                //lprOptionBuilder.UseRecognitionModeVehicleThenCountryLicencePlate();
            }
            else
            {
                var streams = lprOptionBuilder
                    .UseLocalModelsForRecognitionMode(LicensePlateDetectionRecognitionOptions.RecognitionModeType.CountryLicencePlateThenVehicle)
                    .SetLocalModelBasePath(Path.Combine(AppContext.BaseDirectory, "Models"));
                try
                {
                    streams.UseLocalLicensePlateModelFromType(LPModelType.LPModelType640n)
                        .UseLocalLicensePlateTextModelFromType(LPTextModelType.LPTextModelType640n)
                        .UseLocalVehicleModelFromType(VehicleModelType.VehicleModelType640n)
                        .Done();
                }
                catch
                {
                    // The builder transfers stream ownership to the options only at Done().
                    streams.LpModelStream?.Dispose();
                    streams.LpTextModelStream?.Dispose();
                    streams.LpWideTextModelStream?.Dispose();
                    streams.VehicleModelStream?.Dispose();
                    throw;
                }
            }

            // PrepareProcessorsAsync loads models and reports preparation failures.
            lprOptionBuilder.Done();
        }

        _options = optionBuilder.Build();
        if (useOwnProcessor)
        {
            _options.Add(new OwnProcessorOption
            {
                EnableOwnOption = true,
                OwnOption = OwnProcessorOption.OwnOptionEnum.OwnOption1
            });
        }

        _resultDrawingOptions = resultDrawingOptionBuilder.Build();

        var success = recognizer.PrepareProcessorsAsync(bitmap.Width, bitmap.Height, _options).GetAwaiter().GetResult();
        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processors");
        }
        return success;
    }

    private static void AddResultsToList(List<string> resultList, RecognitionResult result)
    {
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
                var plateNumber = $"Type:{licensePlateResult.VehicleType} Country:{licensePlateResult.PlateCountryCode} Plate:{licensePlateResult.PlateNumberValidated ?? licensePlateResult.PlateNumberRaw}";
                resultList.Add(plateNumber);
            }
        }
    }

    private RecognitionProcessorResult.ResultDrawingOptions[]? _resultDrawingOptions;
    private List<RecognitionOptions>? _options;
    private Recognizer? _recognizer;

    public void Dispose()
    {
        try
        {
            _recognizer?.Dispose();
        }
        finally
        {
            _recognizer = null;
            _options?.Dispose();
            _options = null;
            _resultDrawingOptions = null;
        }
    }
}
