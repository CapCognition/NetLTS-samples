using CapCognition.Net.Core.Processing.Common;

namespace CapCognitionNetLTS_Samples.OwnProcessor;

public class OwnProcessorOption : RecognitionOption
{
    public OwnOptionEnum OwnOption { get; set; }

    public bool EnableOwnOption { get; set; }

    //Here you can add your own options

    public enum OwnOptionEnum
    {
        OwnOption1,
        OwnOption2
    }
}