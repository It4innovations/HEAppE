using System;

namespace HEAppE.Utils
{
    public static class ClusterConfigResolver
    {
        public static T GetValue<T>(string sectionName, string propertyName, T defaultValue)
        {
            var customConfig = ClusterContext.Current;
            if (customConfig != null)
            {
                var fullKey = $"{sectionName}:{propertyName}";
                if (customConfig.TryGetValue(fullKey, out var value) || customConfig.TryGetValue(propertyName, out value))
                {
                    if (value != null)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            // Fall back to default if conversion fails
                        }
                    }
                }
            }
            return defaultValue;
        }
    }
}
