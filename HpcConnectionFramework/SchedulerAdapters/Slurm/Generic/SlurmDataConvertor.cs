using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic
{
    /// <summary>
    /// Slurm data convertor
    /// </summary>
    public class SlurmDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
        public SlurmDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {
        }
        #endregion
        #region Local Members
        /// <summary>
        /// Get job ids after submission
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            var scheduledJobIds = new List<string>();
            foreach (Match match in Regex.Matches(responseMessage, @"(Submitted batch job[\s\t]+)(?<JobId>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 3)
                {
                    scheduledJobIds.Add(match.Groups.GetValueOrDefault("JobId").Value);
                }
            }

            return scheduledJobIds.Any() ? scheduledJobIds : throw new FormatException("Unable to parse response from HPC scheduler!");
        }

        /// <summary>
        /// Convert HPC task information from DTO object
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage)
        {
            SlurmJobDTO obj = (SlurmJobDTO)responseMessage;
            return new SubmittedTaskInfo()
            {
                ScheduledJobId = obj.Id,
                Name = obj.Name,
                StartTime = obj.StartTime,
                EndTime = obj.EndTime,
                AllocatedTime = obj.RunTime.TotalSeconds,
                State = obj.TaskState,
                TaskAllocationNodes = obj.AllocatedNodes?.Select(s => new SubmittedTaskAllocationNodeInfo() { AllocationNodeId = s, SubmittedTaskInfoId = long.Parse(obj.Name) })
                                                          .ToList(),
                ErrorMessage = default,
                AllParameters = obj.SchedulerResponseParameters
            };
        }

        /// <summary>
        /// Read job parameters
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object responseMessage)
        {
            string response = (string)responseMessage;
            var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
            foreach (Match match in Regex.Matches(response, @"(?<jobParameters>.*)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    string jobResponseMessage = match.Groups.GetValueOrDefault("jobParameters").Value;
                    //For each HPC scheduler job
                    var parameters = Regex.Matches(jobResponseMessage, @"\s?(?<Key>.[^=]*)=(?<Value>.[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                        .Where(w => w.Success && w.Groups.Count == 3)
                                        .Select(s => new
                                        {
                                            Key = s.Groups[1].Value.Trim(),
                                            Value = (s.Groups[2].Value is "(null)" or "N/A" or "Unknown" ? string.Empty : s.Groups[2].Value.Trim())
                                        })
                                        .Distinct();

                    Dictionary<string, string> parsedParameters = parameters.ToDictionary(i => i.Key, j => j.Value);

                    var slurmAttributes = new SlurmJobInfoAttributesDTO();
                    var jobParameters = new SlurmJobDTO(jobResponseMessage);

                    var slurmProperties = slurmAttributes.GetType().GetProperties();
                    foreach (var slurmProperty in slurmProperties)
                    {
                        var propertyValues = (List<string>)slurmAttributes.GetType().GetProperty(slurmProperty.Name).GetValue(slurmAttributes, null);
                        foreach (var propertyValue in propertyValues)
                        {
                            if (parsedParameters.ContainsKey(propertyValue))
                            {
                                var jobParametersProperty = jobParameters.GetType().GetProperty(slurmProperty.Name);
                                if (jobParametersProperty != null && jobParametersProperty.CanWrite)
                                {
                                    var value = Mapper.ChangeType(parsedParameters[propertyValue], jobParametersProperty.PropertyType);
                                    jobParametersProperty.SetValue(jobParameters, value, null);
                                }
                                break;
                            }
                        }
                    }
                    jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(jobParameters));
                }
                else
                {
                    throw new FormatException("Unable to parse response from HPC scheduler!");
                }
            }
            return jobSubmitedTasksInfo;
        }
        #endregion
    }
}
