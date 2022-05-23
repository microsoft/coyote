// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using ImageGallery.Models;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageGallery.Logging;

namespace ImageGallery.Controllers
{
    [ApiController]
    public class GalleryController : ControllerBase
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

        private readonly IDatabaseProvider DatabaseProvider;
        private IContainerProvider AccountContainer;
        private readonly IBlobContainerProvider StorageProvider;
        private readonly ILogger Logger;

        public GalleryController(IDatabaseProvider databaseProvider, IBlobContainerProvider storageProvider, ILogger<ApplicationLogs> logger)
        {
            this.DatabaseProvider = databaseProvider;
            this.StorageProvider = storageProvider;
            this.Logger = logger;
        }

        private async Task<IContainerProvider> GetOrCreateContainer()
        {
            await InjectYieldsAtMethodStart();
            if (this.AccountContainer == null)
            {
                this.AccountContainer = await this.DatabaseProvider.CreateContainerIfNotExistsAsync(Constants.AccountCollectionName, "/id");
            }
            return this.AccountContainer;
        }

        [HttpPut]
        [Produces(typeof(ActionResult))]
        [Route("api/gallery/store")]
        public async Task<ActionResult> Store(Image image)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Storing image with name '{0}' and acccount id '{1}'.",
                image.Name, image.AccountId);

            // First, check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(image.AccountId, image.AccountId);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling the following APIs after checking if the account exists is racy and can
            // fail due to another concurrent request.

            // The account exists exists, so we can store the image to the blob storage.
            var containerName = Constants.GetContainerName(image.AccountId);
            await this.StorageProvider.CreateContainerIfNotExistsAsync(containerName);
            await this.StorageProvider.CreateBlobAsync(containerName, image.Name, image.Contents);
            return this.Ok();
        }

        [HttpGet]
        [Produces(typeof(ActionResult<Image>))]
        [Route("api/gallery/get/")]
        public async Task<ActionResult<Image>> Get(string accountId, string imageName)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Getting image with name '{0}' and acccount id '{1}'.",
                imageName, accountId);

            // First, check if the blob exists in Azure Storage.
            var containerName = Constants.GetContainerName(accountId);
            var exists = await this.StorageProvider.ExistsBlobAsync(containerName, imageName);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling get on the blob container after checking if the blob exists is racy and
            // can, for example, fail due to another concurrent request that deleted the blob.

            // The blob exists, so get the image.
            byte[] contents = await this.StorageProvider.GetBlobAsync(containerName, imageName);
            return this.Ok(new Image(accountId, imageName, contents));
        }

        [HttpGet]
        [Produces(typeof(ActionResult<Image>))]
        [Route("api/gallery/getlist/")]
        public async Task<ActionResult<Image>> GetList(string accountId, string pageId)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Getting image list for acccount id '{0}' using continuation {1}.",
                accountId, pageId);

            // First, check if the blob exists in Azure Storage.
            var containerName = Constants.GetContainerName(accountId);
            
            // The blob exists, so get the image.
            var list = await this.StorageProvider.GetBlobListAsync(containerName, pageId, 100);
            if (list == null)
            {
                return this.NotFound();
            }

            return this.Ok(new ImageList(accountId, list.Names, list.ContinuationId));
        }

        [HttpDelete]
        [Produces(typeof(ActionResult))]
        [Route("api/gallery/delete/")]
        public async Task<ActionResult> Delete(string accountId, string imageName)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Deleting image with name '{0}' and acccount id '{1}'.",
                imageName, accountId);

            // First, check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(accountId, accountId);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling the following APIs after checking if the account exists is racy and can
            // fail due to another concurrent request.

            // The account exists, so check if the blob exists in Azure Storage.
            var containerName = Constants.GetContainerName(accountId);
            exists = await this.StorageProvider.ExistsBlobAsync(containerName, imageName);
            if (!exists)
            {
                return this.NotFound();
            }

            // The account exists, so delete the blob if it exists in Azure Storage.
            var deleted = await this.StorageProvider.DeleteBlobIfExistsAsync(containerName, imageName);
            if (!deleted)
            {
                return this.NotFound();
            }

            return this.Ok();
        }

        [HttpDelete]
        [Produces(typeof(ActionResult))]
        [Route("api/gallery/deleteall/")]
        public async Task<ActionResult> DeleteAllImages(string accountId)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Deleting all images in acccount id '{0}'.", accountId);

            // First, check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(accountId, accountId);
            if (!exists)
            {
                return this.NotFound();
            }

            var containerName = Constants.GetContainerName(accountId);
            await this.StorageProvider.DeleteAllBlobsAsync(containerName);

            return this.Ok();
        }
    }
}
