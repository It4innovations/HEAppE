using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;

/// <summary>
///     Tunnel info
/// </summary>
/// <param name="LocalPort">Local port</param>
/// <param name="RemotePort">Remote port</param>
/// <param name="NodeHost">Node host address</param>
/// <param name="ForwardedPort">Forwarded port</param>
/// <param name="SshClient">Underlying SSH client to which the forwarded port was added</param>
public record TunnelInfo(int? LocalPort, int? RemotePort, string NodeHost, ForwardedPort ForwardedPort, SshClient? SshClient = null);