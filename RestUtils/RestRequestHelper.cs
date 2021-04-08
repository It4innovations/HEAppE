﻿using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;

namespace HEAppE.RestUtils
{
    public static class RestRequestHelper
    {
        /// <summary>
        /// Add json body to request. This is different from RestSharp AddJsonBody, which tries to serialize the passed body.
        /// </summary>
        /// <param name="request">Rest request.</param>
        /// <param name="jsonBody">Serialized json object.</param>
        /// <returns>The request with the body.</returns>
        public static IRestRequest AddSerializedJsonBody(this IRestRequest request, string jsonBody)
        {
            request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
            return request;
        }

        /// <summary>
        /// Add X-Auth-Token header field with its token.
        /// </summary>
        /// <param name="request">Request to add authorization header to.</param>
        /// <param name="token">Authentication token.</param>
        /// <returns>The request with the authorization header.</returns>
        public static IRestRequest AddXAuthTokenToHeader(this IRestRequest request, string token)
        {
            request.AddHeader("X-Auth-Token", token);
            return request;
        }

        public static IRestRequest AddXWwwFormUrlEncodedBody(this IRestRequest request, params ValueTuple<string, string>[] parameters)
        {
            StringBuilder builder = new StringBuilder();
            for (var i = 0; i < parameters.Length; i++)
            {
                builder.Append(parameters[i].Item1).Append('=').Append(Uri.EscapeDataString(parameters[i].Item2));
                if (i != (parameters.Length - 1))
                    builder.Append('&');
            }

            string body = builder.ToString();
            request.AddParameter("application/x-www-form-urlencoded", body, ParameterType.RequestBody);
            return request;
        }
    }
}