using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;

public class Resources
{
    [JsonProperty("cpus")] public Cpus Cpus { get; set; }

    [JsonProperty("generic")] public List<Generic> Generic { get; set; }

    [JsonProperty("min_time")] public double MinTime { get; set; }

    public override string ToString()
    {
        var genericInfo = Generic != null ? string.Join(", ", Generic.Select(g => g.ToString())) : "null";

        return $"Resources(Cpus={Cpus}, Generic=[{genericInfo}], MinTime={MinTime})";
    }
}