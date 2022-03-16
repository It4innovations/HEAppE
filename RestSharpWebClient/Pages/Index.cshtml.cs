using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.RestApiModels.JobManagement;
using HEAppE.RestApiModels.JobReporting;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharpWebClient.Pages
{
    public class IndexModel : PageModel
    {
        private static string baseUrl = "http://localhost:5000/heappe";
        static StringBuilder sb = new StringBuilder();

        //DELETE LATER...temporary id of last submitted job and session code for testing
        private static long lastSubmittedJobId;

        private static long AsbSubmittedJob;
        private static DataTransferMethodExt AsbDataTransfer;

        public string ResponseContent { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
        }

        public async Task<IActionResult> OnPostListAvailableClustersAsync()
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("ClusterInformation/ListAvailableClusters", Method.Get);
            var response = await client.ExecuteAsync(request);
            var content = response.Content;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                ResponseContent = JToken.Parse(content).ToString(Newtonsoft.Json.Formatting.Indented);
            else ResponseContent = content;

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAndSubmitTestJobAsync()
        {
            string response = "";
            try
            {
                string username = "testuser";
                string password = "testpass";
                string openIdToken = ""; // Fill the OpenId token for testing.

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                //string sessionCode = AuthenticateUserKeycloakOpenId(openIdToken);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //CreateJobSpecification
                JobSpecificationExt jobSpec = CreateJobSpecification();
                sb.AppendLine(String.Format("JobSpecification created"));
                sb.AppendLine(jobSpec.ToString());

                //CreateJob
                SubmittedJobInfoExt submittedJob = await CreateJobAsync(jobSpec, sessionCode);
                sb.AppendLine(String.Format("Job {0} created", submittedJob.Id));
                sb.AppendLine(submittedJob.ToString());

                //FileUpload
                UploadInputFilesAsync((long)submittedJob.Id, submittedJob.Tasks?.Select(s => s.Id).ToList(), sessionCode);
                sb.AppendLine(String.Format("All files uploaded"));

                //SubmitJob
                SubmitJobAsync((long)submittedJob.Id, sessionCode);
                sb.AppendLine(String.Format("Job submitted"));

                //MonitorJob         
                lastSubmittedJobId = (long)submittedJob.Id; //TODO remove later - only for getCurrentJobInfo
                MonitorJob(submittedJob, sessionCode);

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAndSubmitASBJobAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                //string sessionCode = AuthenticateUserKeycloakOpenId(openIdToken);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //CancelJob(2, sessionCode);

                //CreateJobSpecification
                JobSpecificationExt jobSpec = CreateJobSpecification();
                sb.AppendLine(String.Format("JobSpecification created"));
                sb.AppendLine(jobSpec.ToString());

                //CreateJob
                SubmittedJobInfoExt submittedJob = await CreateJobAsync(jobSpec, sessionCode);
                sb.AppendLine(String.Format("Job {0} created", submittedJob.Id));
                sb.AppendLine(submittedJob.ToString());

                //FileUpload
                //UploadInputFiles((long)submittedJob.Id, submittedJob.Tasks?.Select(s => s.Id).ToList(), sessionCode);
                //sb.AppendLine(String.Format("All files uploaded"));

                //SubmitJob
                SubmitJobAsync((long)submittedJob.Id, sessionCode);
                sb.AppendLine(String.Format("Job submitted"));

                AsbSubmittedJob = (long)submittedJob.Id;

                while (submittedJob.State != JobStateExt.Running)
                {
                    Thread.Sleep(30000);
                    submittedJob = await GetCurrentInfoForJobAsync((long)submittedJob.Id, sessionCode);
                }

                sb.AppendLine(String.Format("Job is running"));

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostCreateSshTunnelASBJobAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                //string sessionCode = AuthenticateUserKeycloakOpenId(openIdToken);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //GetAllocatedNodesIps
                var client = new RestClient(baseUrl);
                var request = new RestRequest("JobManagement/GetAllocatedNodesIPs", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                    new GetAllocatedNodesIPsModel
                    {
                        SubmittedJobInfoId = AsbSubmittedJob,
                        SessionCode = sessionCode
                    });
                var response = await client.ExecuteAsync(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception(response.Content.ToString());
                string[] nodeIps = JsonConvert.DeserializeObject<string[]>(response.Content.ToString());
                sb.AppendLine(String.Format("Returned allocated node IPs"));

                //GetDataTransferMethod
                client = new RestClient(baseUrl);
                request = new RestRequest("DataTransfer/GetDataTransferMethod", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                    new GetDataTransferMethodModel
                    {
                        IpAddress = nodeIps[0],
                        Port = 8080,
                        SubmittedJobInfoId = AsbSubmittedJob,
                        SessionCode = sessionCode
                    });
                response = await client.ExecuteAsync(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception(response.Content.ToString());
                DataTransferMethodExt dt = JsonConvert.DeserializeObject<DataTransferMethodExt>(response.Content.ToString());
                AsbDataTransfer = dt;
                sb.AppendLine(String.Format("DataTransfer ssh tunnel created"));

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostHttpGetToJobNodeAsync()
        {
            string requestParams = Request.Form["httpGetRequest"];
            string requestHeadersString = Request.Form["httpGetHeaders"];
            string[] requestHeader = requestHeadersString.Split(',');

            string username = "testuser";
            string password = "testpass";

            //AuthenticateUserPassword
            sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
            string sessionCode = await AuthenticateUserPasswordAsync(username, password);
            sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

            //HttpGetToJobNode
            var client = new RestClient(baseUrl);
            var request = new RestRequest("DataTransfer/HttpGetToJobNode", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new HttpGetToJobNodeModel
                {
                    HttpRequest = requestParams,
                    HttpHeaders = new List<HTTPHeaderExt>()
                    {
                        new HTTPHeaderExt()
                        {
                            Name = requestHeader[0],
                            Value = requestHeader[1]
                        } 
                    },
                    SubmittedJobInfoId = AsbSubmittedJob,
                    NodeIPAddress = AsbDataTransfer.NodeIPAddress,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
            ResponseContent = response.Content.ToString();

            return Page();
        }

        public async Task<IActionResult> OnPostHttpPostToJobNodeAsync()
        {
            string requestParams = Request.Form["httpPostRequest"];
            string requestHeadersString = Request.Form["httpPostHeaders"];
            string[] requestHeader = requestHeadersString.Split(',');

            string requestPayload = Request.Form["httpPostPayload"];

            string username = "testuser";
            string password = "testpass";

            //AuthenticateUserPassword
            sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
            string sessionCode = await AuthenticateUserPasswordAsync(username, password);
            sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

            //HttpGetToJobNode
            var client = new RestClient(baseUrl);
            var request = new RestRequest("DataTransfer/HttpPostToJobNode", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new HttpPostToJobNodeModel
                {
                    HttpRequest = requestParams,
                    HttpHeaders = new List<HTTPHeaderExt>()
                    {
                        new HTTPHeaderExt()
                        {
                            Name = requestHeader[0],
                            Value = requestHeader[1]
                        }
                    },
                    HttpPayload = requestPayload,
                    SubmittedJobInfoId = AsbSubmittedJob,
                    NodeIPAddress = AsbDataTransfer.NodeIPAddress,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
            ResponseContent = response.Content.ToString();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelASBJobAsync()
        {
            string username = "testuser";
            string password = "testpass";

            //AuthenticateUserPassword
            sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
            string sessionCode = await AuthenticateUserPasswordAsync(username, password);
            //string sessionCode = AuthenticateUserKeycloakOpenId(openIdToken);
            sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

            //CancelJob
            //CancelJob(7, sessionCode);

            //EndDataTransfer
            var client = new RestClient(baseUrl);
            var request = new RestRequest("DataTransfer/EndDataTransfer", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new EndDataTransferModel
                {
                    UsedTransferMethod = AsbDataTransfer,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            //CancelJob
            CancelJobAsync(AsbSubmittedJob, sessionCode);

            ResponseContent = "ASB job was canceled";
            return Page();
        }

        public async Task<IActionResult> OnPostGetCurrentInfoForJobAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);

                //GetCurrentInfoForJob
                SubmittedJobInfoExt submittedJobInfo = await GetCurrentInfoForJobAsync(lastSubmittedJobId, sessionCode);
                sb.AppendLine(String.Format("Job {0} info:", submittedJobInfo.Id));
                sb.AppendLine(submittedJobInfo.ToString());

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostTestSubmitAndCancelJobAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //CreateJobSpecification
                JobSpecificationExt jobSpec = CreateJobSpecification();
                sb.AppendLine(String.Format("JobSpecification created"));
                sb.AppendLine(jobSpec.ToString());

                //CreateJob
                SubmittedJobInfoExt submittedJob = await CreateJobAsync(jobSpec, sessionCode);
                sb.AppendLine(String.Format("Job {0} created", submittedJob.Id));
                sb.AppendLine(submittedJob.ToString());

                //FileUpload
                UploadInputFilesAsync((long)submittedJob.Id, submittedJob.Tasks?.Select(s => s.Id).ToList(), sessionCode);
                sb.AppendLine(String.Format("All files uploaded"));

                //SubmitJob
                SubmitJobAsync((long)submittedJob.Id, sessionCode);
                sb.AppendLine(String.Format("Job submitted"));

                //TODO DELETE LATER - only for getCurrentInfoForJob
                lastSubmittedJobId = (long)submittedJob.Id;

                //wait for job to be in running state and then cancel this job and delete job content on cluster
                while (true)
                {
                    submittedJob = await GetCurrentInfoForJobAsync((long)submittedJob.Id, sessionCode);

                    if (submittedJob.State == JobStateExt.Running)
                    {
                        sb.AppendLine(String.Format("Job {0} state: {1}", submittedJob.Id, submittedJob.State));

                        //CancelJob
                        sb.AppendLine(String.Format("Canceling job {0} ...", submittedJob.Id));
                        submittedJob = await CancelJobAsync((long)submittedJob.Id, sessionCode);
                        sb.AppendLine(String.Format("Job {0} was canceled.", submittedJob.Id));

                        //DeleteJob
                        sb.AppendLine(String.Format("Deleting job {0} ...", submittedJob.Id));
                        string deleteJobResponse = await DeleteJobAsync((long)submittedJob.Id, sessionCode);
                        sb.AppendLine(deleteJobResponse);

                        break;
                    }
                    Thread.Sleep(30000);
                }

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }

            return Page();
        }

        //************** REPORTING ************************
        public async Task<IActionResult> OnPostGetUserResourceUsageReportAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //GetUserResourceUsageReport
                //TODO GET USER ID and DATE TIME
                sb.AppendLine(String.Format("Getting report for User: {0}", 1));
                UserResourceUsageReportExt userUsageReport = await GetUserResourceUsageReportAsync(1, new DateTime(2019, 6, 1), DateTime.UtcNow, sessionCode);
                //sb.AppendLine(String.Format(userUsageReport.ToString()));

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGetUserGroupResourceUsageReportAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //GetUserGroupResourceUsageReport
                sb.AppendLine(String.Format("Getting report for Group: {0}", 1));
                UserGroupResourceUsageReportExt userGroupUsageReport = await GetUserGroupResourceUsageReportAsync(1, new DateTime(2019, 6, 1), DateTime.UtcNow, sessionCode); //TODO GET USER ID and DATE TIME
                //sb.AppendLine(String.Format(userGroupUsageReport.ToString()));

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGetResourceUsageReportForJobAsync()
        {
            try
            {
                string username = "testuser";
                string password = "testpass";

                //AuthenticateUserPassword
                sb.AppendLine(String.Format("username: {0}, password: {1}", username, password));
                string sessionCode = await AuthenticateUserPasswordAsync(username, password);
                sb.AppendLine(String.Format("sessionCode: {0}", sessionCode));

                //GetResourceUsageReportForJob
                sb.AppendLine(String.Format("Getting usage report for Job: {0}", 1));
                SubmittedJobInfoUsageReportExt resourceUsageReportforJob = await GetResourceUsageReportForJobAsync(lastSubmittedJobId, sessionCode); //TODO replace lastSubmittedJobId
                //sb.AppendLine(String.Format(resourceUsageReportforJob.ToString()));

                ResponseContent = sb.ToString();
            }
            catch (Exception e)
            {
                ResponseContent = e.Message;
            }

            return Page();
        }

        private static async Task<UserResourceUsageReportExt> GetUserResourceUsageReportAsync(long userId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobReporting/GetUserResourceUsageReport", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new GetUserResourceUsageReportModel
                {
                    UserId = userId,
                    StartTime = startTime,
                    EndTime = endTime,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
            else sb.AppendLine(JToken.Parse(response.Content).ToString(Newtonsoft.Json.Formatting.Indented));

            UserResourceUsageReportExt userUsageReport = JsonConvert.DeserializeObject<UserResourceUsageReportExt>(response.Content.ToString());
            return userUsageReport;
        }

        private static async Task<UserGroupResourceUsageReportExt> GetUserGroupResourceUsageReportAsync(long groupId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobReporting/GetUserGroupResourceUsageReport", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new GetUserGroupResourceUsageReportModel
                {
                    GroupId = groupId,
                    StartTime = startTime,
                    EndTime = endTime,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
            else sb.AppendLine(JToken.Parse(response.Content).ToString(Newtonsoft.Json.Formatting.Indented));

            UserGroupResourceUsageReportExt userUsageReport = JsonConvert.DeserializeObject<UserGroupResourceUsageReportExt>(response.Content.ToString());
            return userUsageReport;
        }

        private static async Task<SubmittedJobInfoUsageReportExt> GetResourceUsageReportForJobAsync(long jobId, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobReporting/GetResourceUsageReportForJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new GetResourceUsageReportForJobModel
                {
                    JobId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
            else sb.AppendLine(JToken.Parse(response.Content).ToString(Newtonsoft.Json.Formatting.Indented));

            SubmittedJobInfoUsageReportExt jobUsageReport = JsonConvert.DeserializeObject<SubmittedJobInfoUsageReportExt>(response.Content.ToString());
            return jobUsageReport;
        }

        private static async Task<SubmittedJobInfoExt> GetCurrentInfoForJobAsync(long jobId, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobManagement/GetCurrentInfoForJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new GetCurrentInfoForJobModel
                {
                    SubmittedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            SubmittedJobInfoExt currentJobInfo = JsonConvert.DeserializeObject<SubmittedJobInfoExt>(response.Content.ToString());
            return currentJobInfo;
        }


        private static async Task<string> AuthenticateUserPasswordAsync(string username, string password)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("UserAndLimitationManagement/AuthenticateUserPassword", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new AuthenticateUserPasswordModel
                {
                    Credentials = new PasswordCredentialsExt
                    {
                        Username = username,
                        Password = password
                    }
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            string sessionCode = JsonConvert.DeserializeObject<string>(response.Content.ToString());
            return sessionCode;
        }

        private static async Task<string> AuthenticateUserKeycloakOpenIdAsync(string openIdAccessToken)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("UserAndLimitationManagement/AuthenticateUserKeycloakOpenId", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new AuthenticateUserOpenIdModel
                {
                    Credentials = new OpenIdCredentialsExt
                    {
                        OpenIdAccessToken = openIdAccessToken
                    }
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            string sessionCode = JsonConvert.DeserializeObject<string>(response.Content.ToString());
            return sessionCode;
        }

        private static JobSpecificationExt CreateJobSpecification()
        {
            TaskSpecificationExt testTask = new TaskSpecificationExt();
            testTask.Name = "ASBTestJob";
            testTask.MinCores = 1;
            testTask.MaxCores = 36;
            testTask.WalltimeLimit = 900;
            //testTask.RequiredNodes = null;
            //testTask.IsExclusive = false;
            //testTask.IsRerunnable = false;
            //testTask.StandardInputFile = null;
            testTask.StandardOutputFile = "stdout";
            testTask.StandardErrorFile = "stderr";
            testTask.ProgressFile = "stdprog";
            testTask.LogFile = "stdlog";
            //testTask.ClusterTaskSubdirectory = null;
            testTask.ClusterNodeTypeId = 2;
            testTask.CommandTemplateId = 2;
            //testTask.EnvironmentVariables = new EnvironmentVariableExt[0];
            //testTask.RequiredNodes = new string[] { "r2i4n2" };
            //testTask.PlacementPolicy= "vscatter";
            //testTask.TaskParalizationParameters = new TaskParalizationParameterExt[]
            //{
            //   new TaskParalizationParameterExt
            //   {
            //       MaxCores = 24,
            //       MPIProcesses = 2,
            //       OpenMPThreads = 1
            //   }
            //};
            //testTask.DependsOn = null;
            testTask.Priority = TaskPriorityExt.Average;
            testTask.EnvironmentVariables = null;
            testTask.TemplateParameterValues = new CommandTemplateParameterValueExt[0];
            //testTask.JobArrays = "1-2";
            //testTask.TemplateParameterValues = new CommandTemplateParameterValueExt[] {
            //        new CommandTemplateParameterValueExt() { CommandParameterIdentifier = "inputParam", ParameterValue = "someStringParam" }
            //    };


            JobSpecificationExt testJob = new JobSpecificationExt();
            testJob.Name = "ASBTestJob";
            testJob.Project = "ExpTests";
            testJob.WaitingLimit = 0;
            //testJob.NotificationEmail = "some.name@mail.cz";
            //testJob.PhoneNumber = "999111000";
            //testJob.NotifyOnAbort = false;
            //testJob.NotifyOnFinish = false;
            //testJob.NotifyOnStart = false;
            testJob.FileTransferMethodId = 1;
            testJob.ClusterId = 1;

            testJob.EnvironmentVariables = new EnvironmentVariableExt[0];
            testJob.Tasks = new TaskSpecificationExt[] { testTask };

            return testJob;
        }

        private static async Task<SubmittedJobInfoExt> CreateJobAsync(JobSpecificationExt jobSpec, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobManagement/CreateJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new CreateJobModel
                {
                    JobSpecification = jobSpec,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            SubmittedJobInfoExt jobInfo = JsonConvert.DeserializeObject<SubmittedJobInfoExt>(response.Content.ToString());
            return jobInfo;
        }

        private static async Task<SubmittedJobInfoExt> CancelJobAsync(long jobId, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobManagement/CancelJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new CancelJobModel
                {
                    SubmittedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            SubmittedJobInfoExt canceledJobInfo = JsonConvert.DeserializeObject<SubmittedJobInfoExt>(response.Content.ToString());
            return canceledJobInfo;
        }

        private static async Task<string> DeleteJobAsync(long jobId, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobManagement/DeleteJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new DeleteJobModel
                {
                    SubmittedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            return response.Content.ToString();
        }


        private static async Task UploadInputFilesAsync(long jobId, List<long?> taskIds, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("FileTransfer/GetFileTransferMethod", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new GetFileTransferMethodModel
                {
                    SubmittedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            FileTransferMethodExt ft = JsonConvert.DeserializeObject<FileTransferMethodExt>(response.Content.ToString());
            using (MemoryStream pKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(ft.Credentials.PrivateKey)))
            {
                using (ScpClient scpClient = new ScpClient(ft.ServerHostname, ft.Credentials.Username, new PrivateKeyFile(pKeyStream)))
                {
                    scpClient.Connect();
                    DirectoryInfo di = new DirectoryInfo(@"C:\Heappe\projects\develop\tests\input\");
                    foreach (var taskId in taskIds)
                    {
                        foreach (FileInfo fi in di.GetFiles())
                        {
                            sb.AppendLine($"Uploading file: {fi.Name}");
                            scpClient.Upload(fi, ft.SharedBasepath + "/" + taskId + "/" + fi.Name);
                            sb.AppendLine($"File uploaded: {fi.Name}");
                        }
                    }
                }
            }

            client = new RestClient(baseUrl);
            request = new RestRequest("FileTransfer/EndFileTransfer", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new EndFileTransferModel
                {
                    SubmittedJobInfoId = jobId,
                    UsedTransferMethod = ft,
                    SessionCode = sessionCode
                });
            response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
        }

        private static async Task<SubmittedJobInfoExt> SubmitJobAsync(long jobId, string sessionCode)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("JobManagement/SubmitJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new SubmitJobModel
                {
                    CreatedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            SubmittedJobInfoExt jobInfo = JsonConvert.DeserializeObject<SubmittedJobInfoExt>(response.Content.ToString());
            return jobInfo;
        }

        private static async void MonitorJob(SubmittedJobInfoExt submittedJob, string sessionCode)
        {
            bool jobDone = false;

            while (!jobDone)
            {
                submittedJob = await GetCurrentInfoForJobAsync((long)submittedJob.Id, sessionCode);

                switch (submittedJob.State)
                {
                    case JobStateExt.Canceled:
                        //DeleteJob
                        sb.AppendLine(String.Format("Cleaning job {0} ...", submittedJob.Id));
                        string deleteJobResponse = await DeleteJobAsync((long)submittedJob.Id, sessionCode);
                        sb.AppendLine(deleteJobResponse);
                        sb.AppendLine(String.Format("Job {0} was canceled.", submittedJob.Id));
                        jobDone = true;
                        break;
                    case JobStateExt.Failed:
                        //DeleteJob
                        sb.AppendLine(String.Format("Cleaning job {0} ...", submittedJob.Id));
                        deleteJobResponse = await DeleteJobAsync((long)submittedJob.Id, sessionCode);
                        sb.AppendLine(deleteJobResponse);
                        sb.AppendLine(String.Format("Job {0} failed.", submittedJob.Id));
                        jobDone = true;
                        break;
                    case JobStateExt.Running:
                        //    DownloadStdOutAndErrFiles((long)submittedJob.id, sessionCode, (long)submittedJob.tasks.First().id);
                        break;
                    case JobStateExt.Finished:
                        //Download output files
                        sb.AppendLine(String.Format("Downloading output files..."));
                        DownloadOutputFilesAsync((long)submittedJob.Id, sessionCode);
                        sb.AppendLine(String.Format("Output files downloaded."));
                        //DeleteJob
                        sb.AppendLine(String.Format("Cleaning job {0} ...", submittedJob.Id));
                        deleteJobResponse = await DeleteJobAsync((long)submittedJob.Id, sessionCode);
                        sb.AppendLine(String.Format("Job {0} finished.", submittedJob.Id));
                        jobDone = true;
                        break;
                    default:
                        Thread.Sleep(30000);
                        break;
                }
            }
        }
        private static async Task DownloadStdOutAndErrFilesAsync(long jobId, string sessionCode, long TaskId)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("FileTransfer/DownloadPartsOfJobFilesFromCluster", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new DownloadPartsOfJobFilesFromClusterModel
                {
                    SubmittedJobInfoId = jobId,
                    TaskFileOffsets = new TaskFileOffsetExt[1] {
                        new TaskFileOffsetExt(){
                           FileType = SynchronizableFilesExt.StandardOutputFile,
                           Offset = 0,
                           SubmittedTaskInfoId = TaskId
                        }},
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            JobFileContentExt[] contents = JsonConvert.DeserializeObject<JobFileContentExt[]>(response.Content.ToString());
        }

        private static async Task DownloadOutputFilesAsync(long jobId, string sessionCode)
        {
            //GetFileTransferMethod
            var client = new RestClient(baseUrl);
            var request = new RestRequest("FileTransfer/GetFileTransferMethod", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new GetFileTransferMethodModel
                {
                    SubmittedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            FileTransferMethodExt ft = JsonConvert.DeserializeObject<FileTransferMethodExt>(response.Content.ToString());

            //ListChangedFilesForJob
            request = new RestRequest("FileTransfer/ListChangedFilesForJob", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new ListChangedFilesForJobModel
                {
                    SubmittedJobInfoId = jobId,
                    SessionCode = sessionCode
                });
            response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());

            var outputFiles = JsonConvert.DeserializeObject<FileInformationExt[]>(response.Content.ToString());

            //DownloadOutputFiles
            using (MemoryStream pKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(ft.Credentials.PrivateKey)))
            {
                using (ScpClient scpClient = new ScpClient(ft.ServerHostname, ft.Credentials.Username, new PrivateKeyFile(pKeyStream)))
                {
                    scpClient.Connect();

                    string localOutputPath = $"C:\\Heappe\\projects\\develop\\tests\\output\\{jobId}\\";
                    DirectoryInfo di = new DirectoryInfo(localOutputPath);
                    if (!di.Exists)
                        di.Create();

                    foreach (var outputFile in outputFiles)
                    {
                        FileInfo fi = new FileInfo(localOutputPath + outputFile.FileName);
                        if (!fi.Directory.Exists)
                            fi.Directory.Create();

                        using (Stream fileStream = System.IO.File.OpenWrite(localOutputPath + outputFile.FileName))
                        {
                            sb.AppendLine(String.Format("Downloading file {0}", outputFile.FileName));
                            scpClient.Download(ft.SharedBasepath + "/" + outputFile.FileName, fileStream);
                            sb.AppendLine(String.Format("File downloaded {0}", outputFile.FileName));
                        }
                    }
                }
            }

            //EndFileTransfer
            request = new RestRequest("FileTransfer/EndFileTransfer", Method.Post) { RequestFormat = DataFormat.Json }.AddJsonBody(
                new EndFileTransferModel
                {
                    SubmittedJobInfoId = jobId,
                    UsedTransferMethod = ft,
                    SessionCode = sessionCode
                });
            response = await client.ExecuteAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.Content.ToString());
        }
    }
}
