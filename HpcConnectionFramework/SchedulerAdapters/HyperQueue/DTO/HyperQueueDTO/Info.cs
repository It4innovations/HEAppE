using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;

public class Info
{
    [JsonProperty("id")] public int Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("resources")] public Resources Resources { get; set; }

    [JsonProperty("task_count")] public int TaskCount { get; set; }

    [JsonProperty("task_stats")] public TaskStats TaskStats { get; set; }

    public override string ToString()
    {
        return $"Info(Id={Id}, Name={Name}, Resources={Resources}, TaskCount={TaskCount}, TaskStats={TaskStats})";
    }
}