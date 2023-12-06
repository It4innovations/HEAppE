using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;

namespace HEAppE.ExternalAuthentication.LEXIS
{ 
    public record PermissionAsRole()
    {
        public PermissionAsRole(bool isMaintainer, bool isSubmitter, bool isReporter, IEnumerable<AdaptorUserRole> existingProjectGroupRoles) : this()
        {
            if (isMaintainer)
            {
                IsMaintainer = true;
                MaintainerRole = existingProjectGroupRoles.FirstOrDefault(x => x.Name == nameof(LexisRoleMapping.Maintainer));
            }

            if (isSubmitter)
            {
                IsSubmitter = true;
                SubmitterRole = existingProjectGroupRoles.FirstOrDefault(x => x.Name == nameof(LexisRoleMapping.Submitter));
            }
            if (isReporter)
            {
                IsReporter = true;
                ReporterRole = existingProjectGroupRoles.FirstOrDefault(x => x.Name == nameof(LexisRoleMapping.Reporter));
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