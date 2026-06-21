using CapCognition.Net;
using CapCognition.Net.Core.Processing.Common;
using CapCognition.Net.YoloModelDetection.Common;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

public class YoloModelDemo : IDisposable
{
    public bool PrepareProcessor(SKBitmap bitmap)
    {
        var recognizer = new Recognizer();

        var fruitsModelStream = new YoloModelDetectionRecognitionOptions.ModelStreamInfo(
            new FileStream("Models/fruits-yolo-model.onnx", FileMode.Open),
            "fruits-yolo-model");

        _options = new RecognitionOptionBuilder()
            .AddYoloModelDetectionRecognitionOption()
            .UseFittedImageForRecognition()
            .UseModel(fruitsModelStream)
            .ConfigureOption(options =>
            {
                //options.ClearModelCacheFolder();
                options.CreateAndLoadModel();
            })
            .Done()
            .Build();

        _resultDrawingOptions = new ResultDrawingOptionsBuilder()
            .AddYoloModelDetectionDrawingOptions()
            .DisplayObjectSurroundingBox()
            .Done()
            .Build();

        var success = recognizer.PrepareProcessorsAsync(bitmap.Width, bitmap.Height, _options).GetAwaiter().GetResult();
        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processors");
        }

        _recognizer = recognizer;
        return success;
    }

    public void Recognize(string recognizePath, string filenameWithoutExt, SKBitmap bitmap)
    {
        if (_recognizer == null)
        {
            Console.WriteLine($"Recognizer is not initialized for {recognizePath}");
            return;
        }

        Console.WriteLine($"Yolo model processing {recognizePath}");

        var result = _recognizer.ProcessImageAsync(bitmap, _resultDrawingOptions, disableBitmapDisposal: true).GetAwaiter().GetResult();
        if (result == null)
        {
            Console.WriteLine($"Failed to recognize image for {recognizePath}");
            return;
        }

        if (result.Results.Count == 0)
        {
            Console.WriteLine($"No elements could be recognized for {recognizePath}");
            return;
        }

        Console.WriteLine($"Recognized {result.Results.Count} fruits for {recognizePath}");
        foreach (var processorResult in result.Results)
        {
            var fruitResult = processorResult as RecognitionProcessorYoloModelDetectionResult;
            if (fruitResult == null)
            {
                continue;
            }
            Console.WriteLine($"Recognized fruit: {fruitResult.ObjectId} '{fruitResult.ObjectIdentifier}' with confidence: {fruitResult.Confidence}");
        }

        var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        var outputImg = Path.Combine(recognizePath, $"{filenameWithoutExt}_result.png");
        File.WriteAllBytes(outputImg, data.ToArray());
    }

    private Recognizer? _recognizer;
    private List<RecognitionOptions>? _options;
    private RecognitionProcessorResult.ResultDrawingOptions[]? _resultDrawingOptions;
    public void Dispose()
    {
        _recognizer?.Dispose();
        _recognizer = null;
        _options?.Dispose();
        _options = null;
    }
}