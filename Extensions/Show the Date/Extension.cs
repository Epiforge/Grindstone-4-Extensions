using Quantum.Client.Windows;
using System;
using System.Windows;

return Extension.OnUiThreadAsync(() => MessageDialog.Present(DateTime.Now.ToString("D"), "Show the Date", MessageBoxImage.Information));