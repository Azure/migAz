﻿using System;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using MigAz.AWS.Generator;
using MigAz.Forms.AWS;
using System.Net.NetworkInformation;
using MigAzAWS.Models;
using System.Xml;
using System.Collections;
using MigAz.AWS;
using MigAz.Core.Interface;
using MigAzAWS.Interface;
using MigAz.AWS.Forms;
using MigAz.Forms.AWS.Provider;

namespace MigAz.Forms.AWS
{
    public partial class Window : Form
    {
        private ILogProvider _logProvider;
        //private EC2Operation ec2 = null;
        // TODO WHERE?? static IAmazonEC2 service;
        // TODO DescribeInstancesResponse instResponse;
        // TODO DescribeVpcsResponse vpcResponse;
        // TODO DescribeVolumesResponse ebsVolumesResponse;
        string accessKeyID;
        string secretKeyID;
        string selectedregion;
        // TODO List<Amazon.RegionEndpoint> Regions;

        //private string subscriptionid;
        //private Dictionary<string, string> subscriptionsAndTenants;
        private AwsRetriever _awsRetriever;
        private TemplateGenerator _templateGenerator;
        private ISaveSelectionProvider _saveSelectionProvider;
        private IStatusProvider _statusProvider;
        private AwsObjectRetriever _awsObjectRetriever;
        private dynamic telemetryProvider;

        private Window() { }

        public Window(IStatusProvider statusProvider, ILogProvider logProvider)
        {
            InitializeComponent();
            _statusProvider = statusProvider;
            _logProvider = logProvider;
            telemetryProvider = new CloudTelemetryProvider();
            _saveSelectionProvider = new UISaveSelectionProvider();

            // TODO
            //Regions = new List<Amazon.RegionEndpoint>();
            //foreach (var region in Amazon.RegionEndpoint.EnumerableAllRegions)
            //{
            //    Regions.Add(region);
            //}
           
            
           
            //var tokenProvider = new InteractiveTokenProvider();
            //
        }

        private void Window_Load(object sender, EventArgs e)
        {
            writeLog("Window_Load", "Program start");
            // TODO instResponse = new DescribeInstancesResponse();
            this.Text = "migAz AWS (" + Assembly.GetEntryAssembly().GetName().Version.ToString() + ")";

            // TODO cmbRegion.DataSource = Regions;
            NewVersionAvailable(); // check if there a new version of the app
        }

        //TODO CHECK
        private void Window_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If save selection option is enabled
            if (app.Default.SaveSelection)
            {
                _saveSelectionProvider.Save(cmbRegion.Text, lvwVirtualNetworks, lvwVirtualMachines);
            }
        }

        private void btnGetToken_Click(object sender, EventArgs e)
        {
            writeLog("GetToken_Click", "Start");

            try
            {
                //Authenticate
                authenticate();

                cmbRegion.Enabled = true;

                //Load Items
                 // TODO Load_Items();

                lblSignInText.Text = $"Signed in as {accessKeyID}";



            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
                        
        }


       

        private void authenticate()
        {
            AuthenticationForm authForm = new AuthenticationForm();
            if (authForm.ShowDialog(this) != DialogResult.OK)
            {
                this.Close();
            }

            accessKeyID = authForm.GetAWSAccessKeyID();
            secretKeyID = authForm.GetAWSSecretKeyID();

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
            int numofobjects = lvwVirtualNetworks.CheckedItems.Count + lvwVirtualMachines.CheckedItems.Count;
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

            Hashtable teleinfo = new Hashtable();
            teleinfo.Add("Region", cmbRegion.Text);
            teleinfo.Add("AccessKey", accessKeyID);

            var artefacts = new AWSArtefacts();

            foreach (var selectedItem in lvwVirtualNetworks.CheckedItems)
            {
                var listItem = (ListViewItem)selectedItem;
                artefacts.VirtualNetworks.Add(new VPC(listItem.Text, listItem.SubItems[1].Text));
            }

            foreach (var selectedItem in lvwVirtualMachines.CheckedItems)
            {
                var listItem = (ListViewItem)selectedItem;
                artefacts.Instances.Add(new Ec2Instance(listItem.Text,listItem.SubItems[1].Text));
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
                    _statusProvider.UpdateStatus("BUSY: Reading saved selection");
                    _saveSelectionProvider.Save(cmbRegion.Text, lvwVirtualNetworks, lvwVirtualMachines);
                }

                var templateWriter = new StreamWriter(Path.Combine(txtDestinationFolder.Text, "export.json"));
                //var blobDetailWriter = new StreamWriter(Path.Combine(txtDestinationFolder.Text, "copyblobdetails.json"));

                _templateGenerator.GenerateTemplate(artefacts, _awsObjectRetriever, templateWriter, teleinfo);
                
                MessageBox.Show("Template has been generated successfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            btnExport.Enabled = true;
        }


        private void writeLog(string function, string message)
        {
            string logfilepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MIGAZ-" + string.Format("{0:yyyyMMdd}", DateTime.Now) + ".log";
            string text = DateTime.Now.ToString() + "   " + function + "  " + message + Environment.NewLine;
            File.AppendAllText(logfilepath, text);
        }

        private void NewVersionAvailable()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://api.migaz.tools/v1/version/AWStoARM");
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
            formOptions formoptions = new formOptions();
            formoptions.ShowDialog(this);
        }

        private void AutoSelectDependencies(ItemCheckedEventArgs listViewRow)
        {
            // TODO
            //string InstanceId = listViewRow.Item.ListView.Items[listViewRow.Item.Index].SubItems[0].Text;

            //var availableInstances = _awsObjectRetriever.Instances;
            ////var selectedVolumes;
            //if (InstanceId != null)
            //{
                    
            //    //var selectedInstances = availableInstances.Reservations[0].Instances.Find(x => x.InstanceId == InstanceId);
            //    var selectedInstances = _awsObjectRetriever.getInstancebyId(InstanceId);

            //    foreach (ListViewItem virtualNetwork in lvwVirtualNetworks.Items)
            //    {
            //            if (selectedInstances.Instances[0].VpcId == virtualNetwork.SubItems[0].Text)
            //            {
            //                virtualNetwork.Checked = true;
            //                virtualNetwork.Selected = true;
            //            }
                    
            //    }

            //}
        }

        private void cmbRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cmbRegion.Enabled == true)
            {
                //Load the Region Items
                // TODO Load_Items();
            }

            // If save selection option is enabled
            if (app.Default.SaveSelection)
            {
                _statusProvider.UpdateStatus("BUSY: Reading saved selection");
                _saveSelectionProvider.Read(cmbRegion.Text,ref  lvwVirtualNetworks, ref lvwVirtualMachines);
            }

        }

       

        private void lvwVirtualMachines_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
