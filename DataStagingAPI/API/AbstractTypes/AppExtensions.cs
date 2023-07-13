using HEAppE.DataStagingAPI.Validations.AbstractTypes;

namespace HEAppE.DataStagingAPI.API.AbstractTypes
{
    /// <summary>
    /// Application extensions
    /// </summary>
    public static class AppExtensions
    {
        /// <summary>
        /// Registration classes into API, that implement IApiRoute
        /// </summary>
        public static void RegisterApiRoutes(this WebApplication app)
        {
            var group = app.MapGroup("api");
            group.AddEndpointFilterFactory(ValidationFilter.ValidationFilterFactory);

            var type = typeof(IApiRoute);
            var types = type.Assembly.GetTypes().Where(p => p.IsClass && p.IsAssignableTo(type));

            foreach (var routeType in types)
            {
                var route = (IApiRoute?)Activator.CreateInstance(routeType);
                route?.Register(group);
            }
        }
    }
}
