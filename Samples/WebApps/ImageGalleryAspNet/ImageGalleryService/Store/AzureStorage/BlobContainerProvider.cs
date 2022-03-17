// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace ImageGallery.Store.AzureStorage
{
    /// <summary>
    /// Production implementation of an Azure Storage blob container provider.
    /// </summary>
    public class BlobContainerProvider : IBlobContainerProvider
    {
        private string ConnectionString;

        public BlobContainerProvider(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public async Task CreateContainerAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.CreateAsync();
        }

        public async Task CreateContainerIfNotExistsAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.CreateIfNotExistsAsync();
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.DeleteAsync();
        }

        public async Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            var deleteInfo = await blobContainerClient.DeleteIfExistsAsync();
            return deleteInfo.Value;
        }

        public async Task CreateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            await blobClient.UploadAsync(new MemoryStream(blobContents));
        }

        public async Task<byte[]> GetBlobAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            var downloadInfo = await blobClient.DownloadAsync();
            var buffer = new MemoryStream();
            downloadInfo.Value.Content.CopyTo(buffer);
            return buffer.ToArray();
        }

        public async Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            var existsInfo = await blobClient.ExistsAsync();
            return existsInfo.Value;
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            await blobClient.DeleteAsync();
        }

        public async Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            var deleteInfo = await blobClient.DeleteIfExistsAsync();
            return deleteInfo.Value;
        }

        public async Task DeleteAllBlobsAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.DeleteAsync();
            await blobContainerClient.CreateIfNotExistsAsync();
        }

        public async Task<BlobPage> GetBlobListAsync(string containerName, string continuationId, int pageSize)
        {
            BlobPage page = new BlobPage();
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            var pageable = blobContainerClient.GetBlobsAsync();
            await foreach(var item in pageable.AsPages(continuationId, pageSizeHint: pageSize))
            {
                List<string> names = new List<string>(item.Values.Select(b => b.Name));
                page.Names = names.ToArray();
                page.ContinuationId = item.ContinuationToken;
                return page;
            }

            return null;
        }
    }
}
