using Newtonsoft.Json;
using System;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;
public class Task
{
    [JsonProperty("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("worker")]
    public int Worker { get; set; }

    [JsonProperty("cwd")]
    public string Cwd { get; set; }

    [JsonProperty("stderr")]
    public string Stderr { get; set; }

    [JsonProperty("stdout")]
    public string Stdout { get; set; }
    public override string ToString()
    {
        return $"Task(Id={Id}, State={State}, Worker={Worker}, StartedAt={StartedAt}, FinishedAt={FinishedAt})";
    }
}
