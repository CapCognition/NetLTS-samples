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
        if (command is "help" or "--help" or "-h" or "/?")
        {
            PrintUsage();
            return;
        }
        if (command is not ("recognize" or "stream" or "streamhls" or "streamtrack" or "ownrecognizer" or "yolomodel"))
        {
            Console.WriteLine($"Unknown command: {args[0]}");
            PrintUsage();
            return;
        }
        if ((command is "stream" or "streamhls" or "streamtrack")
            && (args.Length < 2 || !Uri.TryCreate(args[1], UriKind.Absolute, out _)))
        {
            Console.WriteLine("Please provide a valid absolute stream URL");
            PrintUsage();
            return;
        }

        var trackingDuration = TimeSpan.FromSeconds(60);
        if (command == "streamtrack")
        {
            if (args.Length > 3 || (args.Length == 3
                && (!int.TryParse(args[2], out var seconds) || seconds is < 1 or > 86400)))
            {
                Console.WriteLine("streamTrack expects an RTSP URL and an optional duration from 1 to 86400 seconds.");
                PrintUsage();
                return;
            }
            if (!Uri.TryCreate(args[1], UriKind.Absolute, out var trackingUrl)
                || trackingUrl.Scheme is not ("rtsp" or "rtsps") || string.IsNullOrEmpty(trackingUrl.Host))
            {
                Console.WriteLine("Please provide a valid RTSP URL");
                PrintUsage();
                return;
            }
            if (args.Length == 3)
            {
                trackingDuration = TimeSpan.FromSeconds(int.Parse(args[2]));
            }
        }

        using var loggingFactory = BuildLoggerFactory();
        await InitializeCapCognitionAsync(loggingFactory, command is "stream" or "streamhls" or "streamtrack");

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

            case "streamtrack":
                await new StreamTrackingDemo().StreamAndTrackAsync(new Uri(args[1]), trackingDuration);
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
        Console.WriteLine("""
            CapCognition .NET Samples

            Usage (from the sample project directory):
              dotnet run -- <command> [parameters]

            Commands:
              recognize                              Recognize barcodes and license plates in Resources
              stream <rtspUrl>                       Recognize RTSP frames for 10 seconds
              streamHLS <rtspUrl>                    Stream RTSP to the HLS browser player
              streamTrack <rtspUrl> [durationSeconds] Track license plates in ParkingOfficer mode
              ownRecognizer                          Process Resources with the custom processor
              yoloModel                              Recognize fruits using the local YOLO model
              help, --help, -h, /?                   Show this help

            Parameter legend:
              <parameter>       Required; replace the placeholder without angle brackets.
              [parameter]       Optional; omit it to use the default value.
              rtspUrl           Camera URL: rtsp://[username:password@]host:port/stream
                                Quote the complete URL. Credentials depend on the camera.
                                streamTrack accepts rtsp:// and rtsps:// URLs.
              durationSeconds   Integer from 1 to 86400; only for streamTrack. Default: 60.
              Commands are case-insensitive.

            Keyboard controls:
              streamTrack       n = next vehicle; q or Ctrl+C = stop
                                A confirmed plate remains locked until n is pressed.
              streamHLS         q or Ctrl+C = stop
              With redirected input, use Ctrl+C or the streamTrack duration to stop.

            Files and browser:
              Resources and Models are located beside the application binaries.
              Tracking result images: Resources/TrackingResults
              HLS player: http://localhost:5000/HLSDisplay.html
                          https://localhost:5001/HLSDisplay.html

            Examples:
              dotnet run -- recognize
              dotnet run -- stream "rtsp://user:password@192.168.1.100:554/stream"
              dotnet run -- streamHLS "rtsp://user:password@192.168.1.100:554/stream"
              dotnet run -- streamTrack "rtsp://user:password@192.168.1.100:554/stream" 120
              dotnet run -- streamTrack "rtsp://192.168.1.100:554/stream"
              dotnet run -- ownRecognizer
              dotnet run -- yoloModel
              dotnet run -- --help
            """);
    }
}
