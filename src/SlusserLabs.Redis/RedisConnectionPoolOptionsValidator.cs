// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    internal sealed class RedisConnectionPoolOptionsValidator : IValidateOptions<RedisConnectionPoolOptions>
    {
        private static readonly Regex _paramsRegex = new Regex("([^,=]+)(?:=([^,]+))?", RegexOptions.Compiled);
        private static readonly Regex _paramAssignmentRegex = new Regex(@"\s*=\s*", RegexOptions.Compiled);

        public ValidateOptionsResult Validate(string configurationName, RedisConnectionPoolOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail($"The '{nameof(RedisConnectionPoolOptions.ConnectionString)}' property is required.");
            }

            // Parse the connection string.
            // NOTE: This is not a performance or memory critical section, so we favor maintainability.
            var pairs = options.ConnectionString.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                var name = parts.Length > 0 ? parts[0] : null;
                var value = parts.Length > 1 ? parts[1] : null;

                if (string.IsNullOrEmpty(name))
                {
                    return ValidateOptionsResult.Fail($"The '{nameof(RedisConnectionPoolOptions.ConnectionString)}' contains an invalid name-value pair.");
                }
                else if (string.IsNullOrEmpty(value))
                {
                    // Assume it is the endpoint
                    options.EndPoint = name;
                }
                else
                {
                    // Set option properties that aren't already set
                    if (string.Equals(name, nameof(options.MaxPoolSize), StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(value, out var maxPoolSize))
                        {
                            return FailedPropertyValidateOptionsResult(nameof(options.MaxPoolSize), true);
                        }

                        options.MaxPoolSize ??= maxPoolSize;
                    }
                }
            }

            // Set defaults for option properties not already set
            options.MaxPoolSize ??= RedisConnectionPoolOptions.DefaultMaxPoolSize;

            // Finally, validate the assigned properties
            if (string.IsNullOrEmpty(options.EndPoint))
            {
                return ValidateOptionsResult.Fail($"The endpoint specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' is missing or invalid.");
            }
            else if (options.MaxPoolSize < 1)
            {
                return FailedPropertyValidateOptionsResult(nameof(options.MaxPoolSize), false);
            }

            return ValidateOptionsResult.Success;
        }

        private ValidateOptionsResult FailedPropertyValidateOptionsResult(string propertyName, bool fromConnectionString)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName));

            // NOTE: We don't include any values in the failure messages to prevent disclosure of secrets
            if (fromConnectionString)
            {
                // A more specific message when the input came from the connection string
                return ValidateOptionsResult.Fail($"The '{propertyName}' value specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' is invalid.");
            }

            return ValidateOptionsResult.Fail($"The '{propertyName}' value specified is invalid.");
        }
    }
}
