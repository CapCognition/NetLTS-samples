using CapCognition.Net;
using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;
using CapCognition.Net.LicensePlateDetection.CapCognitionLicensePlateTracking;
using CapCognition.Net.LicensePlateDetection.Common;
using SkiaSharp;

namespace CapCognitionNetLTS_Samples;

// streamTrack rtsp://username:password@ip:port/stream [durationSeconds]
public class StreamTrackingDemo
{
    public async Task StreamAndTrackAsync(Uri url, TimeSpan duration)
    {
        if (!url.IsAbsoluteUri || url.Scheme is not ("rtsp" or "rtsps") || string.IsNullOrEmpty(url.Host))
        {
            Console.WriteLine("Please provide a valid RTSP URL");
            return;
        }
        if (duration < TimeSpan.FromSeconds(1) || duration > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be between one second and one day.");
        }

        var resultDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "TrackingResults");
        Directory.CreateDirectory(resultDirectory);
        var captureOptions = new VideoStreamCaptureSourceOptions(url, null, null)
        {
            // Optional settings for demonstration only. Remove this initializer to use
            // the library defaults; these overrides are not required for RTSP tracking.
            AdditionalStreamingOptionsToSet = new Dictionary<string, string>
            {
                // Example: override the library's 5-second timeout with 4 seconds.
                ["timeout"] = "4000000",
                // Example: identify as VLC for cameras requiring this user agent.
                ["user_agent"] = "LibVLC/3.0.16 (LIVE555 Streaming Media v2021.04.09)"
            }
        };
        var captureSource = CaptureControl.Instance.CreateVideoCaptureSource(captureOptions);
        if (captureSource == null)
        {
            Console.WriteLine("Failed to create RTSP capture source");
            return;
        }

        using var cancellation = new CancellationTokenSource();
        using var processing = new BitmapProcessing();
        using var tracker = new LicensePlateTemporalTracker(LicensePlateTrackingMode.CarDetection);
        tracker.ResultChanged += OnTrackerResultChanged;
        var trackerCallback = tracker.CreateCallback();
        Recognizer? recognizer = null;
        (int Width, int Height)? preparedSize = null;

        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            if (!await captureSource.StartCaptureAsync())
            {
                Console.WriteLine("Failed to start RTSP streaming");
                return;
            }

            cancellation.CancelAfter(duration);
            var token = cancellation.Token;
            Console.WriteLine($"Tracking license plates for {duration.TotalSeconds:0} seconds in ParkingOfficer mode.");
            Console.WriteLine("Press 'n' for the next vehicle, 'q' or Ctrl+C to stop.");
            Console.WriteLine($"Result images: {resultDirectory}");

            while (!token.IsCancellationRequested)
            {
                if (!Console.IsInputRedirected && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    if (key == ConsoleKey.Q)
                    {
                        break;
                    }
                    if (key == ConsoleKey.N)
                    {
                        tracker.NextCar();
                    }
                }

                using var bitmap = await captureSource.GetBitmapAsync(token);
                if (bitmap == null)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                var size = (bitmap.Width, bitmap.Height);
                if (preparedSize != size)
                {
                    if (preparedSize != null)
                    {
                        // Spatial evidence from the old source dimensions cannot be reused.
                        tracker.Reset(emitRemoved: true);
                    }
                    if (!processing.PrepareProcessor(out recognizer, bitmap,
                            useBarcodeDetection: false, useLicensePlateDetection: true,
                            useOwnProcessor: false, downloadModelsFromInternet: false,
                            deliverPlatePictureInResult: true))
                    {
                        Console.WriteLine("Failed to prepare license plate recognition processors");
                        return;
                    }
                    preparedSize = size;
                }

                token.ThrowIfCancellationRequested();
                // Keep the original source image for the tracker. Sequential processing also
                // returns frames without detections, which the tracker needs for temporal state.
                var result = await recognizer!.ProcessImageAsync(bitmap, disableBitmapDisposal: true);
                if (result == null)
                {
                    Console.WriteLine("Failed to recognize stream frame");
                    return;
                }
                try
                {
                    trackerCallback(bitmap, result);
                }
                finally
                {
                    // The tracker copies the retained images; frame results own the originals.
                    foreach (var plate in result.Results.OfType<RecognitionProcessorLicensePlateDetectionResult>())
                    {
                        plate.PlateBitmap?.Dispose();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Duration elapsed or Ctrl+C was pressed.
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            tracker.ResultChanged -= OnTrackerResultChanged;
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

    private static void OnTrackerResultChanged(LicensePlateTemporalTracker tracker, StableLicensePlateResult result)
    {
        // This event handler owns the delivered result and its BestSourceBitmap and
        // BestPlateBitmap copies. Dispose the result when finished: using (result)
        // below disposes both copies, so do not dispose them separately.
        // The tracker owns and releases its internal retained images. The capture-frame
        // bitmap and the per-frame detection's PlateBitmap are separate originals;
        // StreamAndTrackAsync releases those after the tracker has copied them.
        using (result)
        {
            Console.WriteLine($"License plate: {result}");
            if (result.State == StableLicensePlateState.Removed)
            {
                return;
            }

            var plateName = string.Concat((result.PlateNumber ?? "unknown").Select(character =>
                char.IsLetterOrDigit(character) || character == '-' ? character : '_'));
            var directory = Path.Combine(AppContext.BaseDirectory, "Resources", "TrackingResults");
            var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}_{plateName}_{result.State}_{tracker.Mode}";
            SaveBitmap(result.BestSourceBitmap, Path.Combine(directory, $"{fileName}_source.png"));
            SaveBitmap(result.BestPlateBitmap, Path.Combine(directory, $"{fileName}_plate.png"));
        }
    }

    private static void SaveBitmap(SKBitmap? bitmap, string path)
    {
        if (bitmap == null)
        {
            return;
        }
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        if (data != null)
        {
            File.WriteAllBytes(path, data.ToArray());
        }
    }
}
