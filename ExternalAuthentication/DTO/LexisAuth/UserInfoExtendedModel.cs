using System;
using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.DTO.LexisAuth;

public sealed record UserInfoExtendedModel
{
    private string _keycloakSid;
    private string _identityProviderUserId;

    public Guid Id { get; set; }

    public string KeycloakSid
    {
        get => !string.IsNullOrEmpty(_keycloakSid) ? _keycloakSid : _identityProviderUserId;
        set => _keycloakSid = value;
    }

    public string IdentityProviderUserId
    {
        get => !string.IsNullOrEmpty(_identityProviderUserId) ? _identityProviderUserId : _keycloakSid;
        set => _identityProviderUserId = value;
    }

    public string Name { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<UserSystemRoleExtendedModel> SystemRoles { get; set; } = new();
}

public record UserSystemRoleExtendedModel
{
    public UserSystemRoleExtendedModel()
    {
    }

    public UserSystemRoleExtendedModel(
        string projectShortName,
        string userEmail,
        string roleName,
        IEnumerable<ProjectResourceDetailModel> projectResources,
        List<string> systemPermissionTypes)
    {
        ProjectShortName = projectShortName;
        UserEmail = userEmail;
        RoleName = roleName;
        ProjectResources = projectResources;
        SystemPermissionTypes = systemPermissionTypes;
    }

    public string IdpUserId { get; set; }
    public string ProjectShortName { get; set; }
    public string UserEmail { get; set; }
    public string RoleName { get; set; }
    public IEnumerable<ProjectResourceDetailModel> ProjectResources { get; set; } = new List<ProjectResourceDetailModel>();
    public List<string> SystemPermissionTypes { get; set; } = new();
}

public record ProjectResourceDetailModel
{
    private string _name;
    private string _resourceName;
    private IList<LocationDetailModel> _locations;
    private IList<LocationDetailModel> _clusters;

    public ProjectResourceDetailModel()
    {
    }

    public ProjectResourceDetailModel(
        string name,
        string description,
        DateTime? startDate,
        DateTime? endDate,
        IList<LocationDetailModel> locations)
    {
        _name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        _locations = locations;
    }

    public string Name
    {
        get => !string.IsNullOrEmpty(_name) ? _name : _resourceName;
        set => _name = value;
    }

    public string ResourceName
    {
        get => !string.IsNullOrEmpty(_resourceName) ? _resourceName : _name;
        set => _resourceName = value;
    }

    public string Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public IList<LocationDetailModel> Locations
    {
        get => _locations ?? _clusters;
        set => _locations = value;
    }

    public IList<LocationDetailModel> Clusters
    {
        get => _clusters ?? _locations;
        set => _clusters = value;
    }
}

public record LocationDetailModel
{
    private string _description;
    private string _endpoint;
    private string _type;
    private string _allocationUnitType;

    public LocationDetailModel()
    {
    }

    public LocationDetailModel(
        string name,
        string descritpion,
        string endpoint,
        bool isPrivate,
        string type,
        long? allocationUnitCount,
        string allocationUnitType,
        IEnumerable<KeyValuePair<string, string>> values)
    {
        Name = name;
        _description = descritpion;
        _endpoint = endpoint;
        IsPrivate = isPrivate;
        _type = type;
        AllocationUnitCount = allocationUnitCount;
        _allocationUnitType = allocationUnitType;
        Values = values;
    }

    public string Name { get; set; }

    public string Descritpion
    {
        get => _description;
        set => _description = value;
    }

    public string Description
    {
        get => _description;
        set => _description = value;
    }

    public string Endpoint
    {
        get => _endpoint;
        set => _endpoint = value;
    }

    public string Url
    {
        get => _endpoint;
        set => _endpoint = value;
    }

    public bool IsPrivate { get; set; }

    public string Type
    {
        get => _type;
        set => _type = value;
    }

    public string ClusterType
    {
        get => _type;
        set => _type = value;
    }

    public long? AllocationUnitCount { get; set; }

    public string AllocationUnitType
    {
        get => _allocationUnitType;
        set => _allocationUnitType = value;
    }

    public string AllocationUnit
    {
        get => _allocationUnitType;
        set => _allocationUnitType = value;
    }

    public IEnumerable<KeyValuePair<string, string>> Values { get; set; }
}