using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO
{
    class LinuxLocalInfo
    {
        public long Id { get; set; }
        public DateTime? SubmitTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime CreateTime { get; set; }

        public string Name { get; set; }
        public string Project { get; set; }


        [JsonPropertyName("Tasks")]
        public List<LinuxLocalJobDTO> Jobs { get; set; }
    }
}
