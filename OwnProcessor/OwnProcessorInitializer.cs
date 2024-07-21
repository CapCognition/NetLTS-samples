using CapCognition.Net.Core;
using CapCognition.Net.Core.Processing;
using CapCognitionNetLTS_Samples.OwnProcessor;

//Use the CapcognitionExtension attribute so the initializer can find the processor
[assembly: CapcognitionExtension(typeof(OwnProcessorInitializer))]
namespace CapCognitionNetLTS_Samples.OwnProcessor;

public class OwnProcessorInitializer : IRecognitionProcessExtension
{
    public void Initialize()
    {
        GraphicProcessorControl.RegisterRecognitionProcessor(typeof(OwnProcessorOption), () => new OwnProcessor());
    }
}