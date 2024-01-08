using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Monitor;
using Azure.ResourceManager.Monitor.Models;
using Microsoft.Identity.Client;
using Sheesh3Bot.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sheesh3Bot.Services
{
    class AzureService
    {
        private static string _gameServerResourceGroupName = "GameServers";

        private static ArmClient _client = new ArmClient(new DefaultAzureCredential());

        private static ResourceGroupResource _resourceGroup { get; set; }

        private static async Task LoadResourceGroup()
        {
            if (_resourceGroup == null) 
            {
                SubscriptionResource subscription = await _client.GetDefaultSubscriptionAsync();
                ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
                _resourceGroup = await resourceGroups.GetAsync(_gameServerResourceGroupName);
            }
        }

        /// <summary>
        ///     Attempts to turn on a game server in Azure.
        /// </summary>
        /// <param name="serverName">Name of the server in Azure to turn on</param>
        /// <returns>
        ///     <para>
        ///     <list type="bullet">
        ///     <item>
        ///         0 - The provided serverName could not be found, or some other error.
        ///     </item>
        ///     <item>
        ///         1 - The command was successful, the server is now online.
        ///     </item>
        ///     <item>
        ///         2 - The server was already online.    
        ///     </item>
        ///     </list>
        ///     </para>
        /// </returns>
        public static async Task<int> TurnOnGameServer(string resourceId)
        {
            await LoadResourceGroup();

            var vm = _client.GetVirtualMachineResource(ResourceIdentifier.Parse(resourceId));

            var instanceView = await vm.InstanceViewAsync();
            var statuses = instanceView.Value.Statuses;
            foreach (var status in statuses)
            {
                if (status.Code == "PowerState/running")
                {
                    return 2;
                }
            }

            vm.PowerOn(WaitUntil.Completed);

            return 1;
        }

        public static async Task<bool> TurnOffGameServer(string resourceId)
        {
            await LoadResourceGroup();

            var vm = _client.GetVirtualMachineResource(ResourceIdentifier.Parse(resourceId));

            await vm.DeallocateAsync(WaitUntil.Completed);

            return true;
        }

        public static void SendServerShutdownRequest(TableClient tableClient, string serverName, DateTime shutdownTime)
        {
            ShutdownRequest request = new ShutdownRequest()
            {
                ServerName = serverName,
                ScheduledShutdownTime = shutdownTime,
                PartitionKey = serverName,
                RowKey = serverName
            };

            tableClient.UpsertEntity(request);
        }

        private static async Task<string> GetServerIP(string resourceId)
        {
            await LoadResourceGroup();

            var vm = _client.GetVirtualMachineResource(ResourceIdentifier.Parse(resourceId));
            vm = vm.Get();
            var networkProfile = vm.Data.NetworkProfile.NetworkInterfaces.FirstOrDefault();
            var networkInterface = _client.GetNetworkInterfaceResource(new ResourceIdentifier(networkProfile.Id));

            var ipAddressRes = networkInterface.GetNetworkInterfaceIPConfigurations().FirstOrDefault();
            if (ipAddressRes == null) { return ""; }

            if (!ipAddressRes.HasData)
            {
                ipAddressRes = ipAddressRes.Get();
            }

            if (ipAddressRes.Data.PublicIPAddress == null)
            {
                return "";
            }

            var publicIpAddressRes = _client.GetPublicIPAddressResource(new ResourceIdentifier(ipAddressRes.Data.PublicIPAddress.Id));
            if (publicIpAddressRes == null) { return ""; }

            if (!publicIpAddressRes.HasData)
            {
                publicIpAddressRes = publicIpAddressRes.Get();
            }

            return publicIpAddressRes.Data.IPAddress.ToString();
        }

        public static async Task<string> GetServerPublicIP(string serverName, string resourceId)
        {
            try
            {
                return await CreateServerPublicIP(serverName, resourceId);
            }
            catch
            {
                return await GetServerIP(resourceId);
            }
        }

        /// <summary>
        /// Attaches a new public IP address to the provided serverName. 
        /// Should only call this if you've verified there is no public IP, otherwise it will try to assign another.
        /// <br></br>
        /// <br></br>
        /// Documentation: <br></br>
        /// https://github.com/Azure-Samples/azure-samples-net-management/blob/master/samples/network/manage-ip-address/Program.cs
        /// </summary>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public static async Task<string> CreateServerPublicIP(string serverName, string resourceId)
        {
            await LoadResourceGroup();
            // TODO: Dynamically set location based on selected server. Fine for now.
            string location = "eastus2";

            var publicIPAddressContainer = _resourceGroup.GetPublicIPAddresses();
            var networkInterfaceContainer = _resourceGroup.GetNetworkInterfaces();

            // Create Public IP Address and attach it to a new Network Interface
            var publicIPAddressData = new PublicIPAddressData
            {
                PublicIPAddressVersion = Azure.ResourceManager.Network.Models.NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = Azure.ResourceManager.Network.Models.NetworkIPAllocationMethod.Dynamic,
                Location = location
            };

            var publicIpAddress = publicIPAddressContainer.CreateOrUpdate(WaitUntil.Completed, $"{serverName}-ip", publicIPAddressData).Value;
            publicIpAddress = publicIpAddress.Get();
            var vnet = _resourceGroup.GetVirtualNetworks().FirstOrDefault();
            vnet = vnet.Get();

            var networkInterfaceData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "ipconfig1",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = Azure.ResourceManager.Network.Models.NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = publicIpAddress.Id }
                    }
                }
            };

            // Get the server's current Network Interface
            var vm = _client.GetVirtualMachineResource(ResourceIdentifier.Parse(resourceId));
            vm = vm.Get();
            var networkProfile = vm.Data.NetworkProfile.NetworkInterfaces.FirstOrDefault();
            var networkInterface = _client.GetNetworkInterfaceResource(new ResourceIdentifier(networkProfile.Id));

            if (networkInterface == null)
            {
                throw new Exception($"No server named {serverName}, could not update IP");
            }

            // Update the interface to the new data
            await networkInterfaceContainer.CreateOrUpdateAsync(WaitUntil.Completed, networkInterface.Id.Name, networkInterfaceData);

            // For some reason the above function returns before the IP address is fully attached. Giving me a bunch of nulls.
            System.Threading.Thread.Sleep(5000);

            var publicIpAddressResource = _client.GetPublicIPAddressResource(publicIpAddress.Id);
            publicIpAddressResource = publicIpAddressResource.Get();

            return publicIpAddressResource.Data.IPAddress.ToString();
        }

        public static async Task DeleteServerPublicIP(string serverName, string resourceId)
        {
            await LoadResourceGroup();
            // TODO: Dynamically set location based on selected server. Fine for now.
            string location = "eastus2";

            var networkInterfaceContainer = _resourceGroup.GetNetworkInterfaces();
            var vnet = _resourceGroup.GetVirtualNetworks().FirstOrDefault();

            // Create network interface without public IP to replace the old one
            var networkInterfaceData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "ipconfig1",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = Azure.ResourceManager.Network.Models.NetworkIPAllocationMethod.Dynamic,
                    }
                }
            };

            // Get the server's current Network Interface
            var vm = _client.GetVirtualMachineResource(ResourceIdentifier.Parse(resourceId));
            vm = vm.Get();
            var networkProfile = vm.Data.NetworkProfile.NetworkInterfaces.FirstOrDefault();
            var networkInterface = _client.GetNetworkInterfaceResource(new ResourceIdentifier(networkProfile.Id));

            if (networkInterface == null)
            {
                throw new Exception($"No server named {serverName}, could not update IP");
            }

            var ipAddressRes = networkInterface.GetNetworkInterfaceIPConfigurations().FirstOrDefault();
            ipAddressRes = ipAddressRes.Get();

            if (ipAddressRes.Data.PublicIPAddress == null)
            {
                return;
            }

            // Update the interface to the new data and delete IP
            networkInterfaceContainer.CreateOrUpdate(WaitUntil.Completed, networkInterface.Id.Name, networkInterfaceData);

            var publicIP = _client.GetPublicIPAddressResource(ipAddressRes.Data.PublicIPAddress.Id);
            publicIP.Delete(WaitUntil.Completed);
        }

        public static async Task RemovePublicIPFromAllShutdownServers()
        {
            await LoadResourceGroup();

            await foreach (VirtualMachineResource virtualMachine in _resourceGroup.GetVirtualMachines())
            {
                // https://stackoverflow.com/questions/75116030/get-azure-vm-powerstate-in-c-dotnet-using-azure-resourcemanager
                var statuses = virtualMachine.InstanceView().Value.Statuses;
                // statuses[0] refers to the provisioning status
                var displayStatus = statuses[1].DisplayStatus.ToLower();

                if (displayStatus.Contains("deallocated") || displayStatus.Contains("stopped"))
                {
                    await DeleteServerPublicIP(virtualMachine.Data.Name, virtualMachine.Id.ToString());
                }
            }
        }

        public static double GetAverageNetworkUsageBytesPast15Minutes(string resourceId)
        {
            var options = new ArmResourceGetMonitorMetricsOptions()
            {
                //Metricnamespace = "Virtual Machine Host",
                Metricnames = "Network In Total",
                //Interval = TimeSpan.FromMinutes(1),
                Timespan = $"{DateTime.UtcNow.AddMinutes(-15).ToString("o")}/{DateTime.UtcNow.ToString("o")}",
            };
            

            var metrics = _client.GetMonitorMetrics(ResourceIdentifier.Parse(resourceId), options);


            var sum = 0.0;
            var count = 0;
            foreach (MonitorMetric metric in metrics)
            {
                foreach (var timeSeries in metric.Timeseries)
                {
                    foreach (var data in timeSeries.Data)
                    {
                        if (data.Total.HasValue && data.Total.Value > 0.0)
                        {
                            sum += data.Total.Value;
                            count++;
                        }
                    }
                }
            }

            double average = sum / count;

            return average;
        }
    }
}
