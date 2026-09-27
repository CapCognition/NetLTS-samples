using System.Diagnostics;
using System.Runtime.InteropServices;
using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;

namespace CapCognitionNetLTS_Samples;

// streamHLS rtsp://username:password@ip:port/stream
public class StreamHLSDemo
{
    public async Task StreamAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Not enough parameters given: streamHLS <URL>");
            return;
        }

        if (!Uri.TryCreate(args[1], UriKind.Absolute, out var url))
        {
            Console.WriteLine("Invalid stream URL");
            return;
        }

        Directory.CreateDirectory(StreamingRoutePath);
        using var host = HttpStreamingHostDemo.CreateHttpHost(PublicRoutePath);
        try
        {
            await host.StartAsync();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to start HTTP/HTTPS host: {exception.Message}");
            return;
        }

        var captureSourceOptions = new VideoStreamCaptureSourceOptions(url, null, null);

        var captureSource = CaptureControl.Instance.CreateVideoCaptureSource(captureSourceOptions);
        if (captureSource == null)
        {
            Console.WriteLine("Failed to create video capture source");
            await host.StopAsync();
            return;
        }

        IVideoOutputSink? hlsOutput = null;
        try
        {
            var outputOptions = new HLSVideoStreamOutputSinkOptions(StreamingRoutePath);
            hlsOutput = OutputControl.Instance.CreateVideoOutputSink(captureSource, outputOptions);
            if (hlsOutput == null)
            {
                Console.WriteLine("Failed to create HLS output");
                return;
            }

            if (!await hlsOutput.StartOutputAsync())
            {
                Console.WriteLine("Failed to start HLS output");
                return;
            }

            if (!await captureSource.StartCaptureAsync())
            {
                Console.WriteLine("Failed to start capture");
                return;
            }

            Console.WriteLine("Streaming and HLS output started. Press 'q' or Ctrl+C to terminate.");
            Console.WriteLine("HTTP:  http://localhost:5000/HLSDisplay.html");
            Console.WriteLine("HTTPS: https://localhost:5001/HLSDisplay.html");
            Console.WriteLine("IPv6:  http://[::1]:5000/ or https://[::1]:5001/");
            try
            {
                const string demoWebPage = "http://localhost:5000/HLSDisplay.html";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(demoWebPage) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", demoWebPage);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Could not open browser: {exception.Message}");
            }

            await WaitForTerminationAsync();
        }
        finally
        {
            try
            {
                await captureSource.StopCaptureAsync();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Failed to stop capture cleanly: {exception.Message}");
            }

            if (hlsOutput != null)
            {
                OutputControl.Instance.RemoveOutputSink(hlsOutput);
            }
            CaptureControl.Instance.RemoveCaptureSource(captureSource);

            try
            {
                await host.StopAsync();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Failed to stop HTTP/HTTPS host cleanly: {exception.Message}");
            }
        }
    }

    private static async Task WaitForTerminationAsync()
    {
        var termination = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            termination.TrySetResult();
        };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            if (!Console.IsInputRedirected)
            {
                _ = Task.Run(() =>
                {
                    while (Console.ReadKey(intercept: true).KeyChar != 'q')
                    {
                    }
                    termination.TrySetResult();
                });
            }
            await termination.Task;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    public string PublicRoutePath { get; } = Path.Combine(AppContext.BaseDirectory, "public");
    public string StreamingRoutePath { get; } = Path.Combine(AppContext.BaseDirectory, "public", "stream");
}
