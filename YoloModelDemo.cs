using CapCognition.Net;
using CapCognition.Net.Core.Processing.Common;
using CapCognition.Net.YoloModelDetection.Common;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

public class YoloModelDemo : IDisposable
{
    public bool PrepareProcessor(SKBitmap bitmap)
    {
        Dispose();
        var recognizer = _recognizer = new Recognizer();

        var fruitsModelStream = new YoloModelDetectionRecognitionOptions.ModelStreamInfo(
            File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Models", "fruits-yolo-model.onnx")),
            "fruits-yolo-model");

        _modelStream = fruitsModelStream;

        _options = new RecognitionOptionBuilder()
            .AddYoloModelDetectionRecognitionOption()
            .UseFittedImageForRecognition()
            .UseModel(fruitsModelStream)
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

        _prepared = success;
        return success;
    }

    public void Recognize(string recognizePath, string filenameWithoutExt, SKBitmap bitmap)
    {
        if (_recognizer == null || !_prepared)
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

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        var outputImg = Path.Combine(recognizePath, $"{filenameWithoutExt}_result.png");
        File.WriteAllBytes(outputImg, data.ToArray());
    }

    private Recognizer? _recognizer;
    private bool _prepared;
    private YoloModelDetectionRecognitionOptions.ModelStreamInfo? _modelStream;
    private List<RecognitionOptions>? _options;
    private RecognitionProcessorResult.ResultDrawingOptions[]? _resultDrawingOptions;
    public void Dispose()
    {
        try
        {
            _recognizer?.Dispose();
        }
        finally
        {
            _recognizer = null;
            _prepared = false;
            if (_options != null)
            {
                _options.Dispose();
            }
            else
            {
                _modelStream?.Dispose();
            }
            _options = null;
            _modelStream = null;
            _resultDrawingOptions = null;
        }
    }
}
