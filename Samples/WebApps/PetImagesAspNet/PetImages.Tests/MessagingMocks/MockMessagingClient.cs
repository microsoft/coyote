// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using PetImages.Messaging;
using PetImages.Storage;
using PetImages.Worker;

namespace PetImages.Tests.MessagingMocks
{
    public class MockMessagingClient : IMessagingClient
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

        private readonly IWorker GenerateThumbnailWorker;

        public MockMessagingClient(IBlobContainer blobContainer)
        {
            this.GenerateThumbnailWorker = new GenerateThumbnailWorker(blobContainer);
        }

        public Task SubmitMessage(Message message)
        {
            // Fire-and-forget the task to model sending an asynchronous message over the network.
            _ = Task.Run(async () =>
            {
                await InjectYieldsAtMethodStart();
                try
                {
                    if (message.Type == Message.GenerateThumbnailMessageType)
                    {
                        var clonedMessage = TestHelper.Clone((GenerateThumbnailMessage)message);
                        await this.GenerateThumbnailWorker.ProcessMessage(clonedMessage);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (Exception)
                {
                    Specification.Assert(false, "Uncaught exception in worker");
                }
            });

            return Task.CompletedTask;
        }
    }
}
