using System;
using System.Text;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes
{
    public class ErrorResponse
    {
        [JsonProperty("code")]
        public int HttpCode { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HttpCode: ").Append(HttpCode).Append(Environment.NewLine);
            sb.Append("Title: ").Append(Title).Append(Environment.NewLine);
            sb.Append("Message: ").Append(Message).Append(Environment.NewLine);
            return sb.ToString();
        }
    }

    public class ErrorResponseObject
    {
        [JsonProperty("error")]
        public ErrorResponse ErrorResponse { get; set; }

        public override string ToString() => ErrorResponse != null ? ErrorResponse.ToString() : base.ToString();
    }
}