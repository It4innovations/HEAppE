using System;
using System.IO;

namespace HEAppE.DomainObjects.ClusterInformation;

/// <summary>
///     Raw quantum-computer artifact (e.g. IQM settings) as returned by the QScheduler backend.
/// </summary>
public sealed class MachineArtifact : IDisposable
{
    public MachineArtifact(Stream content, string contentType)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
    }

    public Stream Content { get; }

    /// <summary>
    ///     Content type reported by the backend, or application/octet-stream when unknown.
    /// </summary>
    public string ContentType { get; }

    public void Dispose() => Content.Dispose();
}
