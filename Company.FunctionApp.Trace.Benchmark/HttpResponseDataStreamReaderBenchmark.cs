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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace Company.FunctionApp.Trace.Benchmark
{
    [MemoryDiagnoser]
    public class HttpResponseDataStreamReaderBenchmark
    {
        private BenchmarkResponse _response;
        
        [Params(BenchmarkResponse.SIZE_1MB, BenchmarkResponse.SIZE_5MB, BenchmarkResponse.SIZE_10MB, BenchmarkResponse.SIZE_50MB)]
        public int Size;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var contextMock = new Mock<FunctionContext>();
            _response = new BenchmarkResponse(Size, contextMock.Object);
        }
        
        [Benchmark(Baseline = true)]
        public async Task<Stream> GetStream()
        {
            var reader = new HttpResponseDataStreamReader(_response);
            return await reader.GetStream();
        }

        [Benchmark]
        public async Task<ReadOnlyMemory<byte>> GetBytesMemory()
        {
            var reader = new HttpResponseDataStreamReader(_response);
            return await reader.GetBytesMemory();
        }

        [Benchmark]
        public async Task<ReadOnlyMemory<byte>> GetBytesMemoryAll()
        {
            var reader = new HttpResponseDataStreamReader(_response);
            return await reader.GetBytesMemoryAll();
        }
        
        [Benchmark]
        public async Task<ReadOnlyMemory<byte>> GetBytesArray()
        {
            var reader = new HttpResponseDataStreamReader(_response);
            return await reader.GetBytesArray();
        }
    }
 

    internal class BenchmarkResponse : HttpResponseData
    {
        public const int SIZE_1MB = 50000;
        public const int SIZE_5MB = 250000;
        public const int SIZE_10MB = 500000;
        public const int SIZE_50MB = 2500000;
        
        public BenchmarkResponse(FunctionContext functionContext) : this(SIZE_50MB, functionContext)
        {
            
        }

        public BenchmarkResponse(int iterations, FunctionContext context) : base (context)
        {
            StatusCode = HttpStatusCode.OK;
            Headers = new HttpHeadersCollection(new[]
            {
                new KeyValuePair<string, string>("Content-Type", "text/plain; charset=utf-8")
            });

            Body = GetContent(iterations);
        }

        private static Stream GetContent(int iterations)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            
            foreach (var VARIABLE in Enumerable.Range(1, iterations))
            {
                writer.WriteLine(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            }
            writer.Flush();
            ms.Position = 0;
            return ms;
        }

        public sealed override HttpStatusCode StatusCode { get; set; }
        public sealed override HttpHeadersCollection Headers { get; set; }
        public sealed override Stream Body { get; set; }
        public override HttpCookies Cookies { get; }
    }
}