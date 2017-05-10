using System;
using System.Windows.Forms;
using System.Collections;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;
using MIGAZ.Models;
using MIGAZ.Generator;
using MIGAZ.Forms;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MIGAZ
{
    public partial class Window : Form
    {
        private string subscriptionid;
        private Dictionary<string, string> subscriptionsAndTenants;
        private AsmRetriever _asmRetriever;
        private TemplateGenerator _templateGenerator;
        private ISaveSelectionProvider _saveSelectionProvider;
        private ILogProvider _logProvider;
        private IStatusProvider _statusProvider;
        private UserIdentifier _userId=null;

        public Window()
        {
            InitializeComponent();
            _logProvider = new FileLogProvider();
            _statusProvider = new UIStatusProvider(lblStatus);
            _asmRetriever = new AsmRetriever(_logProvider, _statusProvider);
            _saveSelectionProvider = new UISaveSelectionProvider();
            var tokenProvider = new InteractiveTokenProvider();
            var telemetryProvider = new CloudTelemetryProvider();
            var settingsProvider = new AppSettingsProvider();
            _templateGenerator = new TemplateGenerator(_logProvider, _statusProvider, telemetryProvider, tokenProvider, _asmRetriever, settingsProvider);
        }

        public class Susbcription
        {
            public string subscriptionID { get; set; }
            public string DisplayName { get; set; }
        }

        private void Window_Load(object sender, EventArgs e)
        {
            writeLog("Window_Load", "Program start");

            this.Text = "migAz ARM (" + Assembly.GetEntryAssembly().GetName().Version.ToString() + ")";
            NewVersionAvailable(); // check if there a new version of the app
        }

        private void Window_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If save selection option is enabled
            if (app.Default.SaveSelection)
            {
                try
                {
                    _saveSelectionProvider.Save(Guid.Parse(subscriptionid), lvwVirtualNetworks, lvwStorageAccounts, lvwVirtualMachines);
                }
                catch { //Ignore save selection if no objects are selected
                }
            }
        }



        private void btnGetToken_Click(object sender, EventArgs e)
        {
            writeLog("GetToken_Click", "Start");


            cmbSubscriptions.Enabled = false;
            cmbTenants.Enabled = false;
            _userId = null;
            cmbTenants.Items.Clear();
            cmbSubscriptions.Items.Clear();
            lvwVirtualNetworks.Items.Clear();
            lvwStorageAccounts.Items.Clear();
            lvwVirtualMachines.Items.Clear();
            _asmRetriever._documentCache.Clear(); // need to clear cache to allow relogin without returning previous cached list of subscriptions


            string token = GetToken("common", PromptBehavior.Always, true);

            if (token != null)
            {

                //List Tenants
                var TenDetails = _asmRetriever.GetAzureARMResources("Tenants", null, null, token,null);
                var Tenresults = JsonConvert.DeserializeObject<dynamic>(TenDetails);

                foreach (var Tenant in Tenresults.value)
                {
                    string TenId = Tenant.tenantId;
                    
                    //Gathering the Token for Graph to list the Tenant Information
                    var token_grpah = GetToken(TenId, PromptBehavior.Auto,false,"GraphAuth");

                    var DomDetails = _asmRetriever.GetAzureARMResources("Domains", null, null, token_grpah, null);
                    var Domresults = JsonConvert.DeserializeObject<dynamic>(DomDetails);

                    foreach (var Domain in Domresults.value)
                    {

                        if (Domain.isDefault == true)
                        {
                            string TenantDesc = TenId + " | " + Domain.name;
                            cmbTenants.Items.Add(TenantDesc);
                        }   
                    }

                    

                    Application.DoEvents();
                }


           //     cmbSubscriptions.Enabled = true;
                cmbTenants.Enabled = true;
                txtDestinationFolder.Enabled = true;
                btnChoosePath.Enabled = true;
            }
            else
            {
                writeLog("GetToken_Click", "Failed to get token");
            }

            lblStatus.Text = "Ready";
            writeLog("GetToken_Click", "End");
        }

        private string GetToken(string tenantId, PromptBehavior promptBehavior, bool updateUI = false,string AuthType="AzureAuth")
        {
  
            lblStatus.Text = "BUSY: Authenticating...";
            //"d94647e7-c4ff-4a93-bbe0-d993badcc5b8"
            AuthenticationContext context = new AuthenticationContext(ServiceUrls.GetLoginUrl(app.Default.AzureAuth) + tenantId);

            AuthenticationResult result = null;
            string AuthUri = null;

            if (AuthType == "AzureAuth")
            {
                AuthUri = ServiceUrls.GetServiceManagementUrl(app.Default.AzureAuth);
            }
            else
            {
                AuthUri = ServiceUrls.GetServiceManagementUrl(app.Default.GraphAuth);
            }
            
            try
            {
                if(_userId == null)
                { 
                result = context.AcquireToken(AuthUri, app.Default.ClientId, new Uri(app.Default.ReturnURL), promptBehavior);
                _userId = new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.RequiredDisplayableId);
                }
                else
                {
                result = context.AcquireToken(AuthUri, app.Default.ClientId, new Uri(app.Default.ReturnURL), PromptBehavior.Never, _userId);
                // result = context.AcquireTokenSilent(ServiceUrls.GetServiceManagementUrl(app.Default.GraphAuth), app.Default.ClientId, _userId)
                }
                   
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the token");
                }
                if (updateUI)
                {
                    lblSignInText.Text = $"Signed in as {result.UserInfo.DisplayableId}";
                }

                return result.AccessToken;
            }
            catch (Exception exception)
            {
                DialogResult dialogresult = MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void cmbSubscriptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSubscriptions.Enabled == true)
            {

                lvwVirtualNetworks.Items.Clear();
                lvwStorageAccounts.Items.Clear();
                lvwVirtualMachines.Items.Clear();

                subscriptionid = cmbSubscriptions.SelectedItem.ToString().Split(new char[] { '|' })[0].ToString().Trim();
                var token = GetToken(subscriptionsAndTenants[subscriptionid], PromptBehavior.Auto);


                //Resource Group listing
                var RGList = _asmRetriever.GetAzureARMResources("RG", subscriptionid, null, token,null);
                var RGresults = JsonConvert.DeserializeObject<dynamic>(RGList);

                foreach (var RG in RGresults.value)
                {
                    string RGName = RG.name;

                    //Listing Virtual Network from each RG
                    var NWList = _asmRetriever.GetAzureARMResources("VirtualNetworks", subscriptionid, null, token, RGName);
                    var NWresults = JsonConvert.DeserializeObject<dynamic>(NWList);

                    foreach (var vnet in NWresults.value)
                    {
                        string vnetName = vnet.name;
                        var listItem = new ListViewItem(RGName);
                        listItem.SubItems.AddRange(new[] { vnetName });
                        lvwVirtualNetworks.Items.Add(listItem);
                        Application.DoEvents();
                    }

                    //Listing Storage Account from each RG
                    var SAList = _asmRetriever.GetAzureARMResources("StorageAccounts", subscriptionid, null, token, RGName);
                    var SAresults = JsonConvert.DeserializeObject<dynamic>(SAList);

                    foreach (var SA in SAresults.value)
                    {
                        string SAName = SA.name;
                        var listItem = new ListViewItem(RGName);
                        listItem.SubItems.AddRange(new[] { SAName });
                        lvwStorageAccounts.Items.Add(listItem);
                        Application.DoEvents();
                    }

                    //Listing Virtual Machines from each RG
                    var VMList = _asmRetriever.GetAzureARMResources("VirtualMachines", subscriptionid, null, token, RGName);
                    var VMresults = JsonConvert.DeserializeObject<dynamic>(VMList);

                    foreach (var VM in VMresults.value)
                    {
                        string VMName = VM.name;
                        var listItem = new ListViewItem(RGName);
                        listItem.SubItems.AddRange(new[] { VMName });
                        lvwVirtualMachines.Items.Add(listItem);
                        Application.DoEvents();
                    }
                }

                if (app.Default.SaveSelection)
                {
                    lblStatus.Text = "BUSY: Reading saved selection";
                    Application.DoEvents();
                    _saveSelectionProvider.Read(Guid.Parse(subscriptionid),ref lvwVirtualNetworks, ref lvwStorageAccounts, ref lvwVirtualMachines);
                }

                lblStatus.Text = "Ready";

                
                writeLog("Subscriptions_SelectionChanged", "End");
            }
        }


        private void lvwVirtualNetworks_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateExportItemsCount();
        }

        private void lvwStorageAccounts_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateExportItemsCount();
        }

        private void lvwVirtualMachines_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateExportItemsCount();

            if (app.Default.AutoSelectDependencies)
            {
                if (e.Item.Checked)
                {
                    AutoSelectDependencies(e);
                }
            }
            
        }

        private void UpdateExportItemsCount()
        {
            int numofobjects = lvwVirtualNetworks.CheckedItems.Count + lvwStorageAccounts.CheckedItems.Count + lvwVirtualMachines.CheckedItems.Count;
            btnExport.Text = "Export " + numofobjects.ToString() + " objects";
        }

        private void btnChoosePath_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
                txtDestinationFolder.Text = folderBrowserDialog.SelectedPath;
        }

        private void txtDestinationFolder_TextChanged(object sender, EventArgs e)
        {
            if (txtDestinationFolder.Text == "")
                btnExport.Enabled = false;
            else
                btnExport.Enabled = true;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            btnExport.Enabled = false;
            
            var artefacts = new AsmArtefacts();
            foreach (var selectedItem in lvwStorageAccounts.CheckedItems)
            {
                var listItem = (ListViewItem)selectedItem;
                artefacts.StorageAccounts.Add(
               new Storage
                {
                    RGName = listItem.Text,
                    StorageName = listItem.SubItems[1].Text,
                });
            }

            foreach (var selectedItem in lvwStorageAccounts.Items)
            {
                var listItem = (ListViewItem)selectedItem;
                artefacts.AllStorageAccounts.Add(
               new Storage
               {
                   RGName = listItem.Text,
                   StorageName = listItem.SubItems[1].Text,
               });
            }


            foreach (var selectedItem in lvwVirtualNetworks.CheckedItems)
            {
                var listItem = (ListViewItem)selectedItem;
                artefacts.VirtualNetworks.Add(
                    new VirtualNW
                    {
                        RGName = listItem.Text,
                        VirtualNWName = listItem.SubItems[1].Text,
                    });
            }

            foreach (var selectedItem in lvwVirtualMachines.CheckedItems)
            {
                var listItem = (ListViewItem)selectedItem;
                artefacts.VirtualMachines.Add(
                    new VirtualMC
                    {
                        RGName = listItem.Text,
                        VirtualMachine = listItem.SubItems[1].Text,
                    });
            }

            if (!Directory.Exists(txtDestinationFolder.Text))
            {
                MessageBox.Show("The chosen output folder does not exist.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                // If save selection option is enabled
                if (app.Default.SaveSelection)
                {
                    lblStatus.Text = "BUSY: Reading saved selection";
                    Application.DoEvents();
                    _saveSelectionProvider.Save(Guid.Parse(subscriptionid), lvwVirtualNetworks, lvwStorageAccounts, lvwVirtualMachines);
                }

                var instructionsPath = Path.Combine(txtDestinationFolder.Text, "DeployInstructions.html");
                var templatePath = Path.Combine(txtDestinationFolder.Text, "export.json");
                var blobDetailsPath = Path.Combine(txtDestinationFolder.Text, "copyblobdetails.json");
                var templateWriter = new StreamWriter(templatePath);
                var blobDetailWriter = new StreamWriter(blobDetailsPath);
                try
                {
                    var messages = _templateGenerator.GenerateTemplate(subscriptionsAndTenants[subscriptionid], subscriptionid, artefacts, templateWriter, blobDetailWriter);
                    var token = GetToken(subscriptionsAndTenants[subscriptionid], PromptBehavior.Auto);
                    var exportResults = new ExportResults(_asmRetriever, token, messages, subscriptionid, instructionsPath, templatePath, blobDetailsPath);
                    exportResults.ShowDialog(this);
            }
                catch (Exception ex)
            {
                writeLog("btnExport_Click", "Error generating template : " + ex.ToString());
                MessageBox.Show("Something went wrong when generating the template. Check the log file for details.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

            lblStatus.Text = "Done";
            btnExport.Enabled = true;
        }

        

        private void writeLog(string function, string message)
        {
            string logfilepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MigAz\\MIGAZ-" + string.Format("{0:yyyyMMdd}", DateTime.Now) + ".log";
            string text = DateTime.Now.ToString() + "   " + function + "  " + message + Environment.NewLine;
            File.AppendAllText(logfilepath, text);
        }



        private void NewVersionAvailable()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://api.migaz.tools/v1/version/ARMtoARM");
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                string version = "\"" + Assembly.GetEntryAssembly().GetName().Version.ToString() + "\"";
                string availableversion = result.ToString();

                if (version != availableversion)
                {
                    DialogResult dialogresult = MessageBox.Show("New version " + availableversion + " is available at http://aka.ms/MIGAZ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception exception)
            {
                DialogResult dialogresult = MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

       

        private void btnOptions_Click(object sender, EventArgs e)
        {
            Forms.formOptions formoptions = new Forms.formOptions();
            formoptions.ShowDialog(this);
        }


        private void AutoSelectDependencies (ItemCheckedEventArgs listViewRow)
        {
            string RGName = listViewRow.Item.ListView.Items[listViewRow.Item.Index].SubItems[0].Text;
            string virtualMachineName = listViewRow.Item.ListView.Items[listViewRow.Item.Index].SubItems[1].Text;

            string virtualNetworkName = "";


            // Get Subscription from ComboBox
            var token = GetToken(subscriptionsAndTenants[subscriptionid], PromptBehavior.Auto);
            
            // Get VM details
            _asmRetriever.GetARMVMDetails(subscriptionid, RGName, virtualMachineName, token, out virtualNetworkName);

            Hashtable virtualmachineinfo = new Hashtable();
            virtualmachineinfo.Add("resourcegroup", RGName);
            virtualmachineinfo.Add("virtualmachineName", virtualMachineName);
            virtualmachineinfo.Add("virtualnetworkname", virtualNetworkName);
 

            //Listing VMDetails
            var VMDetails = _asmRetriever.GetAzureARMResources("VirtualMachine", subscriptionid, virtualmachineinfo, token, RGName);
            var virtualmachine = JsonConvert.DeserializeObject<dynamic>(VMDetails);

            // process virtual network
            foreach (ListViewItem virtualNetwork in lvwVirtualNetworks.Items)
            {
                string VNSubstring = "/resourceGroups/" + virtualNetwork.SubItems[0].Text + "/providers/Microsoft.Network/virtualNetworks/" + virtualNetwork.SubItems[1].Text + "/";

                bool VNSw = virtualNetworkName.Contains(VNSubstring);

                if (VNSw == true)
                {
                    virtualNetwork.Checked = true;
                    virtualNetwork.Selected = true;
                }
            }


            //Process OS Disk
          
            string OSDiskuri = virtualmachine.properties.storageProfile.osDisk.vhd.uri;
                                     
            string[] splitarray = OSDiskuri.Split(new char[] { '/' });
            string storageaccountname = splitarray[2].Split(new char[] { '.' })[0];

            foreach (ListViewItem storageAccount in lvwStorageAccounts.Items)
            {
                if (storageAccount.SubItems[1].Text == storageaccountname)
                 {
                    lvwStorageAccounts.Items[storageAccount.Index].Checked = true;
                    lvwStorageAccounts.Items[storageAccount.Index].Selected = true;
                  }
            }



            //Process Data Disks

            foreach (var DataDisk in virtualmachine.properties.storageProfile.dataDisks)
            {
                string DataDiskuri = DataDisk.vhd.uri;

                splitarray = DataDiskuri.Split(new char[] { '/' });
                storageaccountname = splitarray[2].Split(new char[] { '.' })[0];

                foreach (ListViewItem storageAccount in lvwStorageAccounts.Items)
                {
                    if (storageAccount.SubItems[1].Text == storageaccountname)
                    {
                        lvwStorageAccounts.Items[storageAccount.Index].Checked = true;
                        lvwStorageAccounts.Items[storageAccount.Index].Selected = true;
                    }
                }
            }


                lblStatus.Text = "Ready";
        }

        private void RG_SelectedIndexChanged(object sender, EventArgs e)
        {
            

        }

        private void lvwVirtualMachines_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmbTenants_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbSubscriptions.Enabled = false;
            cmbSubscriptions.Items.Clear();

            string Tenid = cmbTenants.SelectedItem.ToString().Split(new char[] { '|' })[0].ToString().Trim();


            var token = GetToken(Tenid, PromptBehavior.Auto);
            //  subscriptionid = cmbSubscriptions.SelectedItem.ToString().Split(new char[] { '|' })[0].ToString().Trim();

            //Listing Subscriptions
            var subdetails = _asmRetriever.GetAzureARMResources("Subscriptions", null, null, token,null);
            var results = JsonConvert.DeserializeObject<dynamic>(subdetails);
            subscriptionsAndTenants = new Dictionary<string, string>();

            foreach (var sub in results.value)
            {
                string subId = sub.subscriptionId;
                string subName = sub.displayName;

                cmbSubscriptions.Items.Add(subId + " | " + subName);
                subscriptionsAndTenants.Add(subId, Tenid);

                Application.DoEvents();
            }


            cmbSubscriptions.Enabled = true;


        }

        private void lvwVirtualMachines_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}

