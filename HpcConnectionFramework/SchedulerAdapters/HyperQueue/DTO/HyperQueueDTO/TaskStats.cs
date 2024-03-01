using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;

public class TaskStats
{
    [JsonProperty("canceled")]
    public int Canceled { get; set; }

    [JsonProperty("failed")]
    public int Failed { get; set; }

    [JsonProperty("finished")]
    public int Finished { get; set; }

    [JsonProperty("running")]
    public int Running { get; set; }

    [JsonProperty("waiting")]
    public int Waiting { get; set; }

    public TaskState GetGlobalState()
    {
        Dictionary<TaskState, int> stateCounts = new Dictionary<TaskState, int>
        {
            { TaskState.Canceled, Canceled },
            { TaskState.Failed, Failed },
            { TaskState.Finished, Finished },
            { TaskState.Running, Running },
            { TaskState.Queued, Waiting }
        };

        // Find the state with the highest count
        TaskState highestCountState = stateCounts.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

        return highestCountState;
    }
    
    public override string ToString()
    {
        return $"TaskStats(Canceled={Canceled}, Failed={Failed}, Finished={Finished}, Running={Running}, Waiting={Waiting})";
    }
}