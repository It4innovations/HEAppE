using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer;

/// <summary>
/// Model for retrieving data transfer method
/// </summary>
[DataContract(Name = "GetDataTransferMethodModel")]
[Description("Model for retrieving data transfer method")]
public class GetDataTransferMethodModel : SessionCodeModel
{

    /// <summary>
    /// Submitted task info id
    /// </summary>
    [DataMember(Name = "SubmittedTaskInfoId")]
    [Description("Submitted task info id")]
    public long SubmittedTaskInfoId { get; set; }

    /// <summary>
    /// Ip address
    /// </summary>
    [DataMember(Name = "IpAddress")]
    [StringLength(50)]
    [Description("Ip address")]
    public string IpAddress { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("Port")]
    public int Port { get; set; }

    public override string ToString()
    {
        return $"GetDataTransferMethodModel({base.ToString()}; SubmittedTaskInfoId: {SubmittedTaskInfoId}); IpAddress: {IpAddress}; Port: {Port})";
    }
}

public class GetDataTransferMethodModelValidator : AbstractValidator<GetDataTransferMethodModel>
{
    public GetDataTransferMethodModelValidator()
    {
        Include(new SessionCodeModelValidator());
        RuleFor(x => x.SubmittedTaskInfoId).GreaterThan(0);
    }
}