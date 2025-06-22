using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;
using CapCognition.Net.LicensePlateDetection.Common;
using CapCognitionNetLTS_Samples.OwnProcessor;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CapCognitionNetLTS_Samples;

public class Program
{
    public static void Main(string[] args)
    {
        var loggingFactory = LoggerFactory.Create((builder) =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
        });

        CapCognition.Net.Core.CapCognition.Initialize(
            loggingFactory,
            new[]
            {
                //Add your licenses here
                "",
            },
            //Need to be added so that the dynamically linked assemblies can be found by CapCognition: will hopefully be fixed in the future
            [
                new BarcodeRecognitionOption(),
                new LicensePlateDetectionRecognitionOption(),
                new OwnProcessorOption(),
                new VideoStreamCaptureSourceOptions()
            ]);

        //Enable/Disable logs
        CapCognition.Net.Core.CapCognition.EnableImageProcessingLogs = true;

        CaptureControl.Instance.InitializeAsync().GetAwaiter().GetResult();
        var recognitionResultPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources");

        if (args[0] == "recognize")
        {
            //Get path to the local image, not in bin folder but in the project folder
            
            // for all files in the folder
            foreach (var file in Directory.GetFiles(recognitionResultPath))
            {
                if (file.Contains("_result.png"))
                {
                    continue;
                }
                //Recognize the image with barcode and license plate detection
                new FileRecognitionDemo().Recognize(
                    recognizePath: file,
                    useBarcodeDetection: true,
                    useLicensePlateDetection: true,
                    useParallelProcessing: true,
                    downloadModelsFromInternet: false);
            }
            return;
        }
        if (args[0] == "stream")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please provide the rtsp url");
                return;
            }
            var url = args[1];
            new StreamRecognitionDemo().Stream(url);
            return;
        }
        if (args[0] == "streamHLS")
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

            return;
        }
        if (args[0] == "ownRecognizer")
        {
            foreach (var file in Directory.GetFiles(recognitionResultPath))
            {
                if (file.Contains("_result.png"))
                {
                    continue;
                }
                new OwnProcessorDemo().Recognize(file);
            }
            return;
        }

        Console.WriteLine("Unknown command");
    }
}
