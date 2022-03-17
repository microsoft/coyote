// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using ImageGallery.Filters;
using ImageGallery.Logging;
using ImageGallery.Middleware;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageGallery
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Used to associate each request with a unique id that we use for logging.
            app.UseMiddleware(typeof(RequestLoggingMiddleware));

            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logBuilder =>
            {
                logBuilder.AddConsole();
            });

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(ApiExceptionFilter));
            });

            services.AddSingleton<IDatabaseProvider, DatabaseProvider>(provider =>
            {
                var connectionString = GetSetting(provider, "StorageConnectionString");

                var logger = provider.GetService<ILogger<ApplicationLogs>>();
                services.AddSingleton(typeof(ILogger), logger);

                var client = new CosmosClientBuilder(connectionString)
                    .WithSerializerOptions(new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    })
                    .Build();

                var resp = client.CreateDatabaseIfNotExistsAsync("ImageGallery").Result;
                return new DatabaseProvider(resp.Database);
            });

            services.AddSingleton<IBlobContainerProvider, BlobContainerProvider>(provider =>
            {
                var connectionString = GetSetting(provider, "BlobStorageConnectionString");
                return new BlobContainerProvider(connectionString);
            });
        }

        private string GetSetting(IServiceProvider provider, string name)
        {
            var log = (ILogger)provider.GetService(typeof(ILogger<ApplicationLogs>));
            var config = (IConfiguration)provider.GetService(typeof(IConfiguration));
            var section = config.GetSection(name);
            if (section == null) 
            {
                var msg = string.Format("Missing setting '{0}' in appsettings.json", name);
                log.LogError(msg);
                throw new System.Exception(msg);
            }
            var value = section.Value;
            while (value.Contains("$("))
            {
                int pos = value.IndexOf("$(");
                int end = value.IndexOf(")", pos);
                var head = value.Substring(0, pos);
                var tail = string.Empty;
                if (end > 0 && end < value.Length)
                {
                    tail = value.Substring(end + 1);
                } else
                {
                    end = value.Length;
                }
                pos += 2;
                var variable = value.Substring(pos, end - pos);
                var result = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrEmpty(result))
                {
                    var msg = string.Format("Setting '{0}' in appsettings.json need environment variable '{1}'", name, variable);
                    log.LogError(msg);
                    throw new System.Exception(msg);
                }
                value = head + result + tail;
            }

            return value;
        }
    }
}
