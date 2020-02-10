using Microsoft.CSharp.RuntimeBinder;
using Quantum.Client.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Telerik.Windows.Controls;

class GreaterThanZeroIsVisibleValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int integer && integer > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

var extensionsMenuItem = await Extension.OnUiThreadAsync(() =>
{
    var menuItem = new RadMenuItem { Header = "Extensions" };
    menuItem.SetBinding(RadMenuItem.VisibilityProperty, new Binding("Items.Count")
    {
        Converter = new GreaterThanZeroIsVisibleValueConverter(),
        Source = menuItem
    });
    return menuItem;
});

async Task<RadMenuItem> ConstructMenuItemAsync(dynamic data)
{
    string title = null;
    try
    {
        title = data.Title;
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
    var children = new List<RadMenuItem>();
    dynamic[] items = null;
    try
    {
        items = data.Items;
    }
    catch (RuntimeBinderException)
    {
    }
    if (items is dynamic[])
        foreach (var item in items)
            children.Add(await ConstructMenuItemAsync(item));
    return await Extension.OnUiThreadAsync(() =>
    {
        var menuItem = new RadMenuItem();
        if (title is null)
            menuItem.IsSeparator = true;
        else
            menuItem.Header = title;
        if (onClick is Action)
            menuItem.Click += (sender, e) => onClick();
        foreach (var child in children)
            menuItem.Items.Add(child);
        return menuItem;
    });
}

async void ReceivedMessageHandler(object sender, ReceivedMessageEventArgs e)
{
    var data = e.Data;
    if ((data is RadMenuItem preconstructedMenuItem ? preconstructedMenuItem : await ConstructMenuItemAsync(data)) is RadMenuItem toBeAdded)
        await Extension.OnUiThreadAsync(() => extensionsMenuItem.Items.Add(toBeAdded));
}

void MainWindowClosed(object sender, EventArgs e) =>
    ((RadMenu)LogicalTreeHelper.FindLogicalNode((MainWindow)sender, "mainMenu")).Items.Remove(extensionsMenuItem);

void WindowLoaded(object sender, RoutedEventArgs e)
{
    if (sender is MainWindow mainWindow)
    {
        var mainMenu = (RadMenu)LogicalTreeHelper.FindLogicalNode(mainWindow, "mainMenu");
        mainMenu.Items.Insert(mainMenu.Items.Count - 1, extensionsMenuItem);
        mainWindow.Closed += MainWindowClosed;
    }
}

EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowLoaded));
Extension.ReceivedMessage += ReceivedMessageHandler;