using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.Utils.Validation
{
    /// <summary>
    /// Validator
    /// </summary>
    public abstract class AbstractValidator
    {
        #region Instances
        /// <summary>
        /// Message builder
        /// </summary>
        protected readonly StringBuilder _messageBuilder;

        /// <summary>
        /// Object for validation
        /// </summary>
        protected readonly object _validationObject;
        #endregion
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="validationObj">Object for validation</param>
        protected AbstractValidator(object validationObj)
        {
            _validationObject = validationObj;
            _messageBuilder = new StringBuilder();
        }
        #endregion
        #region Abstract Methods
        /// <summary>
        /// Validate
        /// </summary>
        /// <returns></returns>
        public abstract ValidationResult Validate();
        #endregion
        #region Methods
        /// <summary>
        /// Contains illegal characters
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool ContainsIllegalCharacters(string text)
        {
            return Regex.IsMatch(text, @"[^a-zA-Z0-9_\-\ \.]+");
        }

        /// <summary>
        /// Contains illegal characters for path
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool ContainsIllegalCharactersForPath(string text)
        {
            return Regex.IsMatch(text, @"[^a-zA-Z0-9_\-\ \\\/\.]+");
        }

        /// <summary>
        /// Is phone number
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool IsPhoneNumber(string text)
        {
            return Regex.IsMatch(text, @"^\+?[0-9\ ]+$");
        }

        /// <summary>
        /// Is email address
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool IsEmailAddress(string text)
        {
            return Regex.IsMatch(text, @"^[-0-9a-zA-Z.+_]+@[-0-9a-zA-Z.+_]+\.[a-zA-Z]{2,4}$");
        }
        #endregion
    }
}
