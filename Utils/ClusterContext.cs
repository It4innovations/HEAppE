using System;
using System.Collections.Generic;
using System.Threading;

namespace HEAppE.Utils
{
    public static class ClusterContext
    {
        static ClusterContext()
        {
            SshCaAPI.Configuration.SshCaSettings.ConfigResolver = (section, prop) =>
            {
                var customConfig = Current;
                if (customConfig != null)
                {
                    var fullKey = $"{section}:{prop}";
                    if (customConfig.TryGetValue(fullKey, out var value) || customConfig.TryGetValue(prop, out value))
                    {
                        return value;
                    }
                }
                return null;
            };
        }

        private static readonly AsyncLocal<Dictionary<string, string>?> _currentConfig = new();

        public static Dictionary<string, string>? Current => _currentConfig.Value;

        public static IDisposable Use(Dictionary<string, string>? config)
        {
            var previous = _currentConfig.Value;
            _currentConfig.Value = config;
            return new DisposableAction(() => _currentConfig.Value = previous);
        }
    }

    internal class DisposableAction : IDisposable
    {
        private readonly Action _action;
        public DisposableAction(Action action) => _action = action;
        public void Dispose() => _action();
    }
}
