using HEAppE.DomainObjects.FirecRest;
using HEAppE.ExtModels.FirecRest.Models;

namespace HEAppE.ExtModels.FirecRest.Converts;

public static class FirecRestConverts
{
    #region Methods for Object Converts
    
    public static FirecRestEndpointExt ConvertIntToExt(this FirecRestEndpoint obj)
    {
        var convert = new FirecRestEndpointExt
        {
            Id = obj.Id,
            Name = obj.Name,
            Description = obj.Description,
            Url = obj.Url,
            IdpUrl = obj.IdpUrl
        };
        return convert;
    }

    #endregion
}
