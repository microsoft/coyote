// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Messaging;

namespace PetImages.Worker
{
    public interface IWorker
    {
        Task ProcessMessage(Message message);
    }
}
