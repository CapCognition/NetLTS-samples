using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CapCognitionNetLTS_Samples;

public class HttpStreamingHostDemo
{
    public static IHost CreateHttpHost(string rootPath)
    {
        var cert = GetOrCreateCertificate();

        var contentTypeProvider = new FileExtensionContentTypeProvider();

        contentTypeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
        contentTypeProvider.Mappings[".ts"] = "video/mp2t";

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    // Configure HTTPS endpoint on port 5001
                    serverOptions.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps(cert);
                    });
                    // Configure HTTP endpoint on port 5000
                    serverOptions.ListenAnyIP(5000, listenOptions => { listenOptions.Protocols = HttpProtocols.Http1; });
                });

                webBuilder.Configure(app =>
                {
                    app.UseDefaultFiles(new DefaultFilesOptions
                    {
                        FileProvider = new PhysicalFileProvider(rootPath),
                        DefaultFileNames = { "HLSDisplay.html" }
                    });

                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(rootPath),
                        RequestPath = "", // Serve files at root URL, e.g. /stream/output.m3u8
                        ContentTypeProvider = contentTypeProvider,
                        OnPrepareResponse = ctx =>
                        {
                            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                        }
                    });

                });
            })
            .Build();
        return host;
    }

    static X509Certificate2 GetOrCreateCertificate()
    {
        // Delete cert if it lacks SAN (old certs without SAN are rejected by all modern browsers)
        if (File.Exists(CertPath))
        {
            var existing = X509CertificateLoader.LoadPkcs12FromFile(CertPath, CertPassword, X509KeyStorageFlags.Exportable);
            var hasSan = existing.Extensions["2.5.29.17"] != null; // 2.5.29.17 = SubjectAlternativeName OID
            using var rsaPublicKey = existing.GetRSAPublicKey();
            var hasCompatibleKey = rsaPublicKey != null;
            var isValid = existing.NotBefore.ToUniversalTime() <= DateTime.UtcNow
                && existing.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddDays(1);
            if (!hasSan || !isValid || !hasCompatibleKey || !existing.HasPrivateKey)
            {
                Console.WriteLine("Existing certificate is invalid, expired, or lacks SAN – regenerating...");
                existing.Dispose();
                File.Delete(CertPath);
            }
            else
            {
                Console.WriteLine("Loading existing certificate...");
                return existing;
            }
        }

        Console.WriteLine("Generating and saving new certificate (with SAN)...");
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        var enhancedKeyUsages = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")
        };
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, critical: false));

        // SAN is required by all modern browsers (Chrome 58+, Firefox, Edge)
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);       // 127.0.0.1
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);   // ::1
        req.CertificateExtensions.Add(sanBuilder.Build());

        using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));
        var pfxBytes = cert.Export(X509ContentType.Pfx, CertPassword);
        File.WriteAllBytes(CertPath, pfxBytes);

        return X509CertificateLoader.LoadPkcs12(pfxBytes, CertPassword, X509KeyStorageFlags.Exportable);
    }

    private static readonly string CertPath = Path.Combine(AppContext.BaseDirectory, "localhost-dev-cert.pfx");
    private const string CertPassword = "devpassword";
}
