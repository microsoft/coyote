// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using ImageGallery.Client;
using System.Net.Http;
using ImageGallery.Models;
using System;

namespace ImageGallery.Controllers
{

    public class AccountController : Controller
    {
        public static string ImageGalleryServiceUrl; 

        public AccountController(IServiceProvider provider)
        {
            ImageGalleryServiceUrl = Startup.GetImageGalleryServiceUrl(provider);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        private async Task<bool> ValidateLoginAsync(string userName, string password)
        {
            var client = new ImageGalleryClient(new HttpClient(), ImageGalleryServiceUrl);
            var account = await client.GetAccountAsync(userName);
            if (account != null)
            {
                return account.Password == password;
            }
            return await client.CreateAccountAsync(new Account() { Id = userName, Name = userName, Password = password, Email = "test@yahoo.com" });
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Normally Identity handles sign in, but you can do it directly
            if (await ValidateLoginAsync(userName, password))
            {
                var claims = new List<Claim>
                {
                    new Claim("user", userName),
                    new Claim("role", "Member")
                };

                await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", "user", "role")));

                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return Redirect("/");
                }
            }
            
            return View(new LoginViewModel() { Username = userName, Message = "Login failed" });
        }

        public IActionResult AccessDenied(string returnUrl = null)
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
    }
}
