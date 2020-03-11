using Microsoft.Win32;
using Quantum;
using Quantum.Client.Windows;
using Quantum.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Telerik.Windows.Controls;

class BooleanIsNegatedValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean ? !boolean : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean ? !boolean : Binding.DoNothing;
}

[Table("Assignments")]
class SqliteAssignment
{
    [PrimaryKey] public Guid Id { get; set; }
    [Indexed, NotNull] public Guid WorkItemId { get; set; }
    [Indexed, NotNull] public Guid PersonId { get; set; }
    public DateTimeOffset? Complete { get; set; }
    public DateTimeOffset? Due { get; set; }
    public TimeSpan? Estimate { get; set; }
}

[Table("ListValueProperties")]
class SqliteListValueProperty
{
    [PrimaryKey] public Guid Id { get; set; }
    [NotNull] public string Name { get; set; }
    public string Notes { get; set; }
}

[Table("ListValuePropertyValues")]
class SqliteListValuePropertyValue
{
    [Indexed(Name = "WorkItemAndListValueProperty", Order = 1, Unique = true), NotNull] public Guid WorkItemId { get; set; }
    [Indexed(Name = "WorkItemAndListValueProperty", Order = 2, Unique = true), NotNull] public Guid ListValuePropertyId { get; set; }
    [NotNull] public Guid ListValueId { get; set; }
}

[Table("ListValues")]
class SqliteListValue
{
    [PrimaryKey] public Guid Id { get; set; }
    [Indexed, NotNull] public Guid ListValuePropertyId { get; set; }
    [NotNull] public string Name { get; set; }
}

[Table("People")]
class SqlitePerson
{
    [PrimaryKey] public Guid Id { get; set; }
    [NotNull] public string Name { get; set; }
    public DateTimeOffset? WentAway { get; set; }
}

[Table("TextProperties")]
class SqliteTextProperty
{
    [PrimaryKey] public Guid Id { get; set; }
    [NotNull] public string Name { get; set; }
    public string Notes { get; set; }
}

[Table("TextPropertyValues")]
class SqliteTextPropertyValue
{
    [Indexed(Name = "WorkItemAndTextPropertyId", Order = 1, Unique = true), NotNull] public Guid WorkItemId { get; set; }
    [Indexed(Name = "WorkItemAndTextPropertyId", Order = 2, Unique = true), NotNull] public Guid TextPropertyId { get; set; }
    [NotNull] public string Text { get; set; }
}

[Table("TimeSlices")]
class SqliteTimeSlice
{
    [PrimaryKey] public Guid Id { get; set; }
    [Indexed, NotNull] public Guid WorkItemId { get; set; }
    [Indexed, NotNull] public Guid PersonId { get; set; }
    [NotNull] public DateTimeOffset Start { get; set; }
    [NotNull] public DateTimeOffset End { get; set; }
    public string Notes { get; set; }
}

[Table("TimingSlices")]
class SqliteTimingSlice
{
    [PrimaryKey] public Guid Id { get; set; }
}

[Table("WorkItems")]
class SqliteWorkItem
{
    [PrimaryKey] public Guid Id { get; set; }
    [NotNull] public string Name { get; set; }
    public string Notes { get; set; }
}

class SqliteTransactor : Transactor, INotifyPropertyChanged, INotifyPropertyChanging
{
    public SqliteTransactor(ExtensionGlobals extension, EngineStartingEventArgs engineStartingEventArgs)
    {
        this.extension = extension;
        this.engineStartingEventArgs = engineStartingEventArgs;
    }

    ChangeSet changesLoadedFromSql;
    SQLiteAsyncConnection conn = null;
    readonly EngineStartingEventArgs engineStartingEventArgs;
    readonly ExtensionGlobals extension;
    bool isConnectedToSqlite;

