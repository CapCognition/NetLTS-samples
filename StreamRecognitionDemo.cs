using CapCognition.Net.Core.Capture;
using CapCognition.Net.CaptureSources.VideoStream;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

// stream rtsp://username:password@ip:port/stream
public class StreamRecognitionDemo
{
    public async Task StreamAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var rtspUrl))
        {
            Console.WriteLine("Invalid stream URL");
            return;
        }

        // Credentials may be embedded in the URL or passed in the capture options.
        var captureSourceOptions = new VideoStreamCaptureSourceOptions(rtspUrl, null, null);
        var captureSource = CaptureControl.Instance.CreateVideoCaptureSource(captureSourceOptions);
        if (captureSource == null)
        {
            Console.WriteLine("Failed to create RTSP capture source");
            return;
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(DurationInSeconds));
        using var processing = new BitmapProcessing();
        CapCognition.Net.Recognizer? recognizer = null;
        (int Width, int Height)? preparedSize = null;
        try
        {
            if (!await captureSource.StartCaptureAsync())
            {
                Console.WriteLine("Failed to start RTSP streaming");
                return;
            }

            var token = cancellation.Token;
            var path = Path.Combine(AppContext.BaseDirectory, "Resources");
            Directory.CreateDirectory(path);
            var resultPath = Path.Combine(path, "stream_result");
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(IntervalInMilliseconds, token);
                using var bitmap = await captureSource.GetBitmapAsync(token);
                if (bitmap == null)
                {
                    continue;
                }

                var size = (bitmap.Width, bitmap.Height);
                if (preparedSize != size)
                {
                    if (!processing.PrepareProcessor(out recognizer, bitmap,
                            useBarcodeDetection: true, useLicensePlateDetection: true,
                            useOwnProcessor: false, downloadModelsFromInternet: false))
                    {
                        Console.WriteLine("Failed to prepare recognition processor for stream frame");
                        return;
                    }
                    preparedSize = size;
                }
                processing.ProcessImageSequentially(recognizer!, bitmap, resultPath);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // The sample's capture duration has elapsed.
        }
        finally
        {
            try
            {
                await captureSource.StopCaptureAsync();
            }
            finally
            {
                CaptureControl.Instance.RemoveCaptureSource(captureSource);
            }
        }
    }

    private const int IntervalInMilliseconds = 1000;
    private const int DurationInSeconds = 10;
}
