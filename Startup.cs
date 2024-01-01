using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Collections.Generic;
using MediaCurator.Services;
using MediaCurator.Solr;
using System.Net;
using SolrNet;

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

         // Instantiate and configure an HTTPClient
         services.AddHttpClient();

         // Configure the Solr Index Service
         services.AddSolrNet<Models.MediaContainer>(Configuration.GetSection("Solr:URL").Get<string>());
         services.AddScoped<ISolrIndexService<Models.MediaContainer>, SolrIndexService<Models.MediaContainer, ISolrOperations<Models.MediaContainer>>>();

         // Instantiate the ThumbnailsDatabase
         services.AddSingleton<IThumbnailsDatabase, ThumbnailsDatabase>();

         // Instantiate the MediaLibrary
         services.AddSingleton<IMediaLibrary, MediaLibrary>();

         // Instantiate the BackgroundTaskQueue used by the Scanner
         services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

         // Start the FileSystem Service
         services.AddSingleton<IFileSystemService, FileSystemService>();
         services.AddHostedService<FileSystemService>(provider => (FileSystemService)provider.GetService<IFileSystemService>());

         // Start the Scanner Hosted Service
         services.AddHostedService<ScannerService>();

         // In production, the React files will be served from this directory
         services.AddSpaStaticFiles(configuration =>
         {
            configuration.RootPath = "wwwroot";
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
            app.UseHttpsRedirection();
            app.UseDeveloperExceptionPage();
         }
         else
         {
            app.UseHsts();
            app.UseExceptionHandler("/Error");
         }

         if (env.IsProduction())
         {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
               ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
         }

         app.UseAuthentication();

         app.UseStaticFiles();
         app.UseSpaStaticFiles();

         app.UseRouting();

         app.UseEndpoints(endpoints =>
         {
            endpoints.MapControllers();
         });

         app.UseSpa(spa =>
         {
            spa.Options.SourcePath = "UI";
         });
      }
   }
}
