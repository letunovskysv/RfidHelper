﻿//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidClientService – Веб-клиентский сервис.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Web
{
    #region Using
    using System;
    using System.Reflection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.FileProviders.Physical;
    using SmartMinex.Rfid;
    using SmartMinex.Runtime;
    #endregion Using

    internal class RfidClientService : TModule
    {
        #region Declarations

        readonly CancellationTokenSource _webhost = new();
        readonly TDispatcher _disp;

        public int Port { get; set; }
        public int ViewMode { get; }

        #endregion Declarations

        public RfidClientService(IRuntime runtime, string schema, int? port, int? viewMode) : base(runtime)
        {
            Subscribe = new[] { MSG.ConsoleCommand, MSG.ReadTagsData, MSG.TagsUpdated };
            Port = port ?? 80; // default HTTP port
            Name = "Клиентская служба доступа к данным, http://[::]:" + Port;
            _disp = new TDispatcher(Runtime, ViewMode = viewMode ?? 0);
        }

        protected override async Task ExecuteProcess()
        {
            var srv = Host.CreateDefaultBuilder()
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
                        srv.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                        srv.AddSingleton(_disp);
                        srv.AddDistributedMemoryCache();
                        srv.AddSession(opt =>
                        {
                            opt.Cookie.Name = ".lesev.session";
                            opt.IdleTimeout = TimeSpan.FromMinutes(15);
                            opt.Cookie.IsEssential = true;
                        });
                    });
                    host.UseStartup<SmartWebServer>();
                })
                .Build();

            await srv.StartAsync(_webhost.Token).ConfigureAwait(false);

            Status = RuntimeStatus.Running;
            while (_sync.WaitOne() && (Status & RuntimeStatus.Loop) > 0)
                try
                {
                    while (_esb.TryDequeue(out TMessage m))
                    {
                        switch (m.Msg)
                        {
                            case MSG.TagsUpdated:
                            case MSG.ReadTagsData:
                                await _disp.OnMessageReceivedAsync(m);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Runtime.Send(MSG.ErrorMessage, ProcessId, 0, ex);
                }

            _webhost.Cancel();
            await base.ExecuteProcess();
        }
    }

    static class WebExtensions
    {
        /// <summary> Использовать встроенные статические ресурсы.</summary>
        public static void UseResourceEmbedded(this IWebHostEnvironment env, string path = "SmartMinex.Web.wwwroot")
        {
            var efp = new EmbeddedFileProvider(typeof(SmartWebServer).Assembly, path);
            env.WebRootFileProvider = env.WebRootFileProvider is CompositeFileProvider cfp
                ? new CompositeFileProvider(cfp.FileProviders.Concat(new[] { efp }))
                : efp;
        }
    }
}
