using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;
public class Program
{
    [JsonProperty("args")]
    public List<string> Args { get; set; }

    [JsonProperty("cwd")]
    public string Cwd { get; set; }

    [JsonProperty("env")]
    public Dictionary<string, string> Env { get; set; }

    [JsonProperty("stderr")]
    public string Stderr { get; set; }

    [JsonProperty("stdout")]
    public string Stdout { get; set; }
    public override string ToString()
    {
        string args = Args != null ? string.Join(", ", Args) : "null";
        string env = Env != null ? string.Join(", ", Env.Select(kv => $"{kv.Key}={kv.Value}")) : "null";

        return $"Program(Args=[{args}], Cwd={Cwd}, Env=[{env}], Stderr={Stderr}, Stdout={Stdout})";
    }
}