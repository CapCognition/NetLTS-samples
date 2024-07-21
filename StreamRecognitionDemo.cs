using CapCognition.Net.Core.Capture;
using CapCognition.Net.CaptureSources.VideoStream;
using System;
using SkiaSharp;
using System.Reflection;

namespace CapCognitionNetLTS_Samples;

//stream rtsp://username:password@ip:port/stream
public class StreamRecognitionDemo
{
    public void Stream(string url)
    {
        StartRtspStreaming(url);
    }

    private void StartRtspStreaming(string url)
    {
        //Add username and password to the rtsp url: rtsp://username:password@ip:port/stream
        //or can be added to the constructor of the VideoStreamCaptureSourceOptions
        var rtspUrl = new Uri(url);
        var captureSourceOptions = new VideoStreamCaptureSourceOptions(rtspUrl, null, null);

        StartCaptureSource(out var captureSource, captureSourceOptions);
        if (captureSource == null)
        {
            Console.WriteLine("Failed to start RTSP streaming");
            return;
        }

        var sem = new SemaphoreSlim(0, 1);
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Factory.StartNew(async () =>
        {
            try
            {
                var token = cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(IntervalInMilliseconds, token);
                    var bitmap = await captureSource.GetBitmapAsync(token);
                    if (bitmap != null)
                    {
                        ProcessBitmap(bitmap);
                        continue;
                    }

                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to get bitmap from video source");
            }
            finally
            {
                sem.Release();
            }
        }, cancellationTokenSource.Token);

        Thread.Sleep(DurationInSeconds * 1000);
        cancellationTokenSource.Cancel();
        sem.Wait(5000);

        captureSource.StopCaptureAsync().GetAwaiter().GetResult();
        CaptureControl.Instance.RemoveCaptureSource(captureSource);
    }

    private void ProcessBitmap(SKBitmap bitmap)
    {
        var success = BitmapProcessing.PrepareProcessor(out var recognizer, bitmap, true, true);
        if (!success)
        {
            Console.WriteLine("Failed to prepare recognition processor for bitmap");
            return;
        }

        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources");
        var resultPath = Path.Combine(path, "stream_result");
        BitmapProcessing.ProcessImageSequentially(recognizer, bitmap, resultPath);
    }

    private void StartCaptureSource(out IVideoCaptureSource? captureSource, VideoStreamCaptureSourceOptions captureSourceOptions)
    {
        captureSource = CaptureControl.Instance.CreateVideoCaptureSource(captureSourceOptions);
        if (captureSource == null)
        {
            return;
        }

        var success = captureSource.StartCaptureAsync().GetAwaiter().GetResult();
        if (!success)
        {
            CaptureControl.Instance.RemoveCaptureSource(captureSource);
            return;
        }
    }

    private const int IntervalInMilliseconds = 1000;
    private const int DurationInSeconds = 10;
}
