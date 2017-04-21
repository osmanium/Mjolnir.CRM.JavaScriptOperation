using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System.IO;
using Mjolnir.CRM.Core;
using Mjolnir.ConsoleCommandLine;

namespace Mjolnir.CRM.JavaScriptOperation
{
    public abstract class JavaScriptOperationBase<TRequest, TResponse> : ConsoleCommandBase, IJavaScriptOperationExecuter
        where TResponse : IJavaScriptOperationResponse, new()
        where TRequest : IJavaScriptOperationRequest
    {
        public string Execute(string input, CrmContext context)
        {
            TRequest request = default(TRequest);
            TResponse response = new TResponse();
            string responseJson = string.Empty;
            string errorMessage = string.Empty;
            bool isError = false;

            try
            {
                //Identify the request type
                context.TracingService.TraceVerbose("DeserizalizeRequest started");
                request = DeserizalizeRequest(input);

                //Execute
                context.TracingService.TraceVerbose("ExecuteInternal started");
                response = ExecuteJavascriptOperation(request, response, context);
                response.IsSuccesful = true;
            }
            catch (Exception ex)
            {
                HandleErrorResponse(response, out errorMessage, out isError, ex, context);
            }

            //Serialize response
            context.TracingService.TraceVerbose("SerializeResponse started");
            return SerializeResponse(response);
        }

        private static void HandleErrorResponse(TResponse response, out string errorMessage, out bool isError, Exception ex, CrmContext context)
        {
            isError = true;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                sb.AppendLine(ex.InnerException.Message);
                if (ex.InnerException.StackTrace != null)
                {
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
            }

            errorMessage = sb.ToString();

            if (response == null)
                response = new TResponse();

            context.TracingService.TraceVerbose("Error occured :\n" + errorMessage);
            response.ErrorMessage = errorMessage;
        }

        private string SerializeResponse(TResponse response)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
                {
                    var serializer = JsonSerializer.Create();

                    //Read the request
                    serializer.Serialize(jsonWriter, response);
                }
            }

            return sb.ToString();
        }

        private static TRequest DeserizalizeRequest(string input)
        {
            TRequest request;
            using (StringReader sr = new StringReader(input))
            {
                using (JsonTextReader jsonReader = new JsonTextReader(sr))
                {
                    var serializer = JsonSerializer.Create();

                    //Read the request
                    request = serializer.Deserialize<TRequest>(jsonReader);
                }
            }

            return request;
        }

        public abstract TResponse ExecuteJavascriptOperation(TRequest req, TResponse res, CrmContext context);
    }
}
