using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.Utils;

public class FileSystemUtils
{
    private const int WAITING_TIME_FOR_SCHEDULER_CLOSING_OUTPUT_AND_ERROR_FILE_STREAMS = 1500;

    private static string GetHomeDirectory(JobSpecification jobSpecification, string clusterUser)
    {
        var homeDirTemplate = string.Empty;
        if (jobSpecification.Cluster?.CustomConfiguration != null &&
            jobSpecification.Cluster.CustomConfiguration.TryGetValue("HomeDirectoryTemplate", out var template))
        {
            homeDirTemplate = template;
        }

        return homeDirTemplate
            .Replace("{username}", clusterUser)
            .Replace("{USER}", clusterUser)
            .Replace("$USER", clusterUser);
    }

    public static string GetJobClusterDirectoryPath(JobSpecification jobSpecification, string instanceIdentifierPath, string subExecutionsPath)
    {
        var clusterUser = jobSpecification.ClusterUser.Username;
        var homeDir = GetHomeDirectory(jobSpecification, clusterUser);
        var basePath = jobSpecification.Cluster.ClusterProjects.Find(cp => cp.ProjectId == jobSpecification.ProjectId)
            ?.ScratchStoragePath
            ?.Replace("$USER", clusterUser)
            ?.Replace("${USER}", clusterUser)
            ?.Replace("$HOME", homeDir);
        var localBasePath = $"{basePath}/{instanceIdentifierPath}/{subExecutionsPath}/{clusterUser}";

        return ConcatenatePaths(localBasePath, jobSpecification.Id.ToString(CultureInfo.InvariantCulture));
    }

    public static string GetJobClusterArchiveDirectoryPath(JobSpecification jobSpecification, string instanceIdentifierPath, string subExecutionsPath)
    {
        var clusterUser = jobSpecification.ClusterUser.Username;
        var homeDir = GetHomeDirectory(jobSpecification, clusterUser);
        var basePath = jobSpecification.Cluster.ClusterProjects.Find(cp => cp.ProjectId == jobSpecification.ProjectId)
            ?.ProjectStoragePath
            ?.Replace("$USER", clusterUser)
            ?.Replace("${USER}", clusterUser)
            ?.Replace("$HOME", homeDir);
        if (string.IsNullOrEmpty(basePath))
        {
            basePath = jobSpecification.Cluster.ClusterProjects.Find(cp => cp.ProjectId == jobSpecification.ProjectId)
                ?.ScratchStoragePath
                ?.Replace("$USER", clusterUser)
                ?.Replace("${USER}", clusterUser)
                ?.Replace("$HOME", homeDir);
        }
        var localBasePath = $"{basePath}/{instanceIdentifierPath}/{subExecutionsPath}/{clusterUser}";

        return ConcatenatePaths(localBasePath, jobSpecification.Id.ToString(CultureInfo.InvariantCulture));
    }
    public static string GetTaskClusterDirectoryPath(TaskSpecification taskSpecification, string instanceIdentifierPath, string subExecutionsPath)
    {
        var basePath = GetJobClusterDirectoryPath(taskSpecification.JobSpecification, instanceIdentifierPath, subExecutionsPath);
        var taskSubdirectory = !string.IsNullOrEmpty(taskSpecification.ClusterTaskSubdirectory)
            ? $"{taskSpecification.Id}/{taskSpecification.ClusterTaskSubdirectory}"
            : $"{taskSpecification.Id}";

        return ConcatenatePaths(basePath, taskSubdirectory);
    }

    public static string GetTaskClusterArchiveDirectoryPath(TaskSpecification taskSpecification, string instanceIdentifierPath, string subExecutionsPath)
    {
        var basePath = GetJobClusterArchiveDirectoryPath(taskSpecification.JobSpecification, instanceIdentifierPath, subExecutionsPath);
        var taskSubdirectory = !string.IsNullOrEmpty(taskSpecification.ClusterTaskSubdirectory)
            ? $"{taskSpecification.Id}/{taskSpecification.ClusterTaskSubdirectory}"
            : $"{taskSpecification.Id}";

        return ConcatenatePaths(basePath, taskSubdirectory);
    }

    public static string ReadStreamContentFromSpecifiedOffset(Stream stream, long offset)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        using (var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
        {
            return reader.ReadToEnd();
        }
    }

