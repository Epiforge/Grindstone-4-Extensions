using Newtonsoft.Json;
using Quantum;
using Quantum.Client.Windows;
using Quantum.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
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

class ConnectToSqlServerDialogContext : INotifyPropertyChanged, INotifyPropertyChanging
{
    string connectionString = "Server=localhost; Database=Grindstone; Trusted_connection=yes";

    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangingEventHandler PropertyChanging;

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

    public string ConnectionString
    {
        get => connectionString;
        set => SetBackedProperty(ref connectionString, in value);
    }
}

class SqlServerTransactor : Transactor, INotifyPropertyChanged, INotifyPropertyChanging
{
    public SqlServerTransactor(ExtensionGlobals extension, EngineStartingEventArgs engineStartingEventArgs)
    {
        this.extension = extension;
        this.engineStartingEventArgs = engineStartingEventArgs;
    }

    ChangeSet changesLoadedFromSql;
    SqlConnection conn = null;
    readonly EngineStartingEventArgs engineStartingEventArgs;
    readonly ExtensionGlobals extension;
    bool isConnectedToSql;

    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangingEventHandler PropertyChanging;

    async Task ApplyChangesToSqlAsync(ChangeSet changes)
    {
        if (changes.PeriodsStopTiming?.Any() ?? false)
            foreach (var id in changes.PeriodsStopTiming)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE TimingSlices WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeleteInterests?.Any() ?? false)
            foreach (var id in changes.DeleteInterests)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE Assignments WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeletePeriods?.Any() ?? false)
            foreach (var id in changes.DeletePeriods)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE TimingSlices WHERE Id = @id DELETE TimeSlices WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeleteAttributeValues?.Any() ?? false)
            foreach (var key in changes.DeleteAttributeValues)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE ListValuePropertyValues WHERE WorkItemId = @workItemId AND ListValuePropertyId = @propertyId DELETE TextPropertyValues WHERE WorkItemId = @workItemId AND TextPropertyId = @propertyId";
                    cmd.Parameters.AddWithValue("@workItemId", key.ObjectId);
                    cmd.Parameters.AddWithValue("@propertyId", key.AttributeId);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeleteEnumerationValues?.Any() ?? false)
            foreach (var id in changes.DeleteEnumerationValues)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE ListValuePropertyValues WHERE ListValueId = @id DELETE ListValues WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeleteAttributes?.Any() ?? false)
            foreach (var id in changes.DeleteAttributes)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE ListValuePropertyValues WHERE ListValuePropertyId = @id DELETE ListValues WHERE ListValuePropertyId = @id DELETE TextPropertyValues WHERE TextPropertyId = @id DELETE ListValueProperties WHERE Id = @id DELETE TextProperties WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeletePeople?.Any() ?? false)
            foreach (var id in changes.DeletePeople)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE Assignments WHERE PersonId = @id DELETE TimingSlices WHERE Id IN (SELECT Id FROM TimeSlices WHERE PersonId = @id) DELETE TimeSlices WHERE PersonId = @id DELETE People WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.DeleteItems?.Any() ?? false)
            foreach (var id in changes.DeleteItems)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE ListValuePropertyValues WHERE WorkItemId = @id DELETE TextPropertyValues WHERE WorkItemId = @id DELETE Assignments WHERE WorkItemId = @id DELETE TimingSlices WHERE Id IN (SELECT Id FROM TimeSlices WHERE WorkItemId = @id) DELETE TimeSlices WHERE WorkItemId = @id DELETE WorkItems WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdateAttributes?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateAttributes)
                using (var cmd = conn.CreateCommand())
                {
                    var tablePrefix = kv.Value.IsEnumeration ? "ListValue" : "Text";
                    cmd.CommandText = $"IF EXISTS (SELECT Id FROM {tablePrefix}Properties) UPDATE {tablePrefix}Properties SET Name = @name, Notes = @notes ELSE INSERT {tablePrefix}Properties (Id, Name, Notes) VALUES (@id, @name, @notes)";
                    cmd.Parameters.AddWithValue("@id", kv.Key);
                    cmd.Parameters.AddWithValue("@name", kv.Value.Name);
                    cmd.Parameters.AddWithValue("@notes", (object)kv.Value.Notes ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdatePeople?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdatePeople)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF EXISTS (SELECT Id FROM People WHERE Id = @id) UPDATE People SET Name = @name, WentAway = @wentAway WHERE Id = @id ELSE INSERT People (Id, Name, WentAway) VALUES (@id, @name, @wentAway)";
                    cmd.Parameters.AddWithValue("@id", kv.Key);
                    cmd.Parameters.AddWithValue("@name", kv.Value.Name);
                    cmd.Parameters.AddWithValue("@wentAway", (object)kv.Value.WentAway ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdateItems?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateItems)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF EXISTS (SELECT Id FROM WorkItems WHERE Id = @id) UPDATE WorkItems SET Name = @name, Notes = @notes WHERE Id = @id ELSE INSERT WorkItems (Id, Name, Notes) VALUES (@id, @name, @notes)";
                    cmd.Parameters.AddWithValue("@id", kv.Key);
                    cmd.Parameters.AddWithValue("@name", kv.Value.Name);
                    cmd.Parameters.AddWithValue("@notes", (object)kv.Value.Notes ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdateEnumerationValues?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateEnumerationValues)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF EXISTS (SELECT Id FROM ListValues WHERE Id = @id) UPDATE ListValues SET ListValuePropertyId = @listValuePropertyId, Name = @name WHERE Id = @id ELSE INSERT ListValues (Id, ListValuePropertyId, Name) VALUES (@id, @listValuePropertyId, @name)";
                    cmd.Parameters.AddWithValue("@id", kv.Key);
                    cmd.Parameters.AddWithValue("@listValuePropertyId", kv.Value.AttributeId);
                    cmd.Parameters.AddWithValue("@name", kv.Value.CorrelatedEntity.Name);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdateAttributeValues?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateAttributeValues)
                using (var cmd = conn.CreateCommand())
                {
                    string tablePrefix, columnName;
                    if (kv.Value is Guid)
                    {
                        tablePrefix = "ListValue";
                        columnName = "ListValueId";
                    }
                    else
                    {
                        tablePrefix = "Text";
                        columnName = "[Text]";
                    }
                    cmd.CommandText = $"IF EXISTS (SELECT {columnName} FROM {tablePrefix}PropertyValues WHERE WorkItemId = @workItemId AND {tablePrefix}PropertyId = @propertyId) UPDATE {tablePrefix}PropertyValues SET {columnName} = @value WHERE WorkItemId = @workItemId AND {tablePrefix}PropertyId = @propertyId ELSE INSERT {tablePrefix}PropertyValues (WorkItemId, {tablePrefix}PropertyId, {columnName}) VALUES (@workItemId, @propertyId, @value)";
                    cmd.Parameters.AddWithValue("@workItemId", kv.Key.ObjectId);
                    cmd.Parameters.AddWithValue("@propertyId", kv.Key.AttributeId);
                    cmd.Parameters.AddWithValue("@value", kv.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdateInterests?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdateInterests)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF EXISTS (SELECT Id FROM Assignments WHERE Id = @id) UPDATE Assignments SET WorkItemId = @workItemId, PersonId = @personId, Complete = @complete, Due = @due, Estimate = @estimate WHERE Id = @id ELSE INSERT Assignments (Id, WorkItemId, PersonId, Complete, Due, Estimate) VALUES (@id, @workItemId, @personId, @complete, @due, @estimate)";
                    cmd.Parameters.AddWithValue("@id", kv.Key);
                    cmd.Parameters.AddWithValue("@workItemId", kv.Value.ItemId);
                    cmd.Parameters.AddWithValue("@personId", kv.Value.PersonId);
                    cmd.Parameters.AddWithValue("@complete", (object)kv.Value.CorrelatedEntity.Complete ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@due", (object)kv.Value.CorrelatedEntity.Due ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@estimate", (object)kv.Value.CorrelatedEntity.Estimate?.Ticks ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.CreateOrUpdatePeriods?.Any() ?? false)
            foreach (var kv in changes.CreateOrUpdatePeriods)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF EXISTS (SELECT Id FROM TimeSlices WHERE Id = @id) UPDATE TimeSlices SET WorkItemId = @workItemId, PersonId = @personId, Start = @start, [End] = @end, Notes = @notes WHERE Id = @id ELSE INSERT TimeSlices (Id, WorkItemId, PersonId, Start, [End], Notes) VALUES (@id, @workItemId, @personId, @start, @end, @notes)";
                    cmd.Parameters.AddWithValue("@id", kv.Key);
                    cmd.Parameters.AddWithValue("@workItemId", kv.Value.ItemId);
                    cmd.Parameters.AddWithValue("@personId", kv.Value.PersonId);
                    cmd.Parameters.AddWithValue("@start", kv.Value.CorrelatedEntity.Start);
                    cmd.Parameters.AddWithValue("@end", kv.Value.CorrelatedEntity.End);
                    cmd.Parameters.AddWithValue("@notes", (object)kv.Value.CorrelatedEntity.Notes ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
        if (changes.PeriodsStartTiming?.Any() ?? false)
            foreach (var id in changes.PeriodsStartTiming)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT TimingSlices (Id) VALUES (@id)";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
    }

    public async Task ConnectToSqlServerAsync(string connectionString)
    {
        if (conn is SqlConnection)
            throw new InvalidOperationException();
        try
        {
            conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = await extension.ReadFileTextAsync("PrepareDatabase.sql");
                await cmd.ExecuteNonQueryAsync();
            }
            await ApplyChangesToSqlAsync((await SnapshotAsync()).ToInitializationChangeSet());
            extension.DatabaseStorage.Set("ConnectionString", connectionString);
            IsConnectedToSql = true;
        }
        catch (Exception ex)
        {
            conn?.Dispose();
            conn = null;
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public async Task DisconnectFromSqlServerAsync()
    {
        if (conn is null)
            throw new InvalidOperationException();
        conn.Dispose();
        conn = null;
        extension.DatabaseStorage.Set("ConnectionString", null);
        IsConnectedToSql = false;
    }

    public T GetDatabaseStorageValue<T>(string key) => extension.DatabaseStorage is null ? engineStartingEventArgs.ReadOnlyDatabaseStorage.Get<T>(key) : extension.DatabaseStorage.Get<T>(key);

    public async Task LoadDataFromSqlAsync()
    {
        var attributes = new Dictionary<Guid, Quantum.Entities.Attribute>();
        var attributeValues = new Dictionary<AttributeObjectCompositeKey, object>();
        var enumerationValues = new Dictionary<Guid, AttributeCorrelation<EnumerationValue>>();
        var interests = new Dictionary<Guid, ItemPersonCorrelation<Interest>>();
        var items = new Dictionary<Guid, Item>();
        var people = new Dictionary<Guid, Person>();
        var periods = new Dictionary<Guid, ItemPersonCorrelation<Period>>();
        var periodsTiming = new List<Guid>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, WorkItemId, PersonId, Complete, Due, Estimate FROM Assignments (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                interests.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new ItemPersonCorrelation<Interest>
                    (
                        await reader.GetFieldValueAsync<Guid>(1),
                        await reader.GetFieldValueAsync<Guid>(2),
                        new Interest
                        (
                            complete: (await reader.IsDBNullAsync(3)) ? (DateTime?)null : DateTime.SpecifyKind(await reader.GetFieldValueAsync<DateTime>(3), DateTimeKind.Utc),
                            due: (await reader.IsDBNullAsync(4)) ? (DateTime?)null : DateTime.SpecifyKind(await reader.GetFieldValueAsync<DateTime>(4), DateTimeKind.Utc),
                            estimate: (await reader.IsDBNullAsync(5)) ? (TimeSpan?)null : new TimeSpan(await reader.GetFieldValueAsync<long>(5))
                        )
                    )
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Name, Notes FROM ListValueProperties (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                attributes.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new Quantum.Entities.Attribute
                    (
                        name: await reader.GetFieldValueAsync<string>(1),
                        notes: (await reader.IsDBNullAsync(2)) ? null : await reader.GetFieldValueAsync<string>(2),
                        isEnumeration: true
                    )
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT WorkItemId, ListValuePropertyId, ListValueId FROM ListValuePropertyValues (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                attributeValues.Add
                (
                    new AttributeObjectCompositeKey
                    (
                        await reader.GetFieldValueAsync<Guid>(1),
                        await reader.GetFieldValueAsync<Guid>(0)
                    ),
                    await reader.GetFieldValueAsync<Guid>(2)
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, ListValuePropertyId, Name FROM ListValues (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                enumerationValues.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new AttributeCorrelation<EnumerationValue>
                    (
                        await reader.GetFieldValueAsync<Guid>(1),
                        correlatedEntity: new EnumerationValue(name: await reader.GetFieldValueAsync<string>(2))
                    )
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Name, WentAway FROM People (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                people.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new Person
                    (
                        name: await reader.GetFieldValueAsync<string>(1),
                        wentAway: (await reader.IsDBNullAsync(2)) ? (DateTime?)null : DateTime.SpecifyKind(await reader.GetFieldValueAsync<DateTime>(2), DateTimeKind.Utc)
                    )
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Name, Notes FROM TextProperties (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                attributes.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new Quantum.Entities.Attribute
                    (
                        name: await reader.GetFieldValueAsync<string>(1),
                        notes: (await reader.IsDBNullAsync(2)) ? null : await reader.GetFieldValueAsync<string>(2)
                    )
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT WorkItemId, TextPropertyId, Text FROM TextPropertyValues (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                attributeValues.Add
                (
                    new AttributeObjectCompositeKey
                    (
                        await reader.GetFieldValueAsync<Guid>(1),
                        await reader.GetFieldValueAsync<Guid>(0)
                    ),
                    await reader.GetFieldValueAsync<string>(2)
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, WorkItemId, PersonId, Start, [End], Notes FROM TimeSlices (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                periods.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new ItemPersonCorrelation<Period>
                    (
                        await reader.GetFieldValueAsync<Guid>(1),
                        await reader.GetFieldValueAsync<Guid>(2),
                        new Period
                        (
                            DateTime.SpecifyKind(await reader.GetFieldValueAsync<DateTime>(4), DateTimeKind.Utc),
                            DateTime.SpecifyKind(await reader.GetFieldValueAsync<DateTime>(3), DateTimeKind.Utc),
                            notes: (await reader.IsDBNullAsync(5)) ? null : await reader.GetFieldValueAsync<string>(5)
                        )
                    )
                );
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM TimingSlices (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                periodsTiming.Add(await reader.GetFieldValueAsync<Guid>(0));
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, Name, Notes FROM WorkItems (NOLOCK)";
            using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                items.Add
                (
                    await reader.GetFieldValueAsync<Guid>(0),
                    new Item
                    (
                        name: await reader.GetFieldValueAsync<string>(1),
                        notes: (await reader.IsDBNullAsync(2)) ? null : await reader.GetFieldValueAsync<string>(2)
                    )
                );
        }
        changesLoadedFromSql = (await SnapshotAsync()).Difference(new Quantum.Entities.Frame(attributes, attributeValues, enumerationValues, interests, items, people, periods, periodsTiming));
        await ApplyChangesAsync(changesLoadedFromSql);
        changesLoadedFromSql = null;
    }

    protected override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        if (GetDatabaseStorageValue<string>("ConnectionString") is string connectionString)
        {
            try
            {
                conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                await LoadDataFromSqlAsync();
                IsConnectedToSql = true;
            }
            catch (Exception ex)
            {
                conn?.Dispose();
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
        if (conn is SqlConnection)
        {
            conn.Dispose();
            conn = null;
            await ApplyChangesAsync((await SnapshotAsync()).Difference(Quantum.Entities.Frame.Empty));
            IsConnectedToSql = false;
        }
    }

    protected override async Task<ChangeSet> ProcessAppliedChangesAsync(IndexedFrame originalState, ChangeSet changesApplied, ChangeSet inScopeChangesApplied)
    {
        if (!ReferenceEquals(changesApplied, changesLoadedFromSql) && conn is SqlConnection)
        {
            try
            {
                await ApplyChangesToSqlAsync(inScopeChangesApplied);
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

    protected override async Task<bool> ProcessChangesBeforeApplicationAsync(ChangeSet changesToBeApplied)
    {
        // TODO: make sure this is going to work in SQL Server, and if not, throw
        return await base.ProcessChangesBeforeApplicationAsync(changesToBeApplied);
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

    public bool IsConnectedToSql
    {
        get => isConnectedToSql;
        private set => SetBackedProperty(ref isConnectedToSql, in value);
    }
}

SqlServerTransactor currentSqlServerTransactor;
RadMenuItem extensionMenuItem;
var extensionsMenuExtensionId = Guid.Parse("{27F65593-7235-4108-B5D9-F0DE417D8536}");

void AboutUseSqlServerClick(object sender, RoutedEventArgs e)
{
    MessageDialog.Present(Window.GetWindow((DependencyObject)sender), "Your Mom", "About Use SQL Server", MessageBoxImage.Information);
}

void DatabaseDismountingHandler(object sender, EventArgs e)
{
    currentSqlServerTransactor?.PrepareForDatabaseDismountAsync().Wait();
}

Task DisconnectedAsyncHandler(object sender, DisconnectedEventArgs e)
{
    Extension.OnUiThreadAsync(() => extensionMenuItem.DataContext = null);
    currentSqlServerTransactor = null;
    return Task.CompletedTask;
}

void EngineStartingHandler(object sender, EngineStartingEventArgs e)
{
    currentSqlServerTransactor = new SqlServerTransactor(Extension, e);
    currentSqlServerTransactor.DisconnectedAsync += DisconnectedAsyncHandler;
    Extension.OnUiThreadAsync(() => extensionMenuItem.DataContext = currentSqlServerTransactor);
    e.AddTransactor(currentSqlServerTransactor);
}

await Extension.OnUiThreadAsync(() =>
{
    extensionMenuItem = new RadMenuItem { Header = "Use SQL Server" };

    var connectToSqlServerMenuItem = new RadMenuItem { Header = "Connect to SQL Server" };
    connectToSqlServerMenuItem.SetBinding(RadMenuItem.IsEnabledProperty, new Binding("IsConnectedToSql") { Converter = new BooleanIsNegatedValueConverter() });
    connectToSqlServerMenuItem.Click += async (sender, e) =>
    {
        var mainWindow = Window.GetWindow(extensionMenuItem);
        var connectToSqlServerDialog = (Window)Extension.LoadUiElement("ConnectToSqlServerDialog.xaml");
        connectToSqlServerDialog.Owner = mainWindow;
        var connectToSqlServerDialogContext = new ConnectToSqlServerDialogContext();
        connectToSqlServerDialog.DataContext = connectToSqlServerDialogContext;
        ((Button)LogicalTreeHelper.FindLogicalNode(connectToSqlServerDialog, "ok")).Click += (sender2, e2) => connectToSqlServerDialog.DialogResult = true;
        if (connectToSqlServerDialog.ShowDialog() ?? false)
        {
            try
            {
                await currentSqlServerTransactor.ConnectToSqlServerAsync(connectToSqlServerDialogContext.ConnectionString);
            }
            catch (Exception ex)
            {
                MessageDialog.Present(mainWindow, ex.GetDetails(), "Whoops, well, that didn't work...", MessageBoxImage.Error);
            }
        }
    };
    extensionMenuItem.Items.Add(connectToSqlServerMenuItem);

    var disconnectToSqlServerMenuItem = new RadMenuItem { Header = "Disconnect from SQL Server" };
    disconnectToSqlServerMenuItem.SetBinding(RadMenuItem.IsEnabledProperty, new Binding("IsConnectedToSql"));
    disconnectToSqlServerMenuItem.Click += (sender, e) => currentSqlServerTransactor.DisconnectFromSqlServerAsync();
    extensionMenuItem.Items.Add(disconnectToSqlServerMenuItem);

    extensionMenuItem.Items.Add(new RadMenuItem { IsSeparator = true });

    var reloadDataFromSqlServerMenuItem = new RadMenuItem { Header = "Reload Data from SQL Server" };
    reloadDataFromSqlServerMenuItem.SetBinding(RadMenuItem.IsEnabledProperty, new Binding("IsConnectedToSql"));
    reloadDataFromSqlServerMenuItem.Click += (sender, e) => currentSqlServerTransactor.LoadDataFromSqlAsync();
    extensionMenuItem.Items.Add(reloadDataFromSqlServerMenuItem);

    extensionMenuItem.Items.Add(new RadMenuItem { IsSeparator = true });

    var aboutUseSqlServerMenuItem = new RadMenuItem { Header = "About Use SQL Server" };
    aboutUseSqlServerMenuItem.Click += AboutUseSqlServerClick;
    extensionMenuItem.Items.Add(aboutUseSqlServerMenuItem);
});

Extension.EngineStarting += EngineStartingHandler;
Extension.App.DatabaseDismounting += DatabaseDismountingHandler;
Extension.PostMessage(extensionsMenuExtensionId, extensionMenuItem);