using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.FileTransferFramework.Sftp.Commands;

internal class WorkingDirectory : ICommand<string>
{
    #region Properties

    public string Command => "pwd";

    #endregion

    #region Methods

    public string ProcessResult(SftpCommandResult result)
    {
        var lines = Regex.Split(result.Output, "\r\n|\r|\n")
            .Select(l => Regex.Replace(l, @"\s{2,}", " ").Trim())
            .ToList();

        var pathLine = lines.FirstOrDefault(l => l.Contains("Remote working directory:"));
        if (pathLine == null) return string.Empty;

        return pathLine.Replace("Remote working directory: ", "").Trim();
    }

    #endregion
}