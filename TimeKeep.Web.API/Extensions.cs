using System;
using System.Net;
using System.Net.Http;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API
{
    public static class Extensions
    {
        private static Exception proxyException = new Exception("Could not process request due to an internal error, please try again later or contact support");

        public static HttpResponseMessage CreateResultResponse<T>(this HttpRequestMessage request, T value)
        {
            return request.CreateResponse<ResponseObject<T>>(new ResponseObject<T>(value));
        }

        public static HttpResponseMessage CreateResultResponse<T>(this HttpRequestMessage request, HttpStatusCode code, T value)
        {
            return request.CreateResponse<ResponseObject<T>>(code, new ResponseObject<T>(value));
        }

        public static HttpResponseMessage CreateCustomErrorResponse(this HttpRequestMessage request, HttpStatusCode code, Exception ex)
        {
            Error err = Error.Log(ex, (int)code);
            err.StackTrace = null;
            err.PID = null;
            err.Type = null;
            err.TID = null;
            if (code == HttpStatusCode.InternalServerError)
                err.Message = "Could not process request due to an internal error, please try again later or contact support";
            return request.CreateResponse<Error>(code, err);
        }
    }
}