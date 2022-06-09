using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier.Logic.FileTransfer.Exceptions
{
    public class FileTransferTemporaryKeyException : ExternallyVisibleException
    {
        public FileTransferTemporaryKeyException(string message) : base(message) 
        { 
        
        }
    }
}
