using System.Reflection;
using CapCognition.Net.BarcodeScanning.Common;
using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;
using CapCognition.Net.LicensePlateDetection.Common;

namespace CapCognitionNetLTS_Samples;

public class Program
{
    public static void Main(string[] args)
    {
        CapCognition.Net.Core.CapCognition.Initialize(new[]
        {
            //Add your licenses here
            "",
        },
        [
            new BarcodeRecognitionOption(),
            new LicensePlateDetectionRecognitionOption(),
            new VideoStreamCaptureSourceOptions()
        ]);

        //Enable logs
        CapCognition.Net.Core.CapCognition.EnableImageProcessingLogs = false;

        CaptureControl.Instance.InitializeAsync().GetAwaiter().GetResult();
        //Get path to the local image, not in bin folder but in the project folder
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources");

        if (args[0] == "recognize")
        {
            // for all files in the folder
            foreach (var file in Directory.GetFiles(path))
            {
                if (file.Contains("_result.png"))
                {
                    continue;
                }
                new RecognizerDemo().Recognize(file, true, true, true);
            }
            return;
        }
        if (args[0] == "stream")
        {
            //Stream(args);
            return;
        }

        Console.WriteLine("Unknown command");
    }
}
