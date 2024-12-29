/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Arcadeia.Configuration;
using Arcadeia.Services;
using Arcadeia.Hubs;
using Arcadeia.Solr;
using System.Net;
using SolrNet;

namespace Arcadeia
{
   public class Startup(IConfiguration configuration)
   {
      public IConfiguration Configuration { get; } = configuration;

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         // Settings
         services.AddOptions<Settings>()
                 .Bind(Configuration)
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Session Settings
         services.AddOptions<SessionSettings>()
                 .Bind(Configuration.GetSection("Session"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Thumbnails Settings
         services.AddOptions<ThumbnailsSettings>()
                 .Bind(Configuration.GetSection("Thumbnails"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Streaming Settings
         services.AddOptions<StreamingSettings>()
                 .Bind(Configuration.GetSection("Streaming"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Supported Extensions Settings
         services.AddOptions<SupportedExtensionsSettings>()
                 .Bind(Configuration.GetSection("SupportedExtensions"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // FFmpeg Settings
         services.AddOptions<FFmpegSettings>()
                 .Bind(Configuration.GetSection("FFmpeg"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // yt-dlp Settings
         services.AddOptions<YtDlpSettings>()
                 .Bind(Configuration.GetSection("YtDlp"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Solr Settings
         services.AddOptions<SolrSettings>()
                 .Bind(Configuration.GetSection("Solr"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Scanner Settings
         services.AddOptions<ScannerSettings>()
                 .Bind(Configuration.GetSection("Scanner"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Mounts Settings
         services.AddOptions<List<MountSettings>>()
                 .Bind(Configuration.GetSection("Mounts"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Logging Settings
         services.AddOptions<LoggingSettings>()
                 .Bind(Configuration.GetSection("Logging"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         // Known Proxies
         services.AddOptions<List<string>>()
                 .Bind(Configuration.GetSection("KnownProxies"))
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

         services.AddControllersWithViews();

         // Instantiate the SignalR Hub and NotificationService
         services.AddSignalR(options =>
         {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = null;
            options.StreamBufferCapacity = 10;
         }).AddMessagePackProtocol();

         services.AddSingleton<NotificationService>();

         // Instantiate and configure an HTTPClient
         services.AddHttpClient();

         // Configure the Solr Index Service
         services.AddSolrNet<Models.MediaContainer>(provider =>
         {
            var solrUrl = provider.GetRequiredService<IOptions<SolrSettings>>().Value.URL;

            if (!string.IsNullOrEmpty(solrUrl))
            {
               // Check if it starts with "http://" or "https://"
               if (solrUrl.StartsWith("http://") || solrUrl.StartsWith("https://")) return solrUrl;

               // Check if it starts with "/solr/"
               if (solrUrl.StartsWith("/solr/")) return "http://solr:8983" + solrUrl;

               // Check if it starts with "solr/"
               if (solrUrl.StartsWith("solr/")) return "http://solr:8983/" + solrUrl;

               // Check if it starts with "/"
               if (solrUrl.StartsWith('/')) return "http://solr:8983/solr" + solrUrl;

               // Must be the core name
               return "http://solr:8983/solr/" + solrUrl;
            }

            throw new ArgumentException("The Solr URL is either null or empty.");
         });

         services.AddScoped<ISolrIndexService<Models.MediaContainer>, SolrIndexService<Models.MediaContainer, ISolrOperations<Models.MediaContainer>>>();

         // Instantiate the ThumbnailsDatabase
         services.AddSingleton<IThumbnailsDatabase, ThumbnailsDatabase>();

         // Instantiate the MediaLibrary
         services.AddSingleton<IMediaLibrary, MediaLibrary>();

         // Instantiate the BackgroundTaskQueue used by the Scanner
         services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

         // Start the FileSystem Service
         services.AddSingleton<IFileSystemService, FileSystemService>();
         services.AddHostedService(provider => provider.GetRequiredService<IFileSystemService>());

         // Start the Scanner Hosted Service
         services.AddSingleton<IScannerService, ScannerService>();
         services.AddHostedService(provider => provider.GetRequiredService<IScannerService>());

         // Instantiate the Download Service
         services.AddSingleton<IDownloadService, DownloadService>();
         services.AddHostedService(provider => provider.GetRequiredService<IDownloadService>());

         // In production, the React files will be served from this directory
         services.AddSpaStaticFiles(configuration =>
         {
            configuration.RootPath = "wwwroot";
         });

         // Required Session components
         services.AddDistributedMemoryCache();
         services.AddSession(options =>
         {
            options.IdleTimeout = TimeSpan.FromSeconds(Configuration.GetValue<int>("Session:IdleTimeoutSeconds"));
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
         });

         // Proxies running on loopback addresses (127.0.0.0/8, [::1]), including the standard localhost
         // address (127.0.0.1), are trusted by default. If other trusted proxies or networks within the
         // organization handle requests between the Internet and the web server, add them to the KnownProxies
         // list in appsettings.json.
         foreach (var address in Configuration.GetSection("KnownProxies").Get<IEnumerable<string>>() ?? [])
         {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
               options.KnownProxies.Add(IPAddress.Parse(address));
            });
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

         app.UseSession();

         app.UseEndpoints(endpoints =>
         {
            endpoints.MapControllers();
            endpoints.MapHub<SignalRHub>("/signalr", options =>
            {
               options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                                    | Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents
                                    | Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            });
         });

         app.UseSpa(spa =>
         {
            spa.Options.SourcePath = "UI";

            if (env.IsDevelopment())
            {
               spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
            }
         });
      }
   }
}
