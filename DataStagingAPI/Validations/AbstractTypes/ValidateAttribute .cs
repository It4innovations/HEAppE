namespace HEAppE.DataStagingAPI.Validations.AbstractTypes
{
    /// <summary>
    /// Validation attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValidateAttribute : Attribute
    {
    }
}