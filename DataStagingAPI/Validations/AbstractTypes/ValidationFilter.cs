using FluentValidation;
using System.Net;
using System.Reflection;

namespace HEAppE.DataStagingAPI.Validations.AbstractTypes
{
    /// <summary>
    /// Validation filter
    /// </summary>
    public static class ValidationFilter
    {
        public static EndpointFilterDelegate ValidationFilterFactory(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
        {
            IEnumerable<ValidationDescriptor> validationDescriptors = GetValidators(context.MethodInfo, context.ApplicationServices);

            if (validationDescriptors.Any())
            {
                return invocationContext => Validate(validationDescriptors, invocationContext, next);
            }
            return invocationContext => next(invocationContext);
        }

        private static async ValueTask<object?> Validate(IEnumerable<ValidationDescriptor> validationDescriptors, EndpointFilterInvocationContext invocationContext, EndpointFilterDelegate next)
        {
            foreach (ValidationDescriptor descriptor in validationDescriptors)
            {
                var argument = invocationContext.Arguments[descriptor.ArgumentIndex];

                if (argument is not null)
                {
                    var validationResult = await descriptor.Validator.ValidateAsync(
                        new ValidationContext<object>(argument)
                    );

                    if (!validationResult.IsValid)
                    {
                        return Results.ValidationProblem(validationResult.ToDictionary(),
                            statusCode: (int)HttpStatusCode.UnprocessableEntity);
                    }
                }
            }
            return await next.Invoke(invocationContext);
        }

        static IEnumerable<ValidationDescriptor> GetValidators(MethodInfo methodInfo, IServiceProvider serviceProvider)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                if (parameter.GetCustomAttribute<ValidateAttribute>() is not null)
                {
                    Type validatorType = typeof(IValidator<>).MakeGenericType(parameter.ParameterType);

                    // Note that FluentValidation validators needs to be registered as singleton
                    IValidator? validator = serviceProvider.GetService(validatorType) as IValidator;

                    if (validator is not null)
                    {
                        yield return new ValidationDescriptor { ArgumentIndex = i, ArgumentType = parameter.ParameterType, Validator = validator };
                    }
                }
            }
        }
    }
}
