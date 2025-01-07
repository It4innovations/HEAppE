using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.Management;

namespace HEAppE.RestApi.Configuration;

/// <summary>
///     Deployment informations configuration
/// </summary>
public sealed class DeploymentInformationsConfiguration
{
    #region Instances

    /// <summary>
    ///     Host adress with schema
    /// </summary>
    private static string _host;

    /// <summary>
    ///     Host postfix addres
    /// </summary>
    private static string _hostPostfix;

    /// <summary>
    ///     Deployment type
    /// </summary>
    private static DeploymentType _deploymentType = DeploymentType.Unspecific;

    /// <summary>
    ///     Resource allocation types
    /// </summary>
    private static ResourceAllocationType[] _resourceAllocationTypes = new ResourceAllocationType[1]
        { ResourceAllocationType.None };

    #endregion

    #region Properties

    /// <summary>
    ///     Instance name
    /// </summary>
    public static string Name { get; set; }

    /// <summary>
    ///     Instance description
    /// </summary>
    public static string Description { get; set; }

    /// <summary>
    ///     Instance version
    /// </summary>
    public static string Version { get; set; }

    /// <summary>
    ///     Instance IP
    /// </summary>
    public static string DeployedIPAddress { get; set; }

    /// <summary>
    ///     Instance port
    /// </summary>
    public static int Port { get; set; }

    /// <summary>
    ///     Host adress with schema
    /// </summary>
    public static string Host
    {
        get => _host;
        set => _host = Utils.RemoveCharacterFromBeginAndEnd(value, '/');
    }

    /// <summary>
    ///     Host postfix addres
    /// </summary>
    public static string HostPostfix
    {
        get => _hostPostfix;
        set => _hostPostfix = Utils.RemoveCharacterFromBeginAndEnd(value, '/');
    }

    /// <summary>
    ///     Deployment type
    /// </summary>
    public static string DeploymentEnvironmentType
    {
        get => default;
        set =>
            _deploymentType = Enum.TryParse(value, true, out DeploymentType convert)
                ? convert
                : DeploymentType.Unspecific;
    }

    /// <summary>
    ///     Deployment type
    /// </summary>
    public static DeploymentType DeploymentType => DeploymentType;

    /// <summary>
    ///     Resource allocation types
    /// </summary>
    public static string[] ResourceAllocationInfrastructureTypes
    {
        get => default;
        set
        {
            if (value.Length > 0)
            {
                var allocationTypes = new HashSet<ResourceAllocationType>();
                foreach (var allocationType in value)
                    if (Enum.TryParse(allocationType, out ResourceAllocationType convert))
                    {
                        allocationTypes.Add(convert);
                    }
                    else
                    {
                        _resourceAllocationTypes = new[] { ResourceAllocationType.None };
                        break;
                    }

                _resourceAllocationTypes = value.Length == allocationTypes.Count
                    ? allocationTypes.ToArray()
                    : new[] { ResourceAllocationType.None };
            }
            else
            {
                _resourceAllocationTypes = new[] { ResourceAllocationType.None };
            }
        }
    }

    /// <summary>
    ///     Resource allocation types
    /// </summary>
    public static ResourceAllocationType[] ResourceAllocationTypes => ResourceAllocationTypes;

    #endregion
}