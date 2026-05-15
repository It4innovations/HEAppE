using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.HpcConnectionFramework.Configuration;

public class FirecRestConfiguration
{
    public List<EndpointPair> Endpoints { get; set; } = [];

    public class EndpointPair
    {
        // key
        public string MasterNodeName { get; set; }

        // value
        public FirecRestOptions Options { get; set; }
    }
    
    public FirecRestOptionsGetter FirecRestOptions { get => _getter ??= new(this); }

    public class FirecRestOptionsGetter(FirecRestConfiguration _that)
    {
        public FirecRestOptions this[string masterNodeName] {
            get => _that.Endpoints.FirstOrDefault(p => p.MasterNodeName == masterNodeName)?.Options;
        }
    }

    private FirecRestOptionsGetter _getter = null;
}
