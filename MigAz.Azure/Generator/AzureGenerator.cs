﻿using MigAz.Azure.Interface;
using MigAz.Azure.Models;
using MigAz.Core.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MigAz.Core.Generator;
using System.IO;
using MigAz.Core.ArmTemplate;
using System.Text;
using Newtonsoft.Json;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace MigAz.Azure.Generator.AsmToArm
{
    public class AzureGenerator : TemplateGenerator
    {
        private Azure.MigrationTarget.ResourceGroup _TargetResourceGroup;
        private ITelemetryProvider _telemetryProvider;
        private ISettingsProvider _settingsProvider;
        private ExportArtifacts _ExportArtifacts;
        private List<CopyBlobDetail> _CopyBlobDetails = new List<CopyBlobDetail>();

        private AzureGenerator() : base(null, null, null, null) { } 

        public AzureGenerator(
            ISubscription sourceSubscription, 
            ISubscription targetSubscription,
            Azure.MigrationTarget.ResourceGroup targetResourceGroup,
            ILogProvider logProvider, 
            IStatusProvider statusProvider, 
            ITelemetryProvider telemetryProvider, 
            ISettingsProvider settingsProvider) : base(logProvider, statusProvider, sourceSubscription, targetSubscription)
        {
            _TargetResourceGroup = targetResourceGroup;
            _telemetryProvider = telemetryProvider;
            _settingsProvider = settingsProvider;
        }

        public Azure.MigrationTarget.ResourceGroup TargetResourceGroup { get { return _TargetResourceGroup; } }


        // Use of Treeview has been added here with aspect of transitioning full output towards this as authoritative source
        // Thought is that ExportArtifacts phases out, as it is providing limited context availability.
        public override async Task UpdateArtifacts(IExportArtifacts artifacts)
        {
            LogProvider.WriteLog("UpdateArtifacts", "Start - Execution " + this.ExecutionGuid.ToString());

            Alerts.Clear();
            TemplateStreams.Clear();
            Resources.Clear();
            _CopyBlobDetails.Clear();

            _ExportArtifacts = (ExportArtifacts)artifacts;

            if (_TargetResourceGroup == null)
            {
                this.AddAlert(AlertType.Error, "Target Resource Group must be provided for template generation.", _TargetResourceGroup);
            }

            if (_TargetResourceGroup.TargetLocation == null)
            {
                this.AddAlert(AlertType.Error, "Target Resource Group Location must be provided for template generation.", _TargetResourceGroup);
            }

            foreach (MigrationTarget.NetworkSecurityGroup targetNetworkSecurityGroup in _ExportArtifacts.NetworkSecurityGroups)
            {
                if (targetNetworkSecurityGroup.TargetName == string.Empty)
                    this.AddAlert(AlertType.Error, "Target Name for Network Security Group must be specified.", targetNetworkSecurityGroup);
            }

            foreach (Azure.MigrationTarget.VirtualMachine virtualMachine in _ExportArtifacts.VirtualMachines)
            {
                if (virtualMachine.Source.GetType() == typeof(Azure.Asm.VirtualMachine))
                {
                    Azure.Asm.VirtualMachine asmVirtualMachine = (Azure.Asm.VirtualMachine)virtualMachine.Source;

                    if (virtualMachine.TargetName == string.Empty)
                        this.AddAlert(AlertType.Error, "Target Name for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' must be specified.", asmVirtualMachine);

                    if (virtualMachine.TargetAvailabilitySet == null)
                        this.AddAlert(AlertType.Error, "Target Availability Set for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' must be specified.", asmVirtualMachine);

                    foreach (Azure.MigrationTarget.NetworkInterface networkInterface in virtualMachine.NetworkInterfaces)
                    {
                        if (networkInterface.TargetVirtualNetwork == null)
                            this.AddAlert(AlertType.Error, "Target Virtual Network for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' must be specified.", asmVirtualMachine);
                        else
                        {
                            if (networkInterface.TargetVirtualNetwork.GetType() == typeof(Asm.VirtualNetwork))
                            {
                                Asm.VirtualNetwork targetAsmVirtualNetwork = (Asm.VirtualNetwork)networkInterface.TargetVirtualNetwork;
                                bool targetVNetExists = false;

                                foreach (IVirtualNetwork iVirtualNetwork in _ExportArtifacts.VirtualNetworks)
                                {
                                    if (iVirtualNetwork.GetType() == typeof(Asm.VirtualNetwork) && ((Azure.Asm.VirtualNetwork)iVirtualNetwork).Name == targetAsmVirtualNetwork.Name)
                                    {
                                        targetVNetExists = true;
                                        break;
                                    }
                                }

                                if (!targetVNetExists)
                                    this.AddAlert(AlertType.Error, "Target ASM Virtual Network '" + targetAsmVirtualNetwork.Name + "' for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' is invalid, as it is not included in the migration / template.", asmVirtualMachine);
                            }
                        }

                        if (networkInterface.TargetSubnet == null)
                            this.AddAlert(AlertType.Error, "Target Subnet for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' must be specified.", asmVirtualMachine);
                    }
                    if (asmVirtualMachine.OSVirtualHardDisk.TargetStorageAccount == null)
                        this.AddAlert(AlertType.Error, "Target Storage Account for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' OS Disk must be specified.", asmVirtualMachine);
                    else
                    {
                        if (asmVirtualMachine.OSVirtualHardDisk.TargetStorageAccount.GetType() == typeof(Azure.MigrationTarget.StorageAccount))
                        {
                            Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)asmVirtualMachine.OSVirtualHardDisk.TargetStorageAccount;
                            bool targetAsmStorageExists = false;

                            foreach (Azure.MigrationTarget.StorageAccount asmStorageAccount in _ExportArtifacts.StorageAccounts)
                            {
                                if (asmStorageAccount.SourceAccount.ToString() == targetStorageAccount.ToString())
                                {
                                    targetAsmStorageExists = true;
                                    break;
                                }
                            }

                            if (!targetAsmStorageExists)
                                this.AddAlert(AlertType.Error, "Target ASM Storage Account '" + targetStorageAccount.ToString() + "' for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' OS Disk is invalid, as it is not included in the migration / template.", asmVirtualMachine);
                        }
                    }

                    foreach (Asm.Disk dataDisk in asmVirtualMachine.DataDisks)
                    {
                        if (dataDisk.TargetStorageAccount == null)
                        {
                            this.AddAlert(AlertType.Error, "Target Storage Account for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' Data Disk '" + dataDisk.DiskName + "' must be specified.", dataDisk);
                        }
                        else
                        {
                            if (dataDisk.TargetStorageAccount.GetType() == typeof(Azure.MigrationTarget.StorageAccount))
                            {
                                Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)dataDisk.TargetStorageAccount;
                                bool targetStorageExists = false;

                                foreach (Azure.MigrationTarget.StorageAccount storageAccount in _ExportArtifacts.StorageAccounts)
                                {
                                    if (storageAccount.ToString() == targetStorageAccount.ToString())
                                    {
                                        targetStorageExists = true;
                                        break;
                                    }
                                }

                                if (!targetStorageExists)
                                    this.AddAlert(AlertType.Error, "Target ASM Storage Account '" + targetStorageAccount.ToString() + "' for ASM Virtual Machine '" + asmVirtualMachine.RoleName + "' Data Disk '" + dataDisk.DiskName + "' is invalid, as it is not included in the migration / template.", dataDisk);
                            }
                        }
                    }
                }
                else if (virtualMachine.GetType() == typeof(Azure.Arm.VirtualMachine))
                {
                    Azure.Arm.VirtualMachine armVirtualMachine = (Azure.Arm.VirtualMachine)virtualMachine.Source;

                    if (armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount == null)
                        this.AddAlert(AlertType.Error, "Target Storage Account for ARM Virtual Machine '" + armVirtualMachine.Name + "' OS Disk must be specified.", armVirtualMachine);
                    else
                    {
                        if (armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount.GetType() == typeof(Azure.MigrationTarget.StorageAccount))
                        {
                            Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount;
                            bool targetArmStorageExists = false;

                            foreach (Azure.MigrationTarget.StorageAccount storageAccount in _ExportArtifacts.StorageAccounts)
                            {
                                if (storageAccount.ToString() == targetStorageAccount.ToString())
                                {
                                    targetArmStorageExists = true;
                                    break;
                                }
                            }

                            if (!targetArmStorageExists)
                                this.AddAlert(AlertType.Error, "Target ARM Storage Account '" + targetStorageAccount.ToString() + "' for ASM Virtual Machine '" + armVirtualMachine.Name + "' OS Disk is invalid, as it is not included in the migration / template.", armVirtualMachine);
                        }
                    }

                    foreach (Arm.Disk dataDisk in armVirtualMachine.DataDisks)
                    {
                        if (dataDisk.TargetStorageAccount == null)
                        {
                            this.AddAlert(AlertType.Error, "Target Storage Account for ARM Virtual Machine '" + armVirtualMachine.name + "' Data Disk '" + dataDisk.Name + "' must be specified.", dataDisk);
                        }
                        else
                        {
                            if (dataDisk.TargetStorageAccount.GetType() == typeof(Arm.StorageAccount))
                            {
                                Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)dataDisk.TargetStorageAccount;
                                bool targetArmStorageExists = false;

                                foreach (IStorageAccount storageAccount in _ExportArtifacts.StorageAccounts)
                                {
                                    if (storageAccount.ToString() == targetStorageAccount.ToString())
                                    {
                                        targetArmStorageExists = true;
                                        break;
                                    }
                                }

                                if (!targetArmStorageExists)
                                    this.AddAlert(AlertType.Error, "Target ASM Storage Account '" + targetStorageAccount.ToString() + "' for ARM Virtual Machine '" + armVirtualMachine.Name + "' Data Disk '" + dataDisk.Name + "' is invalid, as it is not included in the migration / template.", dataDisk);
                            }
                        }
                    }
                }
            }

            LogProvider.WriteLog("UpdateArtifacts", "Start processing selected Network Security Groups");
            foreach (MigrationTarget.NetworkSecurityGroup targetNetworkSecurityGroup in _ExportArtifacts.NetworkSecurityGroups)
            {
                StatusProvider.UpdateStatus("BUSY: Exporting Network Security Group : " + targetNetworkSecurityGroup.ToString());
                await BuildNetworkSecurityGroup(targetNetworkSecurityGroup);
            }
            LogProvider.WriteLog("UpdateArtifacts", "End processing selected Network Security Groups");

            LogProvider.WriteLog("UpdateArtifacts", "Start processing selected Virtual Networks");
            foreach (Azure.MigrationTarget.VirtualNetwork virtualNetwork in _ExportArtifacts.VirtualNetworks)
            {
                StatusProvider.UpdateStatus("BUSY: Exporting Virtual Network : " + virtualNetwork.ToString());
                await BuildVirtualNetworkObject(virtualNetwork);
            }
            LogProvider.WriteLog("UpdateArtifacts", "End processing selected Virtual Networks");

            LogProvider.WriteLog("UpdateArtifacts", "Start processing selected Storage Accounts");
            foreach (MigrationTarget.StorageAccount storageAccount in _ExportArtifacts.StorageAccounts)
            {
                StatusProvider.UpdateStatus("BUSY: Exporting Storage Account : " + storageAccount.ToString());
                BuildStorageAccountObject(storageAccount);
            }
            LogProvider.WriteLog("UpdateArtifacts", "End processing selected Storage Accounts");

            LogProvider.WriteLog("UpdateArtifacts", "Start processing selected Cloud Services / Virtual Machines");
            foreach (Azure.MigrationTarget.VirtualMachine virtualMachine in _ExportArtifacts.VirtualMachines)
            {
                StatusProvider.UpdateStatus("BUSY: Exporting Virtual Machine : " + virtualMachine.ToString());

                // process virtual machine
                await BuildVirtualMachineObject(virtualMachine);
            }
            LogProvider.WriteLog("UpdateArtifacts", "End processing selected Cloud Services / Virtual Machines");

            LogProvider.WriteLog("UpdateArtifacts", "Start OnTemplateChanged Event");
            OnTemplateChanged();
            LogProvider.WriteLog("UpdateArtifacts", "End OnTemplateChanged Event");

            StatusProvider.UpdateStatus("Ready");

            LogProvider.WriteLog("UpdateArtifacts", "End - Execution " + this.ExecutionGuid.ToString());
        }

        public override async Task SerializeStreams()
        {
            LogProvider.WriteLog("SerializeStreams", "Start Template Stream Update");

            TemplateStreams.Clear();

            await UpdateExportJsonStream();

            StatusProvider.UpdateStatus("BUSY:  Generating copyblobdetails.json");
            LogProvider.WriteLog("SerializeStreams", "Start copyblobdetails.json stream");
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();

            string jsontext = JsonConvert.SerializeObject(this._CopyBlobDetails, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            byte[] b = asciiEncoding.GetBytes(jsontext);
            MemoryStream copyBlobDetailStream = new MemoryStream();
            copyBlobDetailStream.Write(b, 0, b.Length);
            TemplateStreams.Add("copyblobdetails.json", copyBlobDetailStream);

            LogProvider.WriteLog("SerializeStreams", "End copyblobdetails.json stream");


            StatusProvider.UpdateStatus("BUSY:  Generating DeployInstructions.html");
            LogProvider.WriteLog("SerializeStreams", "Start DeployInstructions.html stream");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MigAz.Azure.Generator.DeployDocTemplate.html";
            string instructionContent;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                instructionContent = reader.ReadToEnd();
            }

            string targetResourceGroupName = String.Empty;
            string resourceGroupLocation = String.Empty;
            string azureEnvironmentSwitch = String.Empty;
            string tenantSwitch = String.Empty;
            string subscriptionSwitch = String.Empty;

            if (_TargetResourceGroup != null)
            {
                targetResourceGroupName = _TargetResourceGroup.ToString();

                if (_TargetResourceGroup.TargetLocation != null)
                    resourceGroupLocation = _TargetResourceGroup.TargetLocation.Name;
            }

            if (this.TargetSubscription != null)
            {

                subscriptionSwitch = " -SubscriptionId '" + this.TargetSubscription.SubscriptionId + "'";


                if (this.TargetSubscription.AzureEnvironment != AzureEnvironment.AzureCloud)
                    azureEnvironmentSwitch = " -EnvironmentName " + this.TargetSubscription.AzureEnvironment.ToString();
                if (this.TargetSubscription.AzureEnvironment != AzureEnvironment.AzureCloud)
                    azureEnvironmentSwitch = " -EnvironmentName " + this.TargetSubscription.AzureEnvironment.ToString();

                if (this.TargetSubscription.AzureAdTenantId != Guid.Empty)
                    tenantSwitch = " -TenantId '" + this.TargetSubscription.AzureAdTenantId.ToString() + "'";
            }


            instructionContent = instructionContent.Replace("{migAzAzureEnvironmentSwitch}", azureEnvironmentSwitch);
            instructionContent = instructionContent.Replace("{tenantSwitch}", tenantSwitch);
            instructionContent = instructionContent.Replace("{subscriptionSwitch}", subscriptionSwitch);
            instructionContent = instructionContent.Replace("{templatePath}", GetTemplatePath());
            instructionContent = instructionContent.Replace("{blobDetailsPath}", GetCopyBlobDetailPath());
            instructionContent = instructionContent.Replace("{resourceGroupName}", targetResourceGroupName);
            instructionContent = instructionContent.Replace("{location}", resourceGroupLocation);
            instructionContent = instructionContent.Replace("{migAzPath}", AppDomain.CurrentDomain.BaseDirectory);
            instructionContent = instructionContent.Replace("{migAzMessages}", BuildMigAzMessages());

            byte[] c = asciiEncoding.GetBytes(instructionContent);
            MemoryStream instructionStream = new MemoryStream();
            instructionStream.Write(c, 0, c.Length);
            TemplateStreams.Add("DeployInstructions.html", instructionStream);

            LogProvider.WriteLog("SerializeStreams", "End DeployInstructions.html stream");

            LogProvider.WriteLog("SerializeStreams", "End Template Stream Update");
            StatusProvider.UpdateStatus("Ready");
        }

        private async Task UpdateExportJsonStream()
        {
            StatusProvider.UpdateStatus("BUSY:  Generating export.json");
            LogProvider.WriteLog("UpdateArtifacts", "Start export.json stream");

            String templateString = await GetTemplateString();
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            byte[] a = asciiEncoding.GetBytes(templateString);
            MemoryStream templateStream = new MemoryStream();
            templateStream.Write(a, 0, a.Length);
            TemplateStreams.Add("export.json", templateStream);

            LogProvider.WriteLog("UpdateArtifacts", "End export.json stream");
        }

        protected override void OnTemplateChanged()
        {
            // Call the base class event invocation method.
            base.OnTemplateChanged();
        }

        private void BuildPublicIPAddressObject(ref Core.ArmTemplate.NetworkInterface networkinterface)
        {
            LogProvider.WriteLog("BuildPublicIPAddressObject", "Start");

            PublicIPAddress publicipaddress = new PublicIPAddress(this.ExecutionGuid);
            publicipaddress.name = networkinterface.name;
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                publicipaddress.location = this.TargetResourceGroup.TargetLocation.Name;
            publicipaddress.properties = new PublicIPAddress_Properties();

            this.AddResource(publicipaddress);

            NetworkInterface_Properties networkinterface_properties = (NetworkInterface_Properties)networkinterface.properties;
            networkinterface_properties.ipConfigurations[0].properties.publicIPAddress = new Core.ArmTemplate.Reference();
            networkinterface_properties.ipConfigurations[0].properties.publicIPAddress.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderPublicIpAddress + publicipaddress.name + "')]";
            networkinterface.properties = networkinterface_properties;

            networkinterface.dependsOn.Add(networkinterface_properties.ipConfigurations[0].properties.publicIPAddress.id);
            LogProvider.WriteLog("BuildPublicIPAddressObject", "End");
        }

        private void BuildPublicIPAddressObject(Asm.VirtualMachine asmVirtualMachine)
        {
            LogProvider.WriteLog("BuildPublicIPAddressObject", "Start");
            
            string publicipaddress_name = asmVirtualMachine.LoadBalancerName;

            string publicipallocationmethod = "Dynamic";
            if (asmVirtualMachine.Parent.AsmReservedIP != null)
                publicipallocationmethod = "Static";

            Hashtable dnssettings = new Hashtable();
            dnssettings.Add("domainNameLabel", (publicipaddress_name + _settingsProvider.StorageAccountSuffix).ToLower());

            PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
            publicipaddress_properties.dnsSettings = dnssettings;
            publicipaddress_properties.publicIPAllocationMethod = publicipallocationmethod;

            PublicIPAddress publicipaddress = new PublicIPAddress(this.ExecutionGuid);
            publicipaddress.name = publicipaddress_name + _settingsProvider.PublicIPSuffix;
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                publicipaddress.location = this.TargetResourceGroup.TargetLocation.Name;
            publicipaddress.properties = publicipaddress_properties;

            this.AddResource(publicipaddress);

            LogProvider.WriteLog("BuildPublicIPAddressObject", "End");
        }

        private void BuildAvailabilitySetObject(Azure.MigrationTarget.AvailabilitySet availabilitySet)
        {
            LogProvider.WriteLog("BuildAvailabilitySetObject", "Start");

            AvailabilitySet availabilityset = new AvailabilitySet(this.ExecutionGuid);

            availabilityset.name = availabilitySet.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                availabilityset.location = this.TargetResourceGroup.TargetLocation.Name;

            this.AddResource(availabilityset);

            LogProvider.WriteLog("BuildAvailabilitySetObject", "End");
        }

        private void BuildLoadBalancerObject(Asm.CloudService asmCloudService, Asm.VirtualMachine asmVirtualMachine, ExportArtifacts artifacts)
        {
            LogProvider.WriteLog("BuildLoadBalancerObject", "Start");

            LoadBalancer loadbalancer = (LoadBalancer) this.GetResource(typeof(LoadBalancer), asmVirtualMachine.LoadBalancerName);

            if (loadbalancer == null)
            {
                loadbalancer = new LoadBalancer(this.ExecutionGuid);
                loadbalancer.name = asmVirtualMachine.LoadBalancerName;
                if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                    loadbalancer.location = this.TargetResourceGroup.TargetLocation.Name;

                FrontendIPConfiguration_Properties frontendipconfiguration_properties = new FrontendIPConfiguration_Properties();

                // if internal load balancer
                // shouldn't this change to a foreach loop?
                if (asmCloudService.LoadBalancers.Count > 0)
                {
                    string virtualnetworkname = String.Empty; // todo now russell asmCloudService.VirtualNetwork.ToString();
                    string subnetname = String.Empty; // todo now russell asmCloudService.LoadBalancers[0].Subnet.TargetName;

                    frontendipconfiguration_properties.privateIPAllocationMethod = "Dynamic";
                    if (asmCloudService.StaticVirtualNetworkIPAddress != String.Empty)
                    {
                        frontendipconfiguration_properties.privateIPAllocationMethod = "Static";
                        frontendipconfiguration_properties.privateIPAddress = asmCloudService.StaticVirtualNetworkIPAddress;
                    }

                    List<string> dependson = new List<string>();
                    dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderVirtualNetwork + virtualnetworkname + "')]");
                    loadbalancer.dependsOn = dependson;

                    Reference subnet_ref = new Reference();
                    subnet_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderVirtualNetwork + virtualnetworkname + "/subnets/" + subnetname + "')]";
                    frontendipconfiguration_properties.subnet = subnet_ref;
                }
                // if external load balancer
                else
                {
                    List<string> dependson = new List<string>();
                    dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderPublicIpAddress + loadbalancer.name + _settingsProvider.PublicIPSuffix + "')]");
                    loadbalancer.dependsOn = dependson;

                    Reference publicipaddress_ref = new Reference();
                    publicipaddress_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderPublicIpAddress + loadbalancer.name + _settingsProvider.PublicIPSuffix + "')]";
                    frontendipconfiguration_properties.publicIPAddress = publicipaddress_ref;
                }

                LoadBalancer_Properties loadbalancer_properties = new LoadBalancer_Properties();

                FrontendIPConfiguration frontendipconfiguration = new FrontendIPConfiguration();
                frontendipconfiguration.properties = frontendipconfiguration_properties;

                List<FrontendIPConfiguration> frontendipconfigurations = new List<FrontendIPConfiguration>();
                frontendipconfigurations.Add(frontendipconfiguration);
                loadbalancer_properties.frontendIPConfigurations = frontendipconfigurations;

                Hashtable backendaddresspool = new Hashtable();
                backendaddresspool.Add("name", "default");
                List<Hashtable> backendaddresspools = new List<Hashtable>();
                backendaddresspools.Add(backendaddresspool);
                loadbalancer_properties.backendAddressPools = backendaddresspools;

                List<InboundNatRule> inboundnatrules = new List<InboundNatRule>();
                List<LoadBalancingRule> loadbalancingrules = new List<LoadBalancingRule>();
                List<Probe> probes = new List<Probe>();

                loadbalancer_properties.inboundNatRules = inboundnatrules;
                loadbalancer_properties.loadBalancingRules = loadbalancingrules;
                loadbalancer_properties.probes = probes;
                loadbalancer.properties = loadbalancer_properties;
            }

            LoadBalancer_Properties properties = (LoadBalancer_Properties)loadbalancer.properties;

            // Add Load Balancer Rules
            foreach (Asm.LoadBalancerRule asmLoadBalancerRule in asmVirtualMachine.LoadBalancerRules)
            {
                if (asmLoadBalancerRule.LoadBalancedEndpointSetName == String.Empty) // if it's a inbound nat rule
                {
                    InboundNatRule_Properties inboundnatrule_properties = new InboundNatRule_Properties();
                    inboundnatrule_properties.frontendPort = asmLoadBalancerRule.Port;
                    inboundnatrule_properties.backendPort = asmLoadBalancerRule.LocalPort;
                    inboundnatrule_properties.protocol = asmLoadBalancerRule.Protocol;

                    Reference frontendIPConfiguration = new Reference();
                    frontendIPConfiguration.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLoadBalancers + loadbalancer.name + "/frontendIPConfigurations/default')]";
                    inboundnatrule_properties.frontendIPConfiguration = frontendIPConfiguration;

                    InboundNatRule inboundnatrule = new InboundNatRule();
                    inboundnatrule.name = asmVirtualMachine.RoleName + "-" + asmLoadBalancerRule.Name;
                    inboundnatrule.name = inboundnatrule.name.Replace(" ", String.Empty);  // future enhancement, move to target name
                    inboundnatrule.properties = inboundnatrule_properties;

                    if (!properties.inboundNatRules.Contains(inboundnatrule))
                        properties.inboundNatRules.Add(inboundnatrule);
                }
                else // if it's a load balancing rule
                {
                    string name = asmLoadBalancerRule.LoadBalancedEndpointSetName.Replace(" ", String.Empty);

                    // build probe
                    Probe_Properties probe_properties = new Probe_Properties();
                    probe_properties.port = asmLoadBalancerRule.ProbePort;
                    probe_properties.protocol = asmLoadBalancerRule.ProbeProtocol;

                    Probe probe = new Probe();
                    probe.name = name;
                    probe.properties = probe_properties;

                    if (!properties.probes.Contains(probe))
                        properties.probes.Add(probe);

                    // build load balancing rule
                    Reference frontendipconfiguration_ref = new Reference();
                    frontendipconfiguration_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLoadBalancers + loadbalancer.name + "/frontendIPConfigurations/default')]";

                    Reference backendaddresspool_ref = new Reference();
                    backendaddresspool_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLoadBalancers + loadbalancer.name + "/backendAddressPools/default')]";

                    Reference probe_ref = new Reference();
                    probe_ref.id = "[concat(" + ArmConst.ResourceGroupId + ",'" + ArmConst.ProviderLoadBalancers + loadbalancer.name + "/probes/" + probe.name + "')]";

                    LoadBalancingRule_Properties loadbalancingrule_properties = new LoadBalancingRule_Properties();
                    loadbalancingrule_properties.frontendIPConfiguration = frontendipconfiguration_ref;
                    loadbalancingrule_properties.backendAddressPool = backendaddresspool_ref;
                    loadbalancingrule_properties.probe = probe_ref;
                    loadbalancingrule_properties.frontendPort = asmLoadBalancerRule.Port;
                    loadbalancingrule_properties.backendPort = asmLoadBalancerRule.LocalPort;
                    loadbalancingrule_properties.protocol = asmLoadBalancerRule.Protocol;

                    LoadBalancingRule loadbalancingrule = new LoadBalancingRule();
                    loadbalancingrule.name = name;
                    loadbalancingrule.properties = loadbalancingrule_properties;

                    if (!properties.loadBalancingRules.Contains(loadbalancingrule))
                        properties.loadBalancingRules.Add(loadbalancingrule);

                    LogProvider.WriteLog("BuildLoadBalancerRules", ArmConst.ProviderLoadBalancers + loadbalancer.name + "/loadBalancingRules/" + loadbalancingrule.name);
                }
            }

            // Add the load balancer only if there is any Load Balancing rule or Inbound NAT rule
            if (properties.inboundNatRules.Count > 0 || properties.loadBalancingRules.Count > 0)
            {
                this.AddResource(loadbalancer);
            }
            else
            {
                LogProvider.WriteLog("BuildLoadBalancerObject", "EMPTY Microsoft.Network/loadBalancers/" + loadbalancer.name);
            }

            LogProvider.WriteLog("BuildLoadBalancerObject", "End");
        }

        private async Task BuildVirtualNetworkObject(Azure.MigrationTarget.VirtualNetwork targetVirtualNetwork)
        {
            LogProvider.WriteLog("BuildVirtualNetworkObject", "Start Microsoft.Network/virtualNetworks/" + targetVirtualNetwork.ToString());

            List<string> dependson = new List<string>();

            AddressSpace addressspace = new AddressSpace();
            addressspace.addressPrefixes = targetVirtualNetwork.AddressPrefixes;

            VirtualNetwork_dhcpOptions dhcpoptions = new VirtualNetwork_dhcpOptions();
            dhcpoptions.dnsServers = targetVirtualNetwork.DnsServers;

            VirtualNetwork virtualnetwork = new VirtualNetwork(this.ExecutionGuid);
            virtualnetwork.name = targetVirtualNetwork.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                virtualnetwork.location = this.TargetResourceGroup.TargetLocation.Name;
            virtualnetwork.dependsOn = dependson;

            List<Subnet> subnets = new List<Subnet>();
            if (targetVirtualNetwork.TargetSubnets.Count == 0)
            {
                Subnet_Properties properties = new Subnet_Properties();
                properties.addressPrefix = targetVirtualNetwork.AddressPrefixes[0];

                Subnet subnet = new Subnet();
                subnet.name = "Subnet1";
                subnet.properties = properties;

                subnets.Add(subnet);
                this.AddAlert(AlertType.Error, $"VNET '{virtualnetwork.name}' has no subnets defined. We've created a default subnet 'Subnet1' covering the entire address space.", targetVirtualNetwork);
            }
            else
            {
                foreach (Azure.MigrationTarget.Subnet targetSubnet in targetVirtualNetwork.TargetSubnets)
                {
                    Subnet_Properties properties = new Subnet_Properties();
                    properties.addressPrefix = targetSubnet.AddressPrefix;

                    Subnet subnet = new Subnet();
                    subnet.name = targetSubnet.TargetName;
                    subnet.properties = properties;

                    subnets.Add(subnet);

                    // add Network Security Group if exists
                    if (targetSubnet.NetworkSecurityGroup != null)
                    {
                        // todo now russell??                Core.ArmTemplate.NetworkSecurityGroup networksecuritygroup = BuildARMNetworkSecurityGroup(armSubnet.NetworkSecurityGroup);
                        MigrationTarget.NetworkSecurityGroup targetNetworkSecurityGroup = (MigrationTarget.NetworkSecurityGroup) _ExportArtifacts.SeekNetworkSecurityGroup(targetSubnet.NetworkSecurityGroup.ToString());

                        if (targetNetworkSecurityGroup == null)
                        {
                            this.AddAlert(AlertType.Error, "Subnet '" + subnet.name + "' utilized ASM Network Security Group (NSG) '" + targetSubnet.NetworkSecurityGroup.ToString() + "', which has not been added to the ARM Subnet as the NSG was not included in the ARM Template (was not selected as an included resources for export).", targetNetworkSecurityGroup);
                        }
                        else
                        {
                            // Add NSG reference to the subnet
                            Reference networksecuritygroup_ref = new Reference();
                            networksecuritygroup_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderNetworkSecurityGroups + targetNetworkSecurityGroup.ToString() + "')]";

                            properties.networkSecurityGroup = networksecuritygroup_ref;

                            // Add NSG dependsOn to the Virtual Network object
                            if (!virtualnetwork.dependsOn.Contains(networksecuritygroup_ref.id))
                            {
                                virtualnetwork.dependsOn.Add(networksecuritygroup_ref.id);
                            }
                        }
                    }

                    // add Route Table if exists
                    if (targetSubnet.RouteTable != null)
                    {
                        RouteTable routetable = await BuildRouteTable(targetSubnet.RouteTable);

                        // Add Route Table reference to the subnet
                        Reference routetable_ref = new Reference();
                        routetable_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderRouteTables + routetable.name + "')]";

                        properties.routeTable = routetable_ref;

                        // Add Route Table dependsOn to the Virtual Network object
                        if (!virtualnetwork.dependsOn.Contains(routetable_ref.id))
                        {
                            virtualnetwork.dependsOn.Add(routetable_ref.id);
                        }
                    }
                }
            }

            VirtualNetwork_Properties virtualnetwork_properties = new VirtualNetwork_Properties();
            virtualnetwork_properties.addressSpace = addressspace;
            virtualnetwork_properties.subnets = subnets;
            virtualnetwork_properties.dhcpOptions = dhcpoptions;

            virtualnetwork.properties = virtualnetwork_properties;

            this.AddResource(virtualnetwork);

            // todo now russell await AddGatewaysToVirtualNetwork(targetVirtualNetwork, virtualnetwork);
            
            LogProvider.WriteLog("BuildVirtualNetworkObject", "End Microsoft.Network/virtualNetworks/" + targetVirtualNetwork.ToString());
        }

        private async Task AddGatewaysToVirtualNetwork(MigrationTarget.VirtualNetwork targetVirtualNetwork, VirtualNetwork templateVirtualNetwork)
        {
            if (targetVirtualNetwork.Source.GetType() == typeof(Azure.Asm.VirtualNetwork))
            {
                Asm.VirtualNetwork asmVirtualNetwork = (Asm.VirtualNetwork)targetVirtualNetwork.Source;

                // Process Virtual Network Gateway, if exists
                if ((asmVirtualNetwork.Gateway != null) && (asmVirtualNetwork.Gateway.IsProvisioned))
                {
                    // Gateway Public IP Address
                    PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
                    publicipaddress_properties.publicIPAllocationMethod = "Dynamic";

                    PublicIPAddress publicipaddress = new PublicIPAddress(this.ExecutionGuid);
                    publicipaddress.name = targetVirtualNetwork.TargetName + _settingsProvider.VirtualNetworkGatewaySuffix + _settingsProvider.PublicIPSuffix;
                    publicipaddress.location = templateVirtualNetwork.location;
                    publicipaddress.properties = publicipaddress_properties;

                    this.AddResource(publicipaddress);

                    // Virtual Network Gateway
                    Reference subnet_ref = new Reference();
                    subnet_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderVirtualNetwork + templateVirtualNetwork.name + "/subnets/" + ArmConst.GatewaySubnetName + "')]";

                    Reference publicipaddress_ref = new Reference();
                    publicipaddress_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderPublicIpAddress + publicipaddress.name + "')]";

                    var dependson = new List<string>();
                    dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderVirtualNetwork + templateVirtualNetwork.name + "')]");
                    dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderPublicIpAddress + publicipaddress.name + "')]");

                    IpConfiguration_Properties ipconfiguration_properties = new IpConfiguration_Properties();
                    ipconfiguration_properties.privateIPAllocationMethod = "Dynamic";
                    ipconfiguration_properties.subnet = subnet_ref;
                    ipconfiguration_properties.publicIPAddress = publicipaddress_ref;

                    IpConfiguration virtualnetworkgateway_ipconfiguration = new IpConfiguration();
                    virtualnetworkgateway_ipconfiguration.name = "GatewayIPConfig";
                    virtualnetworkgateway_ipconfiguration.properties = ipconfiguration_properties;

                    VirtualNetworkGateway_Sku virtualnetworkgateway_sku = new VirtualNetworkGateway_Sku();
                    virtualnetworkgateway_sku.name = "Basic";
                    virtualnetworkgateway_sku.tier = "Basic";

                    List<IpConfiguration> virtualnetworkgateway_ipconfigurations = new List<IpConfiguration>();
                    virtualnetworkgateway_ipconfigurations.Add(virtualnetworkgateway_ipconfiguration);

                    VirtualNetworkGateway_Properties virtualnetworkgateway_properties = new VirtualNetworkGateway_Properties();
                    virtualnetworkgateway_properties.ipConfigurations = virtualnetworkgateway_ipconfigurations;
                    virtualnetworkgateway_properties.sku = virtualnetworkgateway_sku;

                    // If there is VPN Client configuration
                    if (asmVirtualNetwork.VPNClientAddressPrefixes.Count > 0)
                    {
                        AddressSpace vpnclientaddresspool = new AddressSpace();
                        vpnclientaddresspool.addressPrefixes = asmVirtualNetwork.VPNClientAddressPrefixes;

                        VPNClientConfiguration vpnclientconfiguration = new VPNClientConfiguration();
                        vpnclientconfiguration.vpnClientAddressPool = vpnclientaddresspool;

                        //Process vpnClientRootCertificates
                        List<VPNClientCertificate> vpnclientrootcertificates = new List<VPNClientCertificate>();
                        foreach (Asm.ClientRootCertificate certificate in asmVirtualNetwork.ClientRootCertificates)
                        {
                            VPNClientCertificate_Properties vpnclientcertificate_properties = new VPNClientCertificate_Properties();
                            vpnclientcertificate_properties.PublicCertData = certificate.PublicCertData;

                            VPNClientCertificate vpnclientcertificate = new VPNClientCertificate();
                            vpnclientcertificate.name = certificate.TargetSubject;
                            vpnclientcertificate.properties = vpnclientcertificate_properties;

                            vpnclientrootcertificates.Add(vpnclientcertificate);
                        }

                        vpnclientconfiguration.vpnClientRootCertificates = vpnclientrootcertificates;

                        virtualnetworkgateway_properties.vpnClientConfiguration = vpnclientconfiguration;
                    }

                    if (asmVirtualNetwork.LocalNetworkSites.Count > 0 && asmVirtualNetwork.LocalNetworkSites[0].ConnectionType == "Dedicated")
                    {
                        virtualnetworkgateway_properties.gatewayType = "ExpressRoute";
                        virtualnetworkgateway_properties.enableBgp = null;
                        virtualnetworkgateway_properties.vpnType = null;
                    }
                    else
                    {
                        virtualnetworkgateway_properties.gatewayType = "Vpn";
                        string vpnType = asmVirtualNetwork.Gateway.GatewayType;
                        if (vpnType == "StaticRouting")
                        {
                            vpnType = "PolicyBased";
                        }
                        else if (vpnType == "DynamicRouting")
                        {
                            vpnType = "RouteBased";
                        }
                        virtualnetworkgateway_properties.vpnType = vpnType;
                    }

                    VirtualNetworkGateway virtualnetworkgateway = new VirtualNetworkGateway(this.ExecutionGuid);
                    virtualnetworkgateway.location = templateVirtualNetwork.location;
                    virtualnetworkgateway.name = targetVirtualNetwork.TargetName + _settingsProvider.VirtualNetworkGatewaySuffix;
                    virtualnetworkgateway.properties = virtualnetworkgateway_properties;
                    virtualnetworkgateway.dependsOn = dependson;

                    this.AddResource(virtualnetworkgateway);

                    if (!asmVirtualNetwork.HasGatewaySubnet)
                        this.AddAlert(AlertType.Error, "The Virtual Network '" + targetVirtualNetwork.TargetName + "' does not contain the necessary '" + ArmConst.GatewaySubnetName + "' subnet for deployment of the '" + virtualnetworkgateway.name + "' Gateway.", asmVirtualNetwork);

                    await AddLocalSiteToGateway(asmVirtualNetwork, templateVirtualNetwork, virtualnetworkgateway);
                }
            }
        }

        private async Task AddLocalSiteToGateway(Asm.VirtualNetwork asmVirtualNetwork, VirtualNetwork virtualnetwork, VirtualNetworkGateway virtualnetworkgateway)
        {
            // Local Network Gateways & Connections
            foreach (Asm.LocalNetworkSite asmLocalNetworkSite in asmVirtualNetwork.LocalNetworkSites)
            {
                GatewayConnection_Properties gatewayconnection_properties = new GatewayConnection_Properties();
                var dependson = new List<string>();

                if (asmLocalNetworkSite.ConnectionType == "IPsec")
                {
                    // Local Network Gateway
                    List<String> addressprefixes = asmLocalNetworkSite.AddressPrefixes;

                    AddressSpace localnetworkaddressspace = new AddressSpace();
                    localnetworkaddressspace.addressPrefixes = addressprefixes;

                    LocalNetworkGateway_Properties localnetworkgateway_properties = new LocalNetworkGateway_Properties();
                    localnetworkgateway_properties.localNetworkAddressSpace = localnetworkaddressspace;
                    localnetworkgateway_properties.gatewayIpAddress = asmLocalNetworkSite.VpnGatewayAddress;

                    LocalNetworkGateway localnetworkgateway = new LocalNetworkGateway(this.ExecutionGuid);
                    localnetworkgateway.name = asmLocalNetworkSite.Name + "-LocalGateway";
                    localnetworkgateway.name = localnetworkgateway.name.Replace(" ", String.Empty);

                    localnetworkgateway.location = virtualnetwork.location;
                    localnetworkgateway.properties = localnetworkgateway_properties;

                    this.AddResource(localnetworkgateway);

                    Reference localnetworkgateway_ref = new Reference();
                    localnetworkgateway_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLocalNetworkGateways + localnetworkgateway.name + "')]";
                    dependson.Add(localnetworkgateway_ref.id);

                    gatewayconnection_properties.connectionType = asmLocalNetworkSite.ConnectionType;
                    gatewayconnection_properties.localNetworkGateway2 = localnetworkgateway_ref;

                    string connectionShareKey = asmLocalNetworkSite.SharedKey;
                    if (connectionShareKey == String.Empty)
                    {
                        gatewayconnection_properties.sharedKey = "***SHARED KEY GOES HERE***";
                        this.AddAlert(AlertType.Error, $"Unable to retrieve shared key for VPN connection '{virtualnetworkgateway.name}'. Please edit the template to provide this value.", asmVirtualNetwork);
                    }
                    else
                    {
                        gatewayconnection_properties.sharedKey = connectionShareKey;
                    }
                }
                else if (asmLocalNetworkSite.ConnectionType == "Dedicated")
                {
                    gatewayconnection_properties.connectionType = "ExpressRoute";
                    gatewayconnection_properties.peer = new Reference() { id = "/subscriptions/***/resourceGroups/***" + ArmConst.ProviderExpressRouteCircuits + "***" }; // todo, this is incomplete
                    this.AddAlert(AlertType.Error, $"Gateway '{virtualnetworkgateway.name}' connects to ExpressRoute. MigAz is unable to migrate ExpressRoute circuits. Please create or convert the circuit yourself and update the circuit resource ID in the generated template.", asmVirtualNetwork);
                }

                // Connections
                Reference virtualnetworkgateway_ref = new Reference();
                virtualnetworkgateway_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderVirtualNetworkGateways + virtualnetworkgateway.name + "')]";
                     
                dependson.Add(virtualnetworkgateway_ref.id);

                gatewayconnection_properties.virtualNetworkGateway1 = virtualnetworkgateway_ref;

                GatewayConnection gatewayconnection = new GatewayConnection(this.ExecutionGuid);
                gatewayconnection.name = virtualnetworkgateway.name + "-" + asmLocalNetworkSite.TargetName + "-connection"; // TODO, HardCoded
                gatewayconnection.location = virtualnetwork.location;
                gatewayconnection.properties = gatewayconnection_properties;
                gatewayconnection.dependsOn = dependson;

                this.AddResource(gatewayconnection);

            }
        }

        private async Task<NetworkSecurityGroup> BuildNetworkSecurityGroup(MigrationTarget.NetworkSecurityGroup targetNetworkSecurityGroup)
        {
            LogProvider.WriteLog("BuildNetworkSecurityGroup", "Start");

            NetworkSecurityGroup networksecuritygroup = new NetworkSecurityGroup(this.ExecutionGuid);
            networksecuritygroup.name = targetNetworkSecurityGroup.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                networksecuritygroup.location = this.TargetResourceGroup.TargetLocation.Name;

            NetworkSecurityGroup_Properties networksecuritygroup_properties = new NetworkSecurityGroup_Properties();
            networksecuritygroup_properties.securityRules = new List<SecurityRule>();

            // for each rule
            foreach (MigrationTarget.NetworkSecurityGroupRule asmNetworkSecurityGroupRule in targetNetworkSecurityGroup.Rules)
            {
                // if not system rule
                if (!asmNetworkSecurityGroupRule.IsSystemRule)
                {
                    SecurityRule_Properties securityrule_properties = new SecurityRule_Properties();
                    securityrule_properties.description = asmNetworkSecurityGroupRule.Name;
                    securityrule_properties.direction = asmNetworkSecurityGroupRule.Type;
                    securityrule_properties.priority = asmNetworkSecurityGroupRule.Priority;
                    securityrule_properties.access = asmNetworkSecurityGroupRule.Action;
                    securityrule_properties.sourceAddressPrefix = asmNetworkSecurityGroupRule.SourceAddressPrefix;
                    securityrule_properties.destinationAddressPrefix = asmNetworkSecurityGroupRule.DestinationAddressPrefix;
                    securityrule_properties.sourcePortRange = asmNetworkSecurityGroupRule.SourcePortRange;
                    securityrule_properties.destinationPortRange = asmNetworkSecurityGroupRule.DestinationPortRange;
                    securityrule_properties.protocol = asmNetworkSecurityGroupRule.Protocol;

                    SecurityRule securityrule = new SecurityRule();
                    securityrule.name = asmNetworkSecurityGroupRule.Name;
                    securityrule.properties = securityrule_properties;

                    networksecuritygroup_properties.securityRules.Add(securityrule);
                }
            }

            networksecuritygroup.properties = networksecuritygroup_properties;

            this.AddResource(networksecuritygroup);

            LogProvider.WriteLog("BuildNetworkSecurityGroup", "End");

            return networksecuritygroup;
        }

        private Core.ArmTemplate.NetworkSecurityGroup BuildARMNetworkSecurityGroup(Arm.NetworkSecurityGroup networkSecurityGroup)
        {
            LogProvider.WriteLog("BuildNetworkSecurityGroup", "Start Microsoft.Network/networkSecurityGroups/" + networkSecurityGroup.Name);

            Core.ArmTemplate.NetworkSecurityGroup networksecuritygroup = new Core.ArmTemplate.NetworkSecurityGroup(this.ExecutionGuid);
            networksecuritygroup.name = networkSecurityGroup.Name;
            networksecuritygroup.location = _TargetResourceGroup.TargetLocation.Name;

            NetworkSecurityGroup_Properties networksecuritygroup_properties = new NetworkSecurityGroup_Properties();
            networksecuritygroup_properties.securityRules = new List<SecurityRule>();

            //foreach rule without System Rule
            foreach (Arm.NetworkSecurityGroupRule rule in networkSecurityGroup.Rules)
            {
                SecurityRule_Properties securityrule_properties = new SecurityRule_Properties();
                securityrule_properties.description = rule.Name;
                securityrule_properties.direction = rule.Direction;
                securityrule_properties.priority = rule.Priority;
                securityrule_properties.access = rule.Access;
                securityrule_properties.sourceAddressPrefix = rule.SourceAddressPrefix;
                securityrule_properties.sourceAddressPrefix.Replace("_", "");
                securityrule_properties.destinationAddressPrefix = rule.DestinationAddressPrefix;
                securityrule_properties.destinationAddressPrefix.Replace("_", "");
                securityrule_properties.sourcePortRange = rule.SourcePortRange;
                securityrule_properties.destinationPortRange = rule.DestinationPortRange;
                securityrule_properties.protocol = rule.Protocol;

                SecurityRule securityrule = new SecurityRule();
                securityrule.name = rule.Name;
                securityrule.properties = securityrule_properties;

                networksecuritygroup_properties.securityRules.Add(securityrule);
            }

            networksecuritygroup.properties = networksecuritygroup_properties;

            this.AddResource(networksecuritygroup);

            LogProvider.WriteLog("BuildNetworkSecurityGroup", "End Microsoft.Network/networkSecurityGroups/" + networkSecurityGroup.Name);

            return networksecuritygroup;
        }

        private async Task<RouteTable> BuildRouteTable(MigrationTarget.RouteTable routeTable)
        {
            LogProvider.WriteLog("BuildRouteTable", "Start");

            RouteTable routetable = new RouteTable(this.ExecutionGuid);
            routetable.name = routeTable.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                routetable.location = this.TargetResourceGroup.TargetLocation.Name;

            RouteTable_Properties routetable_properties = new RouteTable_Properties();
            routetable_properties.routes = new List<Route>();

            // for each route
            foreach (MigrationTarget.Route migrationRoute in routeTable.Routes)
            {
                //securityrule_properties.protocol = rule.SelectSingleNode("Protocol").InnerText;
                Route_Properties route_properties = new Route_Properties();
                route_properties.addressPrefix = migrationRoute.AddressPrefix;

                // convert next hop type string
                switch (migrationRoute.NextHopType)
                {
                    case "VirtualAppliance":
                        route_properties.nextHopType = "VirtualAppliance";
                        break;
                    case "VPNGateway":
                        route_properties.nextHopType = "VirtualNetworkGateway";
                        break;
                    case "Internet":
                        route_properties.nextHopType = "Internet";
                        break;
                    case "VNETLocal":
                        route_properties.nextHopType = "VnetLocal";
                        break;
                    case "Null":
                        route_properties.nextHopType = "None";
                        break;
                }
                if (route_properties.nextHopType == "VirtualAppliance")
                    route_properties.nextHopIpAddress = migrationRoute.NextHopIpAddress;

                Route route = new Route();
                route.name = migrationRoute.ToString();
                route.properties = route_properties;

                routetable_properties.routes.Add(route);
            }

            routetable.properties = routetable_properties;

            this.AddResource(routetable);

            LogProvider.WriteLog("BuildRouteTable", "End");

            return routetable;
        }

        private Core.ArmTemplate.RouteTable BuildARMRouteTable(Arm.RouteTable routeTable)
        {
            LogProvider.WriteLog("BuildRouteTable", "Start Microsoft.Network/routeTables/" + routeTable.Name);

            Core.ArmTemplate.RouteTable routetable = new Core.ArmTemplate.RouteTable(this.ExecutionGuid);
            routetable.name = routeTable.Name;
            routetable.location = _TargetResourceGroup.TargetLocation.Name;

            RouteTable_Properties routetable_properties = new RouteTable_Properties();
            routetable_properties.routes = new List<Core.ArmTemplate.Route>();

            // for each route
            foreach (Arm.Route armRoute in routeTable.Routes)
            {
                //securityrule_properties.protocol = rule.SelectSingleNode("Protocol").InnerText;
                Route_Properties route_properties = new Route_Properties();
                route_properties.addressPrefix = armRoute.AddressPrefix;
                route_properties.nextHopType = armRoute.NextHopType;


                if (route_properties.nextHopType == "VirtualAppliance")
                    route_properties.nextHopIpAddress = armRoute.NextHopIpAddress;

                Core.ArmTemplate.Route route = new Core.ArmTemplate.Route();
                route.name = armRoute.Name;
                route.properties = route_properties;

                routetable_properties.routes.Add(route);
            }

            routetable.properties = routetable_properties;

            this.AddResource(routetable);

            LogProvider.WriteLog("BuildRouteTable", "End Microsoft.Network/routeTables/" + routeTable.Name);

            return routetable;
        }

        private async Task BuildNetworkInterfaceObject(Azure.MigrationTarget.NetworkInterface targetNetworkInterface, List<NetworkProfile_NetworkInterface> networkinterfaces)
        {
            LogProvider.WriteLog("BuildNetworkInterfaceObject", "Start " + ArmConst.ProviderNetworkInterfaces + targetNetworkInterface.ToString());

            NetworkInterface networkInterface = new NetworkInterface(this.ExecutionGuid);
            networkInterface.name = targetNetworkInterface.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                networkInterface.location = this.TargetResourceGroup.TargetLocation.Name;

            Reference subnet_ref = new Reference();

            if (targetNetworkInterface.TargetSubnet != null)
                subnet_ref.id = targetNetworkInterface.TargetSubnet.Id;

            string privateIPAllocationMethod = "Dynamic";
            string privateIPAddress = null;
            if (targetNetworkInterface.TargetStaticIpAddress != String.Empty)
            {
                privateIPAllocationMethod = "Static";
                privateIPAddress = targetNetworkInterface.TargetStaticIpAddress;
            }

            List<string> dependson = new List<string>();
            if (targetNetworkInterface.TargetVirtualNetwork != null && targetNetworkInterface.TargetVirtualNetwork.GetType() == typeof(Asm.VirtualNetwork)) // todo, gettype is old code, now what?
                dependson.Add(targetNetworkInterface.TargetVirtualNetwork.Id);

            // If there is at least one endpoint add the reference to the LB backend pool
            List<Reference> loadBalancerBackendAddressPools = new List<Reference>();
            if (targetNetworkInterface.LoadBalancerRules.Count > 0)
            {
                Reference loadBalancerBackendAddressPool = new Reference();
                loadBalancerBackendAddressPool.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLoadBalancers + targetNetworkInterface.LoadBalancerName + "/backendAddressPools/default')]";

                loadBalancerBackendAddressPools.Add(loadBalancerBackendAddressPool);

                dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLoadBalancers + targetNetworkInterface.LoadBalancerName + "')]");
            }

            // Adds the references to the inboud nat rules
            List<Reference> loadBalancerInboundNatRules = new List<Reference>();
            foreach (MigrationTarget.LoadBalancerRule loadBalancerRule in targetNetworkInterface.LoadBalancerRules)
            {
                if (loadBalancerRule.LoadBalancedEndpointSetName == String.Empty) // don't want to add a load balance endpoint as an inbound nat rule
                {
                    string inboundnatrulename = networkInterface.ToString() + "-" + loadBalancerRule.Name;
                    inboundnatrulename = inboundnatrulename.Replace(" ", String.Empty);

                    Reference loadBalancerInboundNatRule = new Reference();
                    loadBalancerInboundNatRule.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderLoadBalancers + targetNetworkInterface.LoadBalancerName + "/inboundNatRules/" + inboundnatrulename + "')]";

                    loadBalancerInboundNatRules.Add(loadBalancerInboundNatRule);
                }
            }

            IpConfiguration_Properties ipconfiguration_properties = new IpConfiguration_Properties();
            ipconfiguration_properties.privateIPAllocationMethod = privateIPAllocationMethod;
            ipconfiguration_properties.privateIPAddress = privateIPAddress;
            ipconfiguration_properties.subnet = subnet_ref;
            ipconfiguration_properties.loadBalancerInboundNatRules = loadBalancerInboundNatRules;

            string ipconfiguration_name = "ipconfig1";
            IpConfiguration ipconfiguration = new IpConfiguration();
            ipconfiguration.name = ipconfiguration_name;
            ipconfiguration.properties = ipconfiguration_properties;

            List<IpConfiguration> ipConfigurations = new List<IpConfiguration>();
            ipConfigurations.Add(ipconfiguration);

            NetworkInterface_Properties networkinterface_properties = new NetworkInterface_Properties();
            networkinterface_properties.ipConfigurations = ipConfigurations;
            networkinterface_properties.enableIPForwarding = targetNetworkInterface.EnableIPForwarding;

            networkInterface.properties = networkinterface_properties;
            networkInterface.dependsOn = dependson;

            NetworkProfile_NetworkInterface_Properties networkinterface_ref_properties = new NetworkProfile_NetworkInterface_Properties();

            // todo now russell , this is the difference in code, should not always be primary -- need to move to a variable vs. always true
            networkinterface_ref_properties.primary = true;

            NetworkProfile_NetworkInterface networkinterface_ref = new NetworkProfile_NetworkInterface();
            networkinterface_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderNetworkInterfaces + networkInterface.name + "')]";
            networkinterface_ref.properties = networkinterface_ref_properties;

            if (targetNetworkInterface.NetworkSecurityGroup != null)
            {
                INetworkSecurityGroup networkSecurityGroupInMigration = (INetworkSecurityGroup)_ExportArtifacts.SeekNetworkSecurityGroup(targetNetworkInterface.NetworkSecurityGroup.Name);

                if (networkSecurityGroupInMigration == null)
                {
                    this.AddAlert(AlertType.Error, "Network Interface Card (NIC) '" + networkInterface.name + "' utilized ASM Network Security Group (NSG) '" + targetNetworkInterface.NetworkSecurityGroup.Name + "', which has not been added to the NIC as the NSG was not included in the ARM Template (was not selected as an included resources for export).", networkSecurityGroupInMigration);
                }
                else
                {
                    // Add NSG reference to the network interface
                    Reference networksecuritygroup_ref = new Reference();
                    networksecuritygroup_ref.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderNetworkSecurityGroups + networkSecurityGroupInMigration.ToString() + "')]";

                    networkinterface_properties.NetworkSecurityGroup = networksecuritygroup_ref;
                    networkInterface.properties = networkinterface_properties;

                    // Add NSG dependsOn to the Network Interface object
                    if (!networkInterface.dependsOn.Contains(networksecuritygroup_ref.id))
                    {
                        networkInterface.dependsOn.Add(networksecuritygroup_ref.id);
                    }
                }
            }

            if (targetNetworkInterface.HasPublicIPs)
            {
                BuildPublicIPAddressObject(ref networkInterface);
            }

            dependson.Add(targetNetworkInterface.TargetVirtualNetwork.Id);

            networkinterfaces.Add(networkinterface_ref);

            this.AddResource(networkInterface);

            LogProvider.WriteLog("BuildNetworkInterfaceObject", "End " + ArmConst.ProviderNetworkInterfaces + targetNetworkInterface.ToString());
        }

        private async Task BuildVirtualMachineObject(Azure.MigrationTarget.VirtualMachine virtualMachine)
        {
            LogProvider.WriteLog("BuildVirtualMachineObject", "Start Microsoft.Compute/virtualMachines/" + virtualMachine.ToString());

            VirtualMachine virtualmachine = new VirtualMachine(this.ExecutionGuid);
            virtualmachine.name = virtualMachine.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                virtualmachine.location = this.TargetResourceGroup.TargetLocation.Name;

            List<IStorageTarget> storageaccountdependencies = new List<IStorageTarget>();
            List<string> dependson = new List<string>();

            // todo now russell, placement?  options?  valid in context always??
            //BuildPublicIPAddressObject(asmVirtualMachine);
            //BuildLoadBalancerObject(asmVirtualMachine.Parent, asmVirtualMachine, _ExportArtifacts);



            string newdiskurl = String.Empty;
            string osDiskTargetStorageAccountName = String.Empty;
            if (virtualMachine.OSVirtualHardDisk.TargetStorageAccount != null)
            {
                if (virtualMachine.OSVirtualHardDisk.TargetStorageAccount.GetType() == typeof(Azure.MigrationTarget.StorageAccount))
                {
                    Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)virtualMachine.OSVirtualHardDisk.TargetStorageAccount;
                    osDiskTargetStorageAccountName = targetStorageAccount.ToString();
                }
                

                newdiskurl = virtualMachine.OSVirtualHardDisk.TargetMediaLink;
                storageaccountdependencies.Add(virtualMachine.OSVirtualHardDisk.TargetStorageAccount);
            }

            // process network interface
            List<NetworkProfile_NetworkInterface> networkinterfaces = new List<NetworkProfile_NetworkInterface>();

            foreach (MigrationTarget.NetworkInterface networkInterface in virtualMachine.NetworkInterfaces)
            {
                // todo if (NicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools != null)
                //{
                // todo
                //string[] stringSeparators = new string[] { "/backendAddressPools/" };

                //string NatRuleID = NicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools[0].id;
                //string Loadbalancerid = NatRuleID.Split(stringSeparators, StringSplitOptions.None)[0].Replace("/subscriptions", "subscriptions");
                //NWInfo.Add("LBId", Loadbalancerid);

                ////Get LBDetails
                //var LBDetails = _asmRetriever.GetAzureARMResources("Loadbalancer", NWInfo, null);
                //var LBResults = JsonConvert.DeserializeObject<dynamic>(LBDetails);

                //string PubIPName = null;

                ////Process the Public IP for the Loadbalancer
                //if (LBResults.properties.frontendIPConfigurations[0].properties.publicIPAddress != null)
                //{
                //    //Get PublicIP details
                //    string PubId = LBResults.properties.frontendIPConfigurations[0].properties.publicIPAddress.id;
                //    PubId = PubId.Replace("/subscriptions", "subscriptions");

                //    NWInfo.Add("publicipId", PubId);
                //    var LBPubIpDetails = _asmRetriever.GetAzureARMResources("PublicIP", NWInfo, null);
                //    var LBPubIpResults = JsonConvert.DeserializeObject<dynamic>(LBPubIpDetails);

                //    PubIPName = LBPubIpResults.name;

                //    //Build the Public IP for the Loadbalancer
                //    BuildARMPublicIPAddressObject(LBResults, LBPubIpResults);

                //}

                //Build the Loadbalancer
                // todo BuildARMLoadBalancerObject(LBResults, PubIPName);
                //}

                // todo now russell this moved into for loop
                //await BuildNetworkInterfaceObject(virtualMachine, networkinterfaces);

                dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderNetworkInterfaces + networkInterface.ToString() + "')]");
            }

            HardwareProfile hardwareprofile = new HardwareProfile();
            hardwareprofile.vmSize = GetVMSize(virtualMachine.TargetSize);

            NetworkProfile networkprofile = new NetworkProfile();
            networkprofile.networkInterfaces = networkinterfaces;

            Vhd vhd = new Vhd();
            vhd.uri = newdiskurl;

            OsDisk osdisk = new OsDisk();
            osdisk.name = virtualMachine.OSVirtualHardDisk.Name;
            osdisk.vhd = vhd;
            osdisk.caching = virtualMachine.OSVirtualHardDisk.HostCaching;

            ImageReference imagereference = new ImageReference();
            OsProfile osprofile = new OsProfile();

            // if the tool is configured to create new VMs with empty data disks
            if (_settingsProvider.BuildEmpty)
            {
                osdisk.createOption = "FromImage";

                osprofile.computerName = virtualMachine.ToString();
                osprofile.adminUsername = "[parameters('adminUsername')]";
                osprofile.adminPassword = "[parameters('adminPassword')]";

                if (!this.Parameters.ContainsKey("adminUsername"))
                {
                    Parameter parameter = new Parameter();
                    parameter.type = "string";
                    this.Parameters.Add("adminUsername", parameter);
                }

                if (!this.Parameters.ContainsKey("adminPassword"))
                {
                    Parameter parameter = new Parameter();
                    parameter.type = "securestring";
                    this.Parameters.Add("adminPassword", parameter);
                }

                if (virtualMachine.OSVirtualHardDiskOS == "Windows")
                {
                    imagereference.publisher = "MicrosoftWindowsServer";
                    imagereference.offer = "WindowsServer";
                    imagereference.sku = "2016-Datacenter";
                    imagereference.version = "latest";
                }
                else if (virtualMachine.OSVirtualHardDiskOS == "Linux")
                {
                    imagereference.publisher = "Canonical";
                    imagereference.offer = "UbuntuServer";
                    imagereference.sku = "16.04.0-LTS";
                    imagereference.version = "latest";
                }
                else
                {
                    imagereference.publisher = "<publisher>";
                    imagereference.offer = "<offer>";
                    imagereference.sku = "<sku>";
                    imagereference.version = "<version>";
                }
            }
            // if the tool is configured to attach copied disks
            else
            {
                osdisk.createOption = "Attach";
                osdisk.osType = virtualMachine.OSVirtualHardDiskOS;

                // Block of code to help copying the blobs to the new storage accounts
                CopyBlobDetail copyblobdetail = new CopyBlobDetail();
                if (this.SourceSubscription != null)
                    copyblobdetail.SourceEnvironment = this.SourceSubscription.AzureEnvironment.ToString();
                copyblobdetail.SourceSA = virtualMachine.OSVirtualHardDisk.StorageAccountName;
                copyblobdetail.SourceContainer = virtualMachine.OSVirtualHardDisk.StorageAccountContainer;
                copyblobdetail.SourceBlob = virtualMachine.OSVirtualHardDisk.StorageAccountBlob;
                copyblobdetail.SourceKey = virtualMachine.OSVirtualHardDisk.SourceStorageKey;
                copyblobdetail.DestinationSA = osDiskTargetStorageAccountName;
                copyblobdetail.DestinationContainer = virtualMachine.OSVirtualHardDisk.StorageAccountContainer;
                copyblobdetail.DestinationBlob = virtualMachine.OSVirtualHardDisk.StorageAccountBlob;
                this._CopyBlobDetails.Add(copyblobdetail);
                // end of block of code to help copying the blobs to the new storage accounts
            }

            // process data disks
            List<DataDisk> datadisks = new List<DataDisk>();
            foreach (MigrationTarget.Disk dataDisk in virtualMachine.DataDisks)
            {
                if (dataDisk.TargetStorageAccount == null)
                {
                    this.AddAlert(AlertType.Error, "Target Storage Account must be specified for Data Disk '" + dataDisk.Name + "'.", dataDisk);
                }
                else
                {
                    string dataDiskTargetStorageAccountName = dataDisk.TargetStorageAccount.ToString();
                    DataDisk datadisk = new DataDisk();
                    datadisk.name = dataDisk.Name;
                    datadisk.caching = dataDisk.HostCaching;
                    datadisk.diskSizeGB = dataDisk.DiskSizeInGB;
                    if (dataDisk.Lun.HasValue)
                        datadisk.lun = dataDisk.Lun.Value;

                    newdiskurl = dataDisk.TargetMediaLink;

                    // if the tool is configured to create new VMs with empty data disks
                    if (_settingsProvider.BuildEmpty)
                    {
                        datadisk.createOption = "Empty";
                    }
                    // if the tool is configured to attach copied disks
                    else
                    {
                        datadisk.createOption = "Attach";

                        // Block of code to help copying the blobs to the new storage accounts
                        CopyBlobDetail copyblobdetail = new CopyBlobDetail();
                        if (this.SourceSubscription != null)
                            copyblobdetail.SourceEnvironment = this.SourceSubscription.AzureEnvironment.ToString();
                        copyblobdetail.SourceSA = dataDisk.StorageAccountName;
                        copyblobdetail.SourceContainer = dataDisk.StorageAccountContainer;
                        copyblobdetail.SourceBlob = dataDisk.StorageAccountBlob;
                        copyblobdetail.SourceKey = dataDisk.SourceStorageKey;
                        copyblobdetail.DestinationSA = dataDiskTargetStorageAccountName;
                        copyblobdetail.DestinationContainer = dataDisk.StorageAccountContainer;
                        copyblobdetail.DestinationBlob = dataDisk.StorageAccountBlob;
                        this._CopyBlobDetails.Add(copyblobdetail);
                        // end of block of code to help copying the blobs to the new storage accounts
                    }

                    vhd = new Vhd();
                    vhd.uri = newdiskurl;
                    datadisk.vhd = vhd;

                    if (!storageaccountdependencies.Contains(dataDisk.TargetStorageAccountName))
                        storageaccountdependencies.Add(dataDisk.TargetStorageAccountName);

                    datadisks.Add(datadisk);
                }
            }

            StorageProfile storageprofile = new StorageProfile();
            if (_settingsProvider.BuildEmpty) { storageprofile.imageReference = imagereference; }
            storageprofile.osDisk = osdisk;
            storageprofile.dataDisks = datadisks;

            VirtualMachine_Properties virtualmachine_properties = new VirtualMachine_Properties();
            virtualmachine_properties.hardwareProfile = hardwareprofile;
            if (_settingsProvider.BuildEmpty) { virtualmachine_properties.osProfile = osprofile; }
            virtualmachine_properties.networkProfile = networkprofile;
            virtualmachine_properties.storageProfile = storageprofile;










            //    Hashtable storageaccountdependencies = new Hashtable();

            //    Vhd vhd = new Vhd();

            //    OsDisk osdisk = new OsDisk();
            //    osdisk.name = armVirtualMachine.OSVirtualHardDisk.Name;
            //    osdisk.vhd = vhd;
            //    osdisk.caching = armVirtualMachine.OSVirtualHardDisk.Caching;

            //    HardwareProfile hardwareprofile = new HardwareProfile();
            //    hardwareprofile.vmSize = armVirtualMachine.VmSize;

            //    NetworkProfile networkprofile = new NetworkProfile();
            //    // todo networkprofile.networkInterfaces = networkinterfaces;

            //    ImageReference imagereference = new ImageReference();
            //    OsProfile osprofile = new OsProfile();

            //    // if the tool is configured to create new VMs with empty data disks
            //    if (_settingsProvider.BuildEmpty)
            //    {
            //        osdisk.createOption = "FromImage";

            //        osprofile.computerName = armVirtualMachine.Name;
            //        osprofile.adminUsername = "[parameters('adminUsername')]";
            //        osprofile.adminPassword = "[parameters('adminPassword')]";

            //        if (!Parameters.ContainsKey("adminUsername"))
            //        {
            //            Parameter parameter = new Parameter();
            //            parameter.type = "string";
            //            Parameters.Add("adminUsername", parameter);
            //        }

            //        if (!Parameters.ContainsKey("adminPassword"))
            //        {
            //            Parameter parameter = new Parameter();
            //            parameter.type = "securestring";
            //            Parameters.Add("adminPassword", parameter);
            //        }

            //        //todo
            //        //imagereference.publisher = resource.properties.storageProfile.imageReference.publisher;
            //        //imagereference.offer = resource.properties.storageProfile.imageReference.offer;
            //        //imagereference.sku = resource.properties.storageProfile.imageReference.sku;
            //        //imagereference.version = resource.properties.storageProfile.imageReference.version;
            //    }
            //    // if the tool is configured to attach copied disks
            //    else
            //    {
            //        osdisk.createOption = "Attach";
            //        // todoosdisk.osType = ostype;

            //        if (armVirtualMachine.OSVirtualHardDisk.VhdUri != String.Empty)
            //        {
            //            // todo storageaccountdependencies.Add(newstorageaccountname, "");

            //            CopyBlobDetail copyblobdetail = new CopyBlobDetail();
            //            if (this.SourceSubscription != null)
            //                copyblobdetail.SourceEnvironment = this.SourceSubscription.AzureEnvironment.ToString();
            //            copyblobdetail.SourceSA = armVirtualMachine.OSVirtualHardDisk.StorageAccountName;
            //            copyblobdetail.SourceContainer = armVirtualMachine.OSVirtualHardDisk.StorageAccountContainer;
            //            copyblobdetail.SourceBlob = armVirtualMachine.OSVirtualHardDisk.StorageAccountBlob;
            //            // todo copyblobdetail.SourceKey = armVirtualMachine.OSVirtualHardDisk.SourceStorageAccount.Keys.Primary;
            //            if (armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount != null)
            //            {
            //                if (armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount.GetType() == typeof(Azure.MigrationTarget.StorageAccount))
            //                {
            //                    Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount;
            //                    copyblobdetail.DestinationSA = targetStorageAccount.ToString();
            //                }
            //            }
            //            copyblobdetail.DestinationContainer = armVirtualMachine.OSVirtualHardDisk.StorageAccountContainer;
            //            copyblobdetail.DestinationBlob = armVirtualMachine.OSVirtualHardDisk.StorageAccountBlob;
            //            _CopyBlobDetails.Add(copyblobdetail);
            //        }

            //        // end of block of code to help copying the blobs to the new storage accounts
            //    }

            //    // process data disks
            //    List<DataDisk> datadisks = new List<DataDisk>();
            //    foreach (Arm.DataDisk sourceDataDisk in armVirtualMachine.DataDisks)
            //    {
            //        DataDisk datadisk = new DataDisk();
            //        datadisk.name = sourceDataDisk.Name;
            //        datadisk.caching = sourceDataDisk.Caching;
            //        datadisk.diskSizeGB = sourceDataDisk.DiskSizeGb;
            //        datadisk.lun = sourceDataDisk.Lun;

            //        // if the tool is configured to create new VMs with empty data disks
            //        vhd = new Vhd();
            //        if (_settingsProvider.BuildEmpty)
            //        {
            //            datadisk.createOption = "Empty";
            //        }
            //        // if the tool is configured to attach copied disks
            //        else
            //        {
            //            datadisk.createOption = "Attach";

            //            if (sourceDataDisk.VhdUri != String.Empty)
            //            {
            //                // todo try { storageaccountdependencies.Add(newstorageaccountname, ""); }
            //                // catch { }

            //                CopyBlobDetail copyblobdetail = new CopyBlobDetail();
            //                if (this.SourceSubscription != null)
            //                    copyblobdetail.SourceEnvironment = this.SourceSubscription.AzureEnvironment.ToString();
            //                copyblobdetail.SourceSA = sourceDataDisk.StorageAccountName;
            //                copyblobdetail.SourceContainer = sourceDataDisk.StorageAccountContainer;
            //                copyblobdetail.SourceBlob = sourceDataDisk.StorageAccountBlob;
            //                // todo copyblobdetail.SourceKey = sourceDataDisk.SourceStorageAccount.Keys.Primary;
            //                if (armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount != null)
            //                {
            //                    if (armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount.GetType() == typeof(Azure.MigrationTarget.StorageAccount))
            //                    {
            //                        Azure.MigrationTarget.StorageAccount targetStorageAccount = (Azure.MigrationTarget.StorageAccount)armVirtualMachine.OSVirtualHardDisk.TargetStorageAccount;
            //                        copyblobdetail.DestinationSA = targetStorageAccount.ToString();
            //                    }
            //                }
            //                copyblobdetail.DestinationContainer = sourceDataDisk.StorageAccountContainer;
            //                copyblobdetail.DestinationBlob = sourceDataDisk.StorageAccountBlob;
            //                _CopyBlobDetails.Add(copyblobdetail);
            //                // end of block of code to help copying the blobs to the new storage accounts
            //            }
            //        }

            //        datadisk.vhd = vhd;

            //        datadisks.Add(datadisk);
            //    }

            //    StorageProfile storageprofile = new StorageProfile();
            //    if (_settingsProvider.BuildEmpty) { storageprofile.imageReference = imagereference; }
            //    storageprofile.osDisk = osdisk;
            //    storageprofile.dataDisks = datadisks;

            //    VirtualMachine_Properties virtualmachine_properties = new VirtualMachine_Properties();
            //    virtualmachine_properties.hardwareProfile = hardwareprofile;
            //    if (_settingsProvider.BuildEmpty) { virtualmachine_properties.osProfile = osprofile; }
            //    virtualmachine_properties.networkProfile = networkprofile;
            //    virtualmachine_properties.storageProfile = storageprofile;

            //    List<string> dependson = new List<string>();

            //    //foreach (var nic in networkinterfaces)
            //    //{
            //    //    dependson.Add(nic.id.ToString());
            //    //}

            //    // dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterfacename + "')]");

            // process availability set
            if (virtualMachine.ParentAvailabilitySet != null)
            {
                BuildAvailabilitySetObject(virtualMachine.ParentAvailabilitySet);

                // Availability Set
                // todo now russell
                //if (targetAvailabilitySet != null)
                //{
                //    Reference availabilityset = new Reference();
                //    virtualmachine_properties.availabilitySet = availabilityset;
                //    availabilityset.id = "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderAvailabilitySets + targetAvailabilitySet.ToString() + "')]";
                //    dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderAvailabilitySets + targetAvailabilitySet.ToString() + "')]");
                //}
            }
            //    //}
            //    //else
            //    //{
            //    //    // _messages.Add($"VM '{virtualmachinename}' is not in an availability set. Putting it in a new availability set '{availabilitysetname}'.");
            //    //}

            foreach (IStorageTarget storageaccountdependency in storageaccountdependencies)
            {
                // todo russell validate        if (ResourceExists(typeof(Core.ArmTemplate.StorageAccount), (string)storageaccountdependency.Key))
                if (storageaccountdependency.GetType() == typeof(Asm.StorageAccount))
                    dependson.Add("[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderStorageAccounts + storageaccountdependency + "')]");
            }

            virtualmachine.properties = virtualmachine_properties;
            virtualmachine.dependsOn = dependson;
            virtualmachine.resources = new List<ArmResource>();

            // Diagnostics Extension
            Extension extension_iaasdiagnostics = null;
            if (extension_iaasdiagnostics != null) { virtualmachine.resources.Add(extension_iaasdiagnostics); }

            this.AddResource(virtualmachine);

            LogProvider.WriteLog("BuildVirtualMachineObject", "Start Microsoft.Compute/virtualMachines/" + virtualMachine.ToString());
        }

        private void BuildStorageAccountObject(MigrationTarget.StorageAccount targetStorageAccount)
        {
            LogProvider.WriteLog("BuildStorageAccountObject", "Start Microsoft.Storage/storageAccounts/" + targetStorageAccount.ToString());

            StorageAccount_Properties storageaccount_properties = new StorageAccount_Properties();
            storageaccount_properties.accountType = targetStorageAccount.AccountType;

            StorageAccount storageaccount = new StorageAccount(this.ExecutionGuid);
            storageaccount.name = targetStorageAccount.ToString();
            if (this.TargetResourceGroup != null && this.TargetResourceGroup.TargetLocation != null)
                storageaccount.location = this.TargetResourceGroup.TargetLocation.Name;   
            storageaccount.properties = storageaccount_properties;

            this.AddResource(storageaccount);

            LogProvider.WriteLog("BuildStorageAccountObject", "End");
        }

        private string GetVMSize(string vmsize)
        {
            Dictionary<string, string> VMSizeTable = new Dictionary<string, string>();
            VMSizeTable.Add("ExtraSmall", "Standard_A0");
            VMSizeTable.Add("Small", "Standard_A1");
            VMSizeTable.Add("Medium", "Standard_A2");
            VMSizeTable.Add("Large", "Standard_A3");
            VMSizeTable.Add("ExtraLarge", "Standard_A4");
            VMSizeTable.Add("A5", "Standard_A5");
            VMSizeTable.Add("A6", "Standard_A6");
            VMSizeTable.Add("A7", "Standard_A7");
            VMSizeTable.Add("A8", "Standard_A8");
            VMSizeTable.Add("A9", "Standard_A9");
            VMSizeTable.Add("A10", "Standard_A10");
            VMSizeTable.Add("A11", "Standard_A11");

            if (VMSizeTable.ContainsKey(vmsize))
            {
                return VMSizeTable[vmsize];
            }
            else
            {
                return vmsize;
            }
        }

        public JObject GetTemplate()
        {
            if (!TemplateStreams.ContainsKey("export.json"))
                return null;

            MemoryStream templateStream = TemplateStreams["export.json"];
            templateStream.Position = 0;
            StreamReader sr = new StreamReader(templateStream);
            String myStr = sr.ReadToEnd();

            return JObject.Parse(myStr);
        }

        private async Task<string> GetTemplateString()
        {
            Template template = new Template()
            {
                resources = this.Resources,
                parameters = this.Parameters
            };

            // save JSON template
            string jsontext = JsonConvert.SerializeObject(template, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            jsontext = jsontext.Replace("schemalink", "$schema");

            return jsontext;
        }

        public string GetCopyBlobDetailPath()
        {
            return Path.Combine(this.OutputDirectory, "copyblobdetails.json");
        }


        public string GetTemplatePath()
        {
            return Path.Combine(this.OutputDirectory, "export.json");
        }

        public string GetInstructionPath()
        {
            return Path.Combine(this.OutputDirectory, "DeployInstructions.html");
        }

        public bool OutputFilesExist()
        {
            return File.Exists(GetInstructionPath()) || File.Exists(GetTemplatePath()) || File.Exists(GetInstructionPath());
        }
    }
}