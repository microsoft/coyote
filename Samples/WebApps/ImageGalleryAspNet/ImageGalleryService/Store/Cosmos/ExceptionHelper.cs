// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Store.Cosmos
{
    public static class ExceptionHelper
    {
        public static bool IsCosmosExceptionWithStatusCode(Exception ex, System.Net.HttpStatusCode statusCode)
        {
            var cosmosException = GetCosmosException(ex);
            if (cosmosException is null)
            {
                return false;
            }

            return cosmosException.StatusCode == statusCode;
        }

        private static CosmosException GetCosmosException(Exception exception)
        {
            if (exception is CosmosException cosmosException)
            {
                return cosmosException;
            }

            var innerExceptions = new List<Exception>();
            if (exception is AggregateException aex)
            {
                innerExceptions.AddRange(aex.Flatten().InnerExceptions);
            }
            else if (exception.InnerException != null)
            {
                innerExceptions.Add(exception.InnerException);
            }

            if (innerExceptions.Count > 0)
            {
                return innerExceptions.Select(innerException => GetCosmosException(innerException)).
                    FirstOrDefault(ex => ex != null) ?? null;
            }

            return null;
        }
    }
}
