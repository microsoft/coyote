// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Messaging;
using PetImages.Storage;
using PetImagesTest.Exceptions;

namespace PetImagesTest.Clients
{
    public class TestPetImagesClient : IPetImagesClient
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageContainer;
        private readonly IBlobContainer BlobContainer;
        private readonly IMessagingClient MessagingClient;

        public TestPetImagesClient(ICosmosContainer accountContainer)
        {
            this.AccountContainer = accountContainer;
        }

        public TestPetImagesClient(ICosmosContainer accountContainer, ICosmosContainer imageContainer, IBlobContainer blobContainer)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
            this.BlobContainer = blobContainer;
        }

        public TestPetImagesClient(ICosmosContainer accountContainer, ICosmosContainer imageContainer,
            IBlobContainer blobContainer, IMessagingClient messagingClient)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
            this.BlobContainer = blobContainer;
            this.MessagingClient = messagingClient;
        }

        public async Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            var accountCopy = TestHelper.Clone(account);

            return await Task.Run(async () =>
            {
                var controller = new AccountController(this.AccountContainer);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateAccountAsync(accountCopy));
                return ExtractServiceResponse<Account>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<Image>> CreateImageAsync(string accountName, Image image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateImageAsync(accountName, imageCopy));
                return ExtractServiceResponse<Image>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<Image>> CreateOrUpdateImageAsync(string accountName, Image image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateOrUpdateImageAsync(accountName, imageCopy));
                return ExtractServiceResponse<Image>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.GetImageContentsAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.GetImageThumbnailAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
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
            catch (SimulatedDatabaseFaultException)
            {
                return new ActionResult<T>(new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable));
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
    }
}
