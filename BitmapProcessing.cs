using CapCognition.Net;
using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.Core.Processing.Common;
using CapCognition.Net.LicensePlateDetection.Common;
using CapCognitionNetLTS_Samples.OwnProcessor;
using SkiaSharp;
using System.Text;

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
        var result = recognizer.ProcessImageAsync(bitmap, _resultDrawingOptions).GetAwaiter().GetResult();
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
            _resultDrawingOptions).GetAwaiter().GetResult();

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
    public static bool PrepareProcessor(out Recognizer recognizer, SKBitmap bitmap, bool useBarcodeDetection = true, bool useLicensePlateDetection = true, bool useOwnProcessor = false, bool downloadModelsFromInternet = true)
    {
        if (_options != null)
        {
            _options.Dispose();
            _options = null;
        }
        recognizer = new Recognizer();

        var optionBuilder = new RecognitionOptionBuilder();
        var resultDrawingOptionBuilder = new ResultDrawingOptionsBuilder();

        if (useBarcodeDetection)
        {
            optionBuilder.AddBarcodeRecognitionOption()
                .EnableMultiCodeReader()
                .UseFastRecognition(false)
                .TryInverted()
                .SetBinarizer(BarcodeRecognitionOptions.BinarizerType.HybridBinarizer)
                .SetBarcodeFormats(new[] { BarcodeRecognitionOptions.BarcodeFormat.QRCode })
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
                .UseCroppedImageForRecognition()
                .DoAutomaticDetectionOptimization()
                .SetRecognitionQuality(LicensePlateDetectionRecognitionOptionBuilder.RecognitionQuality.Medium)
                .ConfigureOption(option =>
                {
                    //option.ClearModelCacheFolder();
                    //option.ClearOldModelsFromCacheFolder();
                });

            resultDrawingOptionBuilder.AddLicensePlateDetectionDrawingOptions()
                .DisplayVehicleSurroundingBox(true)
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
                //use one of the following recognition modes:

                //lprOptionBuilder.UseRecognitionModeCountryLicencePlateThenVehicle();
                //lprOptionBuilder.UseRecognitionModeNoCountryLicencePlateThenVehicle();

                //lprOptionBuilder.UseRecognitionModeVehicleOnly();
                //lprOptionBuilder.UseRecognitionModeVehicleThenCountryLicencePlate();
            }
            else
            {
                var licensePlateModelFileName = "Models/" + LicensePlateDetectionConstants.LicensePlateModelFileName640N + ".ccml";
                var noCountryLicensePlateModelFileName = "Models/" + LicensePlateDetectionConstants.LicensePlateNoCountryModelFileName640N + ".ccml";
                var textModelFileName = "Models/" + LicensePlateDetectionConstants.TextModelFileName640N + ".ccml";
                var vehicleModelFileName = "Models/" + LicensePlateDetectionConstants.VehicleModelFileName640N + ".ccml";


                //please choose one of the following recognition modes for demonstration:

                var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.CountryLicencePlateThenVehicle;
                //var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.CountryLicencePlateOnly;
                //var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.VehicleOnly;
                //var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.VehicleThenCountryLicencePlate;
                //var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.NoCountryLicencePlateThenVehicle;
                //var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.NoCountryLicencePlateOnly;
                //var demoRecognitionMode = LicensePlateDetectionRecognitionOptions.RecognitionModeType.VehicleThenNoCountryLicencePlate;

                //----------------- Country licence plate then vehicle ---------------------
                if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.CountryLicencePlateThenVehicle)
                {
                    var licensePlateModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(
                        new FileStream(licensePlateModelFileName, FileMode.Open), licensePlateModelFileName);
                    var textModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(textModelFileName, FileMode.Open), textModelFileName);
                    var vehicleModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(vehicleModelFileName, FileMode.Open), vehicleModelFileName);
                    
                    lprOptionBuilder.UseRecognitionModeCountryLicencePlateThenVehicle(licensePlateModelStream, textModelStream, vehicleModelStream);
                }
                //----------------- licence plate only ---------------------
                else if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.CountryLicencePlateOnly)
                {
                    var licensePlateModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(
                        new FileStream(licensePlateModelFileName, FileMode.Open),licensePlateModelFileName);
                    var textModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(textModelFileName, FileMode.Open), textModelFileName);
                    lprOptionBuilder.UseRecognitionModeCountryLicencePlateOnly(licensePlateModelStream, textModelStream);
                }
                //----------------- vehicle only---------------------
                else if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.VehicleOnly)
                {
                    var vehicleModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(vehicleModelFileName, FileMode.Open), vehicleModelFileName);

                    lprOptionBuilder.UseRecognitionModeVehicleOnly(vehicleModelStream);
                }
                //----------------- Vehicle then country licence plate ---------------------
                else if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.VehicleThenCountryLicencePlate)
                {
                    var licensePlateModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(licensePlateModelFileName, FileMode.Open), licensePlateModelFileName);
                    var textModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(textModelFileName, FileMode.Open), textModelFileName);
                    var vehicleModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(vehicleModelFileName, FileMode.Open), vehicleModelFileName);

                    lprOptionBuilder.UseRecognitionModeVehicleThenCountryLicencePlate(licensePlateModelStream, textModelStream, vehicleModelStream);
                }
                //----------------- licence plate then vehicle ---------------------
                else if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.NoCountryLicencePlateThenVehicle)
                {
                    var noCountryLicensePlateModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(noCountryLicensePlateModelFileName, FileMode.Open), licensePlateModelFileName);
                    var textModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(textModelFileName, FileMode.Open), textModelFileName);
                    var vehicleModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(vehicleModelFileName, FileMode.Open), vehicleModelFileName);

                    lprOptionBuilder.UseRecognitionModeNoCountryLicencePlateThenVehicle(noCountryLicensePlateModelStream, textModelStream, vehicleModelStream);
                }
                //----------------- licence plate then vehicle ---------------------
                else if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.NoCountryLicencePlateOnly)
                {
                    var noCountryLicensePlateModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(noCountryLicensePlateModelFileName, FileMode.Open), licensePlateModelFileName);
                    var textModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(textModelFileName, FileMode.Open), textModelFileName);

                    lprOptionBuilder.UseRecognitionModeNoCountryLicencePlateOnly(noCountryLicensePlateModelStream, textModelStream);
                }
                //----------------- Vehicle then licence plate ---------------------
                else if (demoRecognitionMode == LicensePlateDetectionRecognitionOptions.RecognitionModeType.VehicleThenNoCountryLicencePlate)
                {
                    var noCountryLicensePlateModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(noCountryLicensePlateModelFileName, FileMode.Open), licensePlateModelFileName);
                    var textModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(textModelFileName, FileMode.Open), textModelFileName);
                    var vehicleModelStream = new LicensePlateDetectionRecognitionOptions.StreamInfo(new FileStream(vehicleModelFileName, FileMode.Open), vehicleModelFileName);

                    lprOptionBuilder.UseRecognitionModeVehicleThenNoCountryLicencePlate(noCountryLicensePlateModelStream, textModelStream, vehicleModelStream);
                }
            }

            lprOptionBuilder.ConfigureOption(option =>
            {
                option.CreateAndPrepareModelsAsync().GetAwaiter().GetResult();
            });

            lprOptionBuilder.Done();
        }

        _options = optionBuilder.Build();
        if (useOwnProcessor)
        {
            _options.Add(OwnProcessorOption);
        }

        _resultDrawingOptions = resultDrawingOptionBuilder.Build();

        var success = recognizer.PrepareProcessorsAsync(bitmap.Width, bitmap.Height, _options).GetAwaiter().GetResult();
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
                var plateNumber = $"Type:{licensePlateResult.VehicleType} Country:{licensePlateResult.PlateCountryCode} Plate:{licensePlateResult.PlateNumberValidated ?? licensePlateResult.PlateNumberRaw}";
                resultList.Add(plateNumber);
            }
        }
    }

    private static RecognitionProcessorResult.ResultDrawingOptions[]? _resultDrawingOptions;
    private static List<RecognitionOptions>? _options;

    private static readonly OwnProcessorOption OwnProcessorOption = new()
    {
        EnableOwnOption = true,
        OwnOption = OwnProcessorOption.OwnOptionEnum.OwnOption1
    };
}
