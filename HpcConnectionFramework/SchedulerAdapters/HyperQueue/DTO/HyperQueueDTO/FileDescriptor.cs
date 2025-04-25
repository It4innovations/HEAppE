using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;

public class FileDescriptor
{
    [JsonProperty("File")] public string File { get; set; }
}