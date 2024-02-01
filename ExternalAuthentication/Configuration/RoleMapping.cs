using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ExternalAuthentication.Configuration
{
    public class RoleMapping
    {
        private static readonly Dictionary<string, string> _mappingRoles = new();
        public static string Maintainer { get; set; }
        public static string Submitter { get; set; }
        public static string GroupReporter { get; set; }
        public static string Reporter { get; set; }

        public static Dictionary<string, string> MappingRoles
        {
            get
            {
                if (_mappingRoles.Count == 0)
                {
                    var staticFields = typeof(RoleMapping).GetProperties(BindingFlags.Static | BindingFlags.Public)
                        .Where(w => w.PropertyType == typeof(string));
                    foreach (var field in staticFields)
                    {
                        object value = field.GetValue(null);
                        if (value is not null)
                        {
                            _mappingRoles.Add(value.ToString(), field.Name);
                        }
                    }
                }
                return _mappingRoles;
            }
        }
    }
}