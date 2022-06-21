// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PetImages.Messaging;
using PetImages.Storage;
using PetImages.Tests.MessagingMocks;
using PetImages.Tests.StorageMocks;

namespace PetImages.Tests
{
    internal class ServiceFactory : WebApplicationFactory<Startup>
    {
        public static async Task InjectYieldsAtMethodStart()
        {
            string envYiledLoop = Environment.GetEnvironmentVariable("YIELDS_METHOD_START");
            int envYiledLoopInt = 0;
            if (envYiledLoop != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envYiledLoopInt = int.Parse(envYiledLoop);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            for (int i = 0; i < envYiledLoopInt; i++)
            {
                await Task.Yield();
            }
        }

        private readonly MockBlobContainerProvider BlobContainer;
        private readonly MockMessagingClient MessagingClient;
        private readonly MockCosmosDatabase CosmosDatabase;
        private MockCosmosContainer AccountContainer;
        private MockCosmosContainer ImageContainer;

        public ServiceFactory()
        {
            this.BlobContainer = new MockBlobContainerProvider();
            this.MessagingClient = new MockMessagingClient(this.BlobContainer);
            this.CosmosDatabase = new MockCosmosDatabase(new MockCosmosState());
        }

        internal async Task<MockCosmosContainer> InitializeAccountContainerAsync()
        {
            await InjectYieldsAtMethodStart();

            this.AccountContainer = (MockCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.AccountContainerName);
            return this.AccountContainer;
        }

        internal async Task<MockCosmosContainer> InitializeImageContainerAsync()
        {
            await InjectYieldsAtMethodStart();

            this.ImageContainer = (MockCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.ImageContainerName);
            return this.ImageContainer;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureTestServices(services =>
            {
                // Inject the mocks.
                services.AddSingleton<IAccountContainer, MockCosmosContainer>(container => this.AccountContainer);
                services.AddSingleton<IImageContainer, MockCosmosContainer>(container => this.ImageContainer);
                services.AddSingleton<IBlobContainer, MockBlobContainerProvider>(provider => this.BlobContainer);
                services.AddSingleton<IMessagingClient, MockMessagingClient>(provider => this.MessagingClient);
            });
        }
    }
}
