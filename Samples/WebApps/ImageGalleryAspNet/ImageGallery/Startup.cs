// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageGallery
{
    public class Startup
    {
        public const string CookieScheme = "YourSchemeName";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static string ImageGalleryServiceUrl;

        public static string GetImageGalleryServiceUrl(IServiceProvider provider)
        {
            if (string.IsNullOrEmpty(ImageGalleryServiceUrl))
            {
                var config = (IConfiguration)provider.GetService(typeof(IConfiguration));
                var url = config.GetSection("ImageGalleryServiceUrl").Value;
                if (!url.EndsWith("/"))
                {
                    url += "/";
                }

                ImageGalleryServiceUrl = url;
            }

            return ImageGalleryServiceUrl;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddAuthentication(CookieScheme) // Sets the default scheme to cookies
                .AddCookie(CookieScheme, options =>
                {
                    options.AccessDeniedPath = "/account/denied";
                    options.LoginPath = "/account/login";
                    options.Cookie.IsEssential = true;
                });
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
