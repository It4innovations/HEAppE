using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes;

public class ErrorResponseObject
{
    #region Properties

    [JsonProperty("error")] public ErrorResponse ErrorResponse { get; set; }

    #endregion

    #region Override Methods

    public override string ToString()
    {
        return ErrorResponse != null ? ErrorResponse.ToString() : base.ToString();
    }

    #endregion
}