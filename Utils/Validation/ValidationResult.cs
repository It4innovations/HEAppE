﻿namespace HEAppE.Utils.Validation;

/// <summary>
///     Result of validation
/// </summary>
public class ValidationResult
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="isValid">Is valid</param>
    /// <param name="message">Message</param>
    public ValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Is valid
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    ///     Validation Message
    /// </summary>
    public string Message { get; }

    #endregion
}