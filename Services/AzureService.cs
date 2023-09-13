using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using System.Linq;
using System.Threading.Tasks;
using System;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Sheesh3Bot.Models;

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
        public static async Task<int> TurnOnGameServer(string serverName)
        {
            await LoadResourceGroup();

            await foreach (VirtualMachineResource virtualMachine in _resourceGroup.GetVirtualMachines())
            {
                if (virtualMachine.Data.Name.ToLower() == serverName.ToLower())
                {
                    // Check power state before attempting to turn on
                    var instanceView = await virtualMachine.InstanceViewAsync();
                    var statuses = instanceView.Value.Statuses;
                    foreach (var status in statuses)
                    {
                        if (status.Code == "PowerState/running")
                        {
                            return 2;
                        }
                    }

                    await virtualMachine.PowerOnAsync(WaitUntil.Completed);
                    
                    return 1;
                }
            }
            
            return 0;
        }

        public static async Task<bool> TurnOffGameServer(string serverName)
        {
            await LoadResourceGroup();

            await foreach (VirtualMachineResource virtualMachine in _resourceGroup.GetVirtualMachines())
            {
                if (virtualMachine.Data.Name.ToLower() == serverName.ToLower())
                {
                    await virtualMachine.PowerOffAsync(WaitUntil.Completed);

                    return true;
                }
            }

            return false;
        }

        public static async Task<string> GetServerPublicIP(string serverName)
        {
            await LoadResourceGroup();

            await foreach (VirtualMachineResource virtualMachine in _resourceGroup.GetVirtualMachines())
            {
                if (virtualMachine.Data.Name.ToLower() == serverName.ToLower())
                {
                    var networkProfile = virtualMachine.Data.NetworkProfile.NetworkInterfaces.FirstOrDefault();
                    var networkInterface = _client.GetNetworkInterfaceResource(new ResourceIdentifier(networkProfile.Id));

                    var ipAddressRes = networkInterface.GetNetworkInterfaceIPConfigurations().FirstOrDefault();
                    if (ipAddressRes == null) { continue; }

                    if (!ipAddressRes.HasData)
                    {
                        ipAddressRes = ipAddressRes.Get().Value;
                    }

                    var publicIpAddressRes = _client.GetPublicIPAddressResource(new ResourceIdentifier(ipAddressRes.Data.PublicIPAddress.Id));
                    if (publicIpAddressRes == null) { continue; }

                    if (!publicIpAddressRes.HasData)
                    {
                        publicIpAddressRes = publicIpAddressRes.Get().Value;
                    }

                    return publicIpAddressRes.Data.IPAddress.ToString();
                }
            }

            return "";
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
    }
}
