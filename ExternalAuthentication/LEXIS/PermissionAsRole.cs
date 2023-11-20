using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;

namespace HEAppE.ExternalAuthentication.LEXIS
{ 
    public record PermissionAsRole()
    {
        public PermissionAsRole(bool IsMaintainer, bool IsSubmitter, bool IsReporter, IEnumerable<AdaptorUserUserGroupRole> existingProjectGroupRoles) : this()
        {
            if (IsMaintainer)
            {
                MaintainerRole = existingProjectGroupRoles.FirstOrDefault(x => x.AdaptorUserRole.Name == nameof(LexisRoleMapping.Maintainer))?.AdaptorUserRole;
            }

            if (IsSubmitter)
            {
                SubmitterRole = existingProjectGroupRoles.FirstOrDefault(x => x.AdaptorUserRole.Name == nameof(LexisRoleMapping.Submitter))?.AdaptorUserRole;

            }
            if (IsReporter)
            {
                ReporterRole = existingProjectGroupRoles.FirstOrDefault(x => x.AdaptorUserRole.Name == nameof(LexisRoleMapping.Reporter))?.AdaptorUserRole;
            }
        }
        public AdaptorUserRole MaintainerRole { get; set; } = null;
        public AdaptorUserRole SubmitterRole { get; set; } = null;
        public AdaptorUserRole ReporterRole { get; set; } = null;

        public AdaptorUserRole GetRole => this switch
        {
            { IsMaintainer: true } => MaintainerRole,
            { IsSubmitter: true } => SubmitterRole,
            { IsReporter: true } => ReporterRole,           
            _ => throw new AuthenticationTypeException("NotSupportedRole")
        };

        public bool IsMaintainer { get; init; }
        public bool IsSubmitter { get; init; }
        public bool IsReporter { get; init; }
    }
}