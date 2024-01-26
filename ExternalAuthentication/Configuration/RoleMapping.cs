using System;
using System.Collections.Generic;
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
                    Type myType = typeof(RoleMapping);
                    FieldInfo[] staticFields = myType.GetFields(BindingFlags.Static | BindingFlags.Public);
                    foreach (FieldInfo field in staticFields)
                    {
                        if (field.FieldType == typeof(string))
                        {
                            object value = field.GetValue(null);
                            if (string.IsNullOrEmpty(value.ToString()))
                            {
                                _mappingRoles.Add(field.Name, value.ToString());
                            }
                        }
                    }
                }

                return _mappingRoles;
            }
        }
    }
}