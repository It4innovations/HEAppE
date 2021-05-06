using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.KeycloakOpenIdAuthentication
{
    public class UserOpenId
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public Dictionary<string, IEnumerable<string>> ProjectRoles { get; set; } = new Dictionary<string, IEnumerable<string>>();
    }
}
