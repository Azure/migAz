// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MigAz.Azure.Core.Interface;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigAz.Azure.Arm
{
    public class NetworkSecurityGroup : ArmResource, INetworkSecurityGroup
    {
        private List<NetworkSecurityGroupRule> _Rules = new List<NetworkSecurityGroupRule>();

        private NetworkSecurityGroup() : base(null, null) { }

        public NetworkSecurityGroup(AzureSubscription azureSubscription, JToken resourceToken) : base(azureSubscription, resourceToken)
        {
            foreach (JToken securityRulesToken in ResourceToken["properties"]["securityRules"])
            {
                _Rules.Add(new NetworkSecurityGroupRule(securityRulesToken));
            }
        }

        public List<NetworkSecurityGroupRule> Rules => _Rules;

        public override string ToString()
        {
            return this.Name;
        }
    }
}

