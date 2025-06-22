using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CapCognitionNetLTS_Samples;

public class HttpStreamingHostDemo
{
    public HttpStreamingHostDemo(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public void Start(string[] args)
    {
        var cert = GetOrCreateSelfSignedCertificate();

        var contentTypeProvider = new FileExtensionContentTypeProvider();

        contentTypeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
        contentTypeProvider.Mappings[".m3"] = "application/vnd.apple.mpegurl";
        contentTypeProvider.Mappings[".ts"] = "video/mp2t";

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    services.AddCors();
                });

                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    // Configure HTTPS endpoint on port 5001
                    serverOptions.ListenAnyIP(5001, listenOptions => { listenOptions.UseHttps(cert); });
                    // Configure HTTP endpoint on port 5000
                    serverOptions.ListenAnyIP(5000, listenOptions => { listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3; });
                });

                webBuilder.Configure(app =>
                {
                    app.UseCors(policy => policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                    );

                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(_baseUrl),
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

        Task.Factory.StartNew(host.Run);
    }

    static X509Certificate2 GetOrCreateSelfSignedCertificate()
    {
        if (File.Exists(CertPath))
        {
            Console.WriteLine("Loading existing certificate...");
            return new X509Certificate2(CertPath, CertPassword, X509KeyStorageFlags.Exportable);
        }

        Console.WriteLine("Generating and saving new certificate...");
        using var ecdsa = ECDsa.Create();
        var req = new CertificateRequest(
            "CN=localhost",
            ecdsa,
            HashAlgorithmName.SHA256);

        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        var pfxBytes = cert.Export(X509ContentType.Pfx, CertPassword);
        File.WriteAllBytes(CertPath, pfxBytes);

        return new X509Certificate2(pfxBytes, CertPassword, X509KeyStorageFlags.Exportable);
    }

    const string CertPath = "localhost-dev-cert.pfx";
    const string CertPassword = "devpassword";
    private readonly string _baseUrl;
}