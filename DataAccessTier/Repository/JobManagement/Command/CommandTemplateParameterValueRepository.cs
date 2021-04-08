using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement.Command
{
    internal class CommandTemplateParameterValueRepository : GenericRepository<CommandTemplateParameterValue>, ICommandTemplateParameterValueRepository
    {
        #region Constructors
        internal CommandTemplateParameterValueRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
