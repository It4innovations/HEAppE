using HEAppE.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.FileTransferFramework.Sftp.Commands
{
    internal class ListDirectory : ICommand<IEnumerable<SftpFile>>
    {
        #region Instances
        private readonly string _hostTimeZone;
        private readonly string _remotePath;
        private readonly string _remoteWorkingDirectory;
        #endregion
        #region Properties
        public string Command { get { return "ls -l " + _remotePath; } }
        #endregion
        #region Constructors
        public ListDirectory(string hostTimeZone, string remotePath, string remoteWorkingDirectory) //input
        {
            _hostTimeZone = hostTimeZone;
            _remotePath = remotePath;
            _remoteWorkingDirectory = remoteWorkingDirectory;
        }
        #endregion
        #region Methods
        public IEnumerable<SftpFile> ProcessResult(SftpCommandResult result)
        {
            var text = Regex.Replace(result.Output, @"\s{2,}", " ");
            var lines = Regex.Split(text, "\r\n|\r|\n").ToList();

            //Remove first (command) and last line
            lines.RemoveAt(0);
            lines.RemoveAt(lines.Count - 1);

            var files = new List<SftpFile>();
            foreach (var line in lines)
            {
                var tokens = line.Split(' ');
                string access = tokens[0];
                char type = access[0];
                string month = tokens[5];
                string day = tokens[6];
                string otherDateAttr = tokens[7]; //can be year or actual time 
                string name = tokens[8];
                string fullName = Path.Combine(_remoteWorkingDirectory, _remotePath, name);

                if (!DateTime.TryParseExact($"{month} {day} {otherDateAttr}",
                                             "MMM d yyyy",
                                             CultureInfo.InvariantCulture,
                                             DateTimeStyles.AllowWhiteSpaces,
                                             out DateTime converted)
                    && !DateTime.TryParseExact($"{month} {day} {otherDateAttr}",
                                             "MMM d HH:mm",
                                             CultureInfo.InvariantCulture,
                                             DateTimeStyles.AllowWhiteSpaces,
                                             out converted))
                {
                    throw new NotSupportedException();
                }

                files.Add(new SftpFile
                {
                    IsSymbolicLink = type == 'l',
                    IsDirectory = type == 'd',
                    Name = name,
                    FullName = fullName,
                    LastWriteTime = converted.Convert(_hostTimeZone)
                });
            }
            return files;
        }
        #endregion   
    }
}
