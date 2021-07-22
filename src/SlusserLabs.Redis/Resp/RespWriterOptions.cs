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
    /// Defines custom behavior when writing RESP using a <see cref="RespWriter" />.
    /// </summary>
    public struct RespWriterOptions
    {
        /// <summary>
        /// Gets or sets whether the <see cref="RespWriter" /> should skip structural and content validation
        /// and allow the caller to write invalid RESP.
        /// </summary>
        /// <value>
        /// <c>true</c> to allow the caller to write invalid RESP, otherwise, <c>false</c> to throw and
        /// <see cref="InvalidOperationException" /> when attempting to write invalid RESP.</value>
        /// <remarks>
        /// If the RESP being written is known to be correct, then skipping validation by setting this to
        /// <c>true</c> could improve performance. An example of invalid RESP is where a Simple String contains
        /// either <c>\r</c> or <c>\n</c> or a Bulk String does not match the length of bytes specified.
        /// </remarks>
        public bool SkipValidation { get; set; }
    }
}
