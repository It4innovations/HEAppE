﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation;

[Table("ClusterProxyConnection")]
public class ClusterProxyConnection : IdentifiableDbEntity, ISoftDeletableEntity
{
    #region Override Methods

    public override string ToString()
    {
        return
            $"ClusterProxyConnection: Id={Id}, Host={Host}, Type={Type}, Port={Port}, Username={Username}, Password={Password}";
    }

    #endregion

    #region Properties

    [Required] [StringLength(40)] public string Host { get; set; }

    [Required] public int Port { get; set; }

    [Required] public ProxyType Type { get; set; }

    [StringLength(50)] public string Username { get; set; }

    [StringLength(50)] public string Password { get; set; }

    [Required] public bool IsDeleted { get; set; } = false;

    #endregion
}