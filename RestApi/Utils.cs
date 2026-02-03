using System.Linq;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using log4net;

namespace HEAppE.RestApi;

/// <summary>
///     Utils
/// </summary>
public static class Utils
{
    #region Methods

    /// <summary>
    ///     Remove specific character from begining or end of string
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="character">Character</param>
    /// <returns></returns>
    internal static string RemoveCharacterFromBeginAndEnd(string value, char character)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var tempValue = value;
        if (tempValue.ElementAt(0) == character) tempValue = tempValue.Substring(1);
        var lastCharPosition = tempValue.Length - 1;
        if (tempValue.ElementAt(lastCharPosition) == character) tempValue.Substring(0, lastCharPosition);
        return tempValue;
    }

    #endregion
}