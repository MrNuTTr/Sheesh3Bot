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

        public static async Task<bool> TurnOnGameServer(string serverName)
        {
            await LoadResourceGroup();

            await foreach (VirtualMachineResource virtualMachine in _resourceGroup.GetVirtualMachines())
            {
                if (virtualMachine.Data.Name.ToLower() == serverName.ToLower())
                {
                    await virtualMachine.PowerOnAsync(WaitUntil.Completed);
                    
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
    }
}
