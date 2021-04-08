using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18.ConversionAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm
{
    /// <summary>
    /// Class: Slurm convertion utils
    /// </summary>
    internal static class SlurmConversionUtils
    {
        /// <summary>
        /// Method: Get job id from 
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        internal static int GetJobIdFromJobCode(string responseMessage)
        {
            int lastTextIndex = responseMessage.LastIndexOf(' ');
            responseMessage = responseMessage.Substring(lastTextIndex).Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty);
            return int.Parse(responseMessage);
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
                Dictionary<string, string> parsedValues = responseMessage.Split(' ')
                   .Select(s => s.Split('='))
                   .ToDictionary(i => i[0], j => (j[1] == "(null)" || j[1] == "N/A" || j[1] == "Unknown" ? string.Empty : j[1]));

                SlurmJobInfoAttributesDTO slurmAttributes = new SlurmJobInfoAttributesDTO();
                SlurmJobDTO jobParameters = new SlurmJobDTO(parsedValues);

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
                                var value = MapperV18.ChangeType(parsedValues[propertyValue], jobParametersProperty.PropertyType);
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
