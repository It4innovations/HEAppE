using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

public class ProjectConfigurationException : ExternalException
{
    public ProjectConfigurationException(string message) : base(message)
    {
    }
}