using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.FileTransferFramework.Sftp.Commands
{
    internal class WorkingDirectory : ICommand<string>
    {
        #region Properties
        public string Command => "pwd";
        #endregion
        #region Methods
        public string ProcessResult(SftpCommandResult result)
        {
            var text = Regex.Replace(result.Output, @"\s{2,}", " ");
            var lines = Regex.Split(text, "\r\n|\r|\n").ToList();

            string path = lines[1].Replace("Remote working directory: ", "");
            return path;
        }
        #endregion
    }
}
