using System;
using System.Collections.Generic;
using System.IO;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;

namespace HEAppE.FileTransferFramework.NetworkShare;

public class NetworkShareFileSystemManager : AbstractFileSystemManager
{
    #region Instances

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected new readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    #endregion

    #region Constructors

    public NetworkShareFileSystemManager(ILogger logger, FileTransferMethod configuration,
        FileSystemFactory synchronizerFactory)
        : base(logger, configuration, synchronizerFactory)
    {
    }

    #endregion

    #region AbstractFileSystemManager Members

    public override byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath)
    {
        throw new NotImplementedException();
    }

    public override void DeleteSessionFromCluster(SubmittedJobInfo jobInfo)
    {
        var jobClusterDirectoryPath =
            FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.SubExecutionsPath);
        UnsetReadOnlyForAllFiles(jobClusterDirectoryPath);
        Directory.Delete(jobClusterDirectoryPath, true);
    }

    protected override void CopyAll(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit,
        string[] excludedFiles, ClusterAuthenticationCredentials credentials, Cluster cluster)
    {
        FileSystemUtils.CopyAll(source, target, overwrite, lastModificationLimit, excludedFiles);
    }

    protected override ICollection<FileInformation> ListChangedFilesForTask(string hostTimeZone,
        string taskClusterDirectoryPath, DateTime? jobSubmitTime,
        ClusterAuthenticationCredentials clusterAuthenticationCredentials, Cluster cluster)
    {
        throw new NotImplementedException();
    }

    protected override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo,
        ClusterAuthenticationCredentials credentials)
    {
        return _synchronizerFactory.CreateFileSynchronizer(fileInfo, credentials);
    }

    #endregion

    #region Local Methods

    private static void UnsetReadOnlyForAllFiles(string dirPath)
    {
        var dirInfo = new DirectoryInfo(dirPath);
        foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            try
            {
                file.IsReadOnly = false;
            }
            catch (Exception)
            {
            }
    }

    public override byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification,
        string absoluteFilePath)
    {
        throw new NotImplementedException();
    }

    #endregion
}