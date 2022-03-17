// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PetImages.Exceptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException()
        {
        }

        public DatabaseException(string message) : base(message)
        {
        }
    }

    public class DatabaseContainerAlreadyExists : DatabaseException
    {
    }

    public class DatabaseContainerDoesNotExist : DatabaseException
    {
    }

    public class DatabaseItemAlreadyExistsException : DatabaseException
    {
    }

    public class DatabaseItemDoesNotExistException : DatabaseException
    {
    }
}
