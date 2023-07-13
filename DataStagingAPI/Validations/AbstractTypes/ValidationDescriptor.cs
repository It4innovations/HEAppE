using FluentValidation;

namespace HEAppE.DataStagingAPI.Validations.AbstractTypes
{
    /// <summary>
    /// Validator descriptor
    /// </summary>
    internal class ValidationDescriptor
    {
        public required int ArgumentIndex { get; init; }
        public required Type ArgumentType { get; init; }
        public required IValidator Validator { get; init; }
    }
}
