using CapCognition.Net.Core;
using CapCognition.Net.Core.Processing;
using CapCognition.Net.LicensePlateDetection.Processor;

//Use the CapcognitionExtension attribute so the initializer can find the processor
[assembly: CapcognitionExtension(typeof(CapcognitionLicensePlateDetection))]
namespace CapCognitionNetLTS_Samples.OwnProcessor;

public class OwnProcessorInitializer : CapCognition.Net.Core.Processing.IRecognitionProcessExtension
{
    public void Initialize()
    {
        GraphicProcessorControl.RegisterRecognitionProcessor(typeof(OwnProcessorOption), () => new OwnProcessor());
    }
}