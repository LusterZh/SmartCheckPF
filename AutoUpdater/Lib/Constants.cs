using Ezhu.AutoUpdater.utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ezhu.AutoUpdater
{
    public class Constants
    {
        public static string IP = IniUtils.IniReadValue("Station Configuration", "IP", @"C:\config\config.ini");

        public static readonly string RemoteUrl = "http://"+IP + ":8082/static/file/SmartCheckPF";
    }
}