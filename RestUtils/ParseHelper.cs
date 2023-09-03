using Exceptions.External;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;

namespace HEAppE.RestUtils
{
    public static class ParseHelper
    {
        /// <summary>
        /// Tries to deserialize the REST response content to given type. If deserialization or request fails the exception is thrown.
        /// </summary>
        /// <param name="response">REST response.</param>
        /// <param name="successStatusCode">Expected HTTP code of response.</param>
        /// <typeparam name="TParseResult">Type of the response object.</typeparam>
        /// <typeparam name="TExceptionType">Type of the exception to be thrown.</typeparam>
        /// <returns>Parsed object.</returns>
        /// <exception cref="ExternalException">Is thrown when response is failed or JsonConvert.DeserializeObject fails.</exception>
        public static TParseResult ParseJsonOrThrow<TParseResult, TExceptionType>(RestResponse response, HttpStatusCode successStatusCode)
            where TExceptionType : ExternalException
        {
            if ((response.StatusCode == successStatusCode) && (response.ErrorException == null))
            {
                try
                {
                    return JsonConvert.DeserializeObject<TParseResult>(response.Content);
                }
                catch (JsonSerializationException serializationException)
                {
                    throw (ExternalException)Activator.CreateInstance(typeof(TExceptionType),
                                                                                           "JsonDeserializationException",
                                                                                           serializationException);
                }
            }

            throw (ExternalException)Activator.CreateInstance(typeof(TExceptionType),
                                                                                   response.ErrorException?.Message,
                                                                                   response.ErrorException);
        }
    }
}
