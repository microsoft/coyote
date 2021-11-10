// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable SA1005

// using System;
// using System.Net.Http;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Runtime.CompilerServices;
// using System.Runtime.Versioning;
// using Microsoft.Coyote.Runtime;

// namespace Microsoft.Coyote.Interception
// {
//     /// <summary>
//     /// Provides methods for controlling <see cref="HttpClient"/> during testing.
//     /// </summary>
//     /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
//     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
//     public static class ControlledHttpClient
//     {
//         /// <summary>
//         /// Send a GET request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, string requestUri)
//         {
//             var task = client.GetAsync(requestUri);
//             CoyoteRuntime.Current.WaitTaskCompletes(task);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, Uri requestUri)
//         {
//             var task = client.GetAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri with an HTTP completion option as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, string requestUri,
//             HttpCompletionOption completionOption)
//         {
//             var task = client.GetAsync(requestUri, completionOption);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri with an HTTP completion option as an
//         /// asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, Uri requestUri,
//             HttpCompletionOption completionOption)
//         {
//             var task = client.GetAsync(requestUri, completionOption);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, string requestUri,
//             CancellationToken cancellationToken)
//         {
//             var task = client.GetAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, Uri requestUri,
//             CancellationToken cancellationToken)
//         {
//             var task = client.GetAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri with an HTTP completion option and a
//         /// cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, Uri requestUri,
//             HttpCompletionOption completionOption, CancellationToken cancellationToken)
//         {
//             var task = client.GetAsync(requestUri, completionOption, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri with an HTTP completion option and a
//         /// cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> GetAsync(HttpClient client, string requestUri,
//             HttpCompletionOption completionOption, CancellationToken cancellationToken)
//         {
//             var task = client.GetAsync(requestUri, completionOption, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Sends a GET request to the specified Uri and return the response body as a byte
//         /// array in an asynchronous operation.
//         /// </summary>
//         public static Task<byte[]> GetByteArrayAsync(HttpClient client, string requestUri)
//         {
//             var task = client.GetByteArrayAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a byte
//         /// array in an asynchronous operation.
//         /// </summary>
//         public static Task<byte[]> GetByteArrayAsync(HttpClient client, Uri requestUri)
//         {
//             var task = client.GetByteArrayAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Sends a GET request to the specified Uri and return the response body as a byte
//         /// array in an asynchronous operation.
//         /// </summary>
//         public static Task<byte[]> GetByteArrayAsync(HttpClient client, string requestUri, CancellationToken cancellationToken)
//         {
//             var task = client.GetByteArrayAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a byte
//         /// array in an asynchronous operation.
//         /// </summary>
//         public static Task<byte[]> GetByteArrayAsync(HttpClient client, Uri requestUri, CancellationToken cancellationToken)
//         {
//             var task = client.GetByteArrayAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a stream
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<Stream> GetStreamAsync(HttpClient client, string requestUri)
//         {
//             var task = client.GetStreamAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a stream
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<Stream> GetStreamAsync(HttpClient client, Uri requestUri)
//         {
//             var task = client.GetStreamAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a stream
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<Stream> GetStreamAsync(HttpClient client, string requestUri, CancellationToken cancellationToken)
//         {
//             var task = client.GetStreamAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a stream
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<Stream> GetStreamAsync(HttpClient client, Uri requestUri, CancellationToken cancellationToken)
//         {
//             var task = client.GetStreamAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a string
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<string> GetStringAsync(HttpClient client, string requestUri)
//         {
//             var task = client.GetStringAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a string
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<string> GetStringAsync(HttpClient client, Uri requestUri)
//         {
//             var task = client.GetStringAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a string
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<string> GetStringAsync(HttpClient client, string requestUri, CancellationToken cancellationToken)
//         {
//             var task = client.GetStringAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a GET request to the specified Uri and return the response body as a string
//         /// in an asynchronous operation.
//         /// </summary>
//         public static Task<string> GetStringAsync(HttpClient client, Uri requestUri, CancellationToken cancellationToken)
//         {
//             var task = client.GetStringAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a POST request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PostAsync(HttpClient client, string requestUri, HttpContent content)
//         {
//             var task = client.PostAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Send a POST request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PostAsync(HttpClient client, Uri requestUri, HttpContent content)
//         {
//             var task = client.PostAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Send a POST request with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PostAsync(HttpClient client, string requestUri, HttpContent content,
//             CancellationToken cancellationToken)
//         {
//             var task = client.PostAsync(requestUri, content, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a POST request with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PostAsync(HttpClient client, Uri requestUri, HttpContent content,
//             CancellationToken cancellationToken)
//         {
//             var task = client.PostAsync(requestUri, content, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a PUT request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PutAsync(HttpClient client, string requestUri, HttpContent content)
//         {
//             var task = client.PutAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Send a PUT request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PutAsync(HttpClient client, Uri requestUri, HttpContent content)
//         {
//             var task = client.PutAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Send a PUT request with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PutAsync(HttpClient client, string requestUri, HttpContent content,
//             CancellationToken cancellationToken)
//         {
//             var task = client.PutAsync(requestUri, content, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a PUT request with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PutAsync(HttpClient client, Uri requestUri, HttpContent content,
//             CancellationToken cancellationToken)
//         {
//             var task = client.PutAsync(requestUri, content, cancellationToken);
//             return task;
//         }
//         /// <summary>
//         /// Send a DELETE request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string requestUri)
//         {
//             var task = client.DeleteAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a DELETE request to the specified Uri as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Uri requestUri)
//         {
//             var task = client.DeleteAsync(requestUri);
//             return task;
//         }

