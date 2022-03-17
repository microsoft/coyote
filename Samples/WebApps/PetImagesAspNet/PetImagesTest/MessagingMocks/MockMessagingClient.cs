// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using PetImages.Messaging;
using PetImages.Storage;
using PetImages.Worker;

namespace PetImagesTest.MessagingMocks
{
    public class MockMessagingClient : IMessagingClient
    {
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
