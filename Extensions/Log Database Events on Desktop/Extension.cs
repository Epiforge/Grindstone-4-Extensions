using Quantum.Client.Windows;
using System;
using System.IO;

Extension.App.DatabaseMounted += (sender, e) => File.AppendAllLines(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop\\DatabaseEvents.txt"), new string[] { $"{DateTime.Now}: mounted \"{App.DatabasePath}\"" });

Extension.App.DatabaseDismounting += (sender, e) => File.AppendAllLines(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop\\DatabaseEvents.txt"), new string[] { $"{DateTime.Now}: dismounting \"{App.DatabasePath}\"" });