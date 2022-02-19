using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.MiddlewareUtils
{
    public class FileSystemUtils
    {
        private const int WAITING_TIME_FOR_SCHEDULER_CLOSING_OUTPUT_AND_ERROR_FILE_STREAMS = 1500;

        public static string GetJobClusterDirectoryPath(string basePath, JobSpecification jobSpecification)
        {
            return ConcatenatePaths(basePath, jobSpecification.Id.ToString(CultureInfo.InvariantCulture));
        }

        public static string GetTaskClusterDirectoryPath(string jobClusterDirectoryPath, TaskSpecification taskSpecification)
        {
            string taskSubdirectory = !string.IsNullOrEmpty(taskSpecification.ClusterTaskSubdirectory)
                                        ? $"{taskSpecification.Id}/{taskSpecification.ClusterTaskSubdirectory}"
                                        : $"{taskSpecification.Id}";

            return ConcatenatePaths(jobClusterDirectoryPath, taskSubdirectory);
        }


        public static string ReadStreamContentFromSpecifiedOffset(Stream stream, long offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            StringBuilder synchronizedContent = new StringBuilder();
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                synchronizedContent.Append(Encoding.Default.GetString(buffer, 0, bytesRead));
            }
            return synchronizedContent.ToString();
        }

        public static string WriteStreamToLocalFile(Stream stream, string destinationPath)
        {
            EnsureThatDestinationPathExists(destinationPath);
            using (Stream destinationStream = new FileStream(destinationPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                StringBuilder synchronizedContent = new StringBuilder();
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    destinationStream.Write(buffer, 0, bytesRead);
                    synchronizedContent.Append(Encoding.Default.GetString(buffer, 0, bytesRead));
                }
                return synchronizedContent.ToString();
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
            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }
        }

        private static string TrimSharedPathProtocol(string sharedPath)
        {
            string trimmedPath;
            int startIndex;
            if (sharedPath.StartsWith(@"\\"))
                trimmedPath = sharedPath.TrimStart('\\');
            else if ((startIndex = sharedPath.IndexOf("://")) != -1)
                trimmedPath = sharedPath.Substring(startIndex + 3);
            else
                throw new ArgumentException("The path " + sharedPath +
                                            " is not a correctly formed shared path (it does not start with \"\\\\\" or \"protocolName://\").");
            return trimmedPath;
        }


        public static string ConcatenatePaths(string startPath, string endPath)
        {
            if (string.IsNullOrEmpty(endPath) || endPath == ".")
            {
                return startPath;
            }

            if (startPath.Contains('/'))
            {
                return $"{startPath.TrimEnd('/')}/{endPath.TrimStart('/')}";
            }

            return $"{startPath.TrimEnd('\\')}\\{endPath.TrimStart('\\')}";
        }

        public static void CopyAll(string source, string target, bool overwrite, DateTime? lastModificationLimit, string[] excludedFiles)
        {
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }
            DirectoryInfo sourceDir = new DirectoryInfo(source);
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                if (excludedFiles != null && excludedFiles.Contains(file.Name))
                    continue;

                string targetFilePath = Path.Combine(target, file.Name);
                bool copied = false;
                if (((!File.Exists(targetFilePath)) || overwrite)
                    && ((!lastModificationLimit.HasValue) || (lastModificationLimit.Value < file.LastWriteTime)))
                {
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
            }
            foreach (DirectoryInfo directory in sourceDir.GetDirectories())
            {
                CopyAll(directory.FullName, Path.Combine(target, directory.Name),
                        overwrite, lastModificationLimit,
                        GetExcludedFilesForSubdirectory(excludedFiles, directory.Name));
            }
        }

        public static string[] GetExcludedFilesForSubdirectory(string[] excludedFiles, string subdirectory)
        {
            List<string> subdirExcludedFiles = new List<string>();
            if (excludedFiles != null)
            {
                foreach (string excludedFile in excludedFiles)
                {
                    string delimiter = null;
                    if (excludedFile.StartsWith(subdirectory + @"/"))
                        delimiter = @"/";
                    else if (excludedFile.StartsWith(subdirectory + @"\"))
                        delimiter = @"\";
                    if (delimiter != null)
                        subdirExcludedFiles.Add(excludedFile.Substring(excludedFile.IndexOf(delimiter) + 1));
                }
            }
            return subdirExcludedFiles.ToArray();
        }
    }
}