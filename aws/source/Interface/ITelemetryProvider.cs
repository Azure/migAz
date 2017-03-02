﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigAzAWS.Interface
{
    public interface ITelemetryProvider
    {
        void PostTelemetryRecord(string AccessKey, Dictionary<string, string> processedItems, string Region);
    }
}