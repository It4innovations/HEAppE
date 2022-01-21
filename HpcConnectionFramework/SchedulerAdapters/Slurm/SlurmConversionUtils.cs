using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm
{
    /// <summary>
    /// Class: Slurm convertion utils
    /// </summary>
    internal static class SlurmConversionUtils
    {
        /// <summary>
        /// Method: Get job ids after submission
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        internal static IEnumerable<string> GetJobIds(string responseMessage)
        {
            List<string> scheduledJobIds = new List<string>();
            foreach (Match match in Regex.Matches(responseMessage, @$"(Submitted batch job)+[\s\t]+[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success)
                {
                    scheduledJobIds.Add(match.Value);
                }
            }
            return scheduledJobIds;
        }

        /// <summary>
        /// Method: Read job parameters
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        internal static SlurmJobDTO ReadParametersFromSqueueResponse(string responseMessage)
        {
            if (!string.IsNullOrEmpty(responseMessage) && responseMessage.Length > 0 && responseMessage.Contains("="))
            {
                string modResponseMessage = Regex.Replace(responseMessage, @"\s+", " ").TrimEnd();
                var pars = modResponseMessage.Split(' ')
                                              .Select(s => s.Split('=', 2))
                                              .Select(se => new { Key = se[0], Value = (se[1] == "(null)" || se[1] == "N/A" || se[1] == "Unknown" ? string.Empty : se[1])})
                                              .Distinct();
                Dictionary<string, string> parsedValues = pars.ToDictionary(i => i.Key, j => j.Value);


                var slurmAttributes = new SlurmJobInfoAttributesDTO();
                var jobParameters = new SlurmJobDTO(parsedValues);

                var slurmProperties = slurmAttributes.GetType().GetProperties();
                foreach (var slurmProperty in slurmProperties)
                {
                    List<string> propertyValues = (List<string>)slurmAttributes.GetType().GetProperty(slurmProperty.Name).GetValue(slurmAttributes, null);
                    foreach (var propertyValue in propertyValues)
                    {
                        if (parsedValues.ContainsKey(propertyValue))
                        {
                            var jobParametersProperty = jobParameters.GetType().GetProperty(slurmProperty.Name);
                            if (jobParametersProperty != null && jobParametersProperty.CanWrite)
                            {
                                var value = SlurmMapper.ChangeType(parsedValues[propertyValue], jobParametersProperty.PropertyType);
                                jobParametersProperty.SetValue(jobParameters, value, null);
                            }
                            break;
                        }
                    }
                }
                return jobParameters;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Method: Get allocated nodes for job
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        internal static List<string> GetAllocatedNodes(string responseMessage)
        {
            List<string> allocatedNodesForJob = new List<string>();
            if (!string.IsNullOrEmpty(responseMessage))
            {
                string result = responseMessage.Replace("[", "").Replace("]", "");
                int scorePosition = result.IndexOf('-');
                int commaPosition = result.IndexOf(',');
                if (commaPosition > 0)
                {
                    allocatedNodesForJob = result.Split(',').ToList();
                }
                else
                {
                    if (scorePosition > 0)
                    {
                        int minNodeValue = int.Parse(result.Substring(scorePosition - 1, 1));
                        int maxNodeValue = int.Parse(result.Substring(scorePosition + 1, 1));
                        result = result.Substring(0, scorePosition - 1);

                        for (int i = minNodeValue; i <= maxNodeValue; i++)
                        {
                            allocatedNodesForJob.Add(result + i);
                        }
                    }
                    else
                    {
                        allocatedNodesForJob.Add(result);
                    }
                }
            }
            return allocatedNodesForJob;
        }
    }
}
