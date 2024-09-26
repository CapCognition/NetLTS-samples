# NetLTS-samples
This command line program for .NET 8 shows the different capabilities of the CapCognition libraries.
Start the program with different parameters to see the capabilities.

## recognize
All images under “Resources” are processed by the various decoders (barcode and license plate). The graphical results are also saved as images in the same resource directory.

## stream {URL}
An RTSP stream from the specified URL is started.

The url has the format *“rtsp://{username}:{password}@{FQDN or IP address}:{port}”*.

The stream runs for 10 seconds. An image is captured and recognized every second. The results are saved as images in the “Resources” subdirectory.

For simplicity and structural reasons, the demo code is not written in a very optimized and interlocked form. For an optimized product, some parts can be extremely optimized. 

An example:

In the StreamRecognitionDemo file, an image processor with the entire processor chain is newly created and initialized for each captured image before the image is passed for processing. This image processor only needs to be initialized once for a specific resolution and can then be reused for any further processing.

## ownRecognizer
This demo shows how you can write your own processor.

Simply create your own processing options to configure your processor. Write your processor derived from ImageProcessor and your code will be automatically added to the processing chain when your options are added.
The Initializer class with the attributed notation *[assembly: CapcognitionExtension(typeof(OwnProcessorInitializer))]* is used to activate and register your processor in the framework. The dependency injection pattern is used here to achieve the highest possible modularity and independence.

**Note**:

For reasons of abstraction and provisioning, you can place each of your processors in its own assembly or nuget. This plugin is then integrated into the processing chain at runtime.
