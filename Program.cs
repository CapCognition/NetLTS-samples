using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;
using CapCognition.Net.LicensePlateDetection.Common;
using CapCognitionNetLTS_Samples.OwnProcessor;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CapCognitionNetLTS_Samples;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var loggingFactory = BuildLoggerFactory();
        InitializeCapCognition(loggingFactory);

        var recognitionResultPath = GetRecognitionResultPath();
        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "recognize":
                RunFileRecognition(recognitionResultPath);
                break;

            case "stream":
                RunRtspStream(args);
                break;

            case "streamhls":
                RunStreamHls(args);
                break;

            case "ownrecognizer":
                RunOwnRecognizer(recognitionResultPath);
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

    private static void InitializeCapCognition(ILoggerFactory loggingFactory)
    {
        CapCognition.Net.Core.CapCognition.Initialize(
            loggingFactory,
            new[]
            {
                //Add your licenses here
                "",
            },
            //Need to be added so that the dynamically linked assemblies can be found by CapCognition: will hopefully be fixed in the future
            [
                BarcodeRecognition.Use,
                LicensePlateDetection.Use,
                VideoStreamCapturing.Use,
                OwnRecognition.Use,
            ]);

        CapCognition.Net.Core.CapCognition.EnableImageProcessingLogs = true;
        CaptureControl.Instance.InitializeAsync().GetAwaiter().GetResult();
    }

    private static string GetRecognitionResultPath()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(assemblyDirectory, "Resources");
    }

    private static void RunFileRecognition(string recognitionResultPath)
    {
        foreach (var file in Directory.GetFiles(recognitionResultPath))
        {
            if (file.Contains("_result.png"))
            {
                continue;
            }

            new FileRecognitionDemo().Recognize(
                recognizePath: file,
                useBarcodeDetection: true,
                useLicensePlateDetection: true,
                useParallelProcessing: true,
                downloadModelsFromInternet: false);
        }
    }

    private static void RunRtspStream(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Please provide the rtsp url");
            return;
        }

        var url = args[1];
        new StreamRecognitionDemo().Stream(url);
    }

    private static void RunStreamHls(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Please provide the rtsp url");
            return;
        }

        var url = args[1];
        var streamingDemo = new StreamHLSDemo();

        //create http streaming host
        var httpStreamingHost = new HttpStreamingHostDemo(streamingDemo.PublicRoutePath);
        httpStreamingHost.Start(args);

        //start streaming
        streamingDemo.Stream(url);

        //give stream time to start -> can be optimized with better java script while loading the playlist
        Thread.Sleep(TimeSpan.FromSeconds(5));

        //open demo web page in browser
        var demoWebPage = "http://localhost:5000/HLSDisplay.html";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(demoWebPage) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", demoWebPage);
        }
        else
        {
            Console.WriteLine("Cannot determine OS to open browser.");
        }

        //run for 30 seconds
        Thread.Sleep(TimeSpan.FromSeconds(30));

        //stop streaming
        streamingDemo.StopStream();
    }

    private static void RunOwnRecognizer(string recognitionResultPath)
    {
        foreach (var file in Directory.GetFiles(recognitionResultPath))
        {
            if (file.Contains("_result.png"))
            {
                continue;
            }
            new OwnProcessorDemo().Recognize(file);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Unknown or missing command");
        Console.WriteLine("Usage:");
        Console.WriteLine("  recognize              Process all images in the Resources folder");
        Console.WriteLine("  stream <rtspUrl>       Process RTSP stream");
        Console.WriteLine("  streamHLS <rtspUrl>    Process RTSP stream and expose as HLS");
        Console.WriteLine("  ownRecognizer          Run OwnProcessor demo on the Resources folder");
    }
}
