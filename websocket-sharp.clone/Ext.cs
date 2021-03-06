/*
 * Ext.cs
 *
 * Some parts of this code are derived from Mono (http://www.mono-project.com):
 * - GetStatusDescription is derived from System.Net.HttpListenerResponse.cs
 * - IsPredefinedScheme is derived from System.Uri.cs
 * - MaybeUri is derived from System.Uri.cs
 *
 * The MIT License
 *
 * Copyright (c) 2001 Garrett Rooney
 * Copyright (c) 2003 Ian MacLean
 * Copyright (c) 2003 Ben Maurer
 * Copyright (c) 2003, 2005, 2009 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2009 Stephane Delcroix
 * Copyright (c) 2010-2014 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/*
 * Contributors:
 * - Liryna <liryna.stark@gmail.com>
 */

namespace WebSocketSharp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using WebSocketSharp.Net;
    using WebSocketSharp.Net.WebSockets;
    using WebSocketSharp.Server;

    using CookieCollection = WebSocketSharp.Net.CookieCollection;
    using HttpStatusCode = WebSocketSharp.Net.HttpStatusCode;

    /// <summary>
    ///     Provides a set of static methods for websocket-sharp.
    /// </summary>
    internal static class Ext
    {
        private const string Tspecials = "()<>@,;:\\\"/[]?={} \t";

        public static async Task<Stream> Compress(this Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var compressed = new MemoryStream();
            var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, true);
            await stream.CopyToAsync(deflate).ConfigureAwait(false);
            await deflate.FlushAsync().ConfigureAwait(false);
            deflate.Close();
            await compressed.FlushAsync().ConfigureAwait(false);

            compressed.Seek(0, SeekOrigin.Begin);

            return compressed;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="string" /> contains any of characters
        ///     in the specified array of <see cref="char" />.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="value" /> contains any of <paramref name="chars" />;
        ///     otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        ///     A <see cref="string" /> to test.
        /// </param>
        /// <param name="chars">
        ///     An array of <see cref="char" /> that contains characters to find.
        /// </param>
        public static bool Contains(this string value, params char[] chars)
        {
            return chars.Length == 0 || (!string.IsNullOrEmpty(value) && value.IndexOfAny(chars) > -1);
        }

        /// <summary>
        ///     Determines whether the specified <see cref="NameValueCollection" /> contains the entry
        ///     with the specified both <paramref name="name" /> and <paramref name="value" />.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="collection" /> contains the entry with both
        ///     <paramref name="name" /> and <paramref name="value" />; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="collection">
        ///     A <see cref="NameValueCollection" /> to test.
        /// </param>
        /// <param name="name">
        ///     A <see cref="string" /> that represents the key of the entry to find.
        /// </param>
        /// <param name="value">
        ///     A <see cref="string" /> that represents the value of the entry to find.
        /// </param>
        public static bool Contains(this NameValueCollection collection, string name, string value)
        {
            if (collection == null || collection.Count == 0)
            {
                return false;
            }

            var vals = collection[name];
            if (vals == null)
            {
                return false;
            }

            return vals.Split(',').Any(val => val.Trim().Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Gets the collection of the HTTP cookies from the specified HTTP <paramref name="headers" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="Net.CookieCollection" /> that receives a collection of the HTTP cookies.
        /// </returns>
        /// <param name="headers">
        ///     A <see cref="NameValueCollection" /> that contains a collection of the HTTP headers.
        /// </param>
        /// <param name="response">
        ///     <c>true</c> if <paramref name="headers" /> is a collection of the response headers;
        ///     otherwise, <c>false</c>.
        /// </param>
        public static CookieCollection GetCookies(this NameValueCollection headers, bool response)
        {
            var name = response ? "Set-Cookie" : "Cookie";
            return headers != null && headers.Contains(name)
                       ? CookieCollection.Parse(headers[name], response)
                       : new CookieCollection();
        }

        /// <summary>
        ///     Gets the description of the specified HTTP status <paramref name="code" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that represents the description of the HTTP status code.
        /// </returns>
        /// <param name="code">
        ///     One of <see cref="Net.HttpStatusCode" /> enum values, indicates the HTTP status code.
        /// </param>
        public static string GetDescription(this HttpStatusCode code)
        {
            return ((int)code).GetStatusDescription();
        }

        /// <summary>
        ///     Determines whether the specified <see cref="string" /> is enclosed in the specified
        ///     <see cref="char" />.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="value" /> is enclosed in <paramref name="c" />;
        ///     otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        ///     A <see cref="string" /> to test.
        /// </param>
        /// <param name="c">
        ///     A <see cref="char" /> that represents the character to find.
        /// </param>
        public static bool IsEnclosedIn(this string value, char c)
        {
            return value != null && value.Length > 1 && value[0] == c && value[value.Length - 1] == c;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Net.IPAddress" /> represents
        ///     the local IP address.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="address" /> represents the local IP address;
        ///     otherwise, <c>false</c>.
        /// </returns>
        /// <param name="address">
        ///     A <see cref="System.Net.IPAddress" /> to test.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="address" /> is <see langword="null" />.
        /// </exception>
        public static bool IsLocal(this IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.Equals(IPAddress.Any) || IPAddress.IsLoopback(address))
            {
                return true;
            }

            var host = Dns.GetHostName();
            var addrs = Dns.GetHostAddresses(host);
            return addrs.Contains(address);
        }
        
        /// <summary>
        ///     Determines whether the specified <see cref="string" /> is a URI string.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="value" /> may be a URI string; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        ///     A <see cref="string" /> to test.
        /// </param>
        public static bool MaybeUri(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var i = value.IndexOf(':');
            if (i == -1)
            {
                return false;
            }

            if (i >= 10)
            {
                return false;
            }

            return value.Substring(0, i).IsPredefinedScheme();
        }

        /// <summary>
        ///     Retrieves a sub-array from the specified <paramref name="array" />.
        ///     A sub-array starts at the specified element position in <paramref name="array" />.
        /// </summary>
        /// <returns>
        ///     An array of T that receives a sub-array, or an empty array of T
        ///     if any problems with the parameters.
        /// </returns>
        /// <param name="array">
        ///     An array of T from which to retrieve a sub-array.
        /// </param>
        /// <param name="startIndex">
        ///     An <see cref="int" /> that represents the zero-based starting position of
        ///     a sub-array in <paramref name="array" />.
        /// </param>
        /// <param name="length">
        ///     An <see cref="int" /> that represents the number of elements to retrieve.
        /// </param>
        /// <typeparam name="T">
        ///     The type of elements in <paramref name="array" />.
        /// </typeparam>
        public static T[] SubArray<T>(this T[] array, int startIndex, int length)
        {
            int len;
            if (array == null || (len = array.Length) == 0)
            {
                return new T[0];
            }

            if (startIndex < 0 || length <= 0 || startIndex + length > len)
            {
                return new T[0];
            }

            if (startIndex == 0 && length == len)
            {
                return array;
            }

            var subArray = new T[length];
            Array.Copy(array, startIndex, subArray, 0, length);

            return subArray;
        }
        
        /// <summary>
        ///     Converts the specified <see cref="string" /> to a <see cref="Uri" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="Uri" /> converted from <paramref name="uriString" />, or <see langword="null" />
        ///     if <paramref name="uriString" /> isn't successfully converted.
        /// </returns>
        /// <param name="uriString">
        ///     A <see cref="string" /> to convert.
        /// </param>
        public static Uri ToUri(this string uriString)
        {
            Uri res;
            return Uri.TryCreate(uriString, uriString.MaybeUri() ? UriKind.Absolute : UriKind.Relative, out res)
                       ? res
                       : null;
        }

        /// <summary>
        ///     URL-decodes the specified <see cref="string" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that receives the decoded string, or the <paramref name="value" />
        ///     if it's <see langword="null" /> or empty.
        /// </returns>
        /// <param name="value">
        ///     A <see cref="string" /> to decode.
        /// </param>
        public static string UrlDecode(this string value)
        {
            return !string.IsNullOrEmpty(value) ? HttpUtility.UrlDecode(value) : value;
        }

        /// <summary>
        ///     URL-encodes the specified <see cref="string" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that receives the encoded string, or <paramref name="value" />
        ///     if it's <see langword="null" /> or empty.
        /// </returns>
        /// <param name="value">
        ///     A <see cref="string" /> to encode.
        /// </param>
        public static string UrlEncode(this string value)
        {
            return !string.IsNullOrEmpty(value) ? HttpUtility.UrlEncode(value) : value;
        }

        internal static byte[] Append(this ushort code, string reason)
        {
            return code.InternalToByteArray(ByteOrder.Big).Take(2).Concat(Encoding.UTF8.GetBytes(reason)).ToArray();
        }

        internal static string CheckIfClosable(this WebSocketState state)
        {
            return state == WebSocketState.Closing
                       ? "While closing the WebSocket connection."
                       : state == WebSocketState.Closed ? "The WebSocket connection has already been closed." : null;
        }

        internal static string CheckIfConnectable(this WebSocketState state)
        {
            return state == WebSocketState.Open || state == WebSocketState.Closing
                       ? "A WebSocket connection has already been established."
                       : null;
        }

        internal static string CheckIfOpen(this WebSocketState state)
        {
            return state == WebSocketState.Connecting
                       ? "A WebSocket connection isn't established."
                       : state == WebSocketState.Closing
                             ? "While closing the WebSocket connection."
                             : state == WebSocketState.Closed
                                   ? "The WebSocket connection has already been closed."
                                   : null;
        }

        internal static string CheckIfStart(this ServerState state)
        {
            return state == ServerState.Ready
                       ? "The server hasn't yet started."
                       : state == ServerState.ShuttingDown
                             ? "The server is shutting down."
                             : state == ServerState.Stop ? "The server has already stopped." : null;
        }

        internal static string CheckIfStartable(this ServerState state)
        {
            return state == ServerState.Start
                       ? "The server has already started."
                       : state == ServerState.ShuttingDown ? "The server is shutting down." : null;
        }

        internal static string CheckIfValidControlData(this byte[] data, string paramName)
        {
            return data.Length > 125 ? $"'{paramName}' is greater than the allowable max size." : null;
        }

        internal static string CheckIfValidProtocols(this string[] protocols)
        {
            return protocols.Contains(protocol => string.IsNullOrEmpty(protocol) || !protocol.IsToken())
                       ? "Contains an invalid value."
                       : protocols.ContainsTwice() ? "Contains a value twice." : null;
        }

        internal static string CheckIfValidSendData(this Stream data)
        {
            return IsDataNull(data);
        }

        internal static string CheckIfValidSendData(this byte[] data)
        {
            return IsDataNull(data);
        }

        internal static string CheckIfValidSendData(this string data)
        {
            return IsDataNull(data);
        }

        internal static string CheckIfValidServicePath(this string path)
        {
            return string.IsNullOrEmpty(path)
                       ? "'path' is null or empty."
                       : path[0] != '/'
                             ? "'path' isn't an absolute path."
                             : path.IndexOfAny(new[] { '?', '#' }) > -1
                                   ? "'path' includes either or both query and fragment components."
                                   : null;
        }

        internal static string CheckIfValidSessionId(this string id)
        {
            return string.IsNullOrEmpty(id) ? "'id' is null or empty." : null;
        }

        internal static string CheckIfValidWaitTime(this TimeSpan time)
        {
            return time <= TimeSpan.Zero ? "A wait time is zero or less." : null;
        }

        internal static Task<Stream> Compress(this Stream stream, long length)
        {
            var subStream = new SubStream(stream, length);
            return subStream.Compress();
        }

        internal static bool Contains<T>(this IEnumerable<T> source, Func<T, bool> condition)
        {
            return source.Any(condition);
        }

        /// <summary>
        ///     Determines whether the specified <see cref="int" /> equals the specified <see cref="char" />,
        ///     and invokes the specified Action&lt;int&gt; delegate at the same time.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="value" /> equals <paramref name="c" />;
        ///     otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        ///     An <see cref="int" /> to compare.
        /// </param>
        /// <param name="c">
        ///     A <see cref="char" /> to compare.
        /// </param>
        /// <param name="action">
        ///     An Action&lt;int&gt; delegate that references the method(s) called at
        ///     the same time as comparing. An <see cref="int" /> parameter to pass to
        ///     the method(s) is <paramref name="value" />.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> isn't between 0 and 255.
        /// </exception>
        internal static bool EqualsWith(this int value, char c, Action<int> action)
        {
            if (value < 0 || value > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            action(value);
            return value == c - 0;
        }

        /// <summary>
        ///     Gets the absolute path from the specified <see cref="Uri" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that represents the absolute path if it's successfully found;
        ///     otherwise, <see langword="null" />.
        /// </returns>
        /// <param name="uri">
        ///     A <see cref="Uri" /> that represents the URI to get the absolute path from.
        /// </param>
        internal static string GetAbsolutePath(this Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return uri.AbsolutePath;
            }

            var original = uri.OriginalString;
            if (original[0] != '/')
            {
                return null;
            }

            var i = original.IndexOfAny(new[] { '?', '#' });
            return i > 0 ? original.Substring(0, i) : original;
        }

        internal static string GetMessage(this CloseStatusCode code)
        {
            return code == CloseStatusCode.ProtocolError
                       ? "A WebSocket protocol error has occurred."
                       : code == CloseStatusCode.IncorrectData
                             ? "An incorrect data has been received."
                             : code == CloseStatusCode.Abnormal
                                   ? "An exception has occurred."
                                   : code == CloseStatusCode.InconsistentData
                                         ? "An inconsistent data has been received."
                                         : code == CloseStatusCode.PolicyViolation
                                               ? "A policy violation has occurred."
                                               : code == CloseStatusCode.TooBig
                                                     ? "A too big data has been received."
                                                     : code == CloseStatusCode.IgnoreExtension
                                                           ? "WebSocket client didn't receive expected extension(s)."
                                                           : code == CloseStatusCode.ServerError
                                                                 ? "WebSocket server got an internal error."
                                                                 : code == CloseStatusCode.TlsHandshakeFailure
                                                                       ? "An error has occurred while handshaking."
                                                                       : string.Empty;
        }

        /// <summary>
        ///     Gets the value from the specified <see cref="string" /> that contains a pair of name and
        ///     value separated by a separator character.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that represents the value if any; otherwise, <c>null</c>.
        /// </returns>
        /// <param name="nameAndValue">
        ///     A <see cref="string" /> that contains a pair of name and value separated by a separator
        ///     character.
        /// </param>
        /// <param name="separator">
        ///     A <see cref="char" /> that represents the separator character.
        /// </param>
        internal static string GetValue(this string nameAndValue, char separator)
        {
            var i = nameAndValue.IndexOf(separator);
            return i > -1 && i < nameAndValue.Length - 1 ? nameAndValue.Substring(i + 1).Trim() : null;
        }

        internal static string GetValue(this string nameAndValue, char separator, bool unquote)
        {
            var i = nameAndValue.IndexOf(separator);
            if (i < 0 || i == nameAndValue.Length - 1)
            {
                return null;
            }

            var val = nameAndValue.Substring(i + 1).Trim();
            return unquote ? val.Unquote() : val;
        }

        internal static TcpListenerWebSocketContext GetWebSocketContext(
            this TcpClient tcpClient,
            string protocol,
            bool secure,
            ServerSslConfiguration sslConfiguration)
        {
            return new TcpListenerWebSocketContext(tcpClient, protocol, secure, sslConfiguration);
        }

        internal static bool IncludesReservedCloseStatusCode(this byte[] byteArray)
        {
            return byteArray.Length > 1 && byteArray.SubArray(0, 2).ToUInt16(ByteOrder.Big).IsReserved();
        }

        internal static byte[] InternalToByteArray(this ushort value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal static byte[] InternalToByteArray(this ulong value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal static bool IsCompressionExtension(this string value)
        {
            return value.StartsWith("permessage-");
        }

        internal static bool IsPortNumber(this int value)
        {
            return value > 0 && value < 65536;
        }

        internal static bool IsReserved(this ushort code)
        {
            return code == (ushort)CloseStatusCode.Undefined || code == (ushort)CloseStatusCode.NoStatusCode
                   || code == (ushort)CloseStatusCode.Abnormal || code == (ushort)CloseStatusCode.TlsHandshakeFailure;
        }

        internal static bool IsReserved(this CloseStatusCode code)
        {
            return code == CloseStatusCode.Undefined || code == CloseStatusCode.NoStatusCode
                   || code == CloseStatusCode.Abnormal || code == CloseStatusCode.TlsHandshakeFailure;
        }

        internal static bool IsText(this string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c < 0x20 && !"\r\n\t".Contains(c))
                {
                    return false;
                }

                if (c == 0x7f)
                {
                    return false;
                }

                if (c == '\n' && ++i < len)
                {
                    c = value[i];
                    if (!" \t".Contains(c))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool IsToken(this string value)
        {
            return value.All(c => c >= 0x20 && c < 0x7f && !Tspecials.Contains(c));
        }

        internal static string Quote(this string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        internal static async Task<byte[]> ReadBytes(this Stream stream, long length, int bufferLength)
        {
            using (var res = new MemoryStream())
            {
                var cnt = length / bufferLength;
                var rem = (int)(length % bufferLength);

                var buff = new byte[bufferLength];
                var end = false;
                for (long i = 0; i < cnt; i++)
                {
                    if (!await stream.ReadBytes(buff, 0, bufferLength, res).ConfigureAwait(false))
                    {
                        end = true;
                        break;
                    }
                }

                if (!end && rem > 0)
                {
                    await stream.ReadBytes(new byte[rem], 0, rem, res).ConfigureAwait(false);
                }

                res.Close();
                return res.ToArray();
            }
        }

        internal static async Task<byte[]> ReadBytes(this Stream stream, int length)
        {
            var buff = new byte[length];

            var len = await stream.ReadAsync(buff, 0, length).ConfigureAwait(false);

            var bytes = len < 1
                            ? new byte[0]
                            : len < length
                                  ? await stream.ReadBytes(buff, len, length - len).ConfigureAwait(false)
                                  : buff;

            return bytes;
        }

        internal static string RemovePrefix(this string value, params string[] prefixes)
        {
            var i = (from prefix in prefixes where value.StartsWith(prefix) select prefix.Length).FirstOrDefault();

            return i > 0 ? value.Substring(i) : value;
        }

        internal static IEnumerable<string> SplitHeaderValue(this string value, params char[] separators)
        {
            var len = value.Length;
            var seps = new string(separators);

            var buff = new StringBuilder(32);
            var escaped = false;
            var quoted = false;

            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c == '"')
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == '\\')
                {
                    if (i < len - 1 && value[i + 1] == '"')
                    {
                        escaped = true;
                    }
                }
                else if (seps.Contains(c))
                {
                    if (!quoted)
                    {
                        yield return buff.ToString();
                        buff.Length = 0;

                        continue;
                    }
                }

                buff.Append(c);
            }

            if (buff.Length > 0)
            {
                yield return buff.ToString();
            }
        }

        internal static async Task<byte[]> ToByteArray(this Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            using (var output = new MemoryStream())
            {
                //stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(output).ConfigureAwait(false);
                await output.FlushAsync().ConfigureAwait(false);

                return output.ToArray();
            }
        }

        internal static CompressionMethod ToCompressionMethod(this string value)
        {
            return
                Enum.GetValues(typeof(CompressionMethod))
                    .Cast<CompressionMethod>()
                    .FirstOrDefault(method => method.ToExtensionString() == value);
        }

        internal static string ToExtensionString(this CompressionMethod method)
        {
            return method != CompressionMethod.None ? $"permessage-{method.ToString().ToLower()}" : string.Empty;
        }

        internal static ushort ToUInt16(this byte[] source, ByteOrder sourceOrder)
        {
            return BitConverter.ToUInt16(source.ToHostOrder(sourceOrder), 0);
        }

        internal static ulong ToUInt64(this byte[] source, ByteOrder sourceOrder)
        {
            return BitConverter.ToUInt64(source.ToHostOrder(sourceOrder), 0);
        }

        internal static string TrimEndSlash(this string value)
        {
            value = value.TrimEnd('/');
            return value.Length > 0 ? value : "/";
        }

        /// <summary>
        ///     Tries to create a <see cref="Uri" /> for WebSocket with the specified
        ///     <paramref name="uriString" />.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if a <see cref="Uri" /> is successfully created; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="uriString">
        ///     A <see cref="string" /> that represents the WebSocket URL to try.
        /// </param>
        /// <param name="result">
        ///     When this method returns, a <see cref="Uri" /> that represents the WebSocket URL
        ///     if <paramref name="uriString" /> is valid; otherwise, <see langword="null" />.
        /// </param>
        /// <param name="message">
        ///     When this method returns, a <see cref="string" /> that represents the error message
        ///     if <paramref name="uriString" /> is invalid; otherwise, <see cref="String.Empty" />.
        /// </param>
        internal static bool TryCreateWebSocketUri(this string uriString, out Uri result, out string message)
        {
            result = null;
            if (uriString.Length == 0)
            {
                message = "An empty string.";
                return false;
            }

            var uri = uriString.ToUri();
            if (!uri.IsAbsoluteUri)
            {
                message = "Not an absolute URI: " + uriString;
                return false;
            }

            var schm = uri.Scheme;
            if (schm != "ws" && schm != "wss")
            {
                message = "The scheme part isn't 'ws' or 'wss': " + uriString;
                return false;
            }

            if (uri.Fragment.Length > 0)
            {
                message = "Includes the fragment component: " + uriString;
                return false;
            }

            var port = uri.Port;
            if (port > 0)
            {
                if (port > 65535)
                {
                    message = "The port part is greater than 65535: " + uriString;
                    return false;
                }

                if ((schm == "ws" && port == 443) || (schm == "wss" && port == 80))
                {
                    message = "An invalid pair of scheme and port: " + uriString;
                    return false;
                }
            }
            else
            {
                uri = new Uri($"{schm}://{uri.Host}:{(schm == "ws" ? 80 : 443)}{uri.PathAndQuery}");
            }

            result = uri;
            message = string.Empty;

            return true;
        }

        internal static Task WriteBytes(this Stream stream, byte[] bytes)
        {
            using (var input = new MemoryStream(bytes))
            {
                return input.CopyToAsync(stream);
            }
        }

        /// <summary>
        ///     Determines whether the specified <see cref="NameValueCollection" /> contains the entry
        ///     with the specified <paramref name="name" />.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="collection" /> contains the entry
        ///     with <paramref name="name" />; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="collection">
        ///     A <see cref="NameValueCollection" /> to test.
        /// </param>
        /// <param name="name">
        ///     A <see cref="string" /> that represents the key of the entry to find.
        /// </param>
        private static bool Contains(this NameValueCollection collection, string name)
        {
            return collection != null && collection.Count > 0 && collection[name] != null;
        }

        private static bool ContainsTwice(this string[] values)
        {
            var len = values.Length;

            Func<int, bool> contains = null;
            contains = idx =>
                {
                    if (idx < len - 1)
                    {
                        for (var i = idx + 1; i < len; i++)
                        {
                            if (values[i] == values[idx])
                            {
                                return true;
                            }
                        }

                        return contains(++idx);
                    }

                    return false;
                };

            return contains(0);
        }

        /// <summary>
        ///     Gets the description of the specified HTTP status <paramref name="code" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="string" /> that represents the description of the HTTP status code.
        /// </returns>
        /// <param name="code">
        ///     An <see cref="int" /> that represents the HTTP status code.
        /// </param>
        private static string GetStatusDescription(this int code)
        {
            switch (code)
            {
                case 100:
                    return "Continue";
                case 101:
                    return "Switching Protocols";
                case 102:
                    return "Processing";
                case 200:
                    return "OK";
                case 201:
                    return "Created";
                case 202:
                    return "Accepted";
                case 203:
                    return "Non-Authoritative Information";
                case 204:
                    return "No Content";
                case 205:
                    return "Reset Content";
                case 206:
                    return "Partial Content";
                case 207:
                    return "Multi-Status";
                case 300:
                    return "Multiple Choices";
                case 301:
                    return "Moved Permanently";
                case 302:
                    return "Found";
                case 303:
                    return "See Other";
                case 304:
                    return "Not Modified";
                case 305:
                    return "Use Proxy";
                case 307:
                    return "Temporary Redirect";
                case 400:
                    return "Bad Request";
                case 401:
                    return "Unauthorized";
                case 402:
                    return "Payment Required";
                case 403:
                    return "Forbidden";
                case 404:
                    return "Not Found";
                case 405:
                    return "Method Not Allowed";
                case 406:
                    return "Not Acceptable";
                case 407:
                    return "Proxy Authentication Required";
                case 408:
                    return "Request Timeout";
                case 409:
                    return "Conflict";
                case 410:
                    return "Gone";
                case 411:
                    return "Length Required";
                case 412:
                    return "Precondition Failed";
                case 413:
                    return "Request Entity Too Large";
                case 414:
                    return "Request-Uri Too Long";
                case 415:
                    return "Unsupported Media Type";
                case 416:
                    return "Requested Range Not Satisfiable";
                case 417:
                    return "Expectation Failed";
                case 422:
                    return "Unprocessable Entity";
                case 423:
                    return "Locked";
                case 424:
                    return "Failed Dependency";
                case 500:
                    return "Internal Server Error";
                case 501:
                    return "Not Implemented";
                case 502:
                    return "Bad Gateway";
                case 503:
                    return "Service Unavailable";
                case 504:
                    return "Gateway Timeout";
                case 505:
                    return "Http Version Not Supported";
                case 507:
                    return "Insufficient Storage";
            }

            return string.Empty;
        }

        private static string IsDataNull(object data)
        {
            return data == null ? "'data' is null." : null;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="ByteOrder" /> is host (this computer
        ///     architecture) byte order.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="order" /> is host byte order; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="order">
        ///     One of the <see cref="ByteOrder" /> enum values, to test.
        /// </param>
        private static bool IsHostOrder(this ByteOrder order)
        {
            // true : !(true ^ true)  or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == ByteOrder.Little));
        }

        /// <summary>
        ///     Determines whether the specified <see cref="string" /> is a predefined scheme.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if <paramref name="value" /> is a predefined scheme; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        ///     A <see cref="string" /> to test.
        /// </param>
        private static bool IsPredefinedScheme(this string value)
        {
            if (value == null || value.Length < 2)
            {
                return false;
            }

            var c = value[0];
            if (c == 'h')
            {
                return value == "http" || value == "https";
            }

            if (c == 'w')
            {
                return value == "ws" || value == "wss";
            }

            if (c == 'f')
            {
                return value == "file" || value == "ftp";
            }

            if (c == 'n')
            {
                c = value[1];
                return c == 'e' ? value == "news" || value == "net.pipe" || value == "net.tcp" : value == "nntp";
            }

            return (c == 'g' && value == "gopher") || (c == 'm' && value == "mailto");
        }

        private static async Task<byte[]> ReadBytes(this Stream stream, byte[] buffer, int offset, int length)
        {
            var len = 0;
            try
            {
                len = await stream.ReadAsync(buffer, offset, length).ConfigureAwait(false);
                if (len < 1)
                {
                    return buffer.SubArray(0, offset);
                }

                while (len < length)
                {
                    var readLen = await stream.ReadAsync(buffer, offset + len, length - len).ConfigureAwait(false);
                    if (readLen < 1)
                    {
                        break;
                    }

                    len += readLen;
                }
            }
            catch
            {
            }

            return len < length ? buffer.SubArray(0, offset + len) : buffer;
        }

        private static async Task<bool> ReadBytes(
            this Stream stream,
            byte[] buffer,
            int offset,
            int length,
            Stream destination)
        {
            var bytes = await stream.ReadBytes(buffer, offset, length).ConfigureAwait(false);
            var len = bytes.Length;
            await destination.WriteAsync(bytes, 0, len).ConfigureAwait(false);

            return len == offset + length;
        }

        /// <summary>
        ///     Converts the order of the specified array of <see cref="byte" /> to the host byte order.
        /// </summary>
        /// <returns>
        ///     An array of <see cref="byte" /> converted from <paramref name="source" />.
        /// </returns>
        /// <param name="source">
        ///     An array of <see cref="byte" /> to convert.
        /// </param>
        /// <param name="sourceOrder">
        ///     One of the <see cref="ByteOrder" /> enum values, indicates the byte order of
        ///     <paramref name="source" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        private static byte[] ToHostOrder(this byte[] source, ByteOrder sourceOrder)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Length > 1 && !sourceOrder.IsHostOrder() ? source.Reverse().ToArray() : source;
        }

        private static string Unquote(this string value)
        {
            var start = value.IndexOf('"');
            if (start < 0)
            {
                return value;
            }

            var end = value.LastIndexOf('"');
            var len = end - start - 1;

            return len < 0 ? value : len == 0 ? string.Empty : value.Substring(start + 1, len).Replace("\\\"", "\"");
        }
    }
}