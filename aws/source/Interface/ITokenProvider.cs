﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigAzAWS.Interface
{
    public interface ITokenProvider
    {
        string GetToken(string tenantId);
    }
}
