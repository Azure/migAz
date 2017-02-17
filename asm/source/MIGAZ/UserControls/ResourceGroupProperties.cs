﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MIGAZ.Azure;
using MIGAZ.Arm;

namespace MIGAZ.UserControls
{
    public partial class ResourceGroupProperties : UserControl
    {
        private AsmToArmForm _ParentForm;
        private TreeNode _ResourceGroupNode;

        public ResourceGroupProperties()
        {
            InitializeComponent();
        }

        internal async Task Bind(AsmToArmForm parentForm, TreeNode resourceGroupNode)
        {
            _ParentForm = parentForm;
            _ResourceGroupNode = resourceGroupNode;

            ArmResourceGroup armResourceGroup = (ArmResourceGroup) _ResourceGroupNode.Tag;

            txtName.Text = armResourceGroup.Name;

            cboTargetLocation.Items.Clear();
            foreach (AzureLocation azureLocation in await _ParentForm.AzureContextTargetARM.AzureRetriever.GetAzureLocations())
            {
                cboTargetLocation.Items.Add(azureLocation);
            }

            if (armResourceGroup.Location != null)
            {
                foreach (AzureLocation azureLocation in cboTargetLocation.Items)
                {
                    if (azureLocation.Name == armResourceGroup.Location.Name)
                        cboTargetLocation.SelectedItem = azureLocation;
                }
            }
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            TextBox txtSender = (TextBox)sender;

            ArmResourceGroup armResourceGroup = (ArmResourceGroup)_ResourceGroupNode.Tag;

            armResourceGroup.Name = txtSender.Text;
            _ResourceGroupNode.Text = armResourceGroup.GetFinalTargetName();
            _ResourceGroupNode.Name = armResourceGroup.Name;
        }

        private void cboTargetLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmbSender = (ComboBox)sender;

            ArmResourceGroup armResourceGroup = (ArmResourceGroup)_ResourceGroupNode.Tag;
            armResourceGroup.Location = (AzureLocation)cmbSender.SelectedItem;
        }
    }
}