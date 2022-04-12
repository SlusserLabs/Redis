// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Extension methods for common Redis commands.
    /// </summary>
    public static class RedisConnectionExtensions
    {
        public static Task HSetAsync(this IRedisConnection connection, string key, string field, string value)
        {
            throw new NotImplementedException();
        }
    }
}
