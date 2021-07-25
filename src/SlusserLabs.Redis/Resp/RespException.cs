// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="RespException" /> class.
        /// </summary>
        /// <param name="message">The context-specific error message.</param>
        /// <param name="bytePositon">The byte offset where the exception occurred.</param>
        public RespException(string message, long? bytePositon) : base(message)
        {
            BytePosition = bytePositon;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RespException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="info"/> is <see langword="null" />.
        /// </exception>
        protected RespException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            BytePosition = (long?)info.GetValue("BytePosition", typeof(long?));
        }

        /// <summary>
        /// Gets the byte offset where the exception occurred (starting at 0).
        /// </summary>
        public long? BytePosition { get; }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(BytePosition), BytePosition, typeof(long?));
        }
    }
}

// References:
// https://github.com/dotnet/runtime/blob/v5.0.1/src/libraries/System.Text.Json/src/System/Text/Json/JsonException.cs
// https://blog.gurock.com/articles/creating-custom-exceptions-in-dotnet/
