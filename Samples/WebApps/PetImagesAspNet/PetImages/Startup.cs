// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Coyote.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetImages.Messaging;
using PetImages.Storage;

namespace PetImages
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Add Coyote Middleware that takes control of the controllers during testing.
            app.UseRequestController();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logBuilder =>
            {
                logBuilder.AddConsole();
            });

            services.AddControllers();

            services.AddSingleton<IAccountContainer, CosmosContainer>();
            services.AddSingleton<IImageContainer, CosmosContainer>();
            services.AddSingleton<IBlobContainer, BlobContainer>();
            services.AddSingleton<IMessagingClient, MessagingClient>();
        }
    }
}
