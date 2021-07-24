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
    /// Defines custom behavior when reading RESP using a <see cref="RespReader" />.
    /// </summary>
    public struct RespReaderOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="RespReader" /> should skip structural and content validation
        /// and allow the caller to read invalid RESP.
        /// </summary>
        /// <value>
        /// <c>true</c> to allow the caller to read invalid RESP, otherwise, <c>false</c> to throw a
        /// <see cref="RespException" /> when attempting to read invalid RESP.</value>
        /// <remarks>
        /// Disabling validation is not recommended as it could allow corrupt data to be interpreted as valid.
        /// </remarks>
        public bool SkipValidation { get; set; }
    }
}
