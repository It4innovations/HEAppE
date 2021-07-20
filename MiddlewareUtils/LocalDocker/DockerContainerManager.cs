using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.MiddlewareUtils.LocalDocker.DockerDTO;

namespace HEAppE.MiddlewareUtils.LocalDocker
{
    /// <summary>
    /// (konvicka) TODO REFACTOR THIS, THIS IS ONLY PROTOTYPE
    /// </summary>
    public sealed class DockerContainerManager
    {
        private static readonly object padlock = new object();
        private static DockerContainerManager instance = null;

        private static readonly HttpClient client = new();
        private ContainerInfo DockerContainer { get; set; }
        private ImageInfo DockerImage { get; set; }

        public bool IsContainerRunning { get => (DockerContainer?.State == StateType.RUNNING); }
        public bool ImageExists { get => (DockerImage != null); }


        #region Tasks
        private Task<Stream> GetDockerContainers { get => client.GetStreamAsync($"http://{LocalDockerSettings.Ip}:{LocalDockerSettings.Port}/containers/json?all=1"); }
        private Task<Stream> ListImages { get => client.GetStreamAsync($"http://{LocalDockerSettings.Ip}:{LocalDockerSettings.Port}/images/json?all=1"); }
        private Task<HttpResponseMessage> StartContainer { get => client.PostAsync($"http://{LocalDockerSettings.Ip}:{LocalDockerSettings.Port}/containers/{DockerContainer?.Id}/start", null); }
        private Task<HttpResponseMessage> StopContainer { get => client.PostAsync($"http://{LocalDockerSettings.Ip}:{LocalDockerSettings.Port}/containers/{DockerContainer?.Id}/stop", null); }
        #endregion
        public static DockerContainerManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DockerContainerManager();
                    }
                    return instance;
                }
            }
        }
        private void ReSetHttpRequestHeaders()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
        private DockerContainerManager()
        {
            ReSetHttpRequestHeaders();
        }
        private DockerContainerManager(int port)
        {
            LocalDockerSettings.Port = port;
            ReSetHttpRequestHeaders();
        }

        private async Task<bool> LoadAllContainers()
        {
            //todo log
            var containers = await JsonSerializer.DeserializeAsync<List<ContainerInfo>>(await GetDockerContainers);
            DockerContainer = containers.FirstOrDefault(c =>
            (c.Ports.Any(x => x.PrivatePort == 22 && x.PublicPort == LocalDockerSettings.ContainerExposedPort))
            || c.Names.Any(x => x.Contains(LocalDockerSettings.ContainerName)));
            return DockerContainer != null;
        }

        private async Task<bool> StartContainerAsync()
        {
            //todo log
            var result = await StartContainer;
            return result.StatusCode == System.Net.HttpStatusCode.NoContent;
        }

        public async void StartDockerContainer(string pathToConfiguration)
        {
            //check if image exists
            bool imageExists = await CheckIfImageExists();
            if (!imageExists)
            {
                BuildImageAndStart(pathToConfiguration);
                imageExists = await CheckIfImageExists();
                if (!imageExists)
                {
                    throw new ApplicationException($"Cannot build docker image with configuration at " +
                            $"'{pathToConfiguration}'");
                }
            }

            //check if container is running
            bool containerRunning = await CheckAndStartContainerAsync(pathToConfiguration);
            if (!containerRunning)
            {
                throw new ApplicationException($"Cannot run docker container with configuration at " +
                            $"'{pathToConfiguration}'");
            }
        }


        private async Task<bool> CheckAndStartContainerAsync(string pathToConfiguration)
        {
            //todo log
            bool isStarting = false;
            bool containerExists = await LoadAllContainers();
            if (containerExists)
            {
                if (!IsContainerRunning)
                {
                    isStarting = await StartContainerAsync();
                }

            }
            else
            {
                RunDockerComposeCommand(pathToConfiguration, "up -d");
                isStarting = true;
            }

            int waitingIteration = 0;
            while (isStarting && !IsContainerRunning && waitingIteration < 5)
            {
                if (await LoadAllContainers())
                    break;
                waitingIteration++;
                //wait if Container is still starting
                Thread.Sleep(10);
            }
            return IsContainerRunning;
        }

        private async Task<bool> CheckIfImageExists()
        {
            //todo log
            var images = await JsonSerializer.DeserializeAsync<List<ImageInfo>>(await ListImages);
            DockerImage = images.FirstOrDefault(c =>
            (c.RepoTags.Any(x => x.Contains(LocalDockerSettings.ContainerName)))
            );
            return DockerImage != null;
        }

        /// <summary>
        /// Builds image and starts docker container, uses -> docker-compose 
        /// </summary>
        /// <param name="pathToConfiguration">Directory with docker-compose.yml configuration file</param>
        private void BuildImageAndStart(string pathToConfiguration)
        {
            RunDockerComposeCommand(pathToConfiguration, "up -d");
        }

        private void RunDockerComposeCommand(string workingDirectory, string parametres)
        {
            //Console.WriteLine("Docker image building has started");

            //Console.WriteLine("Docker image created");

            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker-compose";
                proc.StartInfo.WorkingDirectory = workingDirectory;
                proc.StartInfo.Arguments = $"{parametres}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.EnableRaisingEvents = true;
                proc.Start();

                var result = proc.StandardOutput.ReadToEnd();
                var error = proc.StandardError.ReadToEnd();

                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {

                }
            }
            //todo log
        }
    }
}