//         /// <summary>
//         /// Send a DELETE request to the specified Uri with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string requestUri,
//             CancellationToken cancellationToken)
//         {
//             var task = client.DeleteAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send a DELETE request to the specified Uri with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Uri requestUri,
//             CancellationToken cancellationToken)
//         {
//             var task = client.DeleteAsync(requestUri, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Sends a PATCH request to a Uri designated as a string as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent content)
//         {
//             var task = client.PatchAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Sends a PATCH request as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PatchAsync(HttpClient client, Uri requestUri, HttpContent content)
//         {
//             var task = client.PatchAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Sends a PATCH request with a cancellation token to a Uri represented as a string
//         /// as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent content,
//             CancellationToken cancellationToken)
//         {
//             var task = client.PatchAsync(requestUri, content);
//             return task;
//         }

//         /// <summary>
//         /// Sends a PATCH request with a cancellation token as an asynchronous operation.
//         /// </summary>
//         public static Task<HttpResponseMessage> PatchAsync(HttpClient client, Uri requestUri, HttpContent content,
//             CancellationToken cancellationToken)
//         {
//             var task = client.PatchAsync(requestUri, content, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Sends an HTTP request.
//         /// </summary>
//         [UnsupportedOSPlatform("browser")]
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static HttpResponseMessage Send(HttpClient client, HttpRequestMessage request)
//         {
//             return client.Send(request);
//         }

//         /// <summary>
//         /// Sends an HTTP request.
//         /// </summary>
//         [UnsupportedOSPlatform("browser")]
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static HttpResponseMessage Send(HttpClient client, HttpRequestMessage request,
//             HttpCompletionOption completionOption)
//         {
//             return client.Send(request, completionOption);
//         }

//         /// <summary>
//         /// Sends an HTTP request with the specified request and cancellation token.
//         /// </summary>
//         [UnsupportedOSPlatform("browser")]
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static HttpResponseMessage Send(HttpClient client, HttpRequestMessage request,
//             CancellationToken cancellationToken)
//         {
//             return client.Send(request, cancellationToken);
//         }

//         /// <summary>
//         /// Sends an HTTP request with the specified request, completion option and cancellation token.
//         /// </summary>
//         [UnsupportedOSPlatform("browser")]
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static HttpResponseMessage Send(HttpClient client, HttpRequestMessage request,
//             HttpCompletionOption completionOption, CancellationToken cancellationToken)
//         {
//             return client.Send(request, completionOption, cancellationToken);
//         }

//         /// <summary>
//         /// Send an HTTP request as an asynchronous operation.
//         /// </summary>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request)
//         {
//             var task = client.SendAsync(request);
//             return task;
//         }

//         /// <summary>
//         /// Send an HTTP request as an asynchronous operation.
//         /// </summary>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request,
//             HttpCompletionOption completionOption)
//         {
//             var task = client.SendAsync(request, completionOption);
//             return task;
//         }

//         /// <summary>
//         /// Send an HTTP request as an asynchronous operation.
//         /// </summary>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request,
//             CancellationToken cancellationToken)
//         {
//             var task = client.SendAsync(request, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Send an HTTP request as an asynchronous operation.
//         /// </summary>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request,
//             HttpCompletionOption completionOption, CancellationToken cancellationToken)
//         {
//             var task = client.SendAsync(request, completionOption, cancellationToken);
//             return task;
//         }

//         /// <summary>
//         /// Cancel all pending requests on this instance.
//         /// </summary>
//         public static void CancelPendingRequests(HttpClient client)
//         {
//             client.CancelPendingRequests();
//         }
//     }
// }
