// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ImageGallery.Models;

namespace ImageGallery.Client
{
    public class ImageGalleryClient : IDisposable
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

        private readonly HttpClient Client;
        private readonly string BaseUrl;

        public ImageGalleryClient(HttpClient client, string baseUrl = null)
        {
            this.Client = client;
            this.BaseUrl = baseUrl;
        }

        public virtual async Task<bool> CreateAccountAsync(Account account)
        {
            await InjectYieldsAtMethodStart();
            var res = await this.Client.PutAsJsonAsync(new Uri($"{this.BaseUrl}api/account/create", UriKind.RelativeOrAbsolute), account);

            if (!(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound))
            {
                throw new Exception($"Found unexpected error code: {res.StatusCode}");
            }

            return true;
        }

        public virtual async Task<bool> UpdateAccountAsync(Account updatedAccount)
        {
            await InjectYieldsAtMethodStart();
            var res = await this.Client.PutAsJsonAsync(new Uri($"{this.BaseUrl}api/account/update", UriKind.RelativeOrAbsolute), updatedAccount);
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
        }

        public virtual async Task<Account> GetAccountAsync(string id)
        {
            await InjectYieldsAtMethodStart();
            try
            {
                return await this.Client.GetFromJsonAsync<Account>(new Uri($"{this.BaseUrl}api/account/get?id={id}", UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }

        public virtual async Task<bool> DeleteAccountAsync(string id)
        {
            await InjectYieldsAtMethodStart();
            var res = await this.Client.DeleteAsync(new Uri($"{this.BaseUrl}api/account/delete?id={id}", UriKind.RelativeOrAbsolute));
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
        }

        public virtual async Task<bool> CreateOrUpdateImageAsync(Image image)
        {
            await InjectYieldsAtMethodStart();
            var res = await this.Client.PutAsJsonAsync(new Uri($"{this.BaseUrl}api/gallery/store", UriKind.RelativeOrAbsolute), image);
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
        }

        public virtual async Task<Image> GetImageAsync(string accountId, string imageId)
        {
            await InjectYieldsAtMethodStart();
            try
            {
                return await this.Client.GetFromJsonAsync<Image>(new Uri($"{this.BaseUrl}api/gallery/get?accountId={accountId}&imageName={Uri.EscapeDataString(imageId)}", UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }

        public virtual async Task<bool> DeleteImageAsync(string accountId, string imageId)
        {
            await InjectYieldsAtMethodStart();
            try
            {                
                var res = await this.Client.DeleteAsync(new Uri($"{this.BaseUrl}api/gallery/delete?accountId={accountId}&imageName={Uri.EscapeDataString(imageId)}", UriKind.RelativeOrAbsolute));
                return res.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<bool> DeleteAllImagesAsync(string accountId)
        {
            await InjectYieldsAtMethodStart();
            try
            {
                var res = await this.Client.DeleteAsync(new Uri($"{this.BaseUrl}api/gallery/deleteall?accountId={accountId}", UriKind.RelativeOrAbsolute));
                return res.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<ImageList> GetNextImageListAsync(string accountId, string continuationId = null)
        {
            await InjectYieldsAtMethodStart();
            try
            {
                return await this.Client.GetFromJsonAsync<ImageList>(new Uri($"{this.BaseUrl}api/gallery/getlist?accountId={accountId}&pageId={continuationId}", UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            this.Client?.Dispose();
        }
    }
}
