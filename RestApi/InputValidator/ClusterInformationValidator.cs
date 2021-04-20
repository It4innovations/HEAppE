using HEAppE.RestApiModels.ClusterInformation;
using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class ClusterInformationValidator : AbstractValidator
    {
        public ClusterInformationValidator(object validationObj) : base(validationObj)
        {
        }

        public override ValidationResult Validate()
        {
            string message = string.Empty;
            if (_validationObject is CurrentClusterNodeUsageModel validationObject)
            {
                if (validationObject.ClusterNodeId <= 0)
                {
                    _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage("ClusterNodeId"));
                }
                ValidationResult sessionCodeValidation = new SessionCodeValidator(validationObject.SessionCode).Validate();
                if (!sessionCodeValidation.IsValid)
                {
                    _messageBuilder.AppendLine(sessionCodeValidation.Message);
                }
                message = _messageBuilder.ToString();
            }

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }
    }
}
