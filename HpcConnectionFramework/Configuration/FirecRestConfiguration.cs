using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.HpcConnectionFramework.Configuration;

public sealed class FirecRestConfiguration
{
    public static List<EndpointPair> Endpoints { get; set; } = [];

    public class EndpointPair
    {
        // key
        public string MasterNodeName { get; set; }

        // value
        public FirecRestOptions Options { get; set; }
    }
    
    
    public static FirecRestOptionsGetter FirecRestOptions { get => _options ??= new(); }

    public class FirecRestOptionsGetter
    {
        public FirecRestOptions this[string masterNodeName] {
            get => Endpoints.FirstOrDefault(p => p.MasterNodeName == masterNodeName)?.Options;
        }
    }

    private static FirecRestOptionsGetter _options = null;
}
