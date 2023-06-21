//--------------------------------------------------------------------------------------------------
// (C) 2017-2021 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: SmartWebServer – Blazor + Razor => https://blazor.radzen.com/
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Web
{
    #region Using
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using System.Linq;
    #endregion Using

    internal class SmartWebServer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var wwwroot = new ManifestEmbeddedFileProvider(typeof(SmartWebServer).Assembly, "wwwroot");
            env.WebRootFileProvider = env.WebRootFileProvider is CompositeFileProvider def
                ? new CompositeFileProvider(def.FileProviders.Concat(new[] { wwwroot }))
                : wwwroot;

            // Configure the HTTP request pipeline.
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
