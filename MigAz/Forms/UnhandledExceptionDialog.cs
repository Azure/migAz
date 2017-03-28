﻿using MigAz.Core.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MigAz.Forms
{
    public partial class UnhandledExceptionDialog : Form
    {
        Exception _UnhandledException;
        ILogProvider _LogProvider;

        private UnhandledExceptionDialog() { }

        public UnhandledExceptionDialog(ILogProvider fileLogProvider, Exception e)
        {
            InitializeComponent();
            _UnhandledException = e;
            _LogProvider = fileLogProvider;

            textBox1.Text = e.Message + Environment.NewLine + e.StackTrace;

            _LogProvider.WriteLog("UnhandledExceptionDialog", textBox1.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Azure/migAz/issues/new");
        }
    }
}