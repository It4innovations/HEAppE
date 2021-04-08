using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command
{
    public interface ICommandTemplateParameterRepository : IRepository<CommandTemplateParameter>
    {
        CommandTemplateParameter GetByCommandTemplateIdAndCommandParamId(long commandTemplateId, string identifier);
    }
}
