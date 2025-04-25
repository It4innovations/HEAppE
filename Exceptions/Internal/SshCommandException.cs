using System;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal;

public class SshCommandException : InternalException
{
    public SshCommandException(string message) : base(message)
    {
    }

    public SshCommandException(string message, params object[] args) : base(message, args)
    {
    }

    public SshCommandException(string message, Exception innerException, params object[] args) : base(message,
        innerException, args)
    {
    }

    public SshCommandException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    ///     Check if ssh result message contains value
    /// </summary>
    /// <param name="Value"></param>
    /// <returns></returns>
    public bool Contains(string value)
    {
        return Args.Any(m => Regex.Match(m.ToString(), value, RegexOptions.Compiled).Success);
    }
}