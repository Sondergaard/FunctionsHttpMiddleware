using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace Company.FunctionApp.Trace.Benchmark
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                var context = new Mock<FunctionContext>();
                var response = new BenchmarkResponse(BenchmarkResponse.SIZE_5MB, context.Object);
                var reader = new HttpResponseDataStreamReader(response);

                var bytes = await reader.GetBytesArray();
            } else if (args[0].Equals("Benchmark", StringComparison.InvariantCultureIgnoreCase))
            {
                // BenchmarkRunner.Run<StringBuildingBenchmark>();
                BenchmarkRunner.Run<HttpResponseDataStreamReaderBenchmark>();
                
                await Task.CompletedTask;
            }
        }
    }
}