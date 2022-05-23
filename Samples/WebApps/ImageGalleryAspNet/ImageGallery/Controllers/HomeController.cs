// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using ImageGallery.Models;
using System;
using System.IO;
using ImageGallery.Client;
using System.Net.Http;

namespace ImageGallery.Controllers
{
    public class HomeController : Controller
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

        public static string ImageGalleryServiceUrl;

        public HomeController(IServiceProvider provider)
        {
            ImageGalleryServiceUrl = Startup.GetImageGalleryServiceUrl(provider);
        }

        [Authorize]
        public async Task<IActionResult> Index([FromForm] GalleryViewModel model)
        {
            await InjectYieldsAtMethodStart();
            var user = GetUser();
            if (model == null) 
            {
                model = new GalleryViewModel() { User = user, RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            }

            if (!string.IsNullOrEmpty(user))
            {
                var client = new ImageGalleryClient(new HttpClient(), ImageGalleryServiceUrl);
                var list = await client.GetNextImageListAsync(user, model.Continuation);
                if (list != null)
                {
                    model.Images = list.Names;
                    model.Continuation = list.ContinuationId;
                }
            }

            var result = View(model);
            result.ViewData["User"] = user;
            return result;
        }

        private string GetUser()
        {
            return User.Claims.Where(c => c.Type == "user").FirstOrDefault().Value;
        }

        [HttpPost]
        [Route("Upload")]
        public async Task<ActionResult> Upload()
        {
            await InjectYieldsAtMethodStart();
            try
            {
                var files = Request.Form.Files;
                int fileCount = files.Count;

                if (fileCount > 0)
                {
                    for (int i = 0; i < fileCount; i++)
                    {
                        var file = files[i];
                        MemoryStream buffer = new MemoryStream();
                        file.CopyTo(buffer);
                        Image img = new Image()
                        {
                            Name = file.FileName,
                            AccountId = GetUser(),
                            Contents = buffer.ToArray()
                        };

                        var client = new ImageGalleryClient(new HttpClient(), ImageGalleryServiceUrl);
                        await client.CreateOrUpdateImageAsync(img);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel() { RequestId = this.HttpContext.TraceIdentifier, Message = ex.Message, Trace = ex.StackTrace });
            }
        }

        [Authorize]
        public IActionResult MyClaims()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> GetImage(string id)
        {
            await InjectYieldsAtMethodStart();
            var user = GetUser();
            var client = new ImageGalleryClient(new HttpClient(), ImageGalleryServiceUrl);
            var image = await client.GetImageAsync(user, id);
            if (image == null)
            {
                return this.NotFound();
            }

            string ext = Path.GetExtension(image.Name).Trim('.');
            if (ext == null) ext = "png";

            // System.IO.File.WriteAllBytes($"c:\\temp\\test.{ext}", image.Contents);

            return this.File(image.Contents, $"image/{ext}");
        }

        [Authorize]
        public async Task<IActionResult> DeleteImage(string id)
        {
            await InjectYieldsAtMethodStart();
            var user = GetUser();
            var client = new ImageGalleryClient(new HttpClient(), ImageGalleryServiceUrl);
            await client.DeleteImageAsync(user, id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteAll()
        {
            await InjectYieldsAtMethodStart();
            var user = GetUser();
            var client = new ImageGalleryClient(new HttpClient(), ImageGalleryServiceUrl);
            await client.DeleteAllImagesAsync(user);
            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
