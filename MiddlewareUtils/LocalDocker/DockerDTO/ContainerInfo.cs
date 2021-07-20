using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HEAppE.MiddlewareUtils.LocalDocker.DockerDTO
{
    internal enum StateType
    {
        RUNNING,
        EXITED,
    }



    internal class ContainerInfo
    {
        [JsonIgnore]
        internal static Dictionary<string, StateType> StateMapper = new()
        {
            { "running", StateType.RUNNING },
            { "exited", StateType.EXITED },
        };
        public string Id { get; set; }
        public string[] Names { get; set; }
        public string Image { get; set; }
        public string ImageID { get; set; }
        public string Command { get; set; }
        [JsonIgnore]
        public DateTimeOffset Created { get; set; }
        [JsonPropertyName(nameof(Created))]
        public long created { set => Created = DateTimeOffset.FromUnixTimeSeconds(value); }
        public Port[] Ports { get; set; }
        [JsonIgnore]
        public StateType State { get; private set; }
        [JsonPropertyName(nameof(State))]
        public string state { set => State = StateMapper[value]; }
        public string Status { get; set; }

    }

    internal class Port
    {
        public string IP { get; set; }
        public int PrivatePort { get; set; }
        public int PublicPort { get; set; }
        public string Type { get; set; }
    }
}