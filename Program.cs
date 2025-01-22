using System.Reflection;
using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;
using CapCognition.Net.LicensePlateDetection.Common;
using CapCognitionNetLTS_Samples.OwnProcessor;
using Microsoft.Extensions.Logging;

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
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources");

        if (args[0] == "recognize")
        {
            //Get path to the local image, not in bin folder but in the project folder
            
            // for all files in the folder
            foreach (var file in Directory.GetFiles(path))
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
        if (args[0] == "ownRecognizer")
        {
            foreach (var file in Directory.GetFiles(path))
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