    public bool IsConnectedToSqlite
    {
        get => isConnectedToSqlite;
        private set => SetBackedProperty(ref isConnectedToSqlite, in value);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangingEventHandler PropertyChanging;

    async Task ApplyChangesToSqliteAsync(ChangeSet changes)
    {
        if (changes.PeriodsStopTiming?.Any() ?? false)
            foreach (var id in changes.PeriodsStopTiming)
                await conn.DeleteAsync<SqliteTimingSlice>(id);
        if (changes.DeleteInterests?.Any() ?? false)
            foreach (var id in changes.DeleteInterests)
                await conn.DeleteAsync<SqliteAssignment>(id);
        if (changes.DeletePeriods?.Any() ?? false)
            foreach (var id in changes.DeletePeriods)
            {
                await conn.DeleteAsync<SqliteTimingSlice>(id);
                await conn.DeleteAsync<SqliteTimeSlice>(id);
            }
        if (changes.DeleteAttributeValues?.Any() ?? false)
            foreach (var key in changes.DeleteAttributeValues)
            {
                await conn.ExecuteAsync("delete from [ListValuePropertyValues] where [WorkItemId] = ? and [ListValuePropertyId] = ?", key.ObjectId, key.AttributeId);
                await conn.ExecuteAsync("delete from [TextPropertyValues] where [WorkItemId] = ? and [TextPropertyId] = ?", key.ObjectId, key.AttributeId);
            }
        if (changes.DeleteEnumerationValues?.Any() ?? false)
            foreach (var id in changes.DeleteEnumerationValues)
            {
                await conn.ExecuteAsync("delete from [ListValuePropertyValues] where [ListValueId] = ?", id);
                await conn.DeleteAsync<SqliteListValue>(id);
            }
        if (changes.DeleteAttributes?.Any() ?? false)
            foreach (var id in changes.DeleteAttributes)
            {
                await conn.ExecuteAsync("delete from [ListValuePropertyValues] where [ListValuePropertyId] = ?", id);
                await conn.ExecuteAsync("delete from [TextPropertyValues] where [TextPropertyId] = ?", id);
                await conn.ExecuteAsync("delete from [ListValues] where [ListValuePropertyId] = ?", id);
                await conn.ExecuteAsync("delete from [ListValueProperties] where [Id] = ?", id);
                await conn.ExecuteAsync("delete from [TextProperties] where [Id] = ?", id);
            }
        if (changes.DeletePeople?.Any() ?? false)
            foreach (var id in changes.DeletePeople)
            {
                await conn.ExecuteAsync("delete from [Assignments] where [PersonId] = ?", id);
                await conn.ExecuteAsync("delete from [TimingSlices] where [Id] in (select [Id] from [TimeSlices] where [PersonId] = ?)", id);
                await conn.ExecuteAsync("delete from [TimeSlices] where [PersonId] = ?", id);
                await conn.DeleteAsync<SqlitePerson>(id);
            }
        if (changes.DeleteItems?.Any() ?? false)
            foreach (var id in changes.DeleteItems)
            {
                await conn.ExecuteAsync("delete from [ListValuePropertyValues] where [WorkItemId] = ?", id);
                await conn.ExecuteAsync("delete from [TextPropertyValues] where [WorkItemId] = ?", id);
                await conn.ExecuteAsync("delete from [Assignments] where [WorkItemId] = ?", id);
                await conn.ExecuteAsync("delete from [TimingSlices] where [Id] in (select [Id] from [TimeSlices] where [WorkItemId] = ?)", id);
                await conn.ExecuteAsync("delete from [TimeSlices] where [WorkItemId] = ?", id);
                await conn.DeleteAsync<SqliteWorkItem>(id);
            }
        if (changes.CreateOrUpdateAttributes?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateAttributes)
            {
                if (kv.Value.IsEnumeration)
                    await conn.InsertOrReplaceAsync(new SqliteListValueProperty
                    {
                        Id = kv.Key,
                        Name = kv.Value.Name,
                        Notes = kv.Value.Notes
                    });
                else
                    await conn.InsertOrReplaceAsync(new SqliteTextProperty
                    {
                        Id = kv.Key,
                        Name = kv.Value.Name,
                        Notes = kv.Value.Notes
                    });
            }
        if (changes.CreateOrUpdatePeople?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdatePeople)
                await conn.InsertOrReplaceAsync(new SqlitePerson
                {
                    Id = kv.Key,
                    Name = kv.Value.Name,
                    WentAway = kv.Value.WentAway
                });
        if (changes.CreateOrUpdateItems?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateItems)
                await conn.InsertOrReplaceAsync(new SqliteWorkItem
                {
                    Id = kv.Key,
                    Name = kv.Value.Name,
                    Notes = kv.Value.Notes
                });
        if (changes.CreateOrUpdateEnumerationValues?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateEnumerationValues)
                await conn.InsertOrReplaceAsync(new SqliteListValue
                {
                    Id = kv.Key,
                    ListValuePropertyId = kv.Value.AttributeId,
                    Name = kv.Value.CorrelatedEntity.Name
                });
        if (changes.CreateOrUpdateAttributeValues?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateAttributeValues)
            {
                if (kv.Value is Guid guidValue)
                    await conn.InsertOrReplaceAsync(new SqliteListValuePropertyValue
                    {
                        WorkItemId = kv.Key.ObjectId,
                        ListValuePropertyId = kv.Key.AttributeId,
                        ListValueId = guidValue
                    });
                else
                    await conn.InsertOrReplaceAsync(new SqliteTextPropertyValue
                    {
                        WorkItemId = kv.Key.ObjectId,
                        TextPropertyId = kv.Key.AttributeId,
                        Text = (string)kv.Value
                    });
            }
        if (changes.CreateOrUpdateInterests?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateInterests)
                await conn.InsertOrReplaceAsync(new SqliteAssignment
                {
                    Id = kv.Key,
                    WorkItemId = kv.Value.ItemId,
                    PersonId = kv.Value.PersonId,
                    Complete = kv.Value.CorrelatedEntity.Complete,
                    Due = kv.Value.CorrelatedEntity.Due,
                    Estimate = kv.Value.CorrelatedEntity.Estimate
                });
        if (changes.CreateOrUpdatePeriods?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdatePeriods)
                await conn.InsertOrReplaceAsync(new SqliteTimeSlice
                {
                    Id = kv.Key,
                    WorkItemId = kv.Value.ItemId,
                    PersonId = kv.Value.PersonId,
                    Start = kv.Value.CorrelatedEntity.Start,
                    End = kv.Value.CorrelatedEntity.End,
                    Notes = kv.Value.CorrelatedEntity.Notes
                });
        if (changes.PeriodsStartTiming?.Any() ?? false)
            foreach (var id in changes.PeriodsStartTiming)
                await conn.InsertAsync(new SqliteTimingSlice { Id = id });
    }

    public async Task ConnectToSqliteAsync(string filePath)
    {
        if (conn is SQLiteAsyncConnection)
            throw new InvalidOperationException();
        try
        {
            conn = new SQLiteAsyncConnection(new SQLiteConnectionString(filePath));
            await conn.CreateTableAsync<SqliteAssignment>();
            await conn.CreateTableAsync<SqliteListValueProperty>();
            await conn.CreateTableAsync<SqliteListValuePropertyValue>();
            await conn.CreateTableAsync<SqliteListValue>();
            await conn.CreateTableAsync<SqlitePerson>();
            await conn.CreateTableAsync<SqliteTextProperty>();
            await conn.CreateTableAsync<SqliteTextPropertyValue>();
            await conn.CreateTableAsync<SqliteTimeSlice>();
            await conn.CreateTableAsync<SqliteTimingSlice>();
            await conn.CreateTableAsync<SqliteWorkItem>();
            await ApplyChangesToSqliteAsync((await SnapshotAsync()).ToInitializationChangeSet());
            extension.DatabaseStorage.Set("FilePath", filePath);
            IsConnectedToSqlite = true;
        }
        catch (Exception ex)
        {
            if (conn is SQLiteAsyncConnection liveConn)
                await liveConn.CloseAsync();
            conn = null;
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public async Task DisconnectFromSqliteAsync()
    {
        if (conn is null)
            throw new InvalidOperationException();
        await conn.CloseAsync();
        conn = null;
        extension.DatabaseStorage.Set("FilePath", null);
        IsConnectedToSqlite = false;
    }

    public T GetDatabaseStorageValue<T>(string key) => extension.DatabaseStorage is null ? engineStartingEventArgs.ReadOnlyDatabaseStorage.Get<T>(key) : extension.DatabaseStorage.Get<T>(key);

    public async Task LoadDataFromSqlAsync()
    {
        changesLoadedFromSql = (await SnapshotAsync()).Difference(new Quantum.Entities.Frame
        (
            (await conn.Table<SqliteListValueProperty>().ToListAsync())
                .Select(lvp => new KeyValuePair<Guid, Quantum.Entities.Attribute>
                (
                    lvp.Id,
                    new Quantum.Entities.Attribute
                    (
                        name: lvp.Name,
                        notes: lvp.Notes,
                        isEnumeration: true
                    )
                ))
                .Union((await conn.Table<SqliteTextProperty>().ToListAsync())
                    .Select(tp => new KeyValuePair<Guid, Quantum.Entities.Attribute>
                    (
                        tp.Id,
                        new Quantum.Entities.Attribute
                        (
                            name: tp.Name,
                            notes: tp.Notes,
                            isEnumeration: false
                        )
                    )))
                .ToDictionary(kv => kv.Key, kv => kv.Value),
            (await conn.Table<SqliteListValuePropertyValue>().ToListAsync())
                .Select(lvpv => new KeyValuePair<AttributeObjectCompositeKey, object>
                (
                    new AttributeObjectCompositeKey
                    (
                        lvpv.ListValuePropertyId,
                        lvpv.WorkItemId
                    ),
                    lvpv.ListValueId
                ))
                .Union((await conn.Table<SqliteTextPropertyValue>().ToListAsync())
                    .Select(tpv => new KeyValuePair<AttributeObjectCompositeKey, object>
                    (
                        new AttributeObjectCompositeKey
                        (
                            tpv.TextPropertyId,
                            tpv.WorkItemId
                        ),
                        tpv.Text
                    )))
                .ToDictionary(kv => kv.Key, kv => kv.Value),
            (await conn.Table<SqliteListValue>().ToListAsync()).ToDictionary
            (
                lv => lv.Id,
                lv => new AttributeCorrelation<EnumerationValue>
                (
                    lv.ListValuePropertyId,
                    correlatedEntity: new EnumerationValue(name: lv.Name)
                )
            ),
            (await conn.Table<SqliteAssignment>().ToListAsync()).ToDictionary
            (
                a => a.Id,
                a => new ItemPersonCorrelation<Interest>
                (
                    a.WorkItemId,
                    a.PersonId,
                    new Interest
                    (
                        complete: a.Complete?.UtcDateTime,
                        due: a.Due?.UtcDateTime,
                        estimate: a.Estimate
                    )
                )
            ),
            (await conn.Table<SqliteWorkItem>().ToListAsync()).ToDictionary
            (
                wi => wi.Id,
                wi => new Item
                (
                    name: wi.Name,
                    notes: wi.Notes
                )
            ),
            (await conn.Table<SqlitePerson>().ToListAsync()).ToDictionary
            (
                p => p.Id,
                p => new Person
                (
                    name: p.Name,
                    wentAway: p.WentAway?.UtcDateTime
                )
            ),
            (await conn.Table<SqliteTimeSlice>().ToListAsync()).ToDictionary
            (
                ts => ts.Id,
                ts => new ItemPersonCorrelation<Period>
                (
                    ts.WorkItemId,
                    ts.PersonId,
                    new Period
                    (
                        ts.End.UtcDateTime,
                        ts.Start.UtcDateTime,
                        notes: ts.Notes
                    )
                )
            ),
            (await conn.Table<SqliteTimingSlice>().ToListAsync()).Select(ts => ts.Id).ToList()
        ));
        await ApplyChangesAsync(changesLoadedFromSql);
        changesLoadedFromSql = null;
    }

    protected override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        if (GetDatabaseStorageValue<string>("FilePath") is string filePath)
        {
            try
            {
                conn = new SQLiteAsyncConnection(new SQLiteConnectionString(filePath));
                await LoadDataFromSqlAsync();
                IsConnectedToSqlite = true;
            }
            catch (Exception ex)
            {
                if (conn is SQLiteAsyncConnection liveConn)
                    await liveConn.CloseAsync();
                conn = null;
                extension.Error(ex);
            }
        }
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e is null)
            throw new ArgumentNullException(nameof(e));
        PropertyChanged?.Invoke(this, e);
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null)
            throw new ArgumentNullException(nameof(propertyName));
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        if (e is null)
            throw new ArgumentNullException(nameof(e));
        PropertyChanging?.Invoke(this, e);
    }

