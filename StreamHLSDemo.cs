using CapCognition.Net.CaptureSources.VideoStream;
using CapCognition.Net.Core.Capture;
using System.Reflection;

namespace CapCognitionNetLTS_Samples;

//streamHLS rtsp://username:password@ip:port/stream
public class StreamHLSDemo
{
    public StreamHLSDemo()
    {
        Directory.CreateDirectory(StreamingRoutePath);
    }

    public void Stream(string url)
    {
        var u = new Uri(url);
        var captureSourceOptions = new VideoStreamCaptureSourceOptions(u, null, null);

        var captureSource = CaptureControl.Instance.CreateVideoCaptureSource(captureSourceOptions);
        if (captureSource == null)
        {
            Console.WriteLine("Failed to create video capture source");
            return;
        }

        var outputOptions = new HLSVideoStreamOutputSinkOptions(StreamingRoutePath);
        var hlsOutput = OutputControl.Instance.CreateVideoOutputSink(captureSource, outputOptions);
        if (hlsOutput == null)
        {
            CaptureControl.Instance.RemoveCaptureSource(captureSource);
            Console.WriteLine("Failed to create HLS output");
            return;
        }

        // the HLS output can be started before the capture source is started and is automatically synchronized to the capture source start and stop
        var success = hlsOutput.StartOutput();
        if (!success)
        {
            CaptureControl.Instance.RemoveCaptureSource(captureSource);
            OutputControl.Instance.RemoveOutputSink(hlsOutput);
            Console.WriteLine("Failed to start HLS output");
            return;
        }

        // start the capture source, this will also start the HLS output if it is already started
        success = captureSource.StartCapture();
        if (!success)
        {
            CaptureControl.Instance.RemoveCaptureSource(captureSource);
            OutputControl.Instance.RemoveOutputSink(hlsOutput);
            Console.WriteLine("Failed to start capture");
            return;
        }

        //optional: gain full access to the capture source and output sink
        _videoCaptureSource = captureSource.ToVideoStreamCaptureSource();
        _videoStreamOutputSink = hlsOutput.ToVideoStreamOutputSink();

        Console.WriteLine("Streaming and HLS output started. Press 'q' to terminate");
    }

    public void StopStream()
    {
        Console.WriteLine("Stopping streaming and HLS output");
        _videoCaptureSource?.StopCapture();
        _videoStreamOutputSink?.StopOutput();
        CaptureControl.Instance.RemoveCaptureSource(_videoCaptureSource);
        OutputControl.Instance.RemoveOutputSink(_videoStreamOutputSink);
    }

    public string PublicRoutePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "public") ;
    public string StreamingRoutePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "public", "stream");

    private IVideoStreamCaptureSource? _videoCaptureSource;
    private IVideoStreamOutputSink? _videoStreamOutputSink;
}