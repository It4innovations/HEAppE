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
            var scheduledJobIds = new List<string>();
            foreach (Match match in Regex.Matches(responseMessage, @$"(?<JobId>Submitted batch job[\s\t]+)([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 3)
                {
                    scheduledJobIds.Add(match.Groups[2].Value);
                }
            }

            return scheduledJobIds.Any() ? scheduledJobIds : throw new FormatException(responseMessage);
        }

        /// <summary>
        /// Method: Read job parameters
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        internal static IEnumerable<SlurmJobDTO> ReadParametersFromSqueueResponse(string responseMessage)
        {
            var jobsParameters = new List<SlurmJobDTO>();
            foreach (Match match in Regex.Matches(responseMessage, @$"(?<jobParameters>.*)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 1)
                {
                    //For each HPC scheduler job
                    var parameters = Regex.Matches(responseMessage, @$"\s?(?<Key>.[^=]*)=(?<Value>.[^=\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                        .Where(w => w.Success && w.Groups.Count == 2)
                                        .Select(s => new { Key = s.Groups[0].Value, Value = (s.Groups[1].Value == "(null)" || s.Groups[1].Value == "N/A" || s.Groups[1].Value == "Unknown" ? string.Empty : s.Groups[1].Value) })
                                        .Distinct();

                    Dictionary<string, string> parsedParameters = parameters.ToDictionary(i => i.Key, j => j.Value);

                    var slurmAttributes = new SlurmJobInfoAttributesDTO();
                    var jobParameters = new SlurmJobDTO(parsedParameters);

                    var slurmProperties = slurmAttributes.GetType().GetProperties();
                    foreach (var slurmProperty in slurmProperties)
                    {
                        List<string> propertyValues = (List<string>)slurmAttributes.GetType().GetProperty(slurmProperty.Name).GetValue(slurmAttributes, null);
                        foreach (var propertyValue in propertyValues)
                        {
                            if (parsedParameters.ContainsKey(propertyValue))
                            {
                                var jobParametersProperty = jobParameters.GetType().GetProperty(slurmProperty.Name);
                                if (jobParametersProperty != null && jobParametersProperty.CanWrite)
                                {
                                    var value = SlurmMapper.ChangeType(parsedParameters[propertyValue], jobParametersProperty.PropertyType);
                                    jobParametersProperty.SetValue(jobParameters, value, null);
                                }
                                break;
                            }
                        }
                    }
                    jobsParameters.Add(jobParameters);
                }
                else
                {
                    throw new FormatException("Unable to parse response from HPC scheduler!");
                }
            }
            return jobsParameters;
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