    protected void OnPropertyChanging([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null)
            throw new ArgumentNullException(nameof(propertyName));
        OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
    }

    public async Task PrepareForDatabaseDismountAsync()
    {
        if (conn is SQLiteAsyncConnection liveConn)
        {
            await liveConn.CloseAsync().ConfigureAwait(false);
            conn = null;
            await ApplyChangesAsync((await SnapshotAsync()).Difference(Quantum.Entities.Frame.Empty));
            IsConnectedToSqlite = false;
        }
    }

    protected override async Task<ChangeSet> ProcessAppliedChangesAsync(IndexedFrame originalState, ChangeSet changesApplied, ChangeSet inScopeChangesApplied)
    {
        if (!ReferenceEquals(changesApplied, changesLoadedFromSql) && conn is SQLiteAsyncConnection)
        {
            try
            {
                await ApplyChangesToSqliteAsync(inScopeChangesApplied);
            }
            catch (Exception ex)
            {
                extension.Error(ex);
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }
        return await base.ProcessAppliedChangesAsync(originalState, changesApplied, inScopeChangesApplied);
    }

    protected bool SetBackedProperty<TValue>(ref TValue backingField, TValue value, [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<TValue>.Default.Equals(backingField, value))
        {
            OnPropertyChanging(propertyName);
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }

    protected bool SetBackedProperty<TValue>(ref TValue backingField, in TValue value, [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<TValue>.Default.Equals(backingField, value))
        {
            OnPropertyChanging(propertyName);
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }
}

SqliteTransactor currentSqliteTransactor;
RadMenuItem extensionMenuItem;
var extensionsMenuExtensionId = Guid.Parse("{27F65593-7235-4108-B5D9-F0DE417D8536}");

void AboutUseSqliteClick(object sender, RoutedEventArgs e)
{
    MessageDialog.Present(Window.GetWindow((DependencyObject)sender), "This extension demonstrates fully interacting with the lifecycle of changes to the user's data by storing it in a completely different place just for giggles.", "About Use SQLite", MessageBoxImage.Information);
}

void DatabaseDismountingHandler(object sender, EventArgs e)
{
    currentSqliteTransactor?.PrepareForDatabaseDismountAsync().Wait();
}

Task DisconnectedAsyncHandler(object sender, DisconnectedEventArgs e)
{
    Extension.OnUiThreadAsync(() => extensionMenuItem.DataContext = null);
    currentSqliteTransactor = null;
    return Task.CompletedTask;
}

void EngineStartingHandler(object sender, EngineStartingEventArgs e)
{
    currentSqliteTransactor = new SqliteTransactor(Extension, e);
    currentSqliteTransactor.DisconnectedAsync += DisconnectedAsyncHandler;
    Extension.OnUiThreadAsync(() => extensionMenuItem.DataContext = currentSqliteTransactor);
    e.AddTransactor(currentSqliteTransactor);
}

await Extension.OnUiThreadAsync(() =>
{
    extensionMenuItem = new RadMenuItem { Header = "Use SQLite" };

    var connectToSqliteMenuItem = new RadMenuItem { Header = "Connect to SQLite" };
    connectToSqliteMenuItem.SetBinding(RadMenuItem.IsEnabledProperty, new Binding("IsConnectedToSqlite") { Converter = new BooleanIsNegatedValueConverter() });
    connectToSqliteMenuItem.Click += async (sender, e) =>
    {
        var mainWindow = Window.GetWindow(extensionMenuItem);
        var sfd = new SaveFileDialog
        {
            Filter = "SQLite Databases|*.sqlite|All Files|*.*",
            FilterIndex = 0,
            OverwritePrompt = true,
            Title = "Create New SQLite Database",
        };
        if (sfd.ShowDialog(mainWindow) ?? false)
        {
            try
            {
                await currentSqliteTransactor.ConnectToSqliteAsync(sfd.FileName);
            }
            catch (Exception ex)
            {
                Extension.Error(ex);
            }
        }
    };
    extensionMenuItem.Items.Add(connectToSqliteMenuItem);

    var disconnectToSqliteMenuItem = new RadMenuItem { Header = "Disconnect from SQLite" };
    disconnectToSqliteMenuItem.SetBinding(RadMenuItem.IsEnabledProperty, new Binding("IsConnectedToSqlite"));
    disconnectToSqliteMenuItem.Click += (sender, e) => currentSqliteTransactor.DisconnectFromSqliteAsync();
    extensionMenuItem.Items.Add(disconnectToSqliteMenuItem);

    extensionMenuItem.Items.Add(new RadMenuItem { IsSeparator = true });

    var reloadDataFromSqliteMenuItem = new RadMenuItem { Header = "Reload Data from SQLite" };
    reloadDataFromSqliteMenuItem.SetBinding(RadMenuItem.IsEnabledProperty, new Binding("IsConnectedToSqlite"));
    reloadDataFromSqliteMenuItem.Click += (sender, e) => currentSqliteTransactor.LoadDataFromSqlAsync();
    extensionMenuItem.Items.Add(reloadDataFromSqliteMenuItem);

    extensionMenuItem.Items.Add(new RadMenuItem { IsSeparator = true });

    var aboutUseSqliteMenuItem = new RadMenuItem { Header = "About Use SQLite" };
    aboutUseSqliteMenuItem.Click += AboutUseSqliteClick;
    extensionMenuItem.Items.Add(aboutUseSqliteMenuItem);
});

Extension.EngineStarting += EngineStartingHandler;
Extension.App.DatabaseDismounting += DatabaseDismountingHandler;
Extension.PostMessage(extensionsMenuExtensionId, extensionMenuItem);