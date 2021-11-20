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
using System.Linq;
using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace Company.FunctionApp.Trace.Benchmark
{
    [MemoryDiagnoser()]
    public class StringBuildingBenchmark
    {
        private BenchmarkResponse _response;
        private IMemoryOwner<byte> _owner;
        private StringConcatenation _concat;
        private StringInMemory _inMemory;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var contextMock = new Mock<FunctionContext>();
            _owner = MemoryPool<byte>.Shared.Rent();
            _concat = new StringConcatenation(new BenchmarkResponse(5, contextMock.Object));
            _inMemory = new StringInMemory(new BenchmarkResponse(5, contextMock.Object));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _owner.Dispose();
        }

        [Benchmark(Baseline = true)]
        public Memory<byte> StrConcat()
        {
            _concat.GetData(_owner.Memory);
            return _owner.Memory;
        }

        [Benchmark]
        public Memory<byte> StrInMem()
        {
            return _inMemory.GetData(_owner.Memory);
        }
    }

    public class StringInMemory
    {
        private readonly HttpResponseData _responseData;
        private static readonly Dictionary<HttpStatusCode, string> KnownStatusCodes;
        private const string CRLF = "\r\n";
        private const string HTTP_VERSION = "HTTP/1.1 ";
        private const string COLON_SEPARATOR = ": ";
        private const string COMMA_SEPARATOR = ", ";

        static StringInMemory()
        {
            KnownStatusCodes = Enum.GetValues<HttpStatusCode>().Distinct().ToDictionary(
                key => key, 
                val => $"{(int)val} {Enum.GetName(val)}");
        }

        public StringInMemory(HttpResponseData responseData)
        {
            _responseData = responseData;
        }

        public Memory<byte> GetData(Memory<byte> memory)
        {
            var enc = Encoding.UTF8;
            var position = 0;
            position += enc.GetBytes(HTTP_VERSION, memory[position..].Span);
            position += enc.GetBytes(KnownStatusCodes[_responseData.StatusCode], memory[position..].Span);
            position += enc.GetBytes(CRLF, memory[position..].Span);

            foreach (var (key, value) in _responseData.Headers)
            {
                var t = false;
                position += enc.GetBytes(key, memory[position..].Span);
                position += enc.GetBytes(COLON_SEPARATOR, memory[position..].Span);
                foreach (var val in value)
                {
                    if (t)
                        position += enc.GetBytes(COMMA_SEPARATOR, memory[position..].Span);
                    
                    position += enc.GetBytes(val, memory[position..].Span);
                    t = true;
                }
                
                position += enc.GetBytes(CRLF, memory[position..].Span);
            }

            return memory[..position];
        }
    }
    
    public class StringConcatenation
    {
        private readonly HttpResponseData _responseData;
        private static readonly Dictionary<HttpStatusCode, string> KnownStatusCodes;
        private const string CRLF = "\r\n";

        static StringConcatenation()
        {
            KnownStatusCodes = Enum.GetValues<HttpStatusCode>().Distinct().ToDictionary(
                key => key,
                val => $"{(int)val} {Enum.GetName(val)}");
        }

        public StringConcatenation(HttpResponseData responseData)
        {
            _responseData = responseData;
        }

        public void GetData(Memory<byte> memory)
        {
            var enc = Encoding.UTF8;
            var statusLine = enc.GetBytes(GetStatusLine());
            var headers = enc.GetBytes(GetHeaderString());
            var lineBreak = enc.GetBytes(CRLF);
            var position = 0;
            
            statusLine.CopyTo(memory[position..statusLine.Length]);
            position += statusLine.Length;
            
            headers.CopyTo(memory[position..(position+headers.Length)]);
            position += headers.Length;
            
            lineBreak.CopyTo(memory[position..(position+lineBreak.Length)]);
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