    public static string WriteStreamToLocalFile(Stream stream, string destinationPath)
    {
        EnsureThatDestinationPathExists(destinationPath);
        using (Stream destinationStream =
               new FileStream(destinationPath, FileMode.Append, FileAccess.Write, FileShare.Read))
        using (var ms = new MemoryStream())
        {
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                destinationStream.Write(buffer, 0, bytesRead);
                ms.Write(buffer, 0, bytesRead);
            }

            return Encoding.Default.GetString(ms.GetBuffer(), 0, (int)ms.Length);
        }
    }

    public static void WriteStringToLocalFile(string content, string destinationPath)
    {
        EnsureThatDestinationPathExists(destinationPath);
        using TextWriter destinationStream = File.AppendText(destinationPath);
        destinationStream.Write(content);
    }

    private static void EnsureThatDestinationPathExists(string destinationPath)
    {
        var file = new FileInfo(destinationPath);
        if (!file.Directory.Exists) file.Directory.Create();
    }


    public static string ConcatenatePaths(string startPath, string endPath)
    {
        if (string.IsNullOrEmpty(endPath) || endPath == ".") return startPath;

        if (startPath.Contains('/')) return $"{startPath.TrimEnd('/')}/{endPath.TrimStart('/')}";

        return $"{startPath.TrimEnd('\\')}\\{endPath.TrimStart('\\')}";
    }

    public static void CopyAll(string source, string target, bool overwrite, DateTime? lastModificationLimit,
        string[] excludedFiles)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);
        var sourceDir = new DirectoryInfo(source);
        foreach (var file in sourceDir.GetFiles())
        {
            if (excludedFiles != null && excludedFiles.Contains(file.Name))
                continue;

            var targetFilePath = Path.Combine(target, file.Name);
            var copied = false;
            if ((!File.Exists(targetFilePath) || overwrite)
                && (!lastModificationLimit.HasValue || lastModificationLimit.Value < file.LastWriteTime))
                do
                {
                    try
                    {
                        File.Copy(file.FullName, targetFilePath, overwrite);
                        copied = true;
                    }
                    catch (Exception e)
                    {
                        using (TextWriter wrt = new StreamWriter(Path.Combine(target, "FileError.txt"), true))
                        {
                            wrt.WriteLine("Error while copying file: " + file.FullName);
                            wrt.WriteLine(e.Message);
                            wrt.WriteLine(e.StackTrace);
                            wrt.WriteLine();
                        }

                        Thread.Sleep(WAITING_TIME_FOR_SCHEDULER_CLOSING_OUTPUT_AND_ERROR_FILE_STREAMS);
                    }
                } while (!copied);
        }

        foreach (var directory in sourceDir.GetDirectories())
            CopyAll(directory.FullName, Path.Combine(target, directory.Name),
                overwrite, lastModificationLimit,
                GetExcludedFilesForSubdirectory(excludedFiles, directory.Name));
    }

    public static string[] GetExcludedFilesForSubdirectory(string[] excludedFiles, string subdirectory)
    {
        var subdirExcludedFiles = new List<string>();
        if (excludedFiles != null)
            foreach (var excludedFile in excludedFiles)
            {
                string delimiter = null;
                if (excludedFile.StartsWith(subdirectory + @"/"))
                    delimiter = @"/";
                else if (excludedFile.StartsWith(subdirectory + @"\"))
                    delimiter = @"\";
                if (delimiter != null)
                    subdirExcludedFiles.Add(excludedFile.Substring(excludedFile.IndexOf(delimiter) + 1));
            }

        return subdirExcludedFiles.ToArray();
    }

    public static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            result = fileName.Replace(c, '_');
        return result;
    }

    public static string SanitizePath(string filePath)
    {
        var result = filePath.Replace("..", "__").Replace(":", "_");
        foreach (var c in new[] { '/', '\\' })
        {
            var idx = result.LastIndexOf(c);
            if (idx >= 0)
                result = result.Substring(0, idx) + c + SanitizeFileName(result.Substring(idx + 1));
        }
        return result;
    }

    public static bool AddConfigurationFiles(string[] confsDirs, (string, bool) [] confFiles, Action<string> addJsonFile = null, Action<string> addNotJson = null)
    {
        // go through all directories
        foreach (var confDir in confsDirs)
        {
            // all configuration files must be in the same directory
            bool configFound = true;
            foreach (var confFile in confFiles)
            {
                // skip optional files
                if (confFile.Item2)
                    continue;
                // if any configuration file is missing...
                var confPath = $"{confDir}{Path.DirectorySeparatorChar}{confFile.Item1}";
                if (!File.Exists(confPath))
                {
                    configFound = false;
                    break;
                }
            }
            // ...the directory is rejected as a whole
            if (!configFound)
                continue;

            // add all found configuration files
            foreach (var confFile in confFiles)
            {
                var confPath = $"{confDir}{Path.DirectorySeparatorChar}{confFile.Item1}";
                if (!File.Exists(confPath))
                    continue;
                if (confPath.EndsWith(".json"))
                    addJsonFile?.Invoke(confPath);
                else if (confPath.EndsWith(".njson"))
                    addNotJson?.Invoke(confPath);
            }
            // succeeded
            return true;
        }
        // failed
        return false;
    }

    /// <summary>
    /// Expands path variables (like $USER, ~) based on home directory templates.
    /// </summary>
    public static string ExpandRemotePath(string path, string username, string? homeDir = null, Dictionary<string, string>? customConfiguration = null)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        string? resolvedHomeDir = null;
        if (customConfiguration != null && customConfiguration.TryGetValue("HomeDirectoryTemplate", out var template))
        {
            resolvedHomeDir = template
                .Replace("{username}", username)
                .Replace("{USER}", username)
                .Replace("$USER", username);
        }

        var result = path.Trim();
        if (customConfiguration != null && customConfiguration.ContainsKey("HomeDirectoryTemplate") && result.StartsWith("~"))
        {
            result = resolvedHomeDir + result.Substring(1);
        }

        result = result
            .Replace("$USER", username)
            .Replace("${USER}", username)
            .Replace("$HOME", resolvedHomeDir)
            .Replace("${HOME}", resolvedHomeDir);

        return result.Trim();
    }
}