// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PetImages.Messaging;
using PetImages.Storage;

namespace PetImages.Worker
{
    public class GenerateThumbnailWorker : IWorker
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

        private readonly IBlobContainer BlobContainer;

        public GenerateThumbnailWorker(
            IBlobContainer imageBlobContainer)
        {
            this.BlobContainer = imageBlobContainer;
        }

        public async Task ProcessMessage(Message message)
        {
            await InjectYieldsAtMethodStart();

            var thumbnailMessage = (GenerateThumbnailMessage)message;

            var accountName = thumbnailMessage.AccountName;
            var imageStorageName = thumbnailMessage.ImageStorageName;

            var imageContents = await this.BlobContainer.GetBlobAsync(accountName, imageStorageName);

            var thumbnail = GenerateThumbnail(imageContents);

            var containerName = accountName + Constants.ThumbnailContainerNameSuffix;
            var blobName = imageStorageName + Constants.ThumbnailSuffix;

            await this.BlobContainer.CreateContainerIfNotExistsAsync(containerName);
            await this.BlobContainer.CreateOrUpdateBlobAsync(containerName, blobName, thumbnail);
        }

        /// <summary>
        /// Dummy implementation of GenerateThumbnail that returns the same bytes as the image.
        /// </summary>
        private static byte[] GenerateThumbnail(byte[] imageContents) => imageContents;
    }
}
