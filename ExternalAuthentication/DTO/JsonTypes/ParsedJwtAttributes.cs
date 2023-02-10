using System.Collections.Generic;
using System.Linq;

namespace HEAppE.ExternalAuthentication.DTO.JsonTypes
{
    internal class ParsedJwtAttributes
    {
        internal sealed class OrganizationProjectPair
        {
            /// <summary>
            /// Organization name.
            /// </summary>
            internal string OrganizationName { get; }

            /// <summary>
            /// Project name.
            /// </summary>
            internal string ProjectName { get; }

            /// <summary>
            /// Construct organization-project pair.
            /// </summary>
            /// <param name="organization">Organization name.</param>
            /// <param name="project">Project pair.</param>
            internal OrganizationProjectPair(string organization, string project)
            {
                OrganizationName = organization;
                ProjectName = project;
            }
        }

        /// <summary>
        /// Project access type.
        /// </summary>
        internal enum AccessType
        {
            List,
            Read,
            Write
        }

        /// <summary>
        /// Array of organization-project pairs which can be listed by the user.
        /// </summary>
        internal List<OrganizationProjectPair> ListPairs { get; set; } = new();

        /// <summary>
        /// Array of organization-project pairs which can be read by the user.
        /// </summary>
        internal List<OrganizationProjectPair> ReadPairs { get; set; } = new();

        /// <summary>
        /// Array of organization-project pairs which can be written to by the user.
        /// </summary>
        internal List<OrganizationProjectPair> WritePairs { get; set; } = new();

        /// <summary>
        /// Check if user has access to given organization project pair.
        /// </summary>
        /// <param name="accessType">Access type.</param>
        /// <param name="organization">Organization name.</param>
        /// <param name="project">Project name.</param>
        /// <returns></returns>
        internal bool HasAccess(AccessType accessType, string organization, string project)
        {
            var pairArray = accessType switch
            {
                AccessType.List => ListPairs,
                AccessType.Read => ReadPairs,
                AccessType.Write => WritePairs,
                _ => throw new System.ArgumentException("Invalid project access type", nameof(accessType))
            };
            if (pairArray.Count == 0)
                return false;

            return pairArray.SingleOrDefault(x => x.OrganizationName == organization && x.ProjectName == project) is not null;
        }
    }
}
