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
            return Regex.IsMatch(text, @"[^a-zA-Z0-9_\-\ \.]+", RegexOptions.Compiled);
        }

        /// <summary>
        /// Contains illegal characters for path
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool ContainsIllegalCharactersForPath(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return Regex.IsMatch(text, @"[^a-zA-Z0-9_\-\ \\\/\.]+", RegexOptions.Compiled);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Is phone number
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool IsPhoneNumber(string text)
        {
            return Regex.IsMatch(text, @"^\+?[0-9\ ]+$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Is email address
        /// </summary>
        /// <param name="text">text</param>
        /// <returns></returns>
        protected static bool IsEmailAddress(string text)
        {
            return Regex.IsMatch(text, @"^[-0-9a-zA-Z.+_]+@[-0-9a-zA-Z.+_]+\.[a-zA-Z]{2,4}$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Checks if string is in the format of session code
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected static bool IsSessionCode(string text)
        {
            return Regex.IsMatch(text, @"^[0-z]{8}-[0-z]{4}-[0-z]{4}-[0-z]{4}-[0-z]{12}$", RegexOptions.Compiled);
        }


        protected static bool IsIpAddress(string text)
        {
            return Regex.IsMatch(text, @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled)//ipv4
                || Regex.IsMatch(text, @"^(?:[A-F0-9]{1,4}:){7}[A-F0-9]{1,4}$", RegexOptions.Compiled)//ipv6 FULL
                || Regex.IsMatch(text, @"^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\-]*[A-Za-z0-9])$");//hostname dns
        }

        protected static bool IsDomainName(string text)
        {
            //https://regex101.com/r/6vkTb9/1
            return Regex.IsMatch(text, @"^(www\.){0,1}((?![0-9-])[A-Za-z0-9-]{1,63})((\.[A-Za-z]{2,33}))+(\/[A-Za-z0-9-]+)*(\/){0,1}$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Returns id Id has valid signed not zero value
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected static bool IsValidId<T>(T id)
        {
            // casting generic data type
            switch (id)
            {
                case byte i:
                    return i > 0;
                case short i:
                    return i > 0;
                case int i:
                    return i > 0;
                case long i:
                    return i > 0;
                default:
                    throw new ArgumentException($"Is {typeof(T)} valid identifier data type?");
            }
        }

        #endregion

        #region Messages

        protected static string MustBeGreaterThanZeroMessage(string attributeName)
        {
            return $"{attributeName} must be greater than 0";
        }

        #endregion


        #region Validation Methods
        /// <summary>
        /// Validates if Id is Greater than zero, od adds message to message builder
        /// </summary>
        /// <param name="id"></param>
        /// <param name="idName"></param>
        protected void ValidateId<T>(T id, string idName)
        {
            if (!IsValidId(id))
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(idName));
        }
        #endregion
    }
}
