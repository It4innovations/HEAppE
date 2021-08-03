using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework;
using log4net;
using System.Net;
using System.Net.Sockets;
using HEAppE.BusinessLogicTier.Logic.DataTransfer.Exceptions;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace HEAppE.BusinessLogicTier.Logic.DataTransfer
{
    internal class DataTransferLogic : IDataTransferLogic {
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly IUnitOfWork unitOfWork;

        //local sockets for jobs and ips
        //private static Dictionary<long, Dictionary<string, Socket>> jobIpSockets = new Dictionary<long, Dictionary<string, Socket>>();
        //private static Dictionary<string, int> ipLocalPorts = new Dictionary<string, int>();
        private static Dictionary<long, Dictionary<string, int>> jobIpLocalports = new Dictionary<long, Dictionary<string, int>>();

        internal DataTransferLogic(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public DataTransferMethod GetDataTransferMethod(string ipAddress, int port, SubmittedJobInfo jobInfo, AdaptorUser loggedUser)
        {
            log.Info("Getting data transfer method for submitted job info ID " + jobInfo.Id + " with user " + loggedUser.GetLogIdentification());
            if (jobInfo.State == JobState.Running)
            {
                try
                {

                    //ipLocalPorts.Add(ipAddress, allocatedPort);
                    int allocatedPort;

                    if (jobIpLocalports.Keys.Contains(jobInfo.Id) && jobIpLocalports[jobInfo.Id].Keys.Contains(ipAddress))
                    {
                        log.Warn("Job " + jobInfo.Id + " with remote IP " + ipAddress + " already has ssh tunnel.");
                    }
                    else
                    {
                        //Create tunnel
                        allocatedPort = CreateSshTunnel(jobInfo, "127.0.0.1", 4000, jobInfo.Specification.Cluster.MasterNodeName, ipAddress, port);

                        //add tunnel to dictionary
                        if (jobIpLocalports.Keys.Contains(jobInfo.Id))
                            jobIpLocalports[jobInfo.Id].Add(ipAddress, allocatedPort);
                        else jobIpLocalports.Add(jobInfo.Id, new Dictionary<string, int> { { ipAddress, allocatedPort } });
                    }

                    DataTransferMethod dtMethod = new DataTransferMethod
                    {
                        SubmittedJobId = jobInfo.Id,
                        IpAddress = ipAddress,
                        Port = port
                    };
                    return dtMethod;
                }
                catch (Exception e)
                {
                    log.Error("GetDataTransferMethod error: {0}", e);
                    //RemoveSshTunnel(jobInfo, ipAddress);
                    //CloseLocalSocketConnection(jobInfo.Id, ipAddress);
                    return default;
                }
            }
            else
            {
                throw new UnableToCreateConnectionException("JobId " + jobInfo.Id + " is not a currently running job.");
            }
        }

        public void EndDataTransfer(DataTransferMethod transferMethod, SubmittedJobInfo jobInfo, AdaptorUser loggedUser)
        {
            log.Info("Removing data transfer method for submitted job info ID " + transferMethod.SubmittedJobId + " with user " + loggedUser.GetLogIdentification());
            if (jobIpLocalports.Keys.Contains(transferMethod.SubmittedJobId))
            {
                if (jobIpLocalports[transferMethod.SubmittedJobId].Keys.Contains(transferMethod.IpAddress))
                {
                    try
                    {
                        //Remove tunnel
                        RemoveSshTunnel(jobInfo, transferMethod.IpAddress);
                        jobIpLocalports[transferMethod.SubmittedJobId].Remove(transferMethod.IpAddress);
                        if (jobIpLocalports[transferMethod.SubmittedJobId].Count == 0)
                            jobIpLocalports.Remove(transferMethod.SubmittedJobId);
                        //CloseLocalSocketConnection(transferMethod.SubmittedJobId, transferMethod.IpAddress);
                    }
                    catch (Exception e)
                    {
                        log.Error("Error in removing data transfer method for submitted job info ID " + transferMethod.SubmittedJobId + ": {0}", e);
                    }
                }
            }
            else log.Error("Job " + transferMethod.SubmittedJobId + " with IP " + transferMethod.IpAddress + " does not have an active ssh tunnel.");
        }

        //public string HttpGetToJobNodeSocket(string httpRequest, long submittedJobInfoId, string ipAddress, string sessionCode)
        //{
        //    SubmittedJobInfo jobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);

        //    string httpResponse = null;

        //    if (SshTunnelExist(jobInfo, ipAddress))
        //    {
        //        if (!jobIpSockets.Keys.Contains(submittedJobInfoId) || !jobIpSockets[submittedJobInfoId].Keys.Contains(ipAddress))
        //        {
        //            int allocatedPort = ipLocalPorts[ipAddress];

        //            //create new socket
        //            CreateLocalSocketConnection(submittedJobInfoId, ipAddress, allocatedPort);
        //        }
        //        //else
        //        //{
        //        if (httpRequest != null)
        //        {
        //            string httpRequestHeader = "GET " + httpRequest + " HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: keep-alive\r\nAccept: text/html\r\nUser-Agent: Heappe\r\n\r\n";

        //            Socket sock = jobIpSockets[submittedJobInfoId][ipAddress];
        //            try
        //            {
        //                int bytesSent = sock.Send(Encoding.UTF8.GetBytes(httpRequestHeader));
        //                log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + ", HTTP request header: " + httpRequestHeader + ", sent " + bytesSent + " bytes.");

        //                bool flag = true;
        //                string headerString = "";
        //                int contentLength = 0;
        //                byte[] bodyBuff = new byte[0];
        //                while (flag)
        //                {
        //                    byte[] buffer = new byte[1];
        //                    sock.Receive(buffer, 0, 1, 0);
        //                    headerString += Encoding.ASCII.GetString(buffer);
        //                    if (headerString.Contains("\r\n\r\n"))
        //                    {
        //                        Regex reg = new Regex("\\\r\nContent-Length: (.*?)\\\r\n");
        //                        Match m = reg.Match(headerString);
        //                        contentLength = int.Parse(m.Groups[1].ToString());
        //                        flag = false;
        //                        // read the body
        //                        bodyBuff = new byte[contentLength];
        //                        sock.Receive(bodyBuff, 0, contentLength, 0);
        //                    }
        //                }
        //                httpResponse = Encoding.ASCII.GetString(bodyBuff);

        //                //close the socket
        //                CloseLocalSocketConnection(submittedJobInfoId, ipAddress);
                        
        //                log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + ", HTTP response: " + httpResponse);
        //            }
        //            catch (Exception e)
        //            {
        //                log.Error("Socket write error: {0}", e);
        //            }
        //        }
        //        else
        //        {
        //            log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + " : nothing to send, NULL data value.");
        //        }
        //    }
        //    else log.Error("Job " + submittedJobInfoId + " with IP remote " + ipAddress + " does not have an active connection.");

        //    return httpResponse;
        //}

        public string HttpGetToJobNode(string httpRequest, string[] httpHeaders, long submittedJobInfoId, string ipAddress)
        {
            SubmittedJobInfo jobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
            log.Info("HTTP GET from job " + submittedJobInfoId + " with remote IP " + ipAddress + ", HTTP request: " + httpRequest + ", HTTP headers: " + httpHeaders.Aggregate((a, b) => a + ',' + b));

            string httpResponse = null;

            if (SshTunnelExist(jobInfo, ipAddress))
            {
                int allocatedPort = jobIpLocalports[submittedJobInfoId][ipAddress];

                if (httpRequest != null)
                {
                    WebRequest wr = WebRequest.Create("http://localhost:" + allocatedPort + httpRequest);
                    wr.Method = "GET";
                    foreach (string header in httpHeaders)
                    {
                        try
                        {
                            wr.Headers.Add(header.Split(':')[0].Trim(), header.Split(':')[1].Trim());
                        }
                        catch (Exception e)
                        {
                            log.Error("HTTP GET from job " + submittedJobInfoId + " with remote IP " + ipAddress + " - error adding header: " + header);
                        }
                    }

                    HttpWebResponse response = null;
                    try
                    {
                        // Get the response.         
                        response = (HttpWebResponse)wr.GetResponse();

                        if (string.IsNullOrEmpty(response.CharacterSet))
                        {
                            using (var responseStream = response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(responseStream))
                                    httpResponse = reader.ReadToEnd();
                            }
                        }
                        else
                        {
                            using (var responseStream = response.GetResponseStream())
                            {
                                var encoding = Encoding.GetEncoding(response.CharacterSet);
                                using (var reader = new StreamReader(responseStream, encoding))
                                    httpResponse = reader.ReadToEnd();
                            }
                        }
                        log.Info("HTTP GET from job " + submittedJobInfoId + " with remote IP " + ipAddress + ", HTTP response: " + httpResponse);
                    }
                    catch (Exception e)
                    {
                        log.Error("HTTP GET error: {0}", e);
                    }
                }
                else
                {
                    log.Info("HTTP GET from job " + submittedJobInfoId + " with remote IP " + ipAddress + " : missing request.");
                }
            }
            else log.Error("Job " + submittedJobInfoId + " with IP remote " + ipAddress + " does not have an active connection.");

            return httpResponse;
        }

        public string HttpPostToJobNode(string httpRequest, string[] httpHeaders, string httpPayload, long submittedJobInfoId, string ipAddress)
        {
            SubmittedJobInfo jobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
            log.Info("HTTP POST from job " + submittedJobInfoId + " with remote IP " + ipAddress + ", HTTP request: " + httpRequest + ", HTTP Payload: " + httpPayload);

            string httpResponse = null;

            if (SshTunnelExist(jobInfo, ipAddress))
            {
                int allocatedPort = jobIpLocalports[submittedJobInfoId][ipAddress];

                if (httpRequest != null && httpPayload != null)
                {
                    WebRequest wr = WebRequest.Create("http://localhost:" + allocatedPort + httpRequest);
                    wr.Method = "POST";
                    byte[] payload = Encoding.UTF8.GetBytes(httpPayload);
                    //Set the Content Type     
                    wr.ContentType = "application/x-www-form-urlencoded";
                    wr.ContentLength = payload.Length;
                    foreach (string header in httpHeaders)
                    {
                        try
                        {
                            wr.Headers.Add(header.Split(':')[0].Trim(), header.Split(':')[1].Trim());
                        }
                        catch (Exception e)
                        {
                            log.Error("HTTP POST from job " + submittedJobInfoId + " with remote IP " + ipAddress + " - error adding header: " + header);
                        }
                    }
                    Stream reqdataStream = wr.GetRequestStream();
                    // Write the data to the request stream.     
                    reqdataStream.Write(payload, 0, payload.Length);
                    reqdataStream.Close();

                    HttpWebResponse response = null;
                    try
                    {
                        // Get the response.         
                        response = (HttpWebResponse)wr.GetResponse();
                        var encoding = Encoding.GetEncoding(response.CharacterSet);
                        using (var responseStream = response.GetResponseStream())
                        using (var reader = new StreamReader(responseStream, encoding))
                            httpResponse = reader.ReadToEnd();
                        log.Info("HTTP POST from job " + submittedJobInfoId + " with remote IP " + ipAddress + ", HTTP response: " + httpResponse);
                    }
                    catch (Exception e)
                    {
                        log.Error("HTTP POST error: {0}", e);
                    }
                }
                else
                {
                    log.Info("HTTP POST from job " + submittedJobInfoId + " with remote IP " + ipAddress + " : nothing to send, NULL data value.");
                }
            }
            else log.Error("Job " + submittedJobInfoId + " with IP remote " + ipAddress + " does not have an active connection.");

            return httpResponse;
        }

        //public int WriteDataToJobNode(byte[] data, long submittedJobInfoId, string ipAddress, string sessionCode, bool closeConnection)
        //{
        //    SubmittedJobInfo jobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);

        //    int bytesSent = 0;

        //    if (SshTunnelExist(jobInfo, ipAddress))
        //    {
        //        if (!jobIpSockets.Keys.Contains(submittedJobInfoId) || !jobIpSockets[submittedJobInfoId].Keys.Contains(ipAddress))
        //        {
        //            int allocatedPort = ipLocalPorts[ipAddress];

        //            //create new socket
        //            CreateLocalSocketConnection(submittedJobInfoId, ipAddress, allocatedPort);
        //        }
        //        //else
        //        //{
        //        if (data != null)
        //        {
        //            Socket sock = jobIpSockets[submittedJobInfoId][ipAddress];
        //            try
        //            {
        //                bytesSent = sock.Send(data);
        //                log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + " sent " + bytesSent + " bytes.");
        //            }
        //            catch (Exception e)
        //            {
        //                log.Error("Socket write error: {0}", e);
        //            }
        //        }
        //        else
        //        {
        //            log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + " : nothing to send, NULL data value.");
        //        }

        //        if (closeConnection)
        //        {
        //            Socket sock = jobIpSockets[submittedJobInfoId][ipAddress];
        //            sock.Shutdown(SocketShutdown.Send);
        //            log.Info("Closed SEND direction in socket for submitted job info Id: " + submittedJobInfoId + " and remote IP address: " + ipAddress);
        //        }
        //        //}
        //    }
        //    else log.Error("Job " + submittedJobInfoId + " with IP remote " + ipAddress + " does not have an active connection.");


        //    return bytesSent;
        //}

        //public byte[] ReadDataFromJobNode(long submittedJobInfoId, string ipAddress, string sessionCode)
        //{
        //    if (jobIpSockets.Keys.Contains(submittedJobInfoId) && jobIpSockets[submittedJobInfoId].Keys.Contains(ipAddress))
        //    {
        //        Socket sock = jobIpSockets[submittedJobInfoId][ipAddress];
        //        byte[] buffer = new byte[1048576];
        //        byte[] retData = new byte[0];

        //        try
        //        {
        //            int bytesRec = sock.Receive(buffer);
        //            if (bytesRec > 0)
        //            {
        //                retData = new byte[bytesRec];
        //                Array.Copy(buffer, retData, bytesRec);
        //            }
        //            else
        //            {
        //                log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + " received 0 bytes - returning null and closing socket.");
        //                CloseLocalSocketConnection(submittedJobInfoId, ipAddress);
        //                return default;
        //            }
        //            log.Info("Socket from job " + submittedJobInfoId + " with remote IP " + ipAddress + " received " + bytesRec + " bytes.");
        //        }
        //        catch (Exception e)
        //        {
        //            log.Error("Socket read error: {0}", e);
        //        }
        //        return retData;
        //    }
        //    else
        //    {
        //        log.Error("Job " + submittedJobInfoId + " with remote IP " + ipAddress + " does not have an active socket.");
        //        return default;
        //    }
        //}

        public List<long> GetJobIdsForOpenTunnels()
        {
            foreach(long jobId in jobIpLocalports.Keys) 
            { 
                log.InfoFormat("Listing all open tunnels:");
                foreach (string ipAddress in jobIpLocalports[jobId].Keys)
                {
                    log.InfoFormat("Open tunnel for jobId {0}, remote IP address {1}, local port {2}", jobId, ipAddress, jobIpLocalports[jobId][ipAddress]);
                }
            }

            List<long> keyList = new List<long>(jobIpLocalports.Keys);
            return keyList;
        }

        public void CloseAllConnectionsForJob(SubmittedJobInfo jobInfo)
        {
            log.InfoFormat("Closing all connections for jobId {0}", jobInfo.Id);
            foreach (string ipAddress in jobIpLocalports[jobInfo.Id].Keys.ToList())
            {
                //Close tunnel
                RemoveSshTunnel(jobInfo, ipAddress);
                if(jobIpLocalports[jobInfo.Id].Keys.Contains(ipAddress))
                    jobIpLocalports[jobInfo.Id].Remove(ipAddress);
                //ipLocalPorts.Remove(ipAddress);
                //CloseLocalSocketConnection(jobInfo.Id, ipAddress);
            }
            if (jobIpLocalports[jobInfo.Id].Count == 0)
                jobIpLocalports.Remove(jobInfo.Id);

            //jobIpSockets.Remove(jobInfo.Id);
            log.InfoFormat("Closed all connections for jobId {0}", jobInfo.Id);
        }

        #region Protected methods

        protected int CreateSshTunnel(SubmittedJobInfo jobInfo, string localHost, int localPort, string loginHost, string nodeHost, int nodePort)
        {
            while (!IsPortFree(localHost, localPort))
            {
                ++localPort;
            }

            SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                                .CreateScheduler(jobInfo.Specification.Cluster)
                                    .CreateSshTunnel(jobInfo.Specification.Id, localHost, localPort, loginHost, nodeHost, nodePort, jobInfo.Specification.ClusterUser);
            return localPort;
        }

        protected void RemoveSshTunnel(SubmittedJobInfo jobInfo, string nodeHost)
        {
            SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster)
                                                                                        .RemoveSshTunnel(jobInfo.Specification.Id, nodeHost);
        }

        protected bool SshTunnelExist(SubmittedJobInfo jobInfo, string nodeHost)
        {
            return SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster)
                                                                                                .SshTunnelExist(jobInfo.Specification.Id, nodeHost);
        }

        protected bool IsPortFree(string ipAddress, int port)
        {
            try
            {
                IPAddress address = IPAddress.Parse(ipAddress);
                TcpListener tcpListener = new TcpListener(address, port);
                tcpListener.Start();
                tcpListener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        //protected void CreateLocalSocketConnection(long jobId, string remoteIp, int localPort)
        //{
        //    if (jobIpSockets.Keys.Contains(jobId) && jobIpSockets[jobId].Keys.Contains(remoteIp))
        //        log.Error("Job " + jobId + " with remote IP " + remoteIp + " already has socket a connection.");
        //    else
        //    {
        //        Socket socket = null;
        //        try
        //        {
        //            //create socket
        //            IPAddress address = IPAddress.Parse("127.0.0.1");
        //            IPEndPoint remoteEP = new IPEndPoint(address, localPort);
        //            socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //            //socket.NoDelay = true;

        //            //connect to remote host
        //            socket.Connect(remoteEP);

        //            //add socket to dictionary
        //            if (jobIpSockets.Keys.Contains(jobId))
        //                jobIpSockets[jobId].Add(remoteIp, socket);
        //            else jobIpSockets.Add(jobId, new Dictionary<string, Socket> { { remoteIp, socket } });

        //            log.Info("New socket connection created for job " + jobId + " with remote IP " + remoteIp + " and local port " + localPort);
        //        }
        //        catch (Exception e)
        //        {
        //            log.Error("Socket creation error: {0}", e);
        //        }
        //    }
        //}

        //protected bool SocketConnected(Socket socket)
        //{
        //    try
        //    {
        //        return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        //    }
        //    catch (SocketException) { return false; }
        //}

        //protected void CloseLocalSocketConnection(long jobId, string remoteIp)
        //{
        //    if (jobIpSockets.Keys.Contains(jobId) && jobIpSockets[jobId].Keys.Contains(remoteIp))
        //    {
        //        Socket sock = jobIpSockets[jobId][remoteIp];
        //        try
        //        {
        //            sock.Shutdown(SocketShutdown.Both);
        //            sock.Close();
        //        }
        //        catch (Exception e)
        //        {
        //            log.Error("Error in closing socket connection for submitted job info ID " + jobId + ": {0}", e);
        //        }
        //        jobIpSockets[jobId].Remove(remoteIp);
        //        log.Info("Closed socket for submitted job info Id: " + jobId + " and remote IP address: " + remoteIp);
        //    }
        //    else log.Error("Job " + jobId + " with IP " + remoteIp + " does not have an active socket.");
        //}

        #endregion
    }
}