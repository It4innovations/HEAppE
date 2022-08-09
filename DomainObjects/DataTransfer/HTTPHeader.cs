using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.DataTransfer
{
    /// <summary>
    /// HTTP header
    /// </summary>
    public class HTTPHeader
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }
    }
}
