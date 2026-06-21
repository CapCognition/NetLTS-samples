# CapCognition .NET LTS Samples

[![Core](https://img.shields.io/nuget/v/CapCognition.Net.Core?label=Core)](https://www.nuget.org/packages/CapCognition.Net.Core)
[![Capture](https://img.shields.io/nuget/v/CapCognition.Net?label=Capture)](https://www.nuget.org/packages/CapCognition.Net)
[![Barcode](https://img.shields.io/nuget/v/CapCognition.Net.BarcodeScanning?label=Barcode)](https://www.nuget.org/packages/CapCognition.Net.BarcodeScanning)
[![LPR](https://img.shields.io/nuget/v/CapCognition.Net.LicensePlateDetection?label=LPR)](https://www.nuget.org/packages/CapCognition.Net.LicensePlateDetection)
[![YOLO](https://img.shields.io/nuget/v/CapCognition.Net.YoloModelDetection?label=YOLO)](https://www.nuget.org/packages/CapCognition.Net.YoloModelDetection)

Practical .NET console samples for the CapCognition SDK.

Practical .NET console samples for the CapCognition SDK.

This repository demonstrates how to use CapCognition for real-time camera and image processing scenarios, including barcode recognition, QR code recognition, license plate recognition, RTSP stream processing, HLS browser streaming, YOLO model detection and custom image processors.

CapCognition is a cross-platform .NET framework for computer vision and object recognition applications. It can be used to build recognition systems for Windows, Linux and Raspberry Pi environments.

## What you can learn from this repository

This sample project shows how to:

* Recognize barcodes and QR codes from image files
* Detect and read license plates from images
* Process RTSP camera streams
* Capture frames from video streams and run recognition on them
* Convert an RTSP stream into an HLS stream for browser playback
* Integrate YOLO-based object detection models
* Write and register your own custom CapCognition image processor
* Build modular recognition pipelines with dependency injection
* Use CapCognition SDK packages in a .NET LTS application

## Included demos

| Command           | Description                                                                                                                                       |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `recognize`       | Processes all images in the `Resources` folder and runs barcode and license plate recognition. Result images are written back to the same folder. |
| `stream {URL}`    | Opens an RTSP camera stream, captures frames and runs recognition on the stream.                                                                  |
| `streamHLS {URL}` | Converts an RTSP stream into an HLS stream and serves it through a local browser-based demo page.                                                 |
| `ownRecognizer`   | Demonstrates how to build and register a custom image processor.                                                                                  |
| `yoloModel`       | Demonstrates YOLO model detection with the CapCognition YOLO integration.                                                                         |

## Technologies used

* .NET
* C#
* CapCognition.Net
* CapCognition.Net.Core
* CapCognition.Net.BarcodeScanning
* CapCognition.Net.LicensePlateDetection
* CapCognition.Net.YoloModelDetection
* RTSP video streams
* HLS browser streaming
* YOLO object detection
* SkiaSharp image processing

## Typical use cases

This repository is useful for developers who want to build:

* .NET barcode scanner applications
* QR code recognition tools
* License plate recognition systems
* ANPR / ALPR applications
* Parking access control systems
* Raspberry Pi camera recognition systems
* RTSP camera analysis pipelines
* Real-time object recognition services
* Custom computer vision processors in .NET
* YOLO-based recognition workflows

## Requirements

* .NET SDK
* A CapCognition license or trial configuration
* Optional: RTSP-capable IP camera for stream demos
* Optional: YOLO model files for YOLO detection demos

The project is designed as a developer sample and focuses on demonstrating the SDK capabilities. Some demo code is intentionally kept simple so the processing flow is easier to understand.

## NuGet packages

The samples in this repository use the CapCognition .NET LTS NuGet packages for camera capture, image processing, barcode recognition, QR code recognition, license plate recognition, YOLO model detection and licensing.

| Package | Purpose |
|---|---|
| [CapCognition.Net.Core](https://www.nuget.org/packages/CapCognition.Net.Core) | Core SDK components, base classes and recognition pipeline infrastructure used by the CapCognition .NET libraries. |
| [CapCognition.Net](https://www.nuget.org/packages/CapCognition.Net) | Camera capture, RTSP stream processing, H.264 decoding and output streaming support for .NET LTS applications. |
| [CapCognition.Net.BarcodeScanning](https://www.nuget.org/packages/CapCognition.Net.BarcodeScanning) | Barcode and QR code recognition for 1D and 2D barcode formats. |
| [CapCognition.Net.LicensePlateDetection](https://www.nuget.org/packages/CapCognition.Net.LicensePlateDetection) | License plate recognition, ANPR/ALPR workflows and optional vehicle type detection. |
| [CapCognition.Net.YoloModelDetection](https://www.nuget.org/packages/CapCognition.Net.YoloModelDetection) | YOLO model detection integration for custom object recognition workflows. |
| [CapCognition.Net.Licensing](https://www.nuget.org/packages/CapCognition.Net.Licensing) | Licensing infrastructure used by CapCognition packages. |

## Install packages manually

If you want to build your own project instead of using this sample repository, you can install the packages with the .NET CLI:

```bash
dotnet add package CapCognition.Net.Core
dotnet add package CapCognition.Net
dotnet add package CapCognition.Net.BarcodeScanning
dotnet add package CapCognition.Net.LicensePlateDetection
dotnet add package CapCognition.Net.YoloModelDetection
dotnet add package CapCognition.Net.Licensing
```
Depending on your use case, you may not need all packages.

For example:

Scenario	Recommended packages
Barcode and QR code recognition from images	CapCognition.Net.Core, CapCognition.Net.BarcodeScanning
License plate recognition from images	CapCognition.Net.Core, CapCognition.Net.LicensePlateDetection
RTSP camera stream processing	CapCognition.Net.Core, CapCognition.Net
RTSP stream + barcode recognition	CapCognition.Net.Core, CapCognition.Net, CapCognition.Net.BarcodeScanning
RTSP stream + license plate recognition	CapCognition.Net.Core, CapCognition.Net, CapCognition.Net.LicensePlateDetection
YOLO object detection	CapCognition.Net.Core, CapCognition.Net.YoloModelDetection

## Getting started

Clone the repository:

```bash
git clone https://github.com/CapCognition/NetLTS-samples.git
cd NetLTS-samples
```

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the file recognition demo:

```bash
dotnet run -- recognize
```

The `recognize` command processes the images in the `Resources` folder and writes graphical result images back into the same folder.

## Running RTSP stream recognition

Use the `stream` command with an RTSP URL:

```bash
dotnet run -- stream "rtsp://username:password@camera-host:554/stream"
```

The RTSP URL normally follows this structure:

```text
rtsp://{username}:{password}@{FQDN-or-IP-address}:{port}/{stream-path}
```

The sample captures images from the stream and runs recognition on the captured frames.

## Running RTSP to HLS browser streaming

Use the `streamHLS` command:

```bash
dotnet run -- streamHLS "rtsp://username:password@camera-host:554/stream"
```

The demo creates an HLS stream in the `public/stream` directory and starts a local web page for browser playback.

By default, the browser demo is available at:

```text
http://localhost:5000/HLSDisplay.html
```

This is useful if you want to test how a camera stream can be transformed into a browser-compatible playback format.

## Running the custom processor demo

Use:

```bash
dotnet run -- ownRecognizer
```

This demo shows how to create your own processor by deriving from the CapCognition image processing infrastructure.

The custom processor is registered through an initializer and can then be added to the processing chain. This allows you to place processors in separate assemblies or NuGet packages and load them as modular extensions.

## Running the YOLO model demo

Use:

```bash
dotnet run -- yoloModel
```

This demo shows how to integrate YOLO-based model detection into a CapCognition recognition workflow.

Use this as a starting point if you want to combine classical recognition tasks, such as barcode or license plate recognition, with custom object detection models.

## Recognition pipeline

CapCognition uses a modular recognition pipeline. A typical pipeline can include:

1. A capture source, such as a file, camera or RTSP stream
2. One or more image processors
3. Barcode, QR code, license plate or YOLO recognition processors
4. Optional custom processors
5. Result handling, overlays and generated output images

This makes it possible to combine multiple recognition technologies in one application.

## License setup

The sample contains placeholders for CapCognition license configuration.

Look for code sections similar to:

```csharp
VideoStreamCapturing.Use(/* Here comes your license */);
BarcodeRecognition.Use(/* Here comes your license */);
LicensePlateDetection.Use(/* Here comes your license */);
YoloModelDetection.Use(/* Here comes your license */);
```

Replace the placeholders with your own CapCognition license configuration.

## Performance notes

The demo code is written for clarity and learning purposes.

For production systems, you should optimize parts of the processing flow. For example, an image processor chain should normally be initialized once for a specific resolution and then reused for subsequent frames instead of being recreated for every captured image.

This is especially important for:

* High frame rate RTSP streams
* Multiple parallel camera streams
* Raspberry Pi or embedded Linux deployments
* Real-time parking access control systems
* License plate recognition at barriers or gates

## Repository structure

| Path                       | Purpose                                         |
| -------------------------- | ----------------------------------------------- |
| `Program.cs`               | Command line entry point and demo selection     |
| `FileRecognitionDemo.cs`   | Image file recognition demo                     |
| `StreamRecognitionDemo.cs` | RTSP stream recognition demo                    |
| `StreamHLSDemo.cs`         | RTSP to HLS streaming demo                      |
| `HttpStreamingHostDemo.cs` | Local HTTP host for browser streaming           |
| `YoloModelDemo.cs`         | YOLO model detection demo                       |
| `OwnProcessor/`            | Custom processor example                        |
| `Resources/`               | Sample images and generated recognition results |
| `Models/`                  | Model-related files                             |
| `public/`                  | Browser/HLS demo assets                         |

## Useful links

* Website: https://capcognition.com
* Documentation: https://docu.capcognition.com
* Pricing: https://capcognition.com/page/pricing
* GitHub organization: https://github.com/CapCognition
* NuGet packages: https://www.nuget.org/profiles/CapCognition

## Related CapCognition topics

* .NET barcode recognition
* .NET QR code recognition
* .NET license plate recognition
* ANPR / ALPR development
* Computer vision in C#
* RTSP stream processing in .NET
* HLS streaming from IP cameras
* YOLO object detection in .NET
* Raspberry Pi image recognition
* Custom image processors
* Parking access control systems

## License

This sample repository is licensed under the MIT License.

CapCognition SDK packages may require their own license depending on the package and usage scenario.
