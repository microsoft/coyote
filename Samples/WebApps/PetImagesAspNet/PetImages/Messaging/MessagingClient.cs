// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace PetImages.Messaging
{
    internal class MessagingClient : IMessagingClient
    {
        public Task SubmitMessage(Message message) => throw new NotImplementedException();
    }
}
