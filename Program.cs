using CapCognition.Net.BarcodeScanning.Processor;
using CapCognition.Net.CaptureSources;
using CapCognition.Net.Core.Capture;
using CapCognition.Net.LicensePlateDetection.Processor;
using CapCognition.Net.YoloModelDetection.Processor;
using CapCognitionNetLTS_Samples.OwnProcessor;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var command = args[0].ToLowerInvariant();
        if (command is not ("recognize" or "stream" or "streamhls" or "ownrecognizer" or "yolomodel"))
        {
            PrintUsage();
            return;
        }
        if ((command is "stream" or "streamhls")
            && (args.Length < 2 || !Uri.TryCreate(args[1], UriKind.Absolute, out _)))
        {
            Console.WriteLine("Please provide a valid absolute stream URL");
            return;
        }

        using var loggingFactory = BuildLoggerFactory();
        await InitializeCapCognitionAsync(loggingFactory, command is "stream" or "streamhls");

        var recognitionResultPath = GetRecognitionResultPath();

        switch (command)
        {
            case "recognize":
                RunFileRecognition(recognitionResultPath);
                break;

            case "stream":
                await RunRtspStream(args);
                break;

            case "streamhls":
                await RunStreamHls(args);
                break;

            case "ownrecognizer":
                RunOwnRecognizer(recognitionResultPath);
                break;

            case "yolomodel":
                RunYoloModel(recognitionResultPath);
                break;

            default:
                PrintUsage();
                break;
        }
    }

    private static ILoggerFactory BuildLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
        });
    }

    private static async Task InitializeCapCognitionAsync(ILoggerFactory loggingFactory, bool initializeCapture)
    {
        CapCognition.Net.Core.CapCognition.Initialize(
            loggingFactory,
            [
                VideoStreamCapturing.Use(/* Here comes your license */),
                BarcodeRecognition.Use(/* Here comes your license */),
                LicensePlateDetection.Use(/* Here comes your license */),
                YoloModelDetection.Use(/* Here comes your license */),
                YourOwnRecognizer.Use(/* Here comes your license */),
            ]);

        CapCognition.Net.Core.CapCognition.EnableImageProcessingLogs = true;
        if (!CapCognition.Net.Core.CapCognition.Initialized)
        {
            throw new InvalidOperationException("Failed to initialize CapCognition");
        }
        if (initializeCapture)
        {
            await CaptureControl.Instance.InitializeAsync();
        }
    }

    private static string GetRecognitionResultPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Resources");
    }

    private static void RunFileRecognition(string recognitionResultPath)
    {
        foreach (var file in GetInputImages(recognitionResultPath))
        {
            new FileRecognitionDemo().Recognize(
                recognizePath: file,
                useBarcodeDetection: true,
                useLicensePlateDetection: true,
                useParallelProcessing: true,
                downloadModelsFromInternet: false);
        }
    }

    private static async Task RunRtspStream(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Please provide the rtsp url");
            return;
        }

        var url = args[1];
        await new StreamRecognitionDemo().StreamAsync(url);
    }

    private static Task RunStreamHls(string[] args)
    {
        return new StreamHLSDemo().StreamAsync(args);
    }

    private static void RunOwnRecognizer(string recognitionResultPath)
    {
        foreach (var file in GetInputImages(recognitionResultPath))
        {
            new OwnProcessorDemo().Recognize(file);
        }
    }

    private static void RunYoloModel(string recognitionResultPath)
    {
        foreach (var file in GetInputImages(recognitionResultPath))
        {
            using var bitmap = SKBitmap.Decode(file);
            if (bitmap == null)
            {
                Console.WriteLine($"Cannot decode image: {file}");
                continue;
            }
            using var demo = new YoloModelDemo();
            //prepare processor with the image, so that the model is loaded before processing the next images
            //because the images can have different sizes, the processor needs to be prepared with an image before recognition, so that the model is loaded and ready for the next images.
            //If the images have the same size, the processor can be prepared with the first image and then all images can be processed without preparing the processor again.
            if (!demo.PrepareProcessor(bitmap))
            {
                continue;
            }
            demo.Recognize(recognitionResultPath, Path.GetFileNameWithoutExtension(file), bitmap);
        }
    }

    private static IEnumerable<string> GetInputImages(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Image directory does not exist: {directory}");
            return Array.Empty<string>();
        }
        return Directory.GetFiles(directory).Where(file =>
            !Path.GetFileName(file).EndsWith("_result.png", StringComparison.OrdinalIgnoreCase)
            && Path.GetExtension(file).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Unknown or missing command");
        Console.WriteLine("Usage:");
        Console.WriteLine("  recognize              Process all images in the Resources folder");
        Console.WriteLine("  stream <rtspUrl>       Process RTSP stream");
        Console.WriteLine("  streamHLS <rtspUrl>    Process RTSP stream and expose as HLS");
        Console.WriteLine("  ownRecognizer          Run OwnProcessor demo on the Resources folder");
        Console.WriteLine("  yoloModel              Recognize fruits using the local YOLO model");
    }
}
