using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Company.FunctionApp.Trace
{
    public sealed class LogToBlobStorage : IFunctionsWorkerMiddleware
    {
        private readonly BlobServiceClient _blobServiceClient;

        public LogToBlobStorage(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }
        
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var client = _blobServiceClient.GetBlobContainerClient("requests");

            var req = context.GetHttpRequestData();
            if (req != null)
            {
                req.Body.Position = 0;
                await client.UploadBlobAsync($"{context.InvocationId}.txt", req.Body);
            }
            
            await next(context);
            
            var res = context.GetHttpResponseData();
            if (res == null) return;

            var responseStream = new HttpResponseDataStreamReader(res);
            
            var resClient = _blobServiceClient.GetBlobContainerClient("responses");
            await resClient.UploadBlobAsync($"{context.InvocationId}.txt", BinaryData.FromBytes(await responseStream.GetBytesMemory()));
        }
    }
}