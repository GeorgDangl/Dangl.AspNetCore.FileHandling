using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Azure.IntegrationTests
{
    public class DockerTestUtilities : IAsyncLifetime
    {
        public const string DOCKER_AZURE_CONTAINER_PREFIX = "FileHandlingTests_";
        public const string AZURITE_IMAGE = "mcr.microsoft.com/azure-storage/azurite";
        private string _blobPort;
        private string _queuePort;

        public string GetBlobConnectionString()
        {
            return "DefaultEndpointsProtocol=http;" +
                "AccountName=devstoreaccount1;" +
                "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                $"BlobEndpoint=http://127.0.0.1:{_blobPort}/devstoreaccount1;" +
                $"QueueEndpoint=http://127.0.0.1:{_queuePort}/devstoreaccount1;";
        }

        public async Task DisposeAsync()
        {
            await CleanupRunningContainers();
        }

        public async Task InitializeAsync()
        {
            await CleanupRunningContainers();
            var dockerClient = GetDockerClient();
            _blobPort = GetFreePort();
            _queuePort = GetFreePort();

            var containerName = (DOCKER_AZURE_CONTAINER_PREFIX + Guid.NewGuid()).Replace("-", string.Empty);
            var azuriteContainerStartParameters = new CreateContainerParameters
            {
                Name = containerName,
                Image = AZURITE_IMAGE,
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "10000/tcp",
                                new PortBinding[]
                                {
                                    new PortBinding
                                    {
                                        HostPort = _blobPort
                                    }
                                }
                            },
                            {
                                "10001/tcp",
                                new PortBinding[]
                                {
                                    new PortBinding
                                    {
                                        HostPort = _queuePort
                                    }
                                }
                            }
                        }
                }
            };

            var azuriteContainer = await dockerClient
                .Containers
                .CreateContainerAsync(azuriteContainerStartParameters);

            await dockerClient
                .Containers
                .StartContainerAsync(azuriteContainer.ID, new ContainerStartParameters());

            await WaitUntilAzuriteBlobAvailableAsync(_blobPort);
        }

        private async Task WaitUntilAzuriteBlobAvailableAsync(string port)
        {
            using var httpClient = new HttpClient();

            var start = DateTime.UtcNow;
            var isAvailable = false;
            while (!isAvailable && (DateTime.UtcNow - start).TotalSeconds < 60)
            {
                try
                {
                    var httpResponse = await httpClient.GetAsync($"http://127.0.0.1:{_blobPort}/devstoreaccount1");
                    // Bad request indicates that the container is up and ready to serve requests😀
                    isAvailable = httpResponse.IsSuccessStatusCode || httpResponse.StatusCode == HttpStatusCode.BadRequest;
                }
                catch
                {
                    await Task.Delay(500);
                }
            }

            if (!isAvailable)
            {
                throw new Exception("Failed to start the Azurite container.");
            }
        }

        private static DockerClient GetDockerClient()
        {
            var dockerUri = IsRunningOnWindows()
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock";
            return new DockerClientConfiguration(new Uri(dockerUri))
                .CreateClient();
        }

        private static async Task CleanupRunningContainers()
        {
            var dockerClient = GetDockerClient();

            var runningContainers = await dockerClient.Containers
                .ListContainersAsync(new ContainersListParameters
                {
                    All = true
                });

            foreach (var runningContainer in runningContainers.Where(cont => cont.Names.Any(n => n.Contains(DOCKER_AZURE_CONTAINER_PREFIX))))
            {
                await EnsureDockerStoppedAndRemovedAsync(runningContainer.ID);
            }
        }

        public static async Task EnsureDockerStoppedAndRemovedAsync(string dockerContainerId)
        {
            var dockerClient = GetDockerClient();
            await dockerClient.Containers
                .StopContainerAsync(dockerContainerId, new ContainerStopParameters());
            await dockerClient.Containers
                .RemoveContainerAsync(dockerContainerId, new ContainerRemoveParameters());
        }

        private static string GetFreePort()
        {
            // Taken from https://stackoverflow.com/a/150974/4190785
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port.ToString();
        }

        private static bool IsRunningOnWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
    }
}
