//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidClientService – Веб-клиентский сервис.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Web
{
    #region Using
    using System;
    using System.Reflection;
    using Microsoft.Extensions.FileProviders;
    using SmartMinex.Runtime;
    #endregion Using

    internal class RfidClientService : TModule
    {
        #region Declarations

        readonly CancellationTokenSource _webhost = new CancellationTokenSource();

        public int Port { get; set; }

        #endregion Declarations

        public RfidClientService(IRuntime runtime, string schema, int? port) : base(runtime)
        {
            Subscribe = new[] { MSG.ConsoleCommand };
            Port = port ?? 80; // default HTTP port
            Name = "Клиентская служба доступа к данным, http://localhost:" + Port;
        }

        protected override async Task ExecuteProcess()
        {
            await Host.CreateDefaultBuilder()
                .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .ConfigureWebHostDefaults(host =>
                {
                    host.UseKestrel();
                    host.ConfigureKestrel(opt =>
                    {
                        opt.ListenAnyIP(Port, listen =>
                        {
                          //  listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                        });
                    });
                    host.ConfigureServices((srv) =>
                    {
                        srv.AddDistributedMemoryCache();
                    });
                    host.UseStartup<SmartWebServer>();
                })
                .Build()
                .RunAsync(_webhost.Token);

            Status = RuntimeStatus.Running;
        }
    }

    static class WebExtensions
    {
        /// <summary> Использовать встроенные статические ресурсы.</summary>
        /// <remarks> Обязательно:<br/>
        /// Подключить NuGet пакет <strong>Microsoft.Extensions.FileProviders.Embedded</strong><br/>
        /// Включить в настройки проекта запись <strong>&lt;GenerateEmbeddedFilesManifest&gt;true&lt;/GenerateEmbeddedFilesManifest&gt;</strong>
        /// </remarks>
        public static void UseResourceEmbedded(this IWebHostEnvironment env, string path = "wwwroot")
        {
            var src = new ManifestEmbeddedFileProvider(Assembly.GetCallingAssembly(), path);
            env.WebRootFileProvider = env.WebRootFileProvider is CompositeFileProvider cfp
                ? new CompositeFileProvider(cfp.FileProviders.Concat(new[] { src }))
                : src;
        }
    }
}
