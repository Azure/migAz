using MIGAZ.Generator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MIGAZ.Forms;

namespace MIGAZ.Forms
{
    public partial class ExportResults : Form
    {
        private string _migazPath;
        private string _templatePath;
        private string _blobDetailsPath;
        private string _instructionsPath;
        private AsmRetriever _asmRetriever;
        private string _token;

        public ExportResults(AsmRetriever asmRetriever, string token, List<string> messages, string sourceSubscriptionId, string instructionsPath, string templatePath, string blobDetailsPath)
        {
            InitializeComponent();
            _migazPath = AppDomain.CurrentDomain.BaseDirectory;
            _templatePath = templatePath;
            _blobDetailsPath = blobDetailsPath;
            _instructionsPath = instructionsPath;
            _asmRetriever = asmRetriever;
            _token = token;

            // Initialise messages
            foreach (var message in messages)
            {
                txtMessages.Text += message + "\r\n";
            }

            var TenDetails = _asmRetriever.GetAzureARMResources("Tenants", null, null, token, null);
            var Tenresults = JsonConvert.DeserializeObject<dynamic>(TenDetails);
            
            foreach (var Tenant in Tenresults.value)
            {
                string TenId = Tenant.tenantId;
                //   string subName = Tenant.displayName;
                cboTenants.Items.Add(TenId);
                Application.DoEvents();
            }
           
        }

        private class Subscription
        {
            public string SubscriptionName { get; set; }
            public string SubscriptionId { get; set; }
        }



        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }



        private void btnViewTemplate_Click(object sender, EventArgs e)
        {
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = _templatePath;
            pInfo.UseShellExecute = true;
            Process p = Process.Start(pInfo);
        }

        private void btnGenerateInstructions_Click(object sender, EventArgs e)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MIGAZ.DeployDocTemplate.html";
            string content;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }

            content = content.Replace("{tenantId}", cboTenants.SelectedItem.ToString());
            content = content.Replace("{subscriptionId}", ((Subscription)cboSubscription.SelectedItem).SubscriptionId);
            content = content.Replace("{templatePath}", _templatePath);
            content = content.Replace("{blobDetailsPath}", _blobDetailsPath);
            content = content.Replace("{resourceGroupName}", txtRGName.Text);
            content = content.Replace("{location}", cboRGLocation.Text);
            content = content.Replace("{migAzPath}", _migazPath);

            var writer = new StreamWriter(_instructionsPath);
            writer.Write(content);
            writer.Close();

            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = _instructionsPath;
            pInfo.UseShellExecute = true;
            Process p = Process.Start(pInfo);
        }

        private void ExportResults_Load(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Asterisk.Play();
        }

        private void cboTenants_SelectedIndexChanged(object sender, EventArgs e)
        {
            string token = GetToken(cboTenants.SelectedItem.ToString(), PromptBehavior.Auto);

            var SubDetails = _asmRetriever.GetAzureARMResources("Subscriptions", null, null, token, null);
            var Subresults = JsonConvert.DeserializeObject<dynamic>(SubDetails);


            // Initialise subscriptions
            List<Subscription> subscriptions = new List<Subscription>();
            foreach (var subscription in Subresults.value)
            {
                var sub = new Subscription { SubscriptionName = subscription.displayName, SubscriptionId = subscription.subscriptionId };
                subscriptions.Add(sub);
            }
            cboSubscription.DataSource = subscriptions;
            cboRGLocation.DisplayMember = "SubscriptionName";
           // cboSubscription.SelectedItem = currentSubscription;


        }

        private string GetToken(string tenantId, PromptBehavior promptBehavior, bool updateUI = false)
        {
            //"d94647e7-c4ff-4a93-bbe0-d993badcc5b8"
            AuthenticationContext context = new AuthenticationContext(ServiceUrls.GetLoginUrl(app.Default.AzureAuth) + tenantId);

            AuthenticationResult result = null;
            try
            {
                result = context.AcquireToken(ServiceUrls.GetServiceManagementUrl(app.Default.AzureAuth), app.Default.ClientId, new Uri(app.Default.ReturnURL), promptBehavior);
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the token");
                }
                if (updateUI)
                {
                   // lblSignInText.Text = $"Signed in as {result.UserInfo.DisplayableId}";
                }

                return result.AccessToken;
            }
            catch (Exception exception)
            {
                DialogResult dialogresult = MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void txtRGName_Leave(object sender, EventArgs e)
        {
            // Make sure there are no spaces in the resource group name.
            if (!string.IsNullOrWhiteSpace(txtRGName.Text))
            {
                txtRGName.Text = txtRGName.Text.Trim().Replace(" ", "-");
            }
        }
    }
}
