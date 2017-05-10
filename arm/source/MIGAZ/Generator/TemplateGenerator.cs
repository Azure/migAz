using MIGAZ.Models;
using MIGAZ.Models.ARM;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MIGAZ.Generator
{
    public class TemplateGenerator
    {
        private ILogProvider _logProvider;
        private IStatusProvider _statusProvider;
        private ITelemetryProvider _telemetryProvider;
        private ITokenProvider _tokenProvider;
        private AsmRetriever _asmRetriever;
        private ISettingsProvider _settingsProvider;
        private List<Resource> _resources;
        private Dictionary<string, Parameter> _parameters;
        private List<CopyBlobDetail> _copyBlobDetails;
        private Dictionary<string, string> _processedItems;
        public Dictionary<string, string> _storageAccountNames;
        private List<string> _messages;

        public TemplateGenerator(ILogProvider logProvider, IStatusProvider statusProvider, ITelemetryProvider telemetryProvider, 
            ITokenProvider tokenProvider, AsmRetriever asmRetriever, ISettingsProvider settingsProvider)
        {
            _logProvider = logProvider;
            _statusProvider = statusProvider;
            _telemetryProvider = telemetryProvider;
            _tokenProvider = tokenProvider;
            _asmRetriever = asmRetriever;
            _settingsProvider = settingsProvider;
        }
        public List<string> GenerateTemplate(string tenantId, string subscriptionId, AsmArtefacts artefacts, StreamWriter templateWriter, StreamWriter blobDetailWriter)
        {
            _logProvider.WriteLog("GenerateTemplate", "Start");

            _settingsProvider.ExecutionId = Guid.NewGuid().ToString();
            _resources = new List<Resource>();
            _parameters = new Dictionary<string, Parameter>();
            _messages = new List<string>();
            _processedItems = new Dictionary<string, string>();
            _copyBlobDetails = new List<CopyBlobDetail>();
            _storageAccountNames = new Dictionary<string, string>();
            
            var token = _tokenProvider.GetToken(tenantId);

            _logProvider.WriteLog("GenerateTemplate", "Start processing selected virtual networks");
            // process selected virtual networks
            foreach (var virtualnetworkname in artefacts.VirtualNetworks)
            {
                _statusProvider.UpdateStatus("BUSY: Exporting Virtual Network : " + virtualnetworkname);

                //Listing Virtual Network
                var NWList = _asmRetriever.GetAzureARMResources("VirtualNetworks", subscriptionId, null, token, virtualnetworkname.RGName);
                var NWresults = JsonConvert.DeserializeObject<dynamic>(NWList);

                foreach (var vnet in NWresults.value)
                {
                    if (vnet.name == virtualnetworkname.VirtualNWName)
                    { 
                        string vnetName = vnet.name;
                        BuildARMVirtualNetworkObject(subscriptionId, vnet, token);
                    }
                    
                }

            }
            _logProvider.WriteLog("GenerateTemplate", "End processing selected virtual networks");

            _logProvider.WriteLog("GenerateTemplate", "Start processing selected storage accounts");

            // process selected storage accounts
            foreach (var storageaccountname in artefacts.StorageAccounts)
            {
                _statusProvider.UpdateStatus("BUSY: Exporting Storage Account : " + storageaccountname.StorageName);

                Hashtable storageaccountinfo = new Hashtable();
                storageaccountinfo.Add("name", storageaccountname.StorageName);

                var storageaccountdet = _asmRetriever.GetAzureARMResources("StorageAccount", subscriptionId, storageaccountinfo, token, storageaccountname.RGName);
                var storageaccount = JsonConvert.DeserializeObject<dynamic>(storageaccountdet);
                BuildARMStorageAccountObject(storageaccount);
            }
            _logProvider.WriteLog("GenerateTemplate", "End processing selected storage accounts");

            _logProvider.WriteLog("GenerateTemplate", "Start processing selected cloud services and virtual machines");

            // process selected cloud services and virtual machines
            foreach (var VMv2 in artefacts.VirtualMachines)
            {
               // string deploymentName;
                string virtualNetworkName;
//                string loadBalancerName;

               _asmRetriever.GetARMVMDetails(subscriptionId, VMv2.RGName, VMv2.VirtualMachine, token, out virtualNetworkName);

                Hashtable VMInfo = new Hashtable();
                VMInfo.Add("virtualmachineName", VMv2.VirtualMachine);


                //Listing Virtual Network
                var VMDetails = _asmRetriever.GetAzureARMResources("VirtualMachine", subscriptionId, VMInfo, token, VMv2.RGName);
                var VMResults = JsonConvert.DeserializeObject<dynamic>(VMDetails);

                string location = VMResults.location;
            
                
                _statusProvider.UpdateStatus("BUSY: Exporting VM Config : " + VMv2.VirtualMachine);

                //LoadBalancer Processing
                foreach (var nicint in VMResults.properties.networkProfile.networkInterfaces)
                {

                    string nicId = nicint.id;
                    nicId = nicId.Replace("/subscriptions", "subscriptions");

                    Hashtable NWInfo = new Hashtable();
                    NWInfo.Add("networkinterfaceId", nicId);

                    //Get NW Interface details to check Loadbalancer Config
                    var NicDetails = _asmRetriever.GetAzureARMResources("NetworkInterface", subscriptionId, NWInfo, token, null);
                    var NicResults = JsonConvert.DeserializeObject<dynamic>(NicDetails);

                    if (NicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools != null)
                    {

                        string[] stringSeparators = new string[] { "/backendAddressPools/" };

                        string NatRuleID = NicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools[0].id;
                        string Loadbalancerid = NatRuleID.Split(stringSeparators, StringSplitOptions.None)[0].Replace("/subscriptions", "subscriptions");
                        NWInfo.Add("LBId", Loadbalancerid);

                        //Get LBDetails
                        var LBDetails = _asmRetriever.GetAzureARMResources("Loadbalancer", subscriptionId, NWInfo, token, null);
                        var LBResults = JsonConvert.DeserializeObject<dynamic>(LBDetails);

                        string PubIPName = null;

                        //Process the Public IP for the Loadbalancer
                        if (LBResults.properties.frontendIPConfigurations[0].properties.publicIPAddress != null)
                        {
                            //Get PublicIP details
                            string PubId = LBResults.properties.frontendIPConfigurations[0].properties.publicIPAddress.id;
                            PubId = PubId.Replace("/subscriptions", "subscriptions");

                            NWInfo.Add("publicipId", PubId);
                            var LBPubIpDetails = _asmRetriever.GetAzureARMResources("PublicIP", subscriptionId, NWInfo, token, null);
                            var LBPubIpResults = JsonConvert.DeserializeObject<dynamic>(LBPubIpDetails);

                            PubIPName = LBPubIpResults.name;

                            //Build the Public IP for the Loadbalancer
                            BuildARMPublicIPAddressObject(LBResults, LBPubIpResults);
                           
                        }

                        //Build the Loadbalancer
                        BuildARMLoadBalancerObject(subscriptionId, LBResults, artefacts, token, PubIPName);


                    }


                }

                Hashtable virtualmachineinfo = new Hashtable();
                virtualmachineinfo.Add("ResourceGroup", VMv2.RGName);
                virtualmachineinfo.Add("virtualmachinename", VMv2.VirtualMachine);
                virtualmachineinfo.Add("virtualnetworkname", virtualNetworkName);
                virtualmachineinfo.Add("location", location);

               
                // process availability set
                BuildARMAvailabilitySetObject(subscriptionId,VMResults, virtualmachineinfo, token);

                // process network interface
                List<NetworkProfile_NetworkInterface> networkinterfaces = new List<NetworkProfile_NetworkInterface>();
                BuildARMNetworkInterfaceObject(subscriptionId, VMResults, virtualmachineinfo, ref networkinterfaces, token);

                // process virtual machine
                BuildARMVirtualMachineObject(subscriptionId, VMResults, virtualmachineinfo, networkinterfaces, token, artefacts);
            }
            _logProvider.WriteLog("GenerateTemplate", "End processing selected cloud services and virtual machines");

            Template template = new Template();
            template.resources = _resources;
            template.parameters = _parameters;

            // save JSON template
            _statusProvider.UpdateStatus("BUSY: saving JSON template");
            string jsontext = JsonConvert.SerializeObject(template, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            jsontext = jsontext.Replace("schemalink", "$schema");
            WriteStream(templateWriter, jsontext);
            _logProvider.WriteLog("GenerateTemplate", "Write file export.json");

            // save blob copy details file
            _statusProvider.UpdateStatus("BUSY: saving blob copy details file");
            jsontext = JsonConvert.SerializeObject(_copyBlobDetails, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            WriteStream(blobDetailWriter, jsontext);
            _logProvider.WriteLog("GenerateTemplate", "Write file copyblobdetails.json");

            // post Telemetry Record to ASMtoARMToolAPI
            if (_settingsProvider.AllowTelemetry)
            {
                var subscriptionsdet = _asmRetriever.GetAzureARMResources("Subscriptions", subscriptionId, null, token,null);
                var subscriptions = JsonConvert.DeserializeObject<dynamic>(subscriptionsdet);
                string offercategories = "";

                foreach (var subscription in subscriptions.value)
                {
                    if (subscription.subscriptionId == subscriptionId)
                    {
                        //OfferCat is set to TestARM for testing purpose
                        offercategories = "TestARM";
                    }
                }

                _statusProvider.UpdateStatus("BUSY: saving telemetry information");
               _telemetryProvider.PostTelemetryRecord(tenantId, subscriptionId, _processedItems, offercategories);
            }

            _statusProvider.UpdateStatus("Ready");

            _logProvider.WriteLog("GenerateTemplate", "End");
            return _messages;
        }


        private void BuildARMPublicIPAddressObject(ref NetworkInterface networkinterface, dynamic publicip)
        {
            _logProvider.WriteLog("BuildPublicIPAddressObject", "Start");

            string publicipaddress_name = publicip.name;
            string publicipallocationmethod = publicip.properties.publicIPAllocationMethod;

            Hashtable dnssettings = new Hashtable();
            dnssettings.Add("domainNameLabel", (publicipaddress_name + _settingsProvider.UniquenessSuffix).ToLower());

            PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
            publicipaddress_properties.dnsSettings = dnssettings;
            publicipaddress_properties.publicIPAllocationMethod = publicipallocationmethod;

            PublicIPAddress publicipaddress = new PublicIPAddress();
            publicipaddress.name = publicip.name;
            publicipaddress.location = publicip.location;
            publicipaddress.properties = publicipaddress_properties;

            try // it fails if this public ip address was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Network/publicIPAddresses/" + publicipaddress.name, publicipaddress.location);
                _resources.Add(publicipaddress);
                _logProvider.WriteLog("BuildPublicIPAddressObject", "Microsoft.Network/publicIPAddresses/" + publicipaddress.name);
            }
            catch { }

            NetworkInterface_Properties networkinterface_properties = (NetworkInterface_Properties)networkinterface.properties;
            networkinterface_properties.ipConfigurations[0].properties.publicIPAddress = new Reference();
            networkinterface_properties.ipConfigurations[0].properties.publicIPAddress.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + publicipaddress.name + "')]";
            networkinterface.properties = networkinterface_properties;


            networkinterface.dependsOn.Add(networkinterface_properties.ipConfigurations[0].properties.publicIPAddress.id);
            _logProvider.WriteLog("BuildPublicIPAddressObject", "End");
        }

        private void BuildPublicIPAddressObject(ref NetworkInterface networkinterface)
        {
            _logProvider.WriteLog("BuildPublicIPAddressObject", "Start");

            PublicIPAddress publicipaddress = new PublicIPAddress();
            publicipaddress.name = networkinterface.name;
            publicipaddress.location = networkinterface.location;
            publicipaddress.properties = new PublicIPAddress_Properties();

            try // it fails if this public ip address was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Network/publicIPAddresses/" + publicipaddress.name, publicipaddress.location);
                _resources.Add(publicipaddress);
                _logProvider.WriteLog("BuildPublicIPAddressObject", "Microsoft.Network/publicIPAddresses/" + publicipaddress.name);
            }
            catch { }

            NetworkInterface_Properties networkinterface_properties = (NetworkInterface_Properties)networkinterface.properties;
            networkinterface_properties.ipConfigurations[0].properties.publicIPAddress = new Reference();
            networkinterface_properties.ipConfigurations[0].properties.publicIPAddress.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + publicipaddress.name + "')]";
            networkinterface.properties = networkinterface_properties;

            networkinterface.dependsOn.Add(networkinterface_properties.ipConfigurations[0].properties.publicIPAddress.id);
            _logProvider.WriteLog("BuildPublicIPAddressObject", "End");
        }



        private void BuildPublicIPAddressObject(XmlNode resource, string loadbalancername, string cloudservicename, XmlDocument reservedips)
        {
            _logProvider.WriteLog("BuildPublicIPAddressObject", "Start");

            string publicipaddress_name = loadbalancername;

            string publicipallocationmethod = "Dynamic";
            foreach (XmlNode reservedip in reservedips.SelectNodes("/ReservedIPs/ReservedIP"))
            {
                if (reservedip.SelectNodes("ServiceName").Count > 0)
                {
                    if (reservedip.SelectSingleNode("ServiceName").InnerText == cloudservicename)
                    {
                        publicipallocationmethod = "Static";
                    }
                }
            }

            Hashtable dnssettings = new Hashtable();
            dnssettings.Add("domainNameLabel", (publicipaddress_name + _settingsProvider.UniquenessSuffix).ToLower());

            PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
            publicipaddress_properties.dnsSettings = dnssettings;
            publicipaddress_properties.publicIPAllocationMethod = publicipallocationmethod;

            PublicIPAddress publicipaddress = new PublicIPAddress();
            publicipaddress.name = publicipaddress_name + "-PIP";
            publicipaddress.location = resource.SelectSingleNode("//HostedServiceProperties/ExtendedProperties/ExtendedProperty[Name='ResourceLocation']/Value").InnerText;
            publicipaddress.properties = publicipaddress_properties;

            try // it fails if this public ip address was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Network/publicIPAddresses/" + publicipaddress.name, publicipaddress.location);
                _resources.Add(publicipaddress);
                _logProvider.WriteLog("BuildPublicIPAddressObject", "Microsoft.Network/publicIPAddresses/" + publicipaddress.name);
            }
            catch { }

            _logProvider.WriteLog("BuildPublicIPAddressObject", "End");
        }


        private void BuildARMPublicIPAddressObject(dynamic resource, dynamic publicip)
        {
            _logProvider.WriteLog("BuildPublicIPAddressObject", "Start");

            string publicipaddress_name = publicip.name;
            string publicipallocationmethod = publicip.properties.publicIPAllocationMethod;


            Hashtable dnssettings = new Hashtable();
            dnssettings.Add("domainNameLabel", (publicipaddress_name + _settingsProvider.UniquenessSuffix).ToLower());

            PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
            publicipaddress_properties.dnsSettings = dnssettings;
            publicipaddress_properties.publicIPAllocationMethod = publicipallocationmethod;

            PublicIPAddress publicipaddress = new PublicIPAddress();
            publicipaddress.name = publicipaddress_name;
            publicipaddress.location = publicip.location;
            publicipaddress.properties = publicipaddress_properties;

            try // it fails if this public ip address was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Network/publicIPAddresses/" + publicipaddress.name, publicipaddress.location);
                _resources.Add(publicipaddress);
                _logProvider.WriteLog("BuildPublicIPAddressObject", "Microsoft.Network/publicIPAddresses/" + publicipaddress.name);
            }
            catch { }

            _logProvider.WriteLog("BuildPublicIPAddressObject", "End");
        }
        private void BuildARMAvailabilitySetObject(string subscriptionId, dynamic virtualmachine, Hashtable virtualmachineinfo,string token)
        {
            _logProvider.WriteLog("BuildAvailabilitySetObject", "Start");

            if (virtualmachine.properties.availabilitySet != null)
            {
                AvailabilitySet availabilityset = new AvailabilitySet();

                Hashtable availabilitySetinfo = new Hashtable();
                string AVId = virtualmachine.properties.availabilitySet.id;
                AVId = AVId.Replace("/subscriptions", "subscriptions");
                availabilitySetinfo.Add("AvailId", AVId);

                var AvailList = _asmRetriever.GetAzureARMResources("AvailabilitySet", subscriptionId, availabilitySetinfo, token, null);
                var Availresults = JsonConvert.DeserializeObject<dynamic>(AvailList);

                availabilityset.name = Availresults.name;
                availabilityset.location = Availresults.location;
                
                try // it fails if this availability set was already processed. safe to continue.
                {
                    _processedItems.Add("Microsoft.Compute/availabilitySets/" + availabilityset.name, availabilityset.location);
                    _resources.Add(availabilityset);
                    _logProvider.WriteLog("BuildAvailabilitySetObject", "Microsoft.Compute/availabilitySets/" + availabilityset.name);
                }
                catch { }
            }
            

            _logProvider.WriteLog("BuildAvailabilitySetObject", "End");
        }


        private void BuildAvailabilitySetObject(XmlNode virtualmachine, Hashtable virtualmachineinfo)
        {
            _logProvider.WriteLog("BuildAvailabilitySetObject", "Start");

            string virtualmachinename = virtualmachineinfo["virtualmachinename"].ToString();
            string cloudservicename = virtualmachineinfo["cloudservicename"].ToString();

            AvailabilitySet availabilityset = new AvailabilitySet();

            availabilityset.name = cloudservicename + "-defaultAS";
            if (virtualmachine.SelectSingleNode("//AvailabilitySetName") != null)
            {
                availabilityset.name = virtualmachine.SelectSingleNode("//AvailabilitySetName").InnerText;
            }
            availabilityset.location = virtualmachineinfo["location"].ToString();
            try // it fails if this availability set was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Compute/availabilitySets/" + availabilityset.name, availabilityset.location);
                _resources.Add(availabilityset);
                _logProvider.WriteLog("BuildAvailabilitySetObject", "Microsoft.Compute/availabilitySets/" + availabilityset.name);
            }
            catch { }

            _logProvider.WriteLog("BuildAvailabilitySetObject", "End");
        }

        private void BuildARMLoadBalancerObject(string subscriptionId, dynamic LBResource, AsmArtefacts artefacts, string token,string PubIpname)
        {
            _logProvider.WriteLog("BuildLoadBalancerObject", "Start");

            LoadBalancer loadbalancer = new LoadBalancer();
            loadbalancer.name = LBResource.name;
            loadbalancer.location = LBResource.location;

            FrontendIPConfiguration_Properties frontendipconfiguration_properties = new FrontendIPConfiguration_Properties();

            // if internal load balancer
            if (LBResource.properties.frontendIPConfigurations[0].properties.privateIPAddress != null)
            {

                string subnetid = LBResource.properties.frontendIPConfigurations[0].properties.subnet.id;
                string virtualnetworkname = subnetid.Split(new char[] { '/' })[8].ToString(); 
                string subnetname = subnetid.Split(new char[] { '/' })[10].ToString();

                frontendipconfiguration_properties.privateIPAllocationMethod = LBResource.properties.frontendIPConfigurations[0].properties.privateIPAllocationMethod;
                frontendipconfiguration_properties.privateIPAddress = LBResource.properties.frontendIPConfigurations[0].properties.privateIPAddress;
               
                List<string> dependson = new List<string>();
                if (GetProcessedItem("Microsoft.Network/virtualNetworks/" + virtualnetworkname))
                {
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "')]");
                }
                loadbalancer.dependsOn = dependson;

                Reference subnet_ref = new Reference();
                subnet_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "/subnets/" + subnetname + "')]";
                frontendipconfiguration_properties.subnet = subnet_ref;
            }
            // if external load balancer
            else
            {
                List<string> dependson = new List<string>();
                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + PubIpname + "')]");
                loadbalancer.dependsOn = dependson;

                Reference publicipaddress_ref = new Reference();
                publicipaddress_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + PubIpname + "')]";
                frontendipconfiguration_properties.publicIPAddress = publicipaddress_ref;
            }


            LoadBalancer_Properties loadbalancer_properties = new LoadBalancer_Properties();

            FrontendIPConfiguration frontendipconfiguration = new FrontendIPConfiguration();
            frontendipconfiguration.properties = frontendipconfiguration_properties;
            frontendipconfiguration.name = LBResource.properties.frontendIPConfigurations[0].name;

            List <FrontendIPConfiguration> frontendipconfigurations = new List<FrontendIPConfiguration>();
            frontendipconfigurations.Add(frontendipconfiguration);
            loadbalancer_properties.frontendIPConfigurations = frontendipconfigurations;

            Hashtable backendaddresspool = new Hashtable();
            backendaddresspool.Add("name", LBResource.properties.backendAddressPools[0].name);
            List<Hashtable> backendaddresspools = new List<Hashtable>();
            backendaddresspools.Add(backendaddresspool);
            loadbalancer_properties.backendAddressPools = backendaddresspools;

            List<InboundNatRule> inboundnatrules = new List<InboundNatRule>();
            List<LoadBalancingRule> loadbalancingrules = new List<LoadBalancingRule>();
            List<Probe> probes = new List<Probe>();
            BuildARMLoadBalancerRules(LBResource, ref inboundnatrules, ref loadbalancingrules, ref probes);


            //foreach (var cloudServiceVM in artefacts.VirtualMachines)
            //{
            //    string deploymentName;
            //    string virtualNetworkName;
            //    string loadBalancerName = "";
            //    //    _asmRetriever.GetVMDetails(subscriptionId, cloudServiceVM.CloudService, cloudServiceVM.VirtualMachine, token, out deploymentName, out virtualNetworkName, out loadBalancerName);


            //    if (loadBalancerName == loadbalancer.name)
            //    {
            //        //process VM

            //        Hashtable virtualmachineinfo = new Hashtable();
            //        //     virtualmachineinfo.Add("cloudservicename", cloudServiceVM.CloudService);
            //        //   virtualmachineinfo.Add("deploymentname", deploymentName);
            //        virtualmachineinfo.Add("virtualmachinename", cloudServiceVM.VirtualMachine);
            //        XmlDocument virtualmachine = _asmRetriever.GetAzureASMResources("VirtualMachine", subscriptionId, virtualmachineinfo, token);

            //        BuildLoadBalancerRules(virtualmachine, loadbalancer.name, ref inboundnatrules, ref loadbalancingrules, ref probes);
            //    }
            //}

            loadbalancer_properties.inboundNatRules = inboundnatrules;
            loadbalancer_properties.loadBalancingRules = loadbalancingrules;
            loadbalancer_properties.probes = probes;
            loadbalancer.properties = loadbalancer_properties;

            // Add the load balancer only if there is any Load Balancing rule or Inbound NAT rule
            if (inboundnatrules.Count > 0 || loadbalancingrules.Count > 0)
            {
                try // it fails if this load balancer was already processed. safe to continue.
                {
                    _processedItems.Add("Microsoft.Network/loadBalancers/" + loadbalancer.name, loadbalancer.location);
                    _resources.Add(loadbalancer);
                    _logProvider.WriteLog("BuildLoadBalancerObject", "Microsoft.Network/loadBalancers/" + loadbalancer.name);
                }
                catch { }
            }
            else
            {
                _logProvider.WriteLog("BuildLoadBalancerObject", "EMPTY Microsoft.Network/loadBalancers/" + loadbalancer.name);
            }

            _logProvider.WriteLog("BuildLoadBalancerObject", "End");
        }

        private void BuildLoadBalancerObject(string subscriptionId, XmlNode cloudservice, string loadbalancername, AsmArtefacts artefacts, string token)
        {
            _logProvider.WriteLog("BuildLoadBalancerObject", "Start");

            LoadBalancer loadbalancer = new LoadBalancer();
            loadbalancer.name = loadbalancername;
            loadbalancer.location = cloudservice.SelectSingleNode("//HostedServiceProperties/ExtendedProperties/ExtendedProperty[Name='ResourceLocation']/Value").InnerText;

            FrontendIPConfiguration_Properties frontendipconfiguration_properties = new FrontendIPConfiguration_Properties();

            // if internal load balancer
            if (cloudservice.SelectNodes("//Deployments/Deployment/LoadBalancers/LoadBalancer/FrontendIpConfiguration/Type").Count > 0)
            {
                string virtualnetworkname = cloudservice.SelectSingleNode("//Deployments/Deployment/VirtualNetworkName").InnerText;
                string subnetname = cloudservice.SelectSingleNode("//Deployments/Deployment/LoadBalancers/LoadBalancer/FrontendIpConfiguration/SubnetName").InnerText.Replace(" ", "");

                frontendipconfiguration_properties.privateIPAllocationMethod = "Dynamic";
                if (cloudservice.SelectNodes("//Deployments/Deployment/LoadBalancers/LoadBalancer/FrontendIpConfiguration/StaticVirtualNetworkIPAddress").Count > 0)
                {
                    frontendipconfiguration_properties.privateIPAllocationMethod = "Static";
                    frontendipconfiguration_properties.privateIPAddress = cloudservice.SelectSingleNode("//Deployments/Deployment/LoadBalancers/LoadBalancer/FrontendIpConfiguration/StaticVirtualNetworkIPAddress").InnerText;
                }

                List<string> dependson = new List<string>();
                if (GetProcessedItem("Microsoft.Network/virtualNetworks/" + virtualnetworkname))
                {
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "')]");
                }
                loadbalancer.dependsOn = dependson;

                Reference subnet_ref = new Reference();
                subnet_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "/subnets/" + subnetname + "')]";
                frontendipconfiguration_properties.subnet = subnet_ref;
            }
            // if external load balancer
            else
            {
                List<string> dependson = new List<string>();
                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + loadbalancer.name + "-PIP')]");
                loadbalancer.dependsOn = dependson;

                Reference publicipaddress_ref = new Reference();
                publicipaddress_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + loadbalancer.name + "-PIP')]";
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

            foreach (var cloudServiceVM in artefacts.VirtualMachines)
            {
                string loadBalancerName = "";
            //    _asmRetriever.GetVMDetails(subscriptionId, cloudServiceVM.CloudService, cloudServiceVM.VirtualMachine, token, out deploymentName, out virtualNetworkName, out loadBalancerName);


                if (loadBalancerName == loadbalancer.name)
                {
                    //process VM
 
                    Hashtable virtualmachineinfo = new Hashtable();
               //     virtualmachineinfo.Add("cloudservicename", cloudServiceVM.CloudService);
                 //   virtualmachineinfo.Add("deploymentname", deploymentName);
                    virtualmachineinfo.Add("virtualmachinename", cloudServiceVM.VirtualMachine);
                    XmlDocument virtualmachine = _asmRetriever.GetAzureASMResources("VirtualMachine", subscriptionId, virtualmachineinfo, token);

                    BuildLoadBalancerRules(virtualmachine, loadbalancer.name, ref inboundnatrules, ref loadbalancingrules, ref probes);
                }
            }

            loadbalancer_properties.inboundNatRules = inboundnatrules;
            loadbalancer_properties.loadBalancingRules = loadbalancingrules;
            loadbalancer_properties.probes = probes;
            loadbalancer.properties = loadbalancer_properties;

            // Add the load balancer only if there is any Load Balancing rule or Inbound NAT rule
            if (inboundnatrules.Count > 0 || loadbalancingrules.Count > 0)
            {
                try // it fails if this load balancer was already processed. safe to continue.
                {
                    _processedItems.Add("Microsoft.Network/loadBalancers/" + loadbalancer.name, loadbalancer.location);
                    _resources.Add(loadbalancer);
                    _logProvider.WriteLog("BuildLoadBalancerObject", "Microsoft.Network/loadBalancers/" + loadbalancer.name);
                }
                catch { }
            }
            else
            {
                _logProvider.WriteLog("BuildLoadBalancerObject", "EMPTY Microsoft.Network/loadBalancers/" + loadbalancer.name);
            }

            _logProvider.WriteLog("BuildLoadBalancerObject", "End");
        }

        private void BuildARMLoadBalancerRules(dynamic resource, ref List<InboundNatRule> inboundnatrules, ref List<LoadBalancingRule> loadbalancingrules, ref List<Probe> probes)
        {
            _logProvider.WriteLog("BuildLoadBalancerRules", "Start");

            string loadbalancername = resource.name;
            // string virtualmachinename = resource.SelectSingleNode("//RoleName").InnerText;
            
            //Probes Processing
            foreach (var ProbeRule in resource.properties.probes)
            {
                string name = ProbeRule.name;

                // build probe
                Probe_Properties probe_properties = new Probe_Properties();
                probe_properties.port = ProbeRule.properties.port;
                probe_properties.protocol = ProbeRule.properties.protocol;

                Probe probe = new Probe();
                probe.name = name;
                probe.properties = probe_properties;

                try // fails if this probe already exists. safe to continue
                {
                    _processedItems.Add("Microsoft.Network/loadBalancers/" + loadbalancername + "/probes/" + probe.name, "");
                    probes.Add(probe);
                }
                catch { }

            }

            //Processing InboundNATRules
            foreach (var NATRule in resource.properties.inboundNatRules)
            {
                InboundNatRule_Properties inboundnatrule_properties = new InboundNatRule_Properties();
                inboundnatrule_properties.frontendPort = NATRule.properties.frontendPort;
                inboundnatrule_properties.backendPort = NATRule.properties.backendPort;
                inboundnatrule_properties.protocol = NATRule.properties.protocol;

                Reference frontendIPConfiguration = new Reference();
                string frontendIPConfigurationName = NATRule.properties.backendIPConfiguration.id.ToString().Split(new char[] { '/' })[10].ToString();
                frontendIPConfiguration.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/frontendIPConfigurations/" + frontendIPConfigurationName + "')]";
                inboundnatrule_properties.frontendIPConfiguration = frontendIPConfiguration;

                InboundNatRule inboundnatrule = new InboundNatRule();
                inboundnatrule.name = NATRule.name;
                inboundnatrule.properties = inboundnatrule_properties;

                inboundnatrules.Add(inboundnatrule);
            }

            // build load balancing rule
            foreach (var LBRule in resource.properties.loadBalancingRules)
            {
                Reference frontendipconfiguration_ref = new Reference();
                string frontendIPConfigurationName = LBRule.properties.frontendIPConfiguration.id.ToString().Split(new char[] { '/' })[10].ToString();
                frontendipconfiguration_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/frontendIPConfigurations/" + frontendIPConfigurationName + "')]";

                Reference backendaddresspool_ref = new Reference();
                string backendAddressPoolName = LBRule.properties.backendAddressPool.id.ToString().Split(new char[] { '/' })[10].ToString();
                backendaddresspool_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/backendAddressPools/"+ backendAddressPoolName + "')]";

                Reference probe_ref = new Reference();
                string probeName = LBRule.properties.probe.id.ToString().Split(new char[] { '/' })[10].ToString();
                probe_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/probes/" + probeName + "')]";

                LoadBalancingRule_Properties loadbalancingrule_properties = new LoadBalancingRule_Properties();
                loadbalancingrule_properties.frontendIPConfiguration = frontendipconfiguration_ref;
                loadbalancingrule_properties.backendAddressPool = backendaddresspool_ref;
                loadbalancingrule_properties.probe = probe_ref;
                loadbalancingrule_properties.frontendPort = LBRule.properties.frontendPort;
                loadbalancingrule_properties.backendPort = LBRule.properties.backendPort;
                loadbalancingrule_properties.protocol = LBRule.properties.protocol;

                LoadBalancingRule loadbalancingrule = new LoadBalancingRule();
                loadbalancingrule.name = LBRule.name;
                loadbalancingrule.properties = loadbalancingrule_properties;

                try // fails if this load balancing rule already exists. safe to continue
                {
                    _processedItems.Add("Microsoft.Network/loadBalancers/" + loadbalancername + "/loadBalancingRules/" + loadbalancingrule.name, "");
                    loadbalancingrules.Add(loadbalancingrule);
                    _logProvider.WriteLog("BuildLoadBalancerRules", "Microsoft.Network/loadBalancers/" + loadbalancername + "/loadBalancingRules/" + loadbalancingrule.name);
                }
                catch { continue; }
            }

            _logProvider.WriteLog("BuildLoadBalancerRules", "End");
        }


        private void BuildLoadBalancerRules(XmlNode resource, string loadbalancername, ref List<InboundNatRule> inboundnatrules, ref List<LoadBalancingRule> loadbalancingrules, ref List<Probe> probes)
        {
            _logProvider.WriteLog("BuildLoadBalancerRules", "Start");

            string virtualmachinename = resource.SelectSingleNode("//RoleName").InnerText;

            foreach (XmlNode inputendpoint in resource.SelectNodes("//ConfigurationSets/ConfigurationSet/InputEndpoints/InputEndpoint"))
            {
                if (inputendpoint.SelectSingleNode("LoadBalancedEndpointSetName") == null) // if it's a inbound nat rule
                {
                    InboundNatRule_Properties inboundnatrule_properties = new InboundNatRule_Properties();
                    inboundnatrule_properties.frontendPort = Int64.Parse(inputendpoint.SelectSingleNode("Port").InnerText);
                    inboundnatrule_properties.backendPort = Int64.Parse(inputendpoint.SelectSingleNode("LocalPort").InnerText);
                    inboundnatrule_properties.protocol = inputendpoint.SelectSingleNode("Protocol").InnerText;

                    Reference frontendIPConfiguration = new Reference();
                    frontendIPConfiguration.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/frontendIPConfigurations/default')]";
                    inboundnatrule_properties.frontendIPConfiguration = frontendIPConfiguration;

                    InboundNatRule inboundnatrule = new InboundNatRule();
                    inboundnatrule.name = virtualmachinename + "-" + inputendpoint.SelectSingleNode("Name").InnerText;
                    inboundnatrule.name = inboundnatrule.name.Replace(" ", "");
                    inboundnatrule.properties = inboundnatrule_properties;

                    inboundnatrules.Add(inboundnatrule);
                }
                else // if it's a load balancing rule
                {
                    string name = inputendpoint.SelectSingleNode("LoadBalancedEndpointSetName").InnerText;
                    name = name.Replace(" ", "");
                    XmlNode probenode = inputendpoint.SelectSingleNode("LoadBalancerProbe");

                    // build probe
                    Probe_Properties probe_properties = new Probe_Properties();
                    probe_properties.port = Int64.Parse(probenode.SelectSingleNode("Port").InnerText);
                    probe_properties.protocol = probenode.SelectSingleNode("Protocol").InnerText;

                    Probe probe = new Probe();
                    probe.name = name;
                    probe.properties = probe_properties;

                    try // fails if this probe already exists. safe to continue
                    {
                        _processedItems.Add("Microsoft.Network/loadBalancers/" + loadbalancername + "/probes/" + probe.name, "");
                        probes.Add(probe);
                    }
                    catch { }

                    // build load balancing rule
                    Reference frontendipconfiguration_ref = new Reference();
                    frontendipconfiguration_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/frontendIPConfigurations/default')]";

                    Reference backendaddresspool_ref = new Reference();
                    backendaddresspool_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/backendAddressPools/default')]";

                    Reference probe_ref = new Reference();
                    probe_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/probes/" + probe.name + "')]";

                    LoadBalancingRule_Properties loadbalancingrule_properties = new LoadBalancingRule_Properties();
                    loadbalancingrule_properties.frontendIPConfiguration = frontendipconfiguration_ref;
                    loadbalancingrule_properties.backendAddressPool = backendaddresspool_ref;
                    loadbalancingrule_properties.probe = probe_ref;
                    loadbalancingrule_properties.frontendPort = Int64.Parse(inputendpoint.SelectSingleNode("Port").InnerText);
                    loadbalancingrule_properties.backendPort = Int64.Parse(inputendpoint.SelectSingleNode("LocalPort").InnerText);
                    loadbalancingrule_properties.protocol = inputendpoint.SelectSingleNode("Protocol").InnerText;

                    LoadBalancingRule loadbalancingrule = new LoadBalancingRule();
                    loadbalancingrule.name = name;
                    loadbalancingrule.properties = loadbalancingrule_properties;

                    try // fails if this load balancing rule already exists. safe to continue
                    {
                        _processedItems.Add("Microsoft.Network/loadBalancers/" + loadbalancername + "/loadBalancingRules/" + loadbalancingrule.name, "");
                        loadbalancingrules.Add(loadbalancingrule);
                        _logProvider.WriteLog("BuildLoadBalancerRules", "Microsoft.Network/loadBalancers/" + loadbalancername + "/loadBalancingRules/" + loadbalancingrule.name);
                    }
                    catch { continue; }
                }
            }

            _logProvider.WriteLog("BuildLoadBalancerRules", "End");
        }


        private void BuildNewVirtualNetworkObject(XmlNode resource, Hashtable info)
        {
            //string ipaddress = resource.SelectSingleNode("Deployments/Deployment/RoleInstanceList/RoleInstance/IpAddress").InnerText;
            //IPAddress ipaddressIP = IPAddress.Parse(ipaddress);
            //byte[] ipaddressBYTE = ipaddressIP.GetAddressBytes();

            //IPAddress subnetmaskIP = IPAddress.Parse("255.255.254.0");
            //byte[] subnetmaskBYTE = subnetmaskIP.GetAddressBytes();

            //byte[] addressprefixBYTE = new byte[4];
            //addressprefixBYTE[0] = (byte)((byte)ipaddressBYTE[0] & (byte)subnetmaskBYTE[0]);
            //addressprefixBYTE[1] = (byte)((byte)ipaddressBYTE[1] & (byte)subnetmaskBYTE[1]);
            //addressprefixBYTE[2] = (byte)((byte)ipaddressBYTE[2] & (byte)subnetmaskBYTE[2]);
            //addressprefixBYTE[3] = (byte)((byte)ipaddressBYTE[3] & (byte)subnetmaskBYTE[3]);

            //string addressprefix = "";
            //addressprefix += addressprefixBYTE[0].ToString() + ".";
            //addressprefix += addressprefixBYTE[1].ToString() + ".";
            //addressprefix += addressprefixBYTE[2].ToString() + ".";
            //addressprefix += addressprefixBYTE[3].ToString();

            _logProvider.WriteLog("BuildNewVirtualNetworkObject", "Start");

            List<string> addressprefixes = new List<string>();
            addressprefixes.Add("192.168.0.0/23");

            AddressSpace addressspace = new AddressSpace();
            addressspace.addressPrefixes = addressprefixes;

            VirtualNetwork virtualnetwork = new VirtualNetwork();
            virtualnetwork.name = info["cloudservicename"].ToString() + "-VNET";
            virtualnetwork.location = resource.SelectSingleNode("//HostedServiceProperties/ExtendedProperties/ExtendedProperty[Name='ResourceLocation']/Value").InnerText;

            List<Subnet> subnets = new List<Subnet>();
            Subnet_Properties properties = new Subnet_Properties();
            properties.addressPrefix = "192.168.0.0/23";

            Subnet subnet = new Subnet();
            subnet.name = "Subnet1";
            subnet.properties = properties;

            subnets.Add(subnet);

            VirtualNetwork_Properties virtualnetwork_properties = new VirtualNetwork_Properties();
            virtualnetwork_properties.addressSpace = addressspace;
            virtualnetwork_properties.subnets = subnets;

            virtualnetwork.properties = virtualnetwork_properties;

            try
            {
                _processedItems.Add("Microsoft.Network/virtualNetworks/" + virtualnetwork.name, virtualnetwork.location);
                _resources.Add(virtualnetwork);
                _logProvider.WriteLog("BuildNewVirtualNetworkObject", "Microsoft.Network/virtualNetworks/" + virtualnetwork.name);
            }
            catch { }

            _logProvider.WriteLog("BuildNewVirtualNetworkObject", "End");
        }


        private void BuildARMVirtualNetworkObject(string subscriptionId, dynamic resource, string token)
        {
            _logProvider.WriteLog("BuildVirtualNetworkObject", "Start");

            List<string> dependson = new List<string>();
            
            List<string> addressprefixes = new List<string>();

            foreach(var addressprefix in resource.properties.addressSpace.addressPrefixes)
            {
                addressprefixes.Add(addressprefix.Value);
            }
            

            AddressSpace addressspace = new AddressSpace();
            addressspace.addressPrefixes = addressprefixes;

            List<string> dnsservers = new List<string>();

            if (resource.properties.dhcpOptions != null)
            {
                foreach (var dnsserver in resource.properties.dhcpOptions.dnsServers)
                {
                    dnsservers.Add(dnsserver.Value);
                }
            }

            VirtualNetwork_dhcpOptions dhcpoptions = new VirtualNetwork_dhcpOptions();
            dhcpoptions.dnsServers = dnsservers;

            VirtualNetwork virtualnetwork = new VirtualNetwork();

            virtualnetwork.name = resource.name;
            virtualnetwork.location = resource.location;

            virtualnetwork.dependsOn = dependson;
            List<Subnet> subnets = new List<Subnet>();

            if ((resource.properties.subnets).Count == 0)
            {
                Subnet_Properties properties = new Subnet_Properties();
                properties.addressPrefix = addressprefixes[0];

                Subnet subnet = new Subnet();
                subnet.name = "Subnet1";
                subnet.properties = properties;

                subnets.Add(subnet);
                _messages.Add($"VNET '{virtualnetwork.name}' has no subnets defined. We've created a default subnet 'Subnet1' covering the entire address space.");
            }
            else
            {
                foreach (var subnetdet in resource.properties.subnets)
                {
                    Subnet_Properties properties = new Subnet_Properties();
                    properties.addressPrefix = subnetdet.properties.addressPrefix.Value;

                    Subnet subnet = new Subnet();
                    subnet.name = subnetdet.name;
                    subnet.properties = properties;
                    subnets.Add(subnet);

                    //NSG Setup - Single NSG per subnet
                    if (subnetdet.properties.networkSecurityGroup != null)
                    {
                        NetworkSecurityGroup networksecuritygroup = BuildARMNetworkSecurityGroup(subscriptionId, subnetdet.properties.networkSecurityGroup.id.Value, token);

                        // Add NSG reference to the subnet
                        Reference networksecuritygroup_ref = new Reference();
                        networksecuritygroup_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name + "')]";

                        properties.networkSecurityGroup = networksecuritygroup_ref;

                        // Add NSG dependsOn to the Virtual Network object
                        if (!virtualnetwork.dependsOn.Contains(networksecuritygroup_ref.id))
                        {
                            virtualnetwork.dependsOn.Add(networksecuritygroup_ref.id);
                        }
                    }

                    // add Route Table if exists
                    if (subnetdet.properties.routeTable != null)
                    {
                        RouteTable routetable = BuildARMRouteTable(subscriptionId, subnetdet.properties.routeTable.id.Value, token);

                        // Add Route Table reference to the subnet
                        Reference routetable_ref = new Reference();
                        routetable_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/routeTables/" + routetable.name + "')]";

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

            _processedItems.Add("Microsoft.Network/virtualNetworks/" + virtualnetwork.name, virtualnetwork.location);
            _resources.Add(virtualnetwork);
            _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/virtualNetworks/" + virtualnetwork.name);

            AddGatewaysToVirtualNetworkARM(subscriptionId, resource, token, virtualnetwork);

            _logProvider.WriteLog("BuildVirtualNetworkObject", "End");
        }

        private void BuildVirtualNetworkObject(string subscriptionId, XmlNode resource, string token)
        {
            _logProvider.WriteLog("BuildVirtualNetworkObject", "Start");

            List<string> dependson = new List<string>();

            List<string> addressprefixes = new List<string>();
            foreach (XmlNode addressprefix in resource.SelectNodes("AddressSpace/AddressPrefixes"))
            {
                addressprefixes.Add(addressprefix.SelectSingleNode("AddressPrefix").InnerText);
            }

            AddressSpace addressspace = new AddressSpace();
            addressspace.addressPrefixes = addressprefixes;

            List<string> dnsservers = new List<string>();
            foreach (XmlNode dnsserver in resource.SelectNodes("Dns/DnsServers/DnsServer"))
            {
                dnsservers.Add(dnsserver.SelectSingleNode("Address").InnerText);
            }

            VirtualNetwork_dhcpOptions dhcpoptions = new VirtualNetwork_dhcpOptions();
            dhcpoptions.dnsServers = dnsservers;

            VirtualNetwork virtualnetwork = new VirtualNetwork();
            virtualnetwork.name = resource.SelectSingleNode("Name").InnerText.Replace(" ", "");
            // get location if virtual network was not deployed in a affinity group
            if (resource.SelectNodes("Location").Count > 0)
            {
                virtualnetwork.location = resource.SelectSingleNode("Location").InnerText;
            }
            // get location if virtual network was deployed in a affinity group
            else
            {
                Hashtable affinitygroupinfo = new Hashtable();
                affinitygroupinfo.Add("affinitygroupname", resource.SelectSingleNode("AffinityGroup").InnerText);
                XmlDocument affinitygroup = _asmRetriever.GetAzureASMResources("AffinityGroup", subscriptionId, affinitygroupinfo, token);

                virtualnetwork.location = affinitygroup.SelectSingleNode("//Location").InnerText;
            }
            virtualnetwork.dependsOn = dependson;

            List<Subnet> subnets = new List<Subnet>();
            if (resource.SelectNodes("Subnets/Subnet").Count == 0)
            {
                Subnet_Properties properties = new Subnet_Properties();
                properties.addressPrefix = addressprefixes[0];

                Subnet subnet = new Subnet();
                subnet.name = "Subnet1";
                subnet.properties = properties;

                subnets.Add(subnet);
                _messages.Add($"VNET '{virtualnetwork.name}' has no subnets defined. We've created a default subnet 'Subnet1' covering the entire address space.");
            }
            else
            {
                foreach (XmlNode subnetnode in resource.SelectNodes("Subnets/Subnet"))
                {
                    Subnet_Properties properties = new Subnet_Properties();
                    properties.addressPrefix = subnetnode.SelectSingleNode("AddressPrefix").InnerText;

                    Subnet subnet = new Subnet();
                    subnet.name = subnetnode.SelectSingleNode("Name").InnerText.Replace(" ", "");
                    subnet.properties = properties;

                    subnets.Add(subnet);

                    // add Network Security Group if exists
                    if (subnetnode.SelectNodes("NetworkSecurityGroup").Count > 0)
                    {
                        NetworkSecurityGroup networksecuritygroup = BuildNetworkSecurityGroup(subscriptionId, subnetnode.SelectSingleNode("NetworkSecurityGroup").InnerText, token);

                        // Add NSG reference to the subnet
                        Reference networksecuritygroup_ref = new Reference();
                        networksecuritygroup_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name + "')]";

                        properties.networkSecurityGroup = networksecuritygroup_ref;

                        // Add NSG dependsOn to the Virtual Network object
                        if (!virtualnetwork.dependsOn.Contains(networksecuritygroup_ref.id))
                        {
                            virtualnetwork.dependsOn.Add(networksecuritygroup_ref.id);
                        }
                    }

                    // add Route Table if exists
                    if (subnetnode.SelectNodes("RouteTableName").Count > 0)
                    {
                        RouteTable routetable = BuildRouteTable(subscriptionId, subnetnode.SelectSingleNode("RouteTableName").InnerText, token);

                        // Add Route Table reference to the subnet
                        Reference routetable_ref = new Reference();
                        routetable_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/routeTables/" + routetable.name + "')]";

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

            _processedItems.Add("Microsoft.Network/virtualNetworks/" + virtualnetwork.name, virtualnetwork.location);
            _resources.Add(virtualnetwork);
            _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/virtualNetworks/" + virtualnetwork.name);

          //  AddGatewaysToVirtualNetworkARM(subscriptionId, resource, token, virtualnetwork);

            _logProvider.WriteLog("BuildVirtualNetworkObject", "End");
        }

        private void AddGatewaysToVirtualNetworkARM(string subscriptionId, dynamic resources, string token, VirtualNetwork virtualnetwork)
        {

            foreach (var resource in resources.properties.subnets)
            {
                // Process Virtual Network Gateway if one exists
                if (resource.name == "GatewaySubnet" && !(resource.properties.ipConfigurations == null))
                {

                    //Get Gateway Details
                    string[] GWDetails = resource.properties.ipConfigurations[0].id.Value.Split('/');
                    Hashtable GWInfo = new Hashtable();
                    GWInfo.Add("RGName", GWDetails[4]);
                    GWInfo.Add("vnetGWName", GWDetails[8]);

                    var GWDetail = _asmRetriever.GetAzureARMResources("VirtualNetworkGateway", subscriptionId, GWInfo, token, null);
                    var GWResource = JsonConvert.DeserializeObject<dynamic>(GWDetail);


                    // Gateway Public IP Address
                    PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
                    publicipaddress_properties.publicIPAllocationMethod = "Dynamic";

                    PublicIPAddress publicipaddress = new PublicIPAddress();
                    publicipaddress.name = virtualnetwork.name + "-Gateway-PIP";
                    publicipaddress.location = virtualnetwork.location;
                    publicipaddress.properties = publicipaddress_properties;

                    _processedItems.Add("Microsoft.Network/publicIPAddresses/" + publicipaddress.name, publicipaddress.location);
                    _resources.Add(publicipaddress);

                    // Virtual Network Gateway
                    Reference subnet_ref = new Reference();
                    subnet_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetwork.name + "/subnets/GatewaySubnet')]";

                    Reference publicipaddress_ref = new Reference();
                    publicipaddress_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + publicipaddress.name + "')]";

                    var dependson = new List<string>();
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetwork.name + "')]");
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + publicipaddress.name + "')]");

                    IpConfiguration_Properties ipconfiguration_properties = new IpConfiguration_Properties();
                    ipconfiguration_properties.privateIPAllocationMethod = "Dynamic";
                    ipconfiguration_properties.subnet = subnet_ref;
                    ipconfiguration_properties.publicIPAddress = publicipaddress_ref;

                    IpConfiguration virtualnetworkgateway_ipconfiguration = new IpConfiguration();
                    virtualnetworkgateway_ipconfiguration.name = "GatewayIPConfig";
                    virtualnetworkgateway_ipconfiguration.properties = ipconfiguration_properties;

                    VirtualNetworkGateway_Sku virtualnetworkgateway_sku = new VirtualNetworkGateway_Sku();
                    virtualnetworkgateway_sku.name = GWResource.properties.sku.name;
                    virtualnetworkgateway_sku.tier = GWResource.properties.sku.tier;

                    List<IpConfiguration> virtualnetworkgateway_ipconfigurations = new List<IpConfiguration>();
                    virtualnetworkgateway_ipconfigurations.Add(virtualnetworkgateway_ipconfiguration);

                    VirtualNetworkGateway_Properties virtualnetworkgateway_properties = new VirtualNetworkGateway_Properties();
                    virtualnetworkgateway_properties.ipConfigurations = virtualnetworkgateway_ipconfigurations;
                    virtualnetworkgateway_properties.sku = virtualnetworkgateway_sku;

                  
                
                    // If there is VPN Client configuration
                    if (GWResource.properties.vpnClientConfiguration != null)
                    {
                        var addressprefixes = new List<string>();
                        addressprefixes.Add(GWResource.properties.vpnClientConfiguration.vpnClientAddressPool.addressPrefixes[0].Value);

                        AddressSpace vpnclientaddresspool = new AddressSpace();
                        vpnclientaddresspool.addressPrefixes = addressprefixes;

                        VPNClientConfiguration vpnclientconfiguration = new VPNClientConfiguration();
                        vpnclientconfiguration.vpnClientAddressPool = vpnclientaddresspool;

                        List<VPNClientCertificate> vpnclientrootcertificates = new List<VPNClientCertificate>();
                        foreach (var certificate in GWResource.properties.vpnClientConfiguration.vpnClientRootCertificates)
                        {
                           // Hashtable infocert = new Hashtable();
                           // infocert.Add("virtualnetworkname", resource.name.Value);
                           // infocert.Add("thumbprint", certificate.SelectSingleNode("Thumbprint").InnerText);
                           // XmlDocument clientrootcertificate = _asmRetriever.GetAzureASMResources("ClientRootCertificate", subscriptionId, infocert, token);

                            VPNClientCertificate_Properties vpnclientcertificate_properties = new VPNClientCertificate_Properties();
                            vpnclientcertificate_properties.PublicCertData = certificate.properties.publicCertData.Value;

                            VPNClientCertificate vpnclientcertificate = new VPNClientCertificate();
                            vpnclientcertificate.name = certificate.name.Value;
                            vpnclientcertificate.properties = vpnclientcertificate_properties;

                            vpnclientrootcertificates.Add(vpnclientcertificate);
                        }

                        vpnclientconfiguration.vpnClientRootCertificates = vpnclientrootcertificates;

                        virtualnetworkgateway_properties.vpnClientConfiguration = vpnclientconfiguration;
                    }

                    Hashtable virtualnetworkgatewayinfo = new Hashtable();
                    virtualnetworkgatewayinfo.Add("virtualnetworkname", resource.name.Value);
                    virtualnetworkgatewayinfo.Add("virtualnetworkid", GWResource.id.Value);
                    virtualnetworkgatewayinfo.Add("localnetworksitename", "");

                   // var connectionTypeNodes = resource.SelectNodes("Gateway/Sites/LocalNetworkSite/Connections/Connection/Type");
                    if (GWResource.properties.gatewayType.Value == "ExpressRoute")
                    {
                        virtualnetworkgateway_properties.gatewayType = "ExpressRoute";
                        virtualnetworkgateway_properties.enableBgp = null;
                        virtualnetworkgateway_properties.vpnType = null;
                    }
                    else
                    {
                        virtualnetworkgateway_properties.gatewayType = GWResource.properties.gatewayType.Value;
                        virtualnetworkgateway_properties.vpnType = GWResource.properties.vpnType.Value;
                    }

                    VirtualNetworkGateway virtualnetworkgateway = new VirtualNetworkGateway();
                    virtualnetworkgateway.location = virtualnetwork.location;
                    virtualnetworkgateway.name = GWResource.name;
                    virtualnetworkgateway.properties = virtualnetworkgateway_properties;
                    virtualnetworkgateway.dependsOn = dependson;

                    _processedItems.Add("Microsoft.Network/virtualNetworkGateways/" + virtualnetworkgateway.name, virtualnetworkgateway.location);
                    _resources.Add(virtualnetworkgateway);
                    _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/virtualNetworkGateways/" + virtualnetworkgateway.name);
                    AddLocalSiteToGatewayARM(subscriptionId, GWResource, token, virtualnetwork, virtualnetworkgatewayinfo, virtualnetworkgateway);
                }
            }
        }

        private void AddGatewaysToVirtualNetwork(string subscriptionId, XmlNode resource, string token, VirtualNetwork virtualnetwork)
        {
            // Process Virtual Network Gateway if one exists
            if (resource.SelectNodes("Gateway").Count > 0)
            {
                // Gateway Public IP Address
                PublicIPAddress_Properties publicipaddress_properties = new PublicIPAddress_Properties();
                publicipaddress_properties.publicIPAllocationMethod = "Dynamic";

                PublicIPAddress publicipaddress = new PublicIPAddress();
                publicipaddress.name = virtualnetwork.name + "-Gateway-PIP";
                publicipaddress.location = virtualnetwork.location;
                publicipaddress.properties = publicipaddress_properties;

                _processedItems.Add("Microsoft.Network/publicIPAddresses/" + publicipaddress.name, publicipaddress.location);
                _resources.Add(publicipaddress);

                // Virtual Network Gateway
                Reference subnet_ref = new Reference();
                subnet_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetwork.name + "/subnets/GatewaySubnet')]";

                Reference publicipaddress_ref = new Reference();
                publicipaddress_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + publicipaddress.name + "')]";

                var dependson = new List<string>();
                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetwork.name + "')]");
                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/publicIPAddresses/" + publicipaddress.name + "')]");

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
                if (resource.SelectNodes("Gateway/VPNClientAddressPool/AddressPrefixes/AddressPrefix").Count > 0)
                {
                    var addressprefixes = new List<string>();
                    addressprefixes.Add(resource.SelectNodes("Gateway/VPNClientAddressPool/AddressPrefixes/AddressPrefix")[0].InnerText);

                    AddressSpace vpnclientaddresspool = new AddressSpace();
                    vpnclientaddresspool.addressPrefixes = addressprefixes;

                    VPNClientConfiguration vpnclientconfiguration = new VPNClientConfiguration();
                    vpnclientconfiguration.vpnClientAddressPool = vpnclientaddresspool;

                    //Process vpnClientRootCertificates
                    Hashtable infocrc = new Hashtable();
                    infocrc.Add("virtualnetworkname", resource.SelectSingleNode("Name").InnerText);
                    XmlDocument clientrootcertificates = _asmRetriever.GetAzureASMResources("ClientRootCertificates", subscriptionId, infocrc, token);

                    List<VPNClientCertificate> vpnclientrootcertificates = new List<VPNClientCertificate>();
                    foreach (XmlNode certificate in clientrootcertificates.SelectNodes("//ClientRootCertificate"))
                    {
                        Hashtable infocert = new Hashtable();
                        infocert.Add("virtualnetworkname", resource.SelectSingleNode("Name").InnerText);
                        infocert.Add("thumbprint", certificate.SelectSingleNode("Thumbprint").InnerText);
                        XmlDocument clientrootcertificate = _asmRetriever.GetAzureASMResources("ClientRootCertificate", subscriptionId, infocert, token);

                        VPNClientCertificate_Properties vpnclientcertificate_properties = new VPNClientCertificate_Properties();
                        vpnclientcertificate_properties.PublicCertData = Convert.ToBase64String(StrToByteArray(clientrootcertificate.InnerText));

                        VPNClientCertificate vpnclientcertificate = new VPNClientCertificate();
                        vpnclientcertificate.name = certificate.SelectSingleNode("Subject").InnerText.Replace("CN=", "");
                        vpnclientcertificate.properties = vpnclientcertificate_properties;

                        vpnclientrootcertificates.Add(vpnclientcertificate);
                    }

                    vpnclientconfiguration.vpnClientRootCertificates = vpnclientrootcertificates;

                    virtualnetworkgateway_properties.vpnClientConfiguration = vpnclientconfiguration;
                }

                Hashtable virtualnetworkgatewayinfo = new Hashtable();
                virtualnetworkgatewayinfo.Add("virtualnetworkname", resource.SelectSingleNode("Name").InnerText);
                virtualnetworkgatewayinfo.Add("localnetworksitename", "");

                var connectionTypeNodes = resource.SelectNodes("Gateway/Sites/LocalNetworkSite/Connections/Connection/Type");
                if (connectionTypeNodes.Count > 0 && connectionTypeNodes[0].InnerText == "Dedicated")
                {
                    virtualnetworkgateway_properties.gatewayType = "ExpressRoute";
                    virtualnetworkgateway_properties.enableBgp = null;
                    virtualnetworkgateway_properties.vpnType = null;
                }
                else
                {
                    virtualnetworkgateway_properties.gatewayType = "Vpn";
                    XmlDocument gateway = _asmRetriever.GetAzureASMResources("VirtualNetworkGateway", subscriptionId, virtualnetworkgatewayinfo, token);
                    string vpnType = gateway.SelectSingleNode("//GatewayType").InnerText;
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

                VirtualNetworkGateway virtualnetworkgateway = new VirtualNetworkGateway();
                virtualnetworkgateway.location = virtualnetwork.location;
                virtualnetworkgateway.name = virtualnetwork.name + "-Gateway";
                virtualnetworkgateway.properties = virtualnetworkgateway_properties;
                virtualnetworkgateway.dependsOn = dependson;

                _processedItems.Add("Microsoft.Network/virtualNetworkGateways/" + virtualnetworkgateway.name, virtualnetworkgateway.location);
                _resources.Add(virtualnetworkgateway);
                _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/virtualNetworkGateways/" + virtualnetworkgateway.name);
                AddLocalSiteToGateway(subscriptionId, resource, token, virtualnetwork, virtualnetworkgatewayinfo, virtualnetworkgateway);
            }
        }

        private void AddLocalSiteToGatewayARM(string subscriptionId, dynamic resource, string token, VirtualNetwork virtualnetwork, Hashtable virtualnetworkgatewayinfo, VirtualNetworkGateway virtualnetworkgateway)
        {

            var RGList = _asmRetriever.GetAzureARMResources("RG", subscriptionId, null, token, null);
            var RGresults = JsonConvert.DeserializeObject<dynamic>(RGList);

            foreach (var RG in RGresults.value)
            {
                string RGName = RG.name.Value;
                //Connections
                string localnwgwname = null;
                var ConnectionList = _asmRetriever.GetAzureARMResources("Connections", subscriptionId, null, token, RGName);
                var Connectionresults = JsonConvert.DeserializeObject<dynamic>(ConnectionList);

                foreach (var connection in Connectionresults.value)
                {
                    string connectionName = connection.properties.virtualNetworkGateway1.id.Value;
                    if (connectionName == virtualnetworkgatewayinfo["virtualnetworkid"].ToString())
                    {
                        GatewayConnection_Properties gatewayconnection_properties = new GatewayConnection_Properties();
                        var dependson = new List<string>();

                        string connectionType = connection.properties.connectionType.Value;
                        if (connectionType == "IPsec")
                        {
                            Hashtable lnwgwinfo = new Hashtable();
                            string localnwgwid = connection.properties.localNetworkGateway2.id.Value;
                            localnwgwid = localnwgwid.Replace("/subscriptions", "subscriptions");
                            lnwgwinfo.Add("localnwgwid", localnwgwid);

                            //Get LocalNWGateway details
                            var localnwgw = _asmRetriever.GetAzureARMResources("localNetworkGateway", subscriptionId, lnwgwinfo, token, null);
                            var localnwgwresults = JsonConvert.DeserializeObject<dynamic>(localnwgw);

                            localnwgwname = localnwgwresults.name.Value;
                            // Local Network Gateway
                            var addressprefixes = new List<string>();
                            foreach (var addressprefix in localnwgwresults.properties.localNetworkAddressSpace.addressPrefixes)
                            {
                                addressprefixes.Add(addressprefix.Value);
                            }

                            AddressSpace localnetworkaddressspace = new AddressSpace();
                            localnetworkaddressspace.addressPrefixes = addressprefixes;

                            LocalNetworkGateway_Properties localnetworkgateway_properties = new LocalNetworkGateway_Properties();
                            localnetworkgateway_properties.localNetworkAddressSpace = localnetworkaddressspace;
                            localnetworkgateway_properties.gatewayIpAddress = localnwgwresults.properties.gatewayIpAddress;

                            LocalNetworkGateway localnetworkgateway = new LocalNetworkGateway();
                            localnetworkgateway.name = localnwgwresults.name;
                            //localnetworkgateway.name = localnwgwname.Replace(" ", "");

                            localnetworkgateway.location = localnwgwresults.location;
                            localnetworkgateway.properties = localnetworkgateway_properties;

                            try
                            {
                                _processedItems.Add("Microsoft.Network/localNetworkGateways/" + localnetworkgateway.name, localnetworkgateway.location);
                                _resources.Add(localnetworkgateway);
                            }
                            catch { }
                            _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/localNetworkGateways/" + localnetworkgateway.name);

                            Reference localnetworkgateway_ref = new Reference();
                            localnetworkgateway_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/localNetworkGateways/" + localnetworkgateway.name + "')]";
                            dependson.Add(localnetworkgateway_ref.id);

                            gatewayconnection_properties.connectionType = connectionType;
                            gatewayconnection_properties.localNetworkGateway2 = localnetworkgateway_ref;

                            virtualnetworkgatewayinfo["localnetworksitename"] = localnwgwresults.name.Value;

                            string connid = connection.id.Value;
                            connid = connid.Replace("/subscriptions", "subscriptions");

                            Hashtable conninfo = new Hashtable();
                            conninfo.Add("connectionid", connection.id.Value);

                            var SharedKeyList = _asmRetriever.GetAzureARMResources("sharedkey", subscriptionId, conninfo, token, null);
                            var SharedKey = JsonConvert.DeserializeObject<dynamic>(SharedKeyList);


                            // XmlDocument connectionsharekey = _asmRetriever.GetAzureASMResources("VirtualNetworkGatewaySharedKey", subscriptionId, virtualnetworkgatewayinfo, token);
                            if (SharedKey.value == null)
                            {
                                gatewayconnection_properties.sharedKey = "***SHARED KEY GOES HERE***";
                                _messages.Add($"Unable to retrieve shared key for VPN connection '{virtualnetworkgateway.name}'. Please edit the template to provide this value.");
                            }
                            else
                            {
                                gatewayconnection_properties.sharedKey = SharedKey.value;
                            }
                        }
                        else if (connectionType == "Dedicated")
                        {
                            gatewayconnection_properties.connectionType = "ExpressRoute";
                            gatewayconnection_properties.peer = new Reference() { id = "/subscriptions/***/resourceGroups/***/providers/Microsoft.Network/expressRouteCircuits/***" };
                            _messages.Add($"Gateway '{virtualnetworkgateway.name}' connects to ExpressRoute. MigAz is unable to migrate ExpressRoute circuits. Please create or convert the circuit yourself and update the circuit resource ID in the generated template.");
                        }

                        // Connections
                        Reference virtualnetworkgateway_ref = new Reference();
                        virtualnetworkgateway_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworkGateways/" + virtualnetworkgateway.name + "')]";

                        dependson.Add(virtualnetworkgateway_ref.id);

                        gatewayconnection_properties.virtualNetworkGateway1 = virtualnetworkgateway_ref;

                        GatewayConnection gatewayconnection = new GatewayConnection();
                        gatewayconnection.name = connection.name;
                        gatewayconnection.location = connection.location;
                        gatewayconnection.properties = gatewayconnection_properties;
                        gatewayconnection.dependsOn = dependson;

                        _processedItems.Add("Microsoft.Network/connections/" + gatewayconnection.name, gatewayconnection.location);
                        _resources.Add(gatewayconnection);
                        _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/connections/" + gatewayconnection.name);
                    }
                }
            }      
            
        }

        private void AddLocalSiteToGateway(string subscriptionId, XmlNode resource, string token, VirtualNetwork virtualnetwork, Hashtable virtualnetworkgatewayinfo, VirtualNetworkGateway virtualnetworkgateway)
        {
            // Local Network Gateways & Connections
            foreach (XmlNode LocalNetworkSite in resource.SelectNodes("Gateway/Sites/LocalNetworkSite"))
            {
                GatewayConnection_Properties gatewayconnection_properties = new GatewayConnection_Properties();
                var dependson = new List<string>();

                string connectionType = LocalNetworkSite.SelectSingleNode("Connections/Connection/Type").InnerText;
                if (connectionType == "IPsec")
                {
                    // Local Network Gateway
                    var addressprefixes = new List<string>();
                    foreach (XmlNode addressprefix in LocalNetworkSite.SelectNodes("AddressSpace/AddressPrefixes"))
                    {
                        addressprefixes.Add(addressprefix.SelectSingleNode("AddressPrefix").InnerText);
                    }

                    AddressSpace localnetworkaddressspace = new AddressSpace();
                    localnetworkaddressspace.addressPrefixes = addressprefixes;

                    LocalNetworkGateway_Properties localnetworkgateway_properties = new LocalNetworkGateway_Properties();
                    localnetworkgateway_properties.localNetworkAddressSpace = localnetworkaddressspace;
                    localnetworkgateway_properties.gatewayIpAddress = LocalNetworkSite.SelectSingleNode("VpnGatewayAddress").InnerText;

                    LocalNetworkGateway localnetworkgateway = new LocalNetworkGateway();
                    localnetworkgateway.name = LocalNetworkSite.SelectSingleNode("Name").InnerText + "-LocalGateway";
                    localnetworkgateway.name = localnetworkgateway.name.Replace(" ", "");

                    localnetworkgateway.location = virtualnetwork.location;
                    localnetworkgateway.properties = localnetworkgateway_properties;

                    try
                    {
                        _processedItems.Add("Microsoft.Network/localNetworkGateways/" + localnetworkgateway.name, localnetworkgateway.location);
                        _resources.Add(localnetworkgateway);
                    }
                    catch { }
                    _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/localNetworkGateways/" + localnetworkgateway.name);

                    Reference localnetworkgateway_ref = new Reference();
                    localnetworkgateway_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/localNetworkGateways/" + localnetworkgateway.name + "')]";
                    dependson.Add(localnetworkgateway_ref.id);

                    gatewayconnection_properties.connectionType = connectionType;
                    gatewayconnection_properties.localNetworkGateway2 = localnetworkgateway_ref;

                    virtualnetworkgatewayinfo["localnetworksitename"] = LocalNetworkSite.SelectSingleNode("Name").InnerText;
                    XmlDocument connectionsharekey = _asmRetriever.GetAzureASMResources("VirtualNetworkGatewaySharedKey", subscriptionId, virtualnetworkgatewayinfo, token);
                    if (connectionsharekey == null)
                    {
                        gatewayconnection_properties.sharedKey = "***SHARED KEY GOES HERE***";
                        _messages.Add($"Unable to retrieve shared key for VPN connection '{virtualnetworkgateway.name}'. Please edit the template to provide this value.");
                    }
                    else
                    {
                        gatewayconnection_properties.sharedKey = connectionsharekey.SelectSingleNode("//Value").InnerText;
                    }
                }
                else if (connectionType == "Dedicated")
                {
                    gatewayconnection_properties.connectionType = "ExpressRoute";
                    gatewayconnection_properties.peer = new Reference() { id = "/subscriptions/***/resourceGroups/***/providers/Microsoft.Network/expressRouteCircuits/***" };
                    _messages.Add($"Gateway '{virtualnetworkgateway.name}' connects to ExpressRoute. MigAz is unable to migrate ExpressRoute circuits. Please create or convert the circuit yourself and update the circuit resource ID in the generated template.");
                }

                // Connections
                Reference virtualnetworkgateway_ref = new Reference();
                virtualnetworkgateway_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworkGateways/" + virtualnetworkgateway.name + "')]";
                     
                dependson.Add(virtualnetworkgateway_ref.id);

                gatewayconnection_properties.virtualNetworkGateway1 = virtualnetworkgateway_ref;

                GatewayConnection gatewayconnection = new GatewayConnection();
                gatewayconnection.name = virtualnetworkgateway.name + "-" + LocalNetworkSite.SelectSingleNode("Name").InnerText + "-connection";
                gatewayconnection.location = virtualnetwork.location;
                gatewayconnection.properties = gatewayconnection_properties;
                gatewayconnection.dependsOn = dependson;

                _processedItems.Add("Microsoft.Network/connections/" + gatewayconnection.name, gatewayconnection.location);
                _resources.Add(gatewayconnection);
                _logProvider.WriteLog("BuildVirtualNetworkObject", "Microsoft.Network/connections/" + gatewayconnection.name);

            }
        }

        private NetworkSecurityGroup BuildARMNetworkSecurityGroup(string subscriptionId, string networksecuritygroupname, string token)
        {
            _logProvider.WriteLog("BuildNetworkSecurityGroup", "Start");

            Hashtable nsginfo = new Hashtable();
            networksecuritygroupname = networksecuritygroupname.Replace("/subscriptions/", "subscriptions/");
            nsginfo.Add("NetworkSecurityGroup", networksecuritygroupname);
            var NSGDetail = _asmRetriever.GetAzureARMResources("NetworkSecurityGroup", subscriptionId, nsginfo, token,null);
            var resource = JsonConvert.DeserializeObject<dynamic>(NSGDetail);

            NetworkSecurityGroup networksecuritygroup = new NetworkSecurityGroup();
            networksecuritygroup.name = resource.name;
            networksecuritygroup.location = resource.location;

            NetworkSecurityGroup_Properties networksecuritygroup_properties = new NetworkSecurityGroup_Properties();
            networksecuritygroup_properties.securityRules = new List<SecurityRule>();

            //foreach rule without System Rule
            foreach (var rule in resource.properties.securityRules)
            {
                SecurityRule_Properties securityrule_properties = new SecurityRule_Properties();
                securityrule_properties.description = rule.name.Value;
                securityrule_properties.direction = rule.properties.direction.Value;
                securityrule_properties.priority = rule.properties.priority.Value;
                securityrule_properties.access = rule.properties.access.Value;
                securityrule_properties.sourceAddressPrefix = rule.properties.sourceAddressPrefix.Value;
                securityrule_properties.sourceAddressPrefix.Replace("_", "");
                securityrule_properties.destinationAddressPrefix = rule.properties.destinationAddressPrefix.Value;
                securityrule_properties.destinationAddressPrefix.Replace("_", "");
                securityrule_properties.sourcePortRange = rule.properties.sourcePortRange.Value;
                securityrule_properties.destinationPortRange = rule.properties.destinationPortRange.Value;
                securityrule_properties.protocol = rule.properties.protocol.Value;

                SecurityRule securityrule = new SecurityRule();
                securityrule.name = rule.name.Value;
                securityrule.properties = securityrule_properties;

                networksecuritygroup_properties.securityRules.Add(securityrule);
            }

            networksecuritygroup.properties = networksecuritygroup_properties;

            try // it fails if this network security group was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name, networksecuritygroup.location);
                _resources.Add(networksecuritygroup);
                _logProvider.WriteLog("BuildNetworkSecurityGroup", "Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name);
            }
            catch { }

            _logProvider.WriteLog("BuildNetworkSecurityGroup", "End");
            
            return networksecuritygroup;
        }

        private NetworkSecurityGroup BuildNetworkSecurityGroup(string subscriptionId, string networksecuritygroupname, string token)
        {
            _logProvider.WriteLog("BuildNetworkSecurityGroup", "Start");

            Hashtable nsginfo = new Hashtable();
            nsginfo.Add("name", networksecuritygroupname);
            XmlDocument resource = _asmRetriever.GetAzureASMResources("NetworkSecurityGroup", subscriptionId, nsginfo, token);

            NetworkSecurityGroup networksecuritygroup = new NetworkSecurityGroup();
            networksecuritygroup.name = resource.SelectSingleNode("//Name").InnerText.Replace(' ', '-');
            networksecuritygroup.location = resource.SelectSingleNode("//Location").InnerText;

            NetworkSecurityGroup_Properties networksecuritygroup_properties = new NetworkSecurityGroup_Properties();
            networksecuritygroup_properties.securityRules = new List<SecurityRule>();

            // for each rule
            foreach (XmlNode rule in resource.SelectNodes("//Rules/Rule"))
            {
                // if not system rule
                if (rule.SelectNodes("IsDefault").Count == 0)
                {
                    SecurityRule_Properties securityrule_properties = new SecurityRule_Properties();
                    securityrule_properties.description = rule.SelectSingleNode("Name").InnerText;
                    securityrule_properties.direction = rule.SelectSingleNode("Type").InnerText;
                    securityrule_properties.priority = long.Parse(rule.SelectSingleNode("Priority").InnerText);
                    securityrule_properties.access = rule.SelectSingleNode("Action").InnerText;
                    securityrule_properties.sourceAddressPrefix = rule.SelectSingleNode("SourceAddressPrefix").InnerText;
                    securityrule_properties.sourceAddressPrefix.Replace("_", "");
                    securityrule_properties.destinationAddressPrefix = rule.SelectSingleNode("DestinationAddressPrefix").InnerText;
                    securityrule_properties.destinationAddressPrefix.Replace("_", "");
                    securityrule_properties.sourcePortRange = rule.SelectSingleNode("SourcePortRange").InnerText;
                    securityrule_properties.destinationPortRange = rule.SelectSingleNode("DestinationPortRange").InnerText;
                    securityrule_properties.protocol = rule.SelectSingleNode("Protocol").InnerText;

                    SecurityRule securityrule = new SecurityRule();
                    securityrule.name = rule.SelectSingleNode("Name").InnerText.Replace(' ', '-');
                    securityrule.properties = securityrule_properties;

                    networksecuritygroup_properties.securityRules.Add(securityrule);
                }
            }

            networksecuritygroup.properties = networksecuritygroup_properties;

            try // it fails if this network security group was already processed. safe to continue.
            {
                _processedItems.Add("Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name, networksecuritygroup.location);
                _resources.Add(networksecuritygroup);
                _logProvider.WriteLog("BuildNetworkSecurityGroup", "Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name);
            }
            catch { }

            _logProvider.WriteLog("BuildNetworkSecurityGroup", "End");

            return networksecuritygroup;
        }


        private RouteTable BuildARMRouteTable(string subscriptionId, string routetablename, string token)
        {
            _logProvider.WriteLog("BuildRouteTable", "Start");

            Hashtable info = new Hashtable();
            routetablename = routetablename.Replace("/subscriptions/", "subscriptions/");

            info.Add("RouteTable", routetablename);
            var routeDetail = _asmRetriever.GetAzureARMResources("RouteTable", subscriptionId, info, token,null);
            var resource = JsonConvert.DeserializeObject<dynamic>(routeDetail);


            RouteTable routetable = new RouteTable();
            routetable.name = resource.name.Value;
            routetable.location = resource.location.Value;

            RouteTable_Properties routetable_properties = new RouteTable_Properties();
            routetable_properties.routes = new List<Route>();

            // for each route
            foreach (var routenode in resource.properties.routes)
            {
                //securityrule_properties.protocol = rule.SelectSingleNode("Protocol").InnerText;
                Route_Properties route_properties = new Route_Properties();
                route_properties.addressPrefix = routenode.properties.addressPrefix.Value;
                route_properties.nextHopType = routenode.properties.nextHopType.Value;

               
                if (route_properties.nextHopType == "VirtualAppliance")
                    route_properties.nextHopIpAddress = routenode.properties.nextHopIpAddress.Value;

                Route route = new Route();
                route.name = routenode.name.Value;
                route.properties = route_properties;

                routetable_properties.routes.Add(route);
            }

            routetable.properties = routetable_properties;

            if (!_resources.Contains(routetable))
            {
                _processedItems.Add("Microsoft.Network/routeTables/" + routetable.name, routetable.location);
                _resources.Add(routetable);
                _logProvider.WriteLog("BuildRouteTable", "Microsoft.Network/routeTables/" + routetable.name);
            }

            _logProvider.WriteLog("BuildRouteTable", "End");

            return routetable;
        }

        private RouteTable BuildRouteTable(string subscriptionId, string routetablename, string token)
        {
            _logProvider.WriteLog("BuildRouteTable", "Start");

            Hashtable info = new Hashtable();
            info.Add("name", routetablename);
            XmlDocument resource = _asmRetriever.GetAzureASMResources("RouteTable", subscriptionId, info, token);

            RouteTable routetable = new RouteTable();
            routetable.name = resource.SelectSingleNode("//Name").InnerText;
            routetable.location = resource.SelectSingleNode("//Location").InnerText;

            RouteTable_Properties routetable_properties = new RouteTable_Properties();
            routetable_properties.routes = new List<Route>();

            // for each route
            foreach (XmlNode routenode in resource.SelectNodes("//RouteList/Route"))
            {
                //securityrule_properties.protocol = rule.SelectSingleNode("Protocol").InnerText;
                Route_Properties route_properties = new Route_Properties();
                route_properties.addressPrefix = routenode.SelectSingleNode("AddressPrefix").InnerText;

                // convert next hop type string
                switch (routenode.SelectSingleNode("NextHopType/Type").InnerText)
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
                    route_properties.nextHopIpAddress = routenode.SelectSingleNode("NextHopType/IpAddress").InnerText;

                Route route = new Route();
                route.name = routenode.SelectSingleNode("Name").InnerText.Replace(' ', '-');
                route.properties = route_properties;

                routetable_properties.routes.Add(route);
            }

            routetable.properties = routetable_properties;

            if (!_resources.Contains(routetable))
            {
                _processedItems.Add("Microsoft.Network/routeTables/" + routetable.name, routetable.location);
                _resources.Add(routetable);
                _logProvider.WriteLog("BuildRouteTable", "Microsoft.Network/routeTables/" + routetable.name);
            }

            _logProvider.WriteLog("BuildRouteTable", "End");

            return routetable;
        }

        private void BuildNetworkInterfaceObject(string subscriptionId, XmlNode resource, Hashtable virtualmachineinfo, ref List<NetworkProfile_NetworkInterface> networkinterfaces, string token)
        {
            _logProvider.WriteLog("BuildNetworkInterfaceObject", "Start");

            string virtualmachinename = virtualmachineinfo["virtualmachinename"].ToString();
            string cloudservicename = virtualmachineinfo["cloudservicename"].ToString();
            string deploymentname = virtualmachineinfo["deploymentname"].ToString();
            string virtualnetworkname = virtualmachineinfo["virtualnetworkname"].ToString();
            string loadbalancername = virtualmachineinfo["loadbalancername"].ToString();
            string subnet_name = "Subnet1";

            if (virtualnetworkname != "empty")
            {
                virtualnetworkname = virtualmachineinfo["virtualnetworkname"].ToString().Replace(" ", "");
                if (resource.SelectSingleNode("//ConfigurationSets/ConfigurationSet/SubnetNames/SubnetName") != null)
                {
                    subnet_name = resource.SelectSingleNode("//ConfigurationSets/ConfigurationSet/SubnetNames[1]/SubnetName").InnerText.Replace(" ", "");
                }
                else
                {
                    _messages.Add($"VM '{virtualmachinename}' has no subnet defined. We have placed it on a subnet called 'Subnet1'.");
                }
            }
            else
            {
                virtualnetworkname = cloudservicename + "-VNET";
                _messages.Add($"VM '{virtualmachinename}' has no VNET defined. We have placed it in a new VNET called {virtualnetworkname}.");
            }

            Reference subnet_ref = new Reference();
            subnet_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "/subnets/" + subnet_name + "')]";

            string privateIPAllocationMethod = "Dynamic";
            string privateIPAddress = null;
            if (resource.SelectSingleNode("//ConfigurationSets/ConfigurationSet[1]/StaticVirtualNetworkIPAddress") != null)
            {
                privateIPAllocationMethod = "Static";
                privateIPAddress = resource.SelectSingleNode("//ConfigurationSets/ConfigurationSet[1]/StaticVirtualNetworkIPAddress").InnerText;
            }
            // if its a VM not connected to a virtual network
            //else if (virtualmachineinfo["virtualnetworkname"].ToString() == "empty")
            //{
            //    privateIPAllocationMethod = "Static";
            //    privateIPAddress = virtualmachineinfo["ipaddress"].ToString();
            //}

            List<string> dependson = new List<string>();
            if (GetProcessedItem("Microsoft.Network/virtualNetworks/" + virtualnetworkname))
            {
                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "')]");
            }

            // Get the list of endpoints
            XmlNodeList inputendpoints = resource.SelectNodes("//ConfigurationSets/ConfigurationSet/InputEndpoints/InputEndpoint");

            // If there is at least one endpoint add the reference to the LB backend pool
            List<Reference> loadBalancerBackendAddressPools = new List<Reference>();
            if (inputendpoints.Count > 0)
            {
                Reference loadBalancerBackendAddressPool = new Reference();
                loadBalancerBackendAddressPool.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/backendAddressPools/default')]";

                loadBalancerBackendAddressPools.Add(loadBalancerBackendAddressPool);

                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "')]");
            }

            // Adds the references to the inboud nat rules
            List<Reference> loadBalancerInboundNatRules = new List<Reference>();
            foreach (XmlNode inputendpoint in inputendpoints)
            {
                if (inputendpoint.SelectSingleNode("LoadBalancedEndpointSetName") == null) // don't want to add a load balance endpoint as an inbound nat rule
                {
                    string inboundnatrulename = virtualmachinename + "-" + inputendpoint.SelectSingleNode("Name").InnerText;
                    inboundnatrulename = inboundnatrulename.Replace(" ", "");

                    Reference loadBalancerInboundNatRule = new Reference();
                    loadBalancerInboundNatRule.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/inboundNatRules/" + inboundnatrulename + "')]";

                    loadBalancerInboundNatRules.Add(loadBalancerInboundNatRule);
                }
            }

            IpConfiguration_Properties ipconfiguration_properties = new IpConfiguration_Properties();
            ipconfiguration_properties.privateIPAllocationMethod = privateIPAllocationMethod;
            ipconfiguration_properties.privateIPAddress = privateIPAddress;
            ipconfiguration_properties.subnet = subnet_ref;
            ipconfiguration_properties.loadBalancerInboundNatRules = loadBalancerInboundNatRules;

            // basic size VMs cannot have load balancer rules
            if (!resource.SelectSingleNode("//RoleSize").InnerText.Contains("Basic"))
            {
                ipconfiguration_properties.loadBalancerBackendAddressPools = loadBalancerBackendAddressPools;
            }

            string ipconfiguration_name = "ipconfig1";
            IpConfiguration ipconfiguration = new IpConfiguration();
            ipconfiguration.name = ipconfiguration_name;
            ipconfiguration.properties = ipconfiguration_properties;

            List<IpConfiguration> ipConfigurations = new List<IpConfiguration>();
            ipConfigurations.Add(ipconfiguration);

            NetworkInterface_Properties networkinterface_properties = new NetworkInterface_Properties();
            networkinterface_properties.ipConfigurations = ipConfigurations;
            if (resource.SelectNodes("//ConfigurationSets/ConfigurationSet/IPForwarding").Count > 0)
            {
                networkinterface_properties.enableIPForwarding = true;
            }

            string networkinterface_name = virtualmachinename;
            NetworkInterface networkinterface = new NetworkInterface();
            networkinterface.name = networkinterface_name;
            networkinterface.location = virtualmachineinfo["location"].ToString();
            networkinterface.properties = networkinterface_properties;
            networkinterface.dependsOn = dependson;

            NetworkProfile_NetworkInterface_Properties networkinterface_ref_properties = new NetworkProfile_NetworkInterface_Properties();
            networkinterface_ref_properties.primary = true;

            NetworkProfile_NetworkInterface networkinterface_ref = new NetworkProfile_NetworkInterface();
            networkinterface_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterface.name + "')]";
            networkinterface_ref.properties = networkinterface_ref_properties;

            if (resource.SelectNodes("//ConfigurationSets/ConfigurationSet/NetworkSecurityGroup").Count > 0)
            {
                NetworkSecurityGroup networksecuritygroup = BuildNetworkSecurityGroup(subscriptionId, resource.SelectSingleNode("//ConfigurationSets/ConfigurationSet/NetworkSecurityGroup").InnerText, token);

                // Add NSG reference to the network interface
                Reference networksecuritygroup_ref = new Reference();
                networksecuritygroup_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name + "')]";

                networkinterface_properties.NetworkSecurityGroup = networksecuritygroup_ref;
                networkinterface.properties = networkinterface_properties;

                // Add NSG dependsOn to the Network Interface object
                if (!networkinterface.dependsOn.Contains(networksecuritygroup_ref.id))
                {
                    networkinterface.dependsOn.Add(networksecuritygroup_ref.id);
                }

            }

            if (resource.SelectNodes("//ConfigurationSets/ConfigurationSet/PublicIPs").Count > 0)
            {
                BuildPublicIPAddressObject(ref networkinterface);
            }

            networkinterfaces.Add(networkinterface_ref);
            _processedItems.Add("Microsoft.Network/networkInterfaces/" + networkinterface.name, networkinterface.location);
            _resources.Add(networkinterface);
            _logProvider.WriteLog("BuildNetworkInterfaceObject", "Microsoft.Network/networkInterfaces/" + networkinterface.name);

            foreach (XmlNode additionalnetworkinterface in resource.SelectNodes("//ConfigurationSets/ConfigurationSet/NetworkInterfaces/NetworkInterface"))
            {
                subnet_name = additionalnetworkinterface.SelectSingleNode("IPConfigurations/IPConfiguration/SubnetName").InnerText.Replace(" ", "");
                subnet_ref = new Reference();
                subnet_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "/subnets/" + subnet_name + "')]";

                privateIPAllocationMethod = "Dynamic";
                privateIPAddress = null;
                if (additionalnetworkinterface.SelectSingleNode("IPConfigurations/IPConfiguration/StaticVirtualNetworkIPAddress") != null)
                {
                    privateIPAllocationMethod = "Static";
                    privateIPAddress = additionalnetworkinterface.SelectSingleNode("IPConfigurations/IPConfiguration/StaticVirtualNetworkIPAddress").InnerText;
                }

                ipconfiguration_properties = new IpConfiguration_Properties();
                ipconfiguration_properties.privateIPAllocationMethod = privateIPAllocationMethod;
                ipconfiguration_properties.privateIPAddress = privateIPAddress;
                ipconfiguration_properties.subnet = subnet_ref;

                ipconfiguration_name = "ipconfig1";
                ipconfiguration = new IpConfiguration();
                ipconfiguration.name = ipconfiguration_name;
                ipconfiguration.properties = ipconfiguration_properties;

                ipConfigurations = new List<IpConfiguration>();
                ipConfigurations.Add(ipconfiguration);

                networkinterface_properties = new NetworkInterface_Properties();
                networkinterface_properties.ipConfigurations = ipConfigurations;
                if (additionalnetworkinterface.SelectNodes("IPForwarding").Count > 0)
                {
                    networkinterface_properties.enableIPForwarding = true;
                }

                dependson = new List<string>();
                if (GetProcessedItem("Microsoft.Network/virtualNetworks/" + virtualnetworkname))
                {
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "')]");
                }

                networkinterface_name = virtualmachinename + "-" + additionalnetworkinterface.SelectSingleNode("Name").InnerText;
                networkinterface = new NetworkInterface();
                networkinterface.name = networkinterface_name;
                networkinterface.location = virtualmachineinfo["location"].ToString();
                networkinterface.properties = networkinterface_properties;
                networkinterface.dependsOn = dependson;

                networkinterface_ref_properties = new NetworkProfile_NetworkInterface_Properties();
                networkinterface_ref_properties.primary = false;

                networkinterface_ref = new NetworkProfile_NetworkInterface();
                networkinterface_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterface.name + "')]";
                networkinterface_ref.properties = networkinterface_ref_properties;

                networkinterfaces.Add(networkinterface_ref);
                _processedItems.Add("Microsoft.Network/networkInterfaces/" + networkinterface.name, networkinterface.location);
                _resources.Add(networkinterface);
                _logProvider.WriteLog("BuildNetworkInterfaceObject", "Microsoft.Network/networkInterfaces/" + networkinterface.name);
            }

            _logProvider.WriteLog("BuildNetworkInterfaceObject", "End");
        }

        private void BuildARMNetworkInterfaceObject(string subscriptionId, dynamic resource, Hashtable virtualmachineinfo, ref List<NetworkProfile_NetworkInterface> networkinterfaces, string token)
        {
            _logProvider.WriteLog("BuildNetworkInterfaceObject", "Start");

            string virtualmachinename = virtualmachineinfo["virtualmachinename"].ToString();
            string RGName = virtualmachineinfo["ResourceGroup"].ToString();
            //string deploymentname = virtualmachineinfo["deploymentname"].ToString();

            string[] stringSeparators = new string[] { "/subnets/" };
            
            string vnet = (virtualmachineinfo["virtualnetworkname"].ToString().Split(stringSeparators, StringSplitOptions.None))[0].Replace("/subscriptions", "subscriptions");

            //Getting the Subnet Details
            Hashtable VNetInfo = new Hashtable();
            VNetInfo.Add("VirtualNWId", vnet);
            var Vnetdet = _asmRetriever.GetAzureARMResources("VirtualNetwork", subscriptionId, VNetInfo, token, null);
            var VnetResult = JsonConvert.DeserializeObject<dynamic>(Vnetdet);
            string virtualnetworkname = VnetResult.name;

            
            /*
            //Get the NIC details

            string nicId = resource.properties.networkProfile.networkInterfaces[0].id;
            nicId = nicId.Replace("/subscriptions", "subscriptions");

            Hashtable NWInfo = new Hashtable();
            NWInfo.Add("networkinterfaceId", nicId);

            var NicDetails = _asmRetriever.GetAzureARMResources("NetworkInterface", subscriptionId, NWInfo, token, RGName);
            var NicResults = JsonConvert.DeserializeObject<dynamic>(NicDetails);

            //Getting Subnet Details
            string SubnetId = NicResults.properties.ipConfigurations[0].properties.subnet.id;
            SubnetId = SubnetId.Replace("/subscriptions", "subscriptions");

            VNetInfo.Remove("SubnetId");
            VNetInfo.Add("SubnetId", SubnetId);

            var Subnetdet = _asmRetriever.GetAzureARMResources("Subnet", subscriptionId, VNetInfo, token, null);
            var SubnetResult = JsonConvert.DeserializeObject<dynamic>(Subnetdet);

            string subnet_name = SubnetResult.name;


            
            string VnetID = virtualmachineinfo["virtualnetworkname"].ToString();
            VnetID = VnetID.Replace("/subscriptions", "subscriptions");

          
            Reference subnet_ref = new Reference();
            subnet_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "/subnets/" + subnet_name + "')]";

            string privateIPAllocationMethod = NicResults.properties.ipConfigurations[0].properties.privateIPAllocationMethod;
            string privateIPAddress = NicResults.properties.ipConfigurations[0].properties.privateIPAddress;
          
            List<string> dependson = new List<string>();
            if (GetProcessedItem("Microsoft.Network/virtualNetworks/" + virtualnetworkname))
            {
                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "')]");
            }

            // Get the list of endpoints
            List<Reference> loadBalancerBackendAddressPools = new List<Reference>();
            List<Reference> loadBalancerInboundNatRules = new List<Reference>();

            if (NicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools != null)
            {
                foreach (var BAPool in NicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools)
                {
            
                    string BackendAddressPoolId = BAPool.id;
                    string loadbalancername = BackendAddressPoolId.Split(new char[] { '/' })[8].ToString();
                    string BAPoolName = BackendAddressPoolId.Split(new char[] { '/' })[10].ToString();


                    Reference loadBalancerBackendAddressPool = new Reference();
                    loadBalancerBackendAddressPool.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/backendAddressPools/" + BAPoolName + "')]";
                    loadBalancerBackendAddressPools.Add(loadBalancerBackendAddressPool);

                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "')]");
                }
            }
            

            if (NicResults.properties.ipConfigurations[0].properties.loadBalancerInboundNatRules != null)
            {
                foreach (var NicLBRule in NicResults.properties.ipConfigurations[0].properties.loadBalancerInboundNatRules)
                {
                    string LBNATRule = NicLBRule.id;
                    string loadbalancername = LBNATRule.Split(new char[] { '/' })[8].ToString();
                    string inboundnatrulename = LBNATRule.Split(new char[] { '/' })[10].ToString();


                    Reference loadBalancerInboundNatRule = new Reference();
                    loadBalancerInboundNatRule.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/inboundNatRules/" + inboundnatrulename + "')]";

                    loadBalancerInboundNatRules.Add(loadBalancerInboundNatRule);
                }
            }

           
            IpConfiguration_Properties ipconfiguration_properties = new IpConfiguration_Properties();
            ipconfiguration_properties.privateIPAllocationMethod = privateIPAllocationMethod;
            ipconfiguration_properties.privateIPAddress = privateIPAddress;
            ipconfiguration_properties.subnet = subnet_ref;
            ipconfiguration_properties.loadBalancerInboundNatRules = loadBalancerInboundNatRules;
            ipconfiguration_properties.loadBalancerBackendAddressPools = loadBalancerBackendAddressPools;

            string ipconfiguration_name = NicResults.properties.ipConfigurations[0].properties.name;
            IpConfiguration ipconfiguration = new IpConfiguration();
            ipconfiguration.name = ipconfiguration_name;
            ipconfiguration.properties = ipconfiguration_properties;

            List<IpConfiguration> ipConfigurations = new List<IpConfiguration>();
            ipConfigurations.Add(ipconfiguration);

            NetworkInterface_Properties networkinterface_properties = new NetworkInterface_Properties();
            networkinterface_properties.ipConfigurations = ipConfigurations;

            networkinterface_properties.enableIPForwarding = NicResults.properties.enableIPForwarding;


            string networkinterface_name = NicResults.name;
            NetworkInterface networkinterface = new NetworkInterface();
            networkinterface.name = networkinterface_name;
            networkinterface.location = NicResults.location;
            networkinterface.properties = networkinterface_properties;
            networkinterface.dependsOn = dependson;

            NetworkProfile_NetworkInterface_Properties networkinterface_ref_properties = new NetworkProfile_NetworkInterface_Properties();
            networkinterface_ref_properties.primary = NicResults.properties.primary;

            NetworkProfile_NetworkInterface networkinterface_ref = new NetworkProfile_NetworkInterface();
            networkinterface_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterface.name + "')]";
            networkinterface_ref.properties = networkinterface_ref_properties;

            if (NicResults.properties.networkSecurityGroup != null)
            {
                string NSGId = NicResults.properties.networkSecurityGroup.id;
                NetworkSecurityGroup networksecuritygroup = BuildARMNetworkSecurityGroup(subscriptionId, NSGId, token);

                // Add NSG reference to the network interface
                Reference networksecuritygroup_ref = new Reference();
                networksecuritygroup_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name + "')]";

                networkinterface_properties.NetworkSecurityGroup = networksecuritygroup_ref;
                networkinterface.properties = networkinterface_properties;

                // Add NSG dependsOn to the Network Interface object
                if (!networkinterface.dependsOn.Contains(networksecuritygroup_ref.id))
                {
                    networkinterface.dependsOn.Add(networksecuritygroup_ref.id);
                }

            }

            if (NicResults.properties.ipConfigurations[0].properties.publicIPAddress != null)
            {
                BuildARMPublicIPAddressObject(ref networkinterface);
            }

            networkinterfaces.Add(networkinterface_ref);
            _processedItems.Add("Microsoft.Network/networkInterfaces/" + networkinterface.name, networkinterface.location);
            _resources.Add(networkinterface);
            _logProvider.WriteLog("BuildNetworkInterfaceObject", "Microsoft.Network/networkInterfaces/" + networkinterface.name);

            */
         //   int nicCounter = 0;
            foreach (var additionalnetworkinterface in resource.properties.networkProfile.networkInterfaces)
            {

                Hashtable NWInfo = new Hashtable();
                NWInfo.Add("networkinterfaceId", additionalnetworkinterface.id);


                //Get the NIC details
                var additionalNicDetails = _asmRetriever.GetAzureARMResources("NetworkInterface", subscriptionId, NWInfo, token, RGName);
                var additionalNicResults = JsonConvert.DeserializeObject<dynamic>(additionalNicDetails);

                //Getting Subnet Details

                string SubnetId = additionalNicResults.properties.ipConfigurations[0].properties.subnet.id;
                SubnetId = SubnetId.Replace("/subscriptions", "subscriptions");

                VNetInfo.Remove("SubnetId");
                VNetInfo.Add("SubnetId", SubnetId);

                var additionalSubnet = _asmRetriever.GetAzureARMResources("Subnet", subscriptionId, VNetInfo, token, null);
                var additionalSubnetResult = JsonConvert.DeserializeObject<dynamic>(additionalSubnet);

                string subnet_name = additionalSubnetResult.name;


                Reference subnet_ref = new Reference();
                subnet_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "/subnets/" + subnet_name + "')]";

                string privateIPAllocationMethod = additionalNicResults.properties.ipConfigurations[0].properties.privateIPAllocationMethod;
                string privateIPAddress = additionalNicResults.properties.ipConfigurations[0].properties.privateIPAddress;

                List<string> dependson = new List<string>();
                if (GetProcessedItem("Microsoft.Network/virtualNetworks/" + virtualnetworkname))
                {
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/virtualNetworks/" + virtualnetworkname + "')]");
                }


                // Get the list of endpoints
                List<Reference> loadBalancerBackendAddressPools = new List<Reference>();
                List<Reference> loadBalancerInboundNatRules = new List<Reference>();

                if (additionalNicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools != null)
                {
                    foreach (var BAPool in additionalNicResults.properties.ipConfigurations[0].properties.loadBalancerBackendAddressPools)
                    {

                        string BackendAddressPoolId = BAPool.id;
                        string loadbalancername = BackendAddressPoolId.Split(new char[] { '/' })[8].ToString();
                        string BAPoolName = BackendAddressPoolId.Split(new char[] { '/' })[10].ToString();


                        Reference loadBalancerBackendAddressPool = new Reference();
                        loadBalancerBackendAddressPool.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/backendAddressPools/" + BAPoolName + "')]";
                        loadBalancerBackendAddressPools.Add(loadBalancerBackendAddressPool);

                        dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "')]");
                    }
                }


                if (additionalNicResults.properties.ipConfigurations[0].properties.loadBalancerInboundNatRules != null)
                {
                    foreach (var NicLBRule in additionalNicResults.properties.ipConfigurations[0].properties.loadBalancerInboundNatRules)
                    {
                        string LBNATRule = NicLBRule.id;
                        string loadbalancername = LBNATRule.Split(new char[] { '/' })[8].ToString();
                        string inboundnatrulename = LBNATRule.Split(new char[] { '/' })[10].ToString();


                        Reference loadBalancerInboundNatRule = new Reference();
                        loadBalancerInboundNatRule.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/loadBalancers/" + loadbalancername + "/inboundNatRules/" + inboundnatrulename + "')]";

                        loadBalancerInboundNatRules.Add(loadBalancerInboundNatRule);
                    }
                }



                IpConfiguration_Properties ipconfiguration_properties = new IpConfiguration_Properties();
                ipconfiguration_properties.privateIPAllocationMethod = privateIPAllocationMethod;
                ipconfiguration_properties.privateIPAddress = privateIPAddress;
                ipconfiguration_properties.subnet = subnet_ref;
                ipconfiguration_properties.loadBalancerInboundNatRules = loadBalancerInboundNatRules;
                ipconfiguration_properties.loadBalancerBackendAddressPools = loadBalancerBackendAddressPools;


                string ipconfiguration_name = additionalNicResults.properties.ipConfigurations[0].name;
                IpConfiguration ipconfiguration = new IpConfiguration();
                ipconfiguration.name = ipconfiguration_name;
                ipconfiguration.properties = ipconfiguration_properties;

                List<IpConfiguration> ipConfigurations = new List<IpConfiguration>();
                ipConfigurations.Add(ipconfiguration);

                NetworkInterface_Properties networkinterface_properties = new NetworkInterface_Properties();
                networkinterface_properties.ipConfigurations = ipConfigurations;

                networkinterface_properties.enableIPForwarding = additionalNicResults.properties.enableIPForwarding;

               

                string networkinterface_name = additionalNicResults.name;
                NetworkInterface networkinterface = new NetworkInterface();
                networkinterface.name = networkinterface_name;
                networkinterface.location = additionalNicResults.location;
                networkinterface.properties = networkinterface_properties;
                networkinterface.dependsOn = dependson;

                NetworkProfile_NetworkInterface_Properties networkinterface_ref_properties = new NetworkProfile_NetworkInterface_Properties();
                networkinterface_ref_properties.primary = additionalNicResults.properties.primary;

                NetworkProfile_NetworkInterface networkinterface_ref = new NetworkProfile_NetworkInterface();
                networkinterface_ref.id = "[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterface.name + "')]";
                networkinterface_ref.properties = networkinterface_ref_properties;

                if (additionalNicResults.properties.networkSecurityGroup != null)
                {
                    string NSGId = additionalNicResults.properties.networkSecurityGroup.id;
                    NetworkSecurityGroup networksecuritygroup = BuildARMNetworkSecurityGroup(subscriptionId, NSGId, token);

                    // Add NSG reference to the network interface
                    Reference networksecuritygroup_ref = new Reference();
                    networksecuritygroup_ref.id = "[concat(resourceGroup().id,'/providers/Microsoft.Network/networkSecurityGroups/" + networksecuritygroup.name + "')]";

                    networkinterface_properties.NetworkSecurityGroup = networksecuritygroup_ref;
                    networkinterface.properties = networkinterface_properties;

                    // Add NSG dependsOn to the Network Interface object
                    if (!networkinterface.dependsOn.Contains(networksecuritygroup_ref.id))
                    {
                        networkinterface.dependsOn.Add(networksecuritygroup_ref.id);
                    }

                }

                if (additionalNicResults.properties.ipConfigurations[0].properties.publicIPAddress != null)
                {
                    //Get PublicIP details
                    string PubId = additionalNicResults.properties.ipConfigurations[0].properties.publicIPAddress.id;
                    PubId = PubId.Replace("/subscriptions", "subscriptions");

                    NWInfo.Add("publicipId", PubId);
                    var PubIpDetails = _asmRetriever.GetAzureARMResources("PublicIP", subscriptionId, NWInfo, token, null);
                    var PubIpResults = JsonConvert.DeserializeObject<dynamic>(PubIpDetails);

                    BuildARMPublicIPAddressObject(ref networkinterface, PubIpResults);
                }



                try
                {
                    _processedItems.Add("Microsoft.Network/networkInterfaces/" + networkinterface.name, networkinterface.location);
                    networkinterfaces.Add(networkinterface_ref);
                    _resources.Add(networkinterface);
                    _logProvider.WriteLog("BuildNetworkInterfaceObject", "Microsoft.Network/networkInterfaces/" + networkinterface.name);
                }
                catch
                {

                }

             //   nicCounter++;


            }

            _logProvider.WriteLog("BuildNetworkInterfaceObject", "End");
        }

        private void BuildVirtualMachineObject(string subscriptionId, XmlNode resource, Hashtable virtualmachineinfo, List<NetworkProfile_NetworkInterface> networkinterfaces, string token)
        {
            _logProvider.WriteLog("BuildVirtualMachineObject", "Start");

            string virtualmachinename = virtualmachineinfo["virtualmachinename"].ToString();
            string networkinterfacename = virtualmachinename;
            string ostype = resource.SelectSingleNode("//OSVirtualHardDisk/OS").InnerText;

            XmlNode osvirtualharddisk = resource.SelectSingleNode("//OSVirtualHardDisk");
            string olddiskurl = osvirtualharddisk.SelectSingleNode("MediaLink").InnerText;
            string[] splitarray = olddiskurl.Split(new char[] { '/' });
            string oldstorageaccountname = splitarray[2].Split(new char[] { '.' })[0];
            string newstorageaccountname = GetNewStorageAccountName(oldstorageaccountname);
            string newdiskurl = olddiskurl.Replace(oldstorageaccountname + ".", newstorageaccountname + ".");

            Hashtable storageaccountdependencies = new Hashtable();
            storageaccountdependencies.Add(newstorageaccountname, "");

            HardwareProfile hardwareprofile = new HardwareProfile();
            hardwareprofile.vmSize = GetVMSize(resource.SelectSingleNode("//RoleSize").InnerText);

            NetworkProfile networkprofile = new NetworkProfile();
            networkprofile.networkInterfaces = networkinterfaces;

            Vhd vhd = new Vhd();
            vhd.uri = newdiskurl;

            OsDisk osdisk = new OsDisk();
            osdisk.name = resource.SelectSingleNode("//OSVirtualHardDisk/DiskName").InnerText;
            osdisk.vhd = vhd;
            osdisk.caching = resource.SelectSingleNode("//OSVirtualHardDisk/HostCaching").InnerText;

            ImageReference imagereference = new ImageReference();
            OsProfile osprofile = new OsProfile();

            // if the tool is configured to create new VMs with empty data disks
            if (_settingsProvider.BuildEmpty)
            {
                osdisk.createOption = "FromImage";

                osprofile.computerName = virtualmachinename;
                osprofile.adminUsername = "[parameters('adminUsername')]";
                osprofile.adminPassword = "[parameters('adminPassword')]";

                if (!_parameters.ContainsKey("adminUsername"))
                {
                    Parameter parameter = new Parameter();
                    parameter.type = "string";
                    _parameters.Add("adminUsername", parameter);
                }

                if (!_parameters.ContainsKey("adminPassword"))
                {
                    Parameter parameter = new Parameter();
                    parameter.type = "securestring";
                    _parameters.Add("adminPassword", parameter);
                }

                if (ostype == "Windows")
                {
                    imagereference.publisher = "MicrosoftWindowsServer";
                    imagereference.offer = "WindowsServer";
                    imagereference.sku = "2012-R2-Datacenter";
                    imagereference.version = "latest";
                }
                else if (ostype == "Linux")
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
                osdisk.osType = ostype;

                // Block of code to help copying the blobs to the new storage accounts
                Hashtable storageaccountinfo = new Hashtable();
                storageaccountinfo.Add("name", oldstorageaccountname);

                XmlDocument storageaccountkeys = _asmRetriever.GetAzureASMResources("StorageAccountKeys", subscriptionId, storageaccountinfo, token);
                string key = storageaccountkeys.SelectSingleNode("//StorageServiceKeys/Primary").InnerText;

                CopyBlobDetail copyblobdetail = new CopyBlobDetail();
                copyblobdetail.SourceSA = oldstorageaccountname;
                copyblobdetail.SourceContainer = splitarray[3];
                copyblobdetail.SourceBlob = splitarray[4];
                copyblobdetail.SourceKey = key;
                copyblobdetail.DestinationSA = newstorageaccountname;
                copyblobdetail.DestinationContainer = splitarray[3];
                copyblobdetail.DestinationBlob = splitarray[4];
                _copyBlobDetails.Add(copyblobdetail);
                // end of block of code to help copying the blobs to the new storage accounts
            }

            // process data disks
            List<DataDisk> datadisks = new List<DataDisk>();
            XmlNodeList datadisknodes = resource.SelectNodes("//DataVirtualHardDisks/DataVirtualHardDisk");
            foreach (XmlNode datadisknode in datadisknodes)
            {
                DataDisk datadisk = new DataDisk();
                datadisk.name = datadisknode.SelectSingleNode("DiskName").InnerText;
                datadisk.caching = datadisknode.SelectSingleNode("HostCaching").InnerText;
              //  datadisk.diskSizeGB = Int64.Parse(datadisknode.SelectSingleNode("LogicalDiskSizeInGB").InnerText);

                datadisk.lun = 0;
                if (datadisknode.SelectSingleNode("Lun") != null)
                {
                    datadisk.lun = Int64.Parse(datadisknode.SelectSingleNode("Lun").InnerText);
                }

                olddiskurl = datadisknode.SelectSingleNode("MediaLink").InnerText;
                splitarray = olddiskurl.Split(new char[] { '/' });
                oldstorageaccountname = splitarray[2].Split(new char[] { '.' })[0];
                newstorageaccountname = GetNewStorageAccountName(oldstorageaccountname);
                newdiskurl = olddiskurl.Replace(oldstorageaccountname + ".", newstorageaccountname + ".");

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
                    Hashtable storageaccountinfo = new Hashtable();
                    storageaccountinfo.Add("name", oldstorageaccountname);

                    XmlDocument storageaccountkeys = _asmRetriever.GetAzureASMResources("StorageAccountKeys", subscriptionId, storageaccountinfo, token);
                    string key = storageaccountkeys.SelectSingleNode("//StorageServiceKeys/Primary").InnerText;

                    CopyBlobDetail copyblobdetail = new CopyBlobDetail();
                    copyblobdetail.SourceSA = oldstorageaccountname;
                    copyblobdetail.SourceContainer = splitarray[3];
                    copyblobdetail.SourceBlob = splitarray[4];
                    copyblobdetail.SourceKey = key;
                    copyblobdetail.DestinationSA = newstorageaccountname;
                    copyblobdetail.DestinationContainer = splitarray[3];
                    copyblobdetail.DestinationBlob = splitarray[4];
                    _copyBlobDetails.Add(copyblobdetail);
                    // end of block of code to help copying the blobs to the new storage accounts
                }

                vhd = new Vhd();
                vhd.uri = newdiskurl;
                datadisk.vhd = vhd;

                try { storageaccountdependencies.Add(newstorageaccountname, ""); }
                catch { }

                datadisks.Add(datadisk);
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

            List<string> dependson = new List<string>();
            dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterfacename + "')]");

            // Diagnostics Extension
            Extension extension_iaasdiagnostics = null;

            //XmlNodeList resourceextensionreferences = resource.SelectNodes("//ResourceExtensionReferences/ResourceExtensionReference");
            //foreach (XmlNode resourceextensionreference in resourceextensionreferences)
            //{
            //    if (resourceextensionreference.SelectSingleNode("Name").InnerText == "IaaSDiagnostics")
            //    {
            //        string json = Base64Decode(resourceextensionreference.SelectSingleNode("ResourceExtensionParameterValues/ResourceExtensionParameterValue/Value").InnerText);
            //        var resourceextensionparametervalue = JsonConvert.DeserializeObject<dynamic>(json);
            //        string diagnosticsstorageaccount = resourceextensionparametervalue.storageAccount.Value + _settingsProvider.UniquenessSuffix;
            //        string xmlcfgvalue = Base64Decode(resourceextensionparametervalue.xmlCfg.Value);
            //        xmlcfgvalue = xmlcfgvalue.Replace("\n", "");
            //        xmlcfgvalue = xmlcfgvalue.Replace("\r", "");

            //        XmlDocument xmlcfg = new XmlDocument();
            //        xmlcfg.LoadXml(xmlcfgvalue);

            //        XmlNodeList mynodelist = xmlcfg.SelectNodes("/wadCfg/DiagnosticMonitorConfiguration/Metrics");




            //        extension_iaasdiagnostics = new Extension();
            //        extension_iaasdiagnostics.name = "Microsoft.Insights.VMDiagnosticsSettings";
            //        extension_iaasdiagnostics.type = "extensions";
            //        extension_iaasdiagnostics.location = virtualmachineinfo["location"].ToString();
            //        extension_iaasdiagnostics.dependsOn = new List<string>();
            //        extension_iaasdiagnostics.dependsOn.Add("[concat(resourceGroup().id, '/providers/Microsoft.Compute/virtualMachines/" + virtualmachinename + "')]");
            //        extension_iaasdiagnostics.dependsOn.Add("[concat(resourceGroup().id, '/providers/Microsoft.Storage/storageAccounts/" + diagnosticsstorageaccount + "')]");

            //        Extension_Properties extension_iaasdiagnostics_properties = new Extension_Properties();
            //        extension_iaasdiagnostics_properties.publisher = "Microsoft.Azure.Diagnostics";
            //        extension_iaasdiagnostics_properties.type = "IaaSDiagnostics";
            //        extension_iaasdiagnostics_properties.typeHandlerVersion = "1.5";
            //        extension_iaasdiagnostics_properties.autoUpgradeMinorVersion = true;
            //        extension_iaasdiagnostics_properties.settings = new Dictionary<string, string>();
            //        extension_iaasdiagnostics_properties.settings.Add("xmlCfg", "[base64('" + xmlcfgvalue + "')]");
            //        extension_iaasdiagnostics_properties.settings.Add("storageAccount", diagnosticsstorageaccount);
            //        extension_iaasdiagnostics.properties = new Extension_Properties();
            //        extension_iaasdiagnostics.properties = extension_iaasdiagnostics_properties;
            //    }
            //}

            // Availability Set
            string availabilitysetname = virtualmachineinfo["cloudservicename"] + "-defaultAS";
            if (resource.SelectSingleNode("//AvailabilitySetName") != null)
            {
                availabilitysetname = resource.SelectSingleNode("//AvailabilitySetName").InnerText;
            }
            else
            {
                _messages.Add($"VM '{virtualmachinename}' is not in an availability set. Putting it in a new availability set '{availabilitysetname}'.");
            }

            Reference availabilityset = new Reference();
            availabilityset.id = "[concat(resourceGroup().id, '/providers/Microsoft.Compute/availabilitySets/" + availabilitysetname + "')]";
            virtualmachine_properties.availabilitySet = availabilityset;

            dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Compute/availabilitySets/" + availabilitysetname + "')]");

            foreach (DictionaryEntry storageaccountdependency in storageaccountdependencies)
            {
                if (GetProcessedItem("Microsoft.Storage/storageAccounts/" + storageaccountdependency.Key))
                {
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Storage/storageAccounts/" + storageaccountdependency.Key + "')]");
                }
            }

            VirtualMachine virtualmachine = new VirtualMachine();
            virtualmachine.name = virtualmachinename;
            virtualmachine.location = virtualmachineinfo["location"].ToString();
            virtualmachine.properties = virtualmachine_properties;
            virtualmachine.dependsOn = dependson;
            virtualmachine.resources = new List<Resource>();
            if (extension_iaasdiagnostics != null) { virtualmachine.resources.Add(extension_iaasdiagnostics); }

            _processedItems.Add("Microsoft.Compute/virtualMachines/" + virtualmachine.name, virtualmachine.location);
            _resources.Add(virtualmachine);
            _logProvider.WriteLog("BuildVirtualMachineObject", "Microsoft.Compute/virtualMachines/" + virtualmachine.name);

            _logProvider.WriteLog("BuildVirtualMachineObject", "End");
        }

        private void BuildARMVirtualMachineObject(string subscriptionId, dynamic resource, Hashtable virtualmachineinfo, List<NetworkProfile_NetworkInterface> networkinterfaces, string token, AsmArtefacts artefact)
        {
            _logProvider.WriteLog("BuildVirtualMachineObject", "Start");
            //AsmArtefacts artefacts;

            string virtualmachinename = resource.name;
            string networkinterfacename = virtualmachinename;
            string ostype = resource.properties.storageProfile.osDisk.osType;

           // XmlNode osvirtualharddisk = resource.SelectSingleNode("//OSVirtualHardDisk");
            string olddiskurl = resource.properties.storageProfile.osDisk.vhd.uri;
            string[] splitarray = olddiskurl.Split(new char[] { '/' });
            string oldstorageaccountname = splitarray[2].Split(new char[] { '.' })[0];
            string newstorageaccountname = GetNewStorageAccountName(oldstorageaccountname);
            string newdiskurl = olddiskurl.Replace(oldstorageaccountname + ".", newstorageaccountname + ".");

            Hashtable storageaccountdependencies = new Hashtable();
            storageaccountdependencies.Add(newstorageaccountname, "");

            HardwareProfile hardwareprofile = new HardwareProfile();
            hardwareprofile.vmSize = resource.properties.hardwareProfile.vmSize;

            NetworkProfile networkprofile = new NetworkProfile();
            networkprofile.networkInterfaces = networkinterfaces;

            Vhd vhd = new Vhd();
            vhd.uri = newdiskurl;

            OsDisk osdisk = new OsDisk();
            osdisk.name = resource.properties.storageProfile.osDisk.name;
            osdisk.vhd = vhd;
            osdisk.caching = resource.properties.storageProfile.osDisk.caching;

            ImageReference imagereference = new ImageReference();
            OsProfile osprofile = new OsProfile();

            // if the tool is configured to create new VMs with empty data disks
            if (_settingsProvider.BuildEmpty)
            {
                osdisk.createOption = "FromImage";

                osprofile.computerName = virtualmachinename;
                osprofile.adminUsername = "[parameters('adminUsername')]";
                osprofile.adminPassword = "[parameters('adminPassword')]";

                if (!_parameters.ContainsKey("adminUsername"))
                {
                    Parameter parameter = new Parameter();
                    parameter.type = "string";
                    _parameters.Add("adminUsername", parameter);
                }

                if (!_parameters.ContainsKey("adminPassword"))
                {
                    Parameter parameter = new Parameter();
                    parameter.type = "securestring";
                    _parameters.Add("adminPassword", parameter);
                }

                imagereference.publisher = resource.properties.storageProfile.imageReference.publisher;
                imagereference.offer = resource.properties.storageProfile.imageReference.offer;
                imagereference.sku = resource.properties.storageProfile.imageReference.sku;
                imagereference.version = resource.properties.storageProfile.imageReference.version;
            }
            // if the tool is configured to attach copied disks
            else
            {
                osdisk.createOption = "Attach";
                osdisk.osType = ostype;

                // Block of code to help copying the blobs to the new storage accounts
                string RGName = null;
                Hashtable storageaccountinfo = new Hashtable();

                //GetARMRetriev

                foreach (var SA in artefact.AllStorageAccounts)
                {
                    if (SA.StorageName == oldstorageaccountname)
                    {
                        
                        storageaccountinfo.Add("name", oldstorageaccountname);
                        storageaccountinfo.Add("RGName", SA.RGName);
                        RGName = SA.RGName;
                    }
                }

                var SAList = _asmRetriever.GetAzureARMResources("StorageAccountKeys", subscriptionId, storageaccountinfo, token, RGName);
                var SAresults = JsonConvert.DeserializeObject<dynamic>(SAList);


                //XmlDocument storageaccountkeys = _asmRetriever.GetAzureASMResources("StorageAccountKeys", subscriptionId, storageaccountinfo, token);
                // string key = storageaccountkeys.SelectSingleNode("//StorageServiceKeys/Primary").InnerText;
                string key = SAresults.keys[0].value;
                CopyBlobDetail copyblobdetail = new CopyBlobDetail();
                copyblobdetail.SourceSA = oldstorageaccountname;
                copyblobdetail.SourceContainer = splitarray[3];
                copyblobdetail.SourceBlob = splitarray[4];
                copyblobdetail.SourceKey = key;
                copyblobdetail.DestinationSA = newstorageaccountname;
                copyblobdetail.DestinationContainer = splitarray[3];
                copyblobdetail.DestinationBlob = splitarray[4];
                _copyBlobDetails.Add(copyblobdetail);
                // end of block of code to help copying the blobs to the new storage accounts
            }

            // process data disks
            List<DataDisk> datadisks = new List<DataDisk>();
            var datadisknodes = resource.properties.storageProfile.dataDisks;
            foreach (var datadisknode in datadisknodes)
            {
                DataDisk datadisk = new DataDisk();
                datadisk.name = datadisknode.name;
                datadisk.caching = datadisknode.caching;
                if (datadisknode.diskSizeGB != null)
                {
                    datadisk.diskSizeGB = datadisknode.diskSizeGB;
                }
                else
                {
                    datadisk.diskSizeGB = null;
                }

                datadisk.lun = datadisknode.lun;

                olddiskurl = datadisknode.vhd.uri;
                splitarray = olddiskurl.Split(new char[] { '/' });
                oldstorageaccountname = splitarray[2].Split(new char[] { '.' })[0];
                newstorageaccountname = GetNewStorageAccountName(oldstorageaccountname);
                newdiskurl = olddiskurl.Replace(oldstorageaccountname + ".", newstorageaccountname + ".");

                // if the tool is configured to create new VMs with empty data disks
                if (_settingsProvider.BuildEmpty)
                {
                    datadisk.createOption = "Empty";
                }
                // if the tool is configured to attach copied disks
                else
                {
                    datadisk.createOption = "Attach";

                    string RGName = null;
                    Hashtable storageaccountinfo = new Hashtable();

                    foreach (var SA in artefact.AllStorageAccounts)
                    {
                        if (SA.StorageName == oldstorageaccountname)
                        {

                            storageaccountinfo.Add("name", oldstorageaccountname);
                            storageaccountinfo.Add("RGName", SA.RGName);
                            RGName = SA.RGName;
                        }
                    }

                    var SAList = _asmRetriever.GetAzureARMResources("StorageAccountKeys", subscriptionId, storageaccountinfo, token, RGName);
                    var SAresults = JsonConvert.DeserializeObject<dynamic>(SAList);


                    string key = SAresults.keys[0].value;

                    CopyBlobDetail copyblobdetail = new CopyBlobDetail();
                    copyblobdetail.SourceSA = oldstorageaccountname;
                    copyblobdetail.SourceContainer = splitarray[3];
                    copyblobdetail.SourceBlob = splitarray[4];
                    copyblobdetail.SourceKey = key;
                    copyblobdetail.DestinationSA = newstorageaccountname;
                    copyblobdetail.DestinationContainer = splitarray[3];
                    copyblobdetail.DestinationBlob = splitarray[4];
                    _copyBlobDetails.Add(copyblobdetail);
                    // end of block of code to help copying the blobs to the new storage accounts
                }

                vhd = new Vhd();
                vhd.uri = newdiskurl;
                datadisk.vhd = vhd;

                try { storageaccountdependencies.Add(newstorageaccountname, ""); }
                catch { }

                datadisks.Add(datadisk);
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

            List<string> dependson = new List<string>();

            foreach (var nic in networkinterfaces)
            {
                dependson.Add(nic.id.ToString());
            }

           // dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Network/networkInterfaces/" + networkinterfacename + "')]");

            // Diagnostics Extension
            Extension extension_iaasdiagnostics = null;

            //XmlNodeList resourceextensionreferences = resource.SelectNodes("//ResourceExtensionReferences/ResourceExtensionReference");
            //foreach (XmlNode resourceextensionreference in resourceextensionreferences)
            //{
            //    if (resourceextensionreference.SelectSingleNode("Name").InnerText == "IaaSDiagnostics")
            //    {
            //        string json = Base64Decode(resourceextensionreference.SelectSingleNode("ResourceExtensionParameterValues/ResourceExtensionParameterValue/Value").InnerText);
            //        var resourceextensionparametervalue = JsonConvert.DeserializeObject<dynamic>(json);
            //        string diagnosticsstorageaccount = resourceextensionparametervalue.storageAccount.Value + _settingsProvider.UniquenessSuffix;
            //        string xmlcfgvalue = Base64Decode(resourceextensionparametervalue.xmlCfg.Value);
            //        xmlcfgvalue = xmlcfgvalue.Replace("\n", "");
            //        xmlcfgvalue = xmlcfgvalue.Replace("\r", "");

            //        XmlDocument xmlcfg = new XmlDocument();
            //        xmlcfg.LoadXml(xmlcfgvalue);

            //        XmlNodeList mynodelist = xmlcfg.SelectNodes("/wadCfg/DiagnosticMonitorConfiguration/Metrics");




            //        extension_iaasdiagnostics = new Extension();
            //        extension_iaasdiagnostics.name = "Microsoft.Insights.VMDiagnosticsSettings";
            //        extension_iaasdiagnostics.type = "extensions";
            //        extension_iaasdiagnostics.location = virtualmachineinfo["location"].ToString();
            //        extension_iaasdiagnostics.dependsOn = new List<string>();
            //        extension_iaasdiagnostics.dependsOn.Add("[concat(resourceGroup().id, '/providers/Microsoft.Compute/virtualMachines/" + virtualmachinename + "')]");
            //        extension_iaasdiagnostics.dependsOn.Add("[concat(resourceGroup().id, '/providers/Microsoft.Storage/storageAccounts/" + diagnosticsstorageaccount + "')]");

            //        Extension_Properties extension_iaasdiagnostics_properties = new Extension_Properties();
            //        extension_iaasdiagnostics_properties.publisher = "Microsoft.Azure.Diagnostics";
            //        extension_iaasdiagnostics_properties.type = "IaaSDiagnostics";
            //        extension_iaasdiagnostics_properties.typeHandlerVersion = "1.5";
            //        extension_iaasdiagnostics_properties.autoUpgradeMinorVersion = true;
            //        extension_iaasdiagnostics_properties.settings = new Dictionary<string, string>();
            //        extension_iaasdiagnostics_properties.settings.Add("xmlCfg", "[base64('" + xmlcfgvalue + "')]");
            //        extension_iaasdiagnostics_properties.settings.Add("storageAccount", diagnosticsstorageaccount);
            //        extension_iaasdiagnostics.properties = new Extension_Properties();
            //        extension_iaasdiagnostics.properties = extension_iaasdiagnostics_properties;
            //    }
            //}

            // Availability Set
            // string availabilitysetname = virtualmachineinfo["cloudservicename"] + "-defaultAS";
            string availabilitysetname = null;
            if (resource.properties.availabilitySet != null)
            {
                Hashtable availabilitySetinfo = new Hashtable();
                string AVId = resource.properties.availabilitySet.id;
                AVId = AVId.Replace("/subscriptions", "subscriptions");
                availabilitySetinfo.Add("AvailId", AVId);

                var AvailList = _asmRetriever.GetAzureARMResources("AvailabilitySet", subscriptionId, availabilitySetinfo, token, null);
                var Availresults = JsonConvert.DeserializeObject<dynamic>(AvailList);

                availabilitysetname = Availresults.name;

                Reference availabilityset = new Reference();
                availabilityset.id = "[concat(resourceGroup().id, '/providers/Microsoft.Compute/availabilitySets/" + availabilitysetname + "')]";
                virtualmachine_properties.availabilitySet = availabilityset;

                dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Compute/availabilitySets/" + availabilitysetname + "')]");

            }
            else
            {
               // _messages.Add($"VM '{virtualmachinename}' is not in an availability set. Putting it in a new availability set '{availabilitysetname}'.");
            }



            foreach (DictionaryEntry storageaccountdependency in storageaccountdependencies)
            {
                if (GetProcessedItem("Microsoft.Storage/storageAccounts/" + storageaccountdependency.Key))
                {
                    dependson.Add("[concat(resourceGroup().id, '/providers/Microsoft.Storage/storageAccounts/" + storageaccountdependency.Key + "')]");
                }
            }

            VirtualMachine virtualmachine = new VirtualMachine();
            virtualmachine.name = resource.name;
            virtualmachine.location = resource.location;
            virtualmachine.properties = virtualmachine_properties;
            virtualmachine.dependsOn = dependson;
            virtualmachine.resources = new List<Resource>();
            if (extension_iaasdiagnostics != null) { virtualmachine.resources.Add(extension_iaasdiagnostics); }

            _processedItems.Add("Microsoft.Compute/virtualMachines/" + virtualmachine.name, virtualmachine.location);
            _resources.Add(virtualmachine);
            _logProvider.WriteLog("BuildVirtualMachineObject", "Microsoft.Compute/virtualMachines/" + virtualmachine.name);

            _logProvider.WriteLog("BuildVirtualMachineObject", "End");
        }

        private void BuildARMStorageAccountObject(dynamic resource)
        {
            _logProvider.WriteLog("BuildStorageAccountObject", "Start");

            StorageAccount_Properties storageaccount_properties = new StorageAccount_Properties();
            storageaccount_properties.accountType = resource.properties.accountType.Value;

            StorageAccount storageaccount = new StorageAccount();
            //storageaccount.name = resource.SelectSingleNode("//ServiceName").InnerText + _settingsProvider.UniquenessSuffix;
            storageaccount.name = GetNewStorageAccountName(resource.name.Value);
            storageaccount.location = resource.location.Value;
            storageaccount.properties = storageaccount_properties;

            _processedItems.Add("Microsoft.Storage/storageAccounts/" + storageaccount.name, storageaccount.location);
            _resources.Add(storageaccount);
            _logProvider.WriteLog("BuildStorageAccountObject", "Microsoft.Storage/storageAccounts/" + storageaccount.name);

            _logProvider.WriteLog("BuildStorageAccountObject", "End");
        }

        private void BuildStorageAccountObject(XmlNode resource)
        {
            _logProvider.WriteLog("BuildStorageAccountObject", "Start");

            StorageAccount_Properties storageaccount_properties = new StorageAccount_Properties();
            storageaccount_properties.accountType = resource.SelectSingleNode("//StorageServiceProperties/AccountType").InnerText;

            StorageAccount storageaccount = new StorageAccount();
            //storageaccount.name = resource.SelectSingleNode("//ServiceName").InnerText + _settingsProvider.UniquenessSuffix;
            storageaccount.name = GetNewStorageAccountName( resource.SelectSingleNode("//ServiceName").InnerText );
            storageaccount.location = resource.SelectSingleNode("//ExtendedProperties/ExtendedProperty[Name='ResourceLocation']/Value").InnerText;
            storageaccount.properties = storageaccount_properties;

            _processedItems.Add("Microsoft.Storage/storageAccounts/" + storageaccount.name, storageaccount.location);
            _resources.Add(storageaccount);
            _logProvider.WriteLog("BuildStorageAccountObject", "Microsoft.Storage/storageAccounts/" + storageaccount.name);

            _logProvider.WriteLog("BuildStorageAccountObject", "End");
        }

        private void WriteStream(StreamWriter writer, string text)
        {
            writer.Write(text);
            writer.Close();
        }

        private bool GetProcessedItem(string processeditem)
        {
            if (_processedItems.ContainsKey(processeditem))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        

        // convert an hex string into byte array
        public static byte[] StrToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }

        public static string Base64Decode(string base64EncodedData)
        {
            byte[] base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private string GetNewStorageAccountName(string oldStorageAccountName)
        {
            string newStorageAccountName = "";

            if (_storageAccountNames.ContainsKey(oldStorageAccountName))
            {
                _storageAccountNames.TryGetValue(oldStorageAccountName, out newStorageAccountName);
            }
            else
            {
                newStorageAccountName = oldStorageAccountName + _settingsProvider.UniquenessSuffix;

                if (newStorageAccountName.Length > 24)
                {
                    string randomString = Guid.NewGuid().ToString("N").Substring(0, 4);
                    newStorageAccountName = newStorageAccountName.Substring(0, (24 - randomString.Length - _settingsProvider.UniquenessSuffix.Length)) + randomString + _settingsProvider.UniquenessSuffix;
                }

                _storageAccountNames.Add(oldStorageAccountName, newStorageAccountName);
            }

            return newStorageAccountName;
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
    }
}
