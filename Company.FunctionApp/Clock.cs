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
using System.Globalization;
using System.Linq;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Company.FunctionApp {
    public static class Clock
    {
        [Function("Clock")]
        public static HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] 
            HttpRequestData req,
            FunctionContext executionContext)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            foreach (var VARIABLE in Enumerable.Range(1, 50000))
            {
                response.WriteString(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                response.WriteString("\r\n");
            }

            return response;
        }
    }    
}
