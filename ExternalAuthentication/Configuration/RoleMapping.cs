using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ExternalAuthentication.Configuration;

public class RoleMapping
{
    private static readonly object _lock = new();
    private static Dictionary<string, List<string>> _mappingRoles;

    private static string _managementAdmin;
    public static string ManagementAdmin
    {
        get => _managementAdmin;
        set
        {
            _managementAdmin = value;
            Reset();
        }
    }

    private static string _maintainer;
    public static string Maintainer
    {
        get => _maintainer;
        set
        {
            _maintainer = value;
            Reset();
        }
    }

    private static string _manager;
    public static string Manager
    {
        get => _manager;
        set
        {
            _manager = value;
            Reset();
        }
    }

    private static string _submitter;
    public static string Submitter
    {
        get => _submitter;
        set
        {
            _submitter = value;
            Reset();
        }
    }

    private static string _groupReporter;
    public static string GroupReporter
    {
        get => _groupReporter;
        set
        {
            _groupReporter = value;
            Reset();
        }
    }

    private static string _reporter;
    public static string Reporter
    {
        get => _reporter;
        set
        {
            _reporter = value;
            Reset();
        }
    }

    public static Dictionary<string, List<string>> MappingRoles
    {
        get
        {
            if (_mappingRoles is not null)
                return _mappingRoles;

            lock (_lock)
            {
                if (_mappingRoles is not null)
                    return _mappingRoles;

                var mapping = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var staticFields = typeof(RoleMapping).GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(w => w.PropertyType == typeof(string));

                foreach (var field in staticFields)
                {
                    var rawValue = field.GetValue(null)?.ToString();
                    if (string.IsNullOrWhiteSpace(rawValue)) continue;

                    var permissions = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var permission in permissions)
                    {
                        if (!mapping.TryGetValue(permission, out var roles))
                        {
                            roles = new List<string>();
                            mapping[permission] = roles;
                        }

                        if (!roles.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            roles.Add(field.Name);
                        }
                    }
                }

                _mappingRoles = mapping;
                return _mappingRoles;
            }
        }
    }

    public static IEnumerable<string> GetRolesForPermission(string permission)
    {
        if (!string.IsNullOrWhiteSpace(permission) && MappingRoles.TryGetValue(permission, out var roles))
        {
            return roles;
        }

        return Enumerable.Empty<string>();
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _mappingRoles = null;
        }
    }
}