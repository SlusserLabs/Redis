// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    internal sealed class RedisConnectionPoolOptionsValidator : IValidateOptions<RedisConnectionPoolOptions>
    {
        public ValidateOptionsResult Validate(string configurationName, RedisConnectionPoolOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail($"The '{nameof(RedisConnectionPoolOptions.ConnectionString)}' property is required.");
            }

            string? failureMessage;

            if (!TryParseConnectionString(options, out failureMessage))
            {
                return ValidateOptionsResult.Fail(failureMessage);
            }

            if (!TryValidateOptions(options, out failureMessage))
            {
                return ValidateOptionsResult.Fail(failureMessage);
            }

            return ValidateOptionsResult.Success;
        }


        private static bool TryParseConnectionString(RedisConnectionPoolOptions options, out string? failureMessage)
        {
            // NOTE: We favor maintainability and simplicity here because this is not
            //       a performance or memory critical section, hence the simple string parsing
            //       and try/catch blocks.

            var pairs = options.ConnectionString!.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                var name = parts.Length > 0 ? parts[0].Trim() : null;
                var value = parts.Length > 1 ? parts[1].Trim() : null;

                if (string.IsNullOrEmpty(name))
                {
                    failureMessage = $"The '{nameof(RedisConnectionPoolOptions.ConnectionString)}' contains an invalid name-value pair.";
                    return false;
                }
                else if (string.IsNullOrEmpty(value))
                {
                    // Assume it's the endpoint
                    try
                    {
                        var startIndex = name.StartsWith('[') ? name.IndexOf(']') : 0;
                        var portIndex = name.IndexOf(':', startIndex);
                        if (portIndex == -1)
                        {
                            // Requiring the user to specify a port simplifies our logic and avoids guessing at the TLS port
                            failureMessage = $"The endpoint specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' requires a port number.";
                            return false;
                        }

                        var host = name.Substring(0, portIndex);
                        var port = short.Parse(name.Substring(portIndex + 1));

                        switch (Uri.CheckHostName(host))
                        {
                            case UriHostNameType.Dns:
                                options.EndPoint ??= new DnsEndPoint(host, port);
                                break;
                            case UriHostNameType.IPv4:
                            case UriHostNameType.IPv6:
                                options.EndPoint ??= new IPEndPoint(IPAddress.Parse(host), port);
                                break;
                            default:
                                failureMessage = $"The endpoint specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' is a format not supported.";
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);

                        failureMessage = $"The endpoint specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' could not be parsed.";
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        // Set option properties that aren't already set
                        if (string.Equals(name, nameof(options.MaxPoolSize), StringComparison.OrdinalIgnoreCase))
                        {
                            var maxPoolSize = int.Parse(value);
                            options.MaxPoolSize ??= maxPoolSize;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);

                        failureMessage = $"The '{name}' value specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' could not be parsed.";
                        return false;
                    }
                }
            }

            AssignDefaults(options);

            failureMessage = null;
            return true;
        }

        private static bool TryValidateOptions(RedisConnectionPoolOptions options, out string? failureMessage)
        {
            // Finally, validate the assigned properties
            if (options.EndPoint == null)
            {
                failureMessage = $"The endpoint specified in the '{nameof(RedisConnectionPoolOptions.ConnectionString)}' is missing or invalid.";
                return false;
            }
            else if (options.MaxPoolSize < 1)
            {
                failureMessage = $"The '{nameof(options.MaxPoolSize)}' value specified is invalid.";
                return false;
            }

            failureMessage = null;
            return true;
        }

        private static void AssignDefaults(RedisConnectionPoolOptions options)
        {
            // Set defaults for option properties not already set
            options.MaxPoolSize ??= RedisConnectionPoolOptions.DefaultMaxPoolSize;
        }
    }
}
