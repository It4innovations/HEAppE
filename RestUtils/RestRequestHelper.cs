using System;
using System.Text;
using RestSharp;

namespace HEAppE.RestUtils;

public static class RestRequestHelper
{
    /// <summary>
    ///     Add json body to request. This is different from RestSharp AddJsonBody, which tries to serialize the passed body.
    /// </summary>
    /// <param name="request">Rest request.</param>
    /// <param name="jsonBody">Serialized json object.</param>
    /// <returns>The request with the body.</returns>
    public static RestRequest AddSerializedJsonBody(this RestRequest request, string jsonBody)
    {
        request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
        return request;
    }

    /// <summary>
    ///     Add X-Auth-Token header field with its token.
    /// </summary>
    /// <param name="request">Request to add authorization header to.</param>
    /// <param name="token">Authentication token.</param>
    /// <returns>The request with the authorization header.</returns>
    public static RestRequest AddXAuthTokenToHeader(this RestRequest request, string token)
    {
        request.AddHeader("X-Auth-Token", token);
        return request;
    }

    public static RestRequest AddXWwwFormUrlEncodedBody(this RestRequest request,
        params ValueTuple<string, string>[] parameters)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < parameters.Length; i++)
        {
            builder.Append(parameters[i].Item1).Append('=').Append(Uri.EscapeDataString(parameters[i].Item2));
            if (i != parameters.Length - 1)
                builder.Append('&');
        }

        var body = builder.ToString();
        request.AddParameter("application/x-www-form-urlencoded", body, ParameterType.RequestBody);
        return request;
    }
}