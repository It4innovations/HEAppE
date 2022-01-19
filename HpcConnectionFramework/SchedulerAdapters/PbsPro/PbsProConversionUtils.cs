using System;
using System.Collections.Generic;
using System.Globalization;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro
{
    public static class PbsProConversionUtils
    {
        public static Dictionary<string, string> ReadQstatResultFromJobSource(string jobSource)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(jobSource))
            {
                string[] lines = jobSource.Split('\n');
                if (lines[0].IndexOf("Job Id:") != -1)
                {
                    int separatorIndex = lines[0].IndexOf(":");
                    //MessageLogger.Log("Index of : character in " + lines[0] + ">> " + separatorIndex);
                    info[lines[0].Substring(0, separatorIndex).Trim()] = lines[0].Substring(separatorIndex + 1).Trim();
                    string name = null;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("\t"))
                        {
                            //MessageLogger.Log("Appending " + lines[i] + " to " + name);
                            if (name != null)
                                info[name] += lines[i].Trim();
                        }
                        else
                        {
                            separatorIndex = lines[i].IndexOf('=');
                            //MessageLogger.Log("Index of = character in " + lines[i] + " >> " + separatorIndex);
                            if (separatorIndex > 0)
                            {
                                name = lines[i].Substring(0, separatorIndex).Trim();
                                info[name] = lines[i].Substring(separatorIndex + 1).Trim();
                            }
                        }
                    }
                }
                /*MessageLogger.Log("ReadQstatResultsFromJobSource Results:");
        MessageLogger.Log(info);*/
            }
            return info;
        }

        public static DateTime ConvertQstatDateStringToDateTime(string result)
        {
#warning todo Get zone from PBS
            var dateInLocalTime = DateTime.ParseExact(result.Replace("  ", " "), "ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
            return new DateTime(dateInLocalTime.Ticks, DateTimeKind.Local).ToUniversalTime();
        }

        public static int ConvertQstatTimeStringToSeconds(string qstatTime)
        {
            string[] resultSplit = qstatTime.Split(':');
            return Convert.ToInt32(resultSplit[0]) * 3600 + Convert.ToInt32(resultSplit[1]) * 60 + Convert.ToInt32(resultSplit[2]);
        }

        public static string ConvertSecondsToQstatTimeString(int seconds)
        {
            return (seconds / 3600) + ":" + ((seconds % 3600) / 60) + ":" + (seconds % 60);
        }

        public static string GetJobIdFromJobCode(string result)
        {
            return result;
        }

        public static string[] GetJobIdsFromJobCode(string result) {
            List<string> ids = new List<string>();
            string[] resultSplit = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var r in resultSplit) {
                ids.Add(r);
            }

            return ids.ToArray();
        }

        public static List<string> ConvertNodesUrlsToList(string result)
        {
            List<string> nodesUrls = new List<string>();
            if (!string.IsNullOrEmpty(result))
            {
                string[] lines = result.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string nodeId = lines[i].Trim();
                    if (nodeId != null && nodeId != "")
                        nodesUrls.Add(nodeId);
                }
            }
            return nodesUrls;
        }
    }
}