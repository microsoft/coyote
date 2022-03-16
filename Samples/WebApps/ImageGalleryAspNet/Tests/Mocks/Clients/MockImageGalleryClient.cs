// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ImageGallery.Client;
using ImageGallery.Controllers;
using ImageGallery.Logging;
using ImageGallery.Models;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Tests.Mocks.Clients
{
    internal class MockImageGalleryClient : ImageGalleryClient
    {
        internal readonly IBlobContainerProvider AzureStorageProvider;
        internal readonly IClientProvider CosmosClientProvider;
        private IDatabaseProvider CosmosDbProvider;

        private readonly MockLogger Logger;

        public MockImageGalleryClient(Cosmos.MockCosmosState cosmosState, MockLogger logger) :
            base(null)
        {
            this.AzureStorageProvider = new AzureStorage.MockBlobContainerProvider(logger);
            this.CosmosClientProvider = new Cosmos.MockClientProvider(cosmosState, logger);
            this.Logger = logger;
        }

        internal async Task<IDatabaseProvider> InitializeCosmosDbAsync()
        {
            this.CosmosDbProvider = await this.CosmosClientProvider.CreateDatabaseIfNotExistsAsync(Constants.DatabaseName);
            await this.CosmosDbProvider.CreateContainerIfNotExistsAsync(Constants.AccountCollectionName, "/id");
            return this.CosmosDbProvider;
        }

        public override Task<bool> CreateAccountAsync(Account account)
        {
            var accountCopy = Clone(account);

            return Task.Run(async () =>
            {
                var controller = new AccountController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Create(accountCopy));
                var res = ExtractServiceResponse<Account>(actionResult.Result);

                if (!(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {res.StatusCode}");
                }

                return true;
            });
        }

        public override Task<bool> UpdateAccountAsync(Account updatedAccount)
        {
            var accountCopy = Clone(updatedAccount);

            return Task.Run(async () =>
            {
                var controller = new AccountController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Update(accountCopy));
                var res = ExtractServiceResponse<Account>(actionResult.Result);
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (!(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {res.StatusCode}");
                }

                return true;
            });
        }

        public override Task<Account> GetAccountAsync(string id)
        {
            return Task.Run(async () =>
            {
                var controller = new AccountController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Get(id));
                var res = ExtractServiceResponse<Account>(actionResult.Result);
                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {res.StatusCode}");
                }

                return Clone(res.Resource);
            });
        }

        public override Task<bool> DeleteAccountAsync(string id)
        {
            return Task.Run(async () =>
            {
                var controller = new AccountController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Delete(id));
                var statusCode = ExtractHttpStatusCode(actionResult);

                if (statusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else if (statusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (!(statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {statusCode}");
                }

                return true;
            });
        }

        public override Task<bool> CreateOrUpdateImageAsync(Image image)
        {
            var imageCopy = Clone(image);

            return Task.Run(async () =>
            {
                var controller = new GalleryController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Store(imageCopy));
                var statusCode = ExtractHttpStatusCode(actionResult);

                if (statusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else if (statusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (!(statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {statusCode}");
                }

                return true;
            });
        }

        public override Task<Image> GetImageAsync(string accountId, string imageId)
        {
            return Task.Run(async () =>
            {
                var controller = new GalleryController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Get(accountId, imageId));
                var res = ExtractServiceResponse<Image>(actionResult.Result);
                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {res.StatusCode}");
                }

                return Clone(res.Resource);
            });
        }

        public override Task<bool> DeleteImageAsync(string accountId, string imageId)
        {
            return Task.Run(async () =>
            {
                var controller = new GalleryController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.Delete(accountId, imageId));
                var statusCode = ExtractHttpStatusCode(actionResult);

                if (statusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else if (statusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (!(statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {statusCode}");
                }

                return true;
            });
        }

        public override Task<bool> DeleteAllImagesAsync(string accountId)
        {
            return Task.Run(async () =>
            {
                var controller = new GalleryController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.DeleteAllImages(accountId));
                var statusCode = ExtractHttpStatusCode(actionResult);

                if (statusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else if (statusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (!(statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {statusCode}");
                }

                return true;
            });
        }

        public override Task<ImageList> GetNextImageListAsync(string accountId, string continuationId = null)
        {
            return Task.Run(async () =>
            {
                var controller = new GalleryController(this.CosmosDbProvider, this.AzureStorageProvider, this.Logger);
                var actionResult = await InvokeControllerAction(async () => await controller.GetList(accountId, continuationId));
                var res = ExtractServiceResponse<ImageList>(actionResult.Result);
                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound))
                {
                    throw new Exception($"Found unexpected error code: {res.StatusCode}");
                }

                return Clone(res.Resource);
            });
        }

        /// <summary>
        /// Simulate middleware by wrapping invocation of controller in exception handling
        /// code which runs in middleware in production.
        /// </summary>
        private static async Task<ActionResult> InvokeControllerAction(Func<Task<ActionResult>> lambda)
        {
            try
            {
                return await lambda();
            }
            catch (CosmosException)
            {
                return new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <summary>
        /// Simulate middleware by wrapping invocation of controller in exception handling
        /// code which runs in middleware in production.
        /// </summary>
        private static async Task<ActionResult<T>> InvokeControllerAction<T>(Func<Task<ActionResult<T>>> lambda)
        {
            try
            {
                return await lambda();
            }
            catch (CosmosException)
            {
                return new ActionResult<T>(new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable));
            }
        }

        private static HttpStatusCode ExtractHttpStatusCode(ActionResult actionResult)
        {
            if (actionResult is OkObjectResult okObjectResult)
            {
                return (HttpStatusCode)okObjectResult.StatusCode;
            }
            else if (actionResult is StatusCodeResult statusCodeResult)
            {
                return (HttpStatusCode)statusCodeResult.StatusCode;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static ServiceResponse<T> ExtractServiceResponse<T>(ActionResult<T> actionResult)
        {
            var response = actionResult.Result;
            if (response is OkObjectResult okObjectResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)okObjectResult.StatusCode,
                    Resource = (T)okObjectResult.Value
                };
            }
            else if (response is StatusCodeResult statusCodeResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)statusCodeResult.StatusCode
                };
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static T Clone<T>(T obj)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
        }
    }
}
