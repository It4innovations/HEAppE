using Newtonsoft.Json;

namespace HEAppE.RestUtils;

/// <summary>
///     Serialized which ensures that null values are not serialized into the json string.
/// </summary>
public class IgnoreNullSerializer : JsonSerializerSettings
{
    private static readonly object _lock = new();
    private static IgnoreNullSerializer _instance;


    private IgnoreNullSerializer()
    {
        NullValueHandling = NullValueHandling.Ignore;
    }

    public static IgnoreNullSerializer Instance
    {
        get
        {
            if (_instance == null)
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new IgnoreNullSerializer();
                }

            return _instance;
        }
    }
}