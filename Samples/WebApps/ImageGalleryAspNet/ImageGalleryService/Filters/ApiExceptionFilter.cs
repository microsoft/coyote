// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ImageGallery.Filters
{
    /// <summary>
    /// Filter that treats an unhandled exception in a controller as a 500 internal
    /// server error, which we consider as a bug.
    /// </summary>
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.Result = new JsonResult(context.Exception.Message)
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
