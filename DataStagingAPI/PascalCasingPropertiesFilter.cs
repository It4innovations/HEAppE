using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HEAppE.DataStagingAPI
{
    public class PascalCasingPropertiesFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(schema);
            schema.Properties = schema.Properties.ToDictionary(
                d => char.ToUpper(d.Key[0], System.Globalization.CultureInfo.CurrentCulture) + d.Key[1..],
                d => d.Value);
        }
    }
}