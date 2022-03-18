// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
            this.AccountContainer = (MockCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.AccountContainerName);
            return this.AccountContainer;
        }

        internal async Task<MockCosmosContainer> InitializeImageContainerAsync()
        {
            this.ImageContainer = (MockCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.ImageContainerName);
            return this.ImageContainer;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
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
