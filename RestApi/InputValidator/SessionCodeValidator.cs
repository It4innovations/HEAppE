using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class SessionCodeValidator : AbstractValidator
    {

        public SessionCodeValidator(object sessionCode) : base(sessionCode)
        {
        }

        public override ValidationResult Validate()
        {
            string message = string.Empty;
            if(_validationObject is string validationObject)
            {
                message = ValidateSessionCode(validationObject);
            }
            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        /// <summary>
        /// Validates session code string, returns errors
        /// </summary>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        protected string ValidateSessionCode(string sessionCode)
        {
            if (string.IsNullOrEmpty(sessionCode))
            {
                _messageBuilder.AppendLine("SessionCode cannot be empty.");
            }
            else if (!IsSessionCode(sessionCode))
            {
                _messageBuilder.AppendLine("SessionCode has wrong format.");
            }
            else if (ContainsIllegalCharacters(sessionCode))
            {
                _messageBuilder.AppendLine("SessionCode contains illegal characters.");
            }
            return _messageBuilder.ToString();
        }
    }
}
