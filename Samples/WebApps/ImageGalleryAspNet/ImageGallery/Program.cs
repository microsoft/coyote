// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

namespace ImageGallery
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>();

            if (!Debugger.IsAttached)
            {                
                builder.UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = null;
                });
            }

            return builder;
        }
    }
}
