using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper
{
    public class ProjectReference
    {
        public Project Project { get; set; }
        public AdaptorUserRole Role { get; set; }
    }
}
