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
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Company.FunctionApp.Trace
{
    // Replace with a better solution once the HttpContext gets available in middleware (if ever).
    internal static class FunctionContextExtensions
    {
        // Thank you https://github.com/Azure/azure-functions-dotnet-worker/issues/414#issuecomment-828810635
        public static HttpRequestData? GetHttpRequestData(this FunctionContext functionContext)
        {
            try
            {
                var keyValuePair = functionContext.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
                if (keyValuePair.Equals(default(KeyValuePair<Type, object>))) return null;
                var functionBindingsFeature = keyValuePair.Value;
                var type = functionBindingsFeature.GetType();
                var inputData = type.GetProperties().Single(p => p.Name == "InputData").GetValue(functionBindingsFeature) as IReadOnlyDictionary<string, object>;
                return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
            }
            catch
            {
                return null;
            }
        }

        // Thank you https://github.com/Azure/azure-functions-dotnet-worker/issues/414#issuecomment-872818004
        public static HttpResponseData? GetHttpResponseData(this FunctionContext functionContext)
        {
            try
            {
                var keyValuePair = functionContext.Features.FirstOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
                if (keyValuePair.Equals(default(KeyValuePair<Type, object>))) return null;
                var functionBindingsFeature = keyValuePair.Value;
                var propertyInfo = functionBindingsFeature.GetType().GetProperty("InvocationResult");
                return propertyInfo?.GetValue(functionBindingsFeature) as HttpResponseData;
            }
            catch
            {
                return null;
            }
        }
    }
}