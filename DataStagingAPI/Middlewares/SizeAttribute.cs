namespace HEAppE.DataStagingAPI;

/// <summary>
///     Size attribute for check request size
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class SizeAttribute : Attribute
{
    /// <summary>
    ///     Size in Bytes
    /// </summary>
    public readonly long Size;

    public SizeAttribute(long size)
    {
        Size = size;
    }
}