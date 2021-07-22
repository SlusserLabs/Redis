// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    /// <summary>
    /// Defines an exception when invalid RESP data is encountered.
    /// </summary>
    // [Serializable]
    public class RespException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RespException" /> class.
        /// </summary>
        public RespException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RespException" /> class.
        /// </summary>
        /// <param name="message">The context-specific error message.</param>
        public RespException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RespException" /> class.
        /// </summary>
        /// <param name="message">The context-specific error message.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public RespException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

// References:
// https://github.com/dotnet/runtime/blob/v5.0.1/src/libraries/System.Text.Json/src/System/Text/Json/JsonException.cs
// https://blog.gurock.com/articles/creating-custom-exceptions-in-dotnet/
