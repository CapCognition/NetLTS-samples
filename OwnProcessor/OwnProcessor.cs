using CapCognition.Net.Core.Processing;
using CapCognition.Net.Core.Processing.Common;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples.OwnProcessor;

public class OwnProcessor : ImageProcessor
{
    public override Type OptionType => typeof(OwnProcessorOption);

    public override void StopProcessing()
    {
        //Here you can implement your own logic when the processor is stopped
    }

    public override async Task<bool> StartProcessingAsync(RecognitionOption option, int captureResolutionWidth, int captureResolutionHeight)
    {
        await base.StartProcessingAsync(option, captureResolutionWidth, captureResolutionHeight);

        //Here you can implement your own logic when the processor is started
        var yourOptions = (OwnProcessorOption)option;

        if (yourOptions.EnableOwnOption)
        {
            //Implement logic based on your own option
        }

        return true;
    }

    public override Task DoProcessImageAsync(SKBitmap bitmap, RecognitionResult result)
    {
        //Here you can implement your own recognition logic for whatever you want to recognize
        return Task.CompletedTask;
    }

    public override void ProcessorRemoved()
    {
        //Here you can implement your own logic when the processor is removed
    }
}