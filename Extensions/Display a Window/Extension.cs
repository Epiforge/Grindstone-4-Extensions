using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

return Extension.OnUiThreadAsync(() =>
{
    var exampleWindow = (Window)Extension.LoadUiElement("ExampleWindow.xaml");
    ((Button)LogicalTreeHelper.FindLogicalNode(exampleWindow, "goodbye")).Click += (sender, e) => exampleWindow.DialogResult = true;
    exampleWindow.ShowDialog();
});