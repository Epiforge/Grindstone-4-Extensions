using Microsoft.CSharp.RuntimeBinder;
using Quantum.Client.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

async void ReceivedMessageHandler(object sender, ReceivedMessageEventArgs e)
{
    var data = e.Data;
    string title;
    try
    {
        title = data.Title;
    }
    catch (RuntimeBinderException)
    {
        return;
    }
    Visual content = null;
    string text = null;
    try
    {
        if (data.Content is Visual visual)
            content = visual;
    }
    catch (RuntimeBinderException)
    {
    }
    if (content == null)
    {
        try
        {
            text = data.Text;
        }
        catch (RuntimeBinderException)
        {
            return;
        }
    }
    int? autoCloseMilliseconds = null;
    try
    {
        autoCloseMilliseconds = data.AutoCloseMilliseconds;
    }
    catch (RuntimeBinderException)
    {
    }
    bool canUserClose = true;
    try
    {
        canUserClose = data.CanUserClose;
    }
    catch (RuntimeBinderException)
    {
    }
    Action onClick = null;
    try
    {
        onClick = data.OnClick;
    }
    catch (RuntimeBinderException)
    {
    }
    if (content != null)
        await App.ShowToastAsync(title, content, null, autoCloseMilliseconds, canUserClose, onClick);
    else
        await App.ShowToastAsync(title, text, null, autoCloseMilliseconds, canUserClose, onClick);
}

Extension.ReceivedMessage += ReceivedMessageHandler;