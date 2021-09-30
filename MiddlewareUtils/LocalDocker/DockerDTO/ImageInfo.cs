using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HEAppE.MiddlewareUtils.LocalDocker.DockerDTO
{
    internal class ImageInfo
    {
        public int Containers { get; set;}
        [JsonIgnore]
        public DateTimeOffset Created { get; set; }
        [JsonPropertyName(nameof(Created))]
        public long created { set => Created = DateTimeOffset.FromUnixTimeSeconds(value); }
        public string Id {get; set;}
        public string[] RepoTags { get; set;}
        public long Size { get; set;}
    }
}