using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class PathValidator : AbstractValidator
    {
        public PathValidator(object validationObj) : base(validationObj)
        {
        }

        public override ValidationResult Validate()
        {
            string message = string.Empty;
            if(_validationObject is string validationObject)
            {
                if(ContainsIllegalCharactersForPath(validationObject))
                {
                    message = "Path contains some illegal characters";
                }
            }

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }
    }
}
