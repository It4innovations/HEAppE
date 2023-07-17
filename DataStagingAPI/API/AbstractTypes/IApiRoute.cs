namespace HEAppE.DataStagingAPI.API.AbstractTypes
{
    /// <summary>
    /// API route interface
    /// </summary>
    public interface IApiRoute
    {
        void Register(RouteGroupBuilder group);
    }
}
    