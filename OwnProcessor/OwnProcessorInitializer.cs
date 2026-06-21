using CapCognition.Licensing;
using CapCognition.Licensing.Validation;
using CapCognition.Net.Core.Processing;
using CapCognition.Net.Core.Utils;
using System.Reflection;

namespace CapCognitionNetLTS_Samples.OwnProcessor;

internal class YourLicenseValidator
{
    internal static void SetLicense(License? license)
    {
        try
        {
            if (license == null)
            {
                return;
            }

            var validationErrors = license.Validate()
                .ExpirationDate()
                .And()
                .Signature(YourLicensePublicKey)
                .AssertValidLicense()
                .ToList();

            if (validationErrors.Count > 0)
            {
                throw new ApplicationException($"{validationErrors[0].Message}. {validationErrors[0].HowToResolve}");
            }

            if (license.ProductFeature != FeatureNameYourOwnRecognizer)
            {
                throw new ApplicationException($"Feature {FeatureNameYourOwnRecognizer} is not contained in the license");
            }

            if (license.LicenseType == LicenseType.Trial)
            {
                return;
            }

            var version = Assembly.GetCallingAssembly().GetName().Version;

            var licenseParts = license.Version.Split('.');
            var mjv = licenseParts.Length >= 1 ? licenseParts[0] : null;
            var miv = licenseParts.Length >= 2 ? licenseParts[1] : null;
            if (mjv != null)
            {
                var v = int.Parse(mjv);
                if (version?.Major > v)
                {
                    throw new ApplicationException($"Major version of library {version} is higher than licensed version {v}.x");
                }

                if (!string.IsNullOrEmpty(miv))
                {
                    var mv = int.Parse(miv);
                    if (version?.Major == v && version.Minor > mv)
                    {
                        throw new ApplicationException($"Minor version of library {version} is higher than licensed version {mjv}.{miv}");
                    }
                }
            }

            LicenseGuard.SetLicensed(true);
        }
        finally
        {
            if (!LicenseGuard.IsSet)
            {
                LicenseGuard.SetLicensed(false);
            }
        }
    }

    internal static bool IsLicensed => LicenseGuard.IsLicensed;

    private static readonly LicenseGuard LicenseGuard = new();

    private const string FeatureNameYourOwnRecognizer = "Your Own Recognizer";
    private const string YourLicensePublicKey = "";
}

internal class OwnProcessorInitializer : IRecognitionProcessorInitializer
{
    internal OwnProcessorInitializer()
    {
        
    }

    public string ProcessorName => "My own Processor";

    public bool Initialize()
    {
        GraphicProcessorControl.RegisterRecognitionProcessor(typeof(OwnProcessorOption), () => new OwnProcessor());
        return true;
    }
}

public static class YourOwnRecognizer
{
    public static IRecognitionProcessorInitializer Use(string? license = null)
    {
        License? lic = null;
        if (!string.IsNullOrEmpty(license))
        {
            lic = License.Load(license);
        }
        YourLicenseValidator.SetLicense(lic);
        return new OwnProcessorInitializer();
    }
}
