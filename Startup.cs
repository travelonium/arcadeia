using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MediaCurator.Services;
using Microsoft.AspNetCore.HttpOverrides;
using System.Collections.Generic;
using System.Net;

namespace MediaCurator
{
   public class Startup
   {
      public Startup(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         services.AddControllersWithViews();

         // Instantiate the ThumbnailsDatabase
         services.AddSingleton<IThumbnailsDatabase, ThumbnailsDatabase>();

         // Instantiate the MediaLibrary
         services.AddSingleton<IMediaLibrary, MediaLibrary>();

         // Instantiate the BackgroundTaskQueue used by the Scanner
         services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

         // Start the Scanner Hosted Service
         services.AddHostedService<ScannerService>();

         // In production, the React files will be served from this directory
         services.AddSpaStaticFiles(configuration =>
         {
            configuration.RootPath = "UI/build";
         });

         // Proxies running on loopback addresses (127.0.0.0/8, [::1]), including the standard localhost
         // address (127.0.0.1), are trusted by default. If other trusted proxies or networks within the
         // organization handle requests between the Internet and the web server, add them to the KnownProxies
         // list in appsettings.json.
         if (Configuration.GetSection("KnownProxies").Exists())
         {
            foreach (var address in Configuration.GetSection("KnownProxies").Get<IEnumerable<string>>())
            {
               services.Configure<ForwardedHeadersOptions>(options =>
               {
                  options.KnownProxies.Add(IPAddress.Parse(address));
               });
            }
         }
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         if (env.IsDevelopment())
         {
            app.UseDeveloperExceptionPage();
         }
         else
         {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
         }

         if (env.IsProduction())
         {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
               ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
         }
         else
         {
            app.UseHttpsRedirection();
         }

         app.UseAuthentication();
         app.UseStaticFiles();
         app.UseSpaStaticFiles();

         app.UseRouting();

         app.UseEndpoints(endpoints =>
         {
            endpoints.MapControllerRoute(
                   name: "default",
                   pattern: "{controller}/{action=Index}/{id?}");
         });

         app.UseSpa(spa =>
         {
            spa.Options.SourcePath = "UI";

            if (env.IsDevelopment())
            {
               spa.UseReactDevelopmentServer(npmScript: "start");
            }
         });
      }
   }
}
