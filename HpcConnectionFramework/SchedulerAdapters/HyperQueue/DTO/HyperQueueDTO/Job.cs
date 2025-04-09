using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;

public class Job
{
    [JsonProperty("finished_at")] public DateTime? FinishedAt { get; set; }

    [JsonProperty("info")] public Info Info { get; set; }

    [JsonProperty("max_fails")] public object MaxFails { get; set; }

    [JsonProperty("pin_mode")] public object Pin { get; set; }

    [JsonProperty("priority")] public int Priority { get; set; }

    [JsonProperty("program")] public Program Program { get; set; }

    [JsonProperty("started_at")] public DateTime? StartedAt { get; set; }

    [JsonProperty("tasks")] public List<Task> Tasks { get; set; }

    [JsonProperty("time_limit")] public float TimeLimit { get; set; }

    [JsonProperty("submit_dir")] public string SubmitDir { get; set; }

    public override string ToString()
    {
        var tasksString = Tasks != null ? string.Join(", ", Tasks.Select(t => t.ToString())) : "null";
        return
            $"Job(FinishedAt={FinishedAt}, Info={Info}, MaxFails={MaxFails}, Pin={Pin}, Priority={Priority}, Program={Program}, StartedAt={StartedAt}, Tasks=[{tasksString}], TimeLimit={TimeLimit}, SubmitDir={SubmitDir})";
    }
}