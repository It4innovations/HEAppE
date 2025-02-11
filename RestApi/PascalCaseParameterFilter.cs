using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

using System.Globalization;

namespace HEAppE.RestApi;

public class PascalCaseParameterFilter : IParameterFilter
{

    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        parameter.Name = ToPascalCase(parameter.Name);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0], CultureInfo.InvariantCulture) + input.Substring(1);
    }
}
