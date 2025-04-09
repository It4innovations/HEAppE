using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;

public class Cpus
{
    [JsonProperty("cpus")] public int CpusValue { get; set; }

    [JsonProperty("type")] public string Type { get; set; }

    public override string ToString()
    {
        return $"Cpus(Value={CpusValue}, Type: {Type})";
    }
}