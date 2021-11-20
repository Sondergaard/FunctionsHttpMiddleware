// Copyright 2020 Energinet DataHub A/S
// 
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Azure;

namespace Company.FunctionApp.Trace
{
    public class HttpResponseDataStreamReader
    {
        private readonly HttpResponseData _responseData;
        private static readonly Dictionary<HttpStatusCode, string> KnownStatusCodes;
        private const string CRLF = "\r\n";
        private const string HTTP_VERSION = "HTTP/1.1 ";
        private const string COLON_SEPARATOR = ": ";
        private const string COMMA_SEPARATOR = ", ";
        private const int HeaderAllocationInBytes = 4096;

        static HttpResponseDataStreamReader()
        {
            KnownStatusCodes = Enum.GetValues<HttpStatusCode>().Distinct().ToDictionary(
                key => key,
                val => $"{(int)val} {Enum.GetName(val)}");
        }
        
        public HttpResponseDataStreamReader(HttpResponseData responseData)
        {
            _responseData = responseData;
        }

        public async Task<Stream> GetStream()
        {
            var ms = new MemoryStream(4096);

            var writer = new StreamWriter(ms, Encoding.UTF8);
            await writer.WriteLineAsync(GetStatusLine());
            
            await writer.WriteLineAsync(GetHeaderString());

            await writer.FlushAsync();
            
            _responseData.Body.Position = 0;
            await _responseData.Body.CopyToAsync(ms);

            await writer.FlushAsync();
            
            ms.Position = 0;
            return ms;
        }

        public async Task<ReadOnlyMemory<byte>> GetBytesMemoryAll()
        {
            var enc = Encoding.UTF8;
            var statusLine = enc.GetBytes(GetStatusLine());
            var headers = enc.GetBytes(GetHeaderString());
            var lineBreak = enc.GetBytes(CRLF);
            var bodyLength = (int)_responseData.Body.Length; // we don't support more then 2GB of data and that's OK
            
            var totalLength = statusLine.Length + headers.Length + bodyLength + lineBreak.Length;
            var owner = MemoryPool<byte>.Shared.Rent(totalLength);
            
            try
            {
                var memory = owner.Memory;
                var position = 0;
                
                statusLine.CopyTo(memory[position..statusLine.Length]);
                position += statusLine.Length;
                
                headers.CopyTo(memory[position..(position+headers.Length)]);
                position += headers.Length;

                lineBreak.CopyTo(memory[position..(position+lineBreak.Length)]);
                position += lineBreak.Length;
                
                _responseData.Body.Position = 0;
                await _responseData.Body.ReadAsync(memory[position..]);
                
                return memory;
            }
            finally
            {
                owner.Dispose();
            }
        }

        public async Task<ReadOnlyMemory<byte>> GetBytesMemory()
        {
            var enc = Encoding.UTF8;
            var bodyLength = (int)_responseData.Body.Length; // we don't support more then 2GB of data and that's OK
            
            var owner = MemoryPool<byte>.Shared.Rent(bodyLength + HeaderAllocationInBytes);
            
            try
            {
                var memory = owner.Memory;
                var position = ComposeHeader(memory, enc);
                
                _responseData.Body.Position = 0;

                int bytesRead;
                do
                {
                    var read = Math.Min(bodyLength, 4096);
                    var tmp = position + read;

                    bytesRead = await _responseData.Body.ReadAsync(memory[position..tmp]);
                    
                    bodyLength -= read;
                    position = tmp;

                } while (bytesRead > 0);
                
                return memory;
            }
            finally
            {
                owner.Dispose();
            }
        }

        private int ComposeHeader(Memory<byte> memory, Encoding encoding)
        {
            var position = 0;
            position += encoding.GetBytes(HTTP_VERSION, memory[position..].Span);
            position += encoding.GetBytes(KnownStatusCodes[_responseData.StatusCode], memory[position..].Span);
            position += encoding.GetBytes(CRLF, memory[position..].Span);

            foreach (var (key, value) in _responseData.Headers)
            {
                var t = false;
                position += encoding.GetBytes(key, memory[position..].Span);
                position += encoding.GetBytes(COLON_SEPARATOR, memory[position..].Span);
                foreach (var val in value)
                {
                    if (t)
                        position += encoding.GetBytes(COMMA_SEPARATOR, memory[position..].Span);
                    
                    position += encoding.GetBytes(val, memory[position..].Span);
                    t = true;
                }
                
                position += encoding.GetBytes(CRLF, memory[position..].Span);
            }

            return position;
        }

        public async Task<ReadOnlyMemory<byte>> GetBytesArray()
        {
            var enc = Encoding.UTF8;
            var statusLine = enc.GetBytes(GetStatusLine());
            var headers = enc.GetBytes(GetHeaderString());
            var lineBreak = enc.GetBytes(CRLF);
            var bodyLength = (int)_responseData.Body.Length; // we don't support more then 2GB of data and that's OK
            
            var totalLength = statusLine.Length + headers.Length + bodyLength + lineBreak.Length + 2;
            var owner = MemoryPool<byte>.Shared.Rent(totalLength);
            var arrayPool = ArrayPool<byte>.Shared;

            try
            {
                var memory = owner.Memory;
                var position = 0;
                
                statusLine.CopyTo(memory[position..statusLine.Length]);
                position += statusLine.Length;
                
                headers.CopyTo(memory[position..(position+headers.Length)]);
                position += headers.Length;

                lineBreak.CopyTo(memory[position..(position+lineBreak.Length)]);
                position += lineBreak.Length;
                
                
                _responseData.Body.Position = 0;

                int bytesRead;
                do
                {
                    var read = Math.Min(bodyLength, 4096);
                    var array = arrayPool.Rent(read);
                    bytesRead = await _responseData.Body.ReadAsync(array, 0, read);

                    var tmp = position + read;
                    bodyLength -= read;
                    try
                    {
                        array.AsSpan(0, read).CopyTo(memory[position..].Span);
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                    position = tmp;
                    arrayPool.Return(array);
                } while (bytesRead > 0);


                return memory;
            }
            finally
            {
                owner.Dispose();
            }
        }

        private string GetHeaderString()
        {
            var headerCollection = _responseData.Headers
                .Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")
                .ToArray<string>();

            var headerString = (headerCollection.Any()) ? string.Join(CRLF, headerCollection) + CRLF : string.Empty;
            return headerString;
        }

        private string GetStatusLine() => 
            "HTTP/1.1 " + (KnownStatusCodes.GetValueOrDefault(_responseData.StatusCode) ?? $"{(int)_responseData.StatusCode} {Enum.GetName(_responseData.StatusCode)}") + CRLF;
        
    }
}