// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace MIGAZ.Generator
{
    class FileLogProvider : ILogProvider
    {
        public FileLogProvider()
        {

        }

        public void WriteLog(string function, string message)
        {
            string logfiledir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MIGAZ";
            if (!Directory.Exists(logfiledir)) { Directory.CreateDirectory(logfiledir); }

            string logfilepath = logfiledir + "\\MIGAZ-" + string.Format("{0:yyyyMMdd}", DateTime.Now) + ".log";
            string text = DateTime.Now.ToString() + "   " + function + "  " + message + Environment.NewLine;
            File.AppendAllText(logfilepath, text);
        }
    }
}

