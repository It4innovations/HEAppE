﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.Utils;

namespace HEAppE.FileTransferFramework.Sftp.Commands;

internal class ListDirectory : ICommand<IEnumerable<SftpFile>>
{
    #region Constructors

    public ListDirectory(string hostTimeZone, string remotePath, string remoteWorkingDirectory) //input
    {
        _hostTimeZone = hostTimeZone;
        _remotePath = remotePath;
        _remoteWorkingDirectory = remoteWorkingDirectory;
    }

    #endregion

    #region Properties

    public string Command => "ls -l " + _remotePath;

    #endregion

    #region Methods

    public IEnumerable<SftpFile> ProcessResult(SftpCommandResult result)
    {
        var text = Regex.Replace(result.Output, @"\s{2,}", " ");
        var lines = Regex.Split(text, "\r\n|\r|\n").Where(w => !string.IsNullOrEmpty(w) && !w.StartsWith("sftp>"))
            .ToList();

        var files = new List<SftpFile>();
        foreach (var line in lines)
        {
            var tokens = line.Split(' ');
            var access = tokens[0];
            var type = access[0];
            var month = tokens[5];
            var day = tokens[6];
            var otherDateAttr = tokens[7]; //can be year or actual time 
            var name = tokens[8];
            var fullName = Path.Combine(_remoteWorkingDirectory, _remotePath, name);

            if (!DateTime.TryParseExact($"{month} {day} {otherDateAttr}",
                    "MMM d yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var converted)
                && !DateTime.TryParseExact($"{month} {day} {otherDateAttr}",
                    "MMM d HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out converted))
                throw new NotSupportedException();

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

    #region Instances

    private readonly string _hostTimeZone;
    private readonly string _remotePath;
    private readonly string _remoteWorkingDirectory;

    #endregion
}