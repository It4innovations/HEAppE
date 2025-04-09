using System;
using System.Text;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes;

public class ErrorResponse
{
    #region Override Methods

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("HttpCode: ").Append(HttpCode).Append(Environment.NewLine);
        sb.Append("Title: ").Append(Title).Append(Environment.NewLine);
        sb.Append("Message: ").Append(Message).Append(Environment.NewLine);
        return sb.ToString();
    }

    #endregion

    #region Properties

    [JsonProperty("code")] public int HttpCode { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("message")] public string Message { get; set; }

    #endregion
}