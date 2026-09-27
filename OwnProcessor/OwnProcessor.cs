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

    public override async Task<bool> StartProcessingAsync(RecognitionOptions option, int captureResolutionWidth, int captureResolutionHeight)
    {
        if (option is not OwnProcessorOption yourOptions
            || !await base.StartProcessingAsync(option, captureResolutionWidth, captureResolutionHeight))
        {
            return false;
        }

        //Here you can implement your own logic when the processor is started

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
