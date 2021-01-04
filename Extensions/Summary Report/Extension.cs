using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Gear.ActiveQuery;
using Gear.Components;
using Microsoft.Win32;
using Quantum;
using Quantum.Client;
using Quantum.Client.Views;
using Quantum.Client.Views.Entities;
using Quantum.Client.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using Telerik.Windows.Controls;

class Csv
{
    public static void ToCsv(DataTable table, TextWriter writer, bool includeHeaderRow = true, char columnDelimeter = ',', string recordDelimeter = "\r\n")
    {
        var controlCharacters = recordDelimeter.ToCharArray().Concat(new char[] { columnDelimeter, '\"' }).ToArray();
        var columns = new DataColumn[table.Columns.Count];
        table.Columns.CopyTo(columns, 0);
        if (includeHeaderRow)
        {
            var items = new List<object>();
            items.AddRange(columns.Select(c => c.ColumnName));
            WriteCsvLine(writer, items, columnDelimeter, controlCharacters);
        }
        var firstRecord = !includeHeaderRow;
        foreach (DataRow row in table.Rows)
        {
            if (!firstRecord)
                writer.Write(recordDelimeter);
            else
                firstRecord = false;
            var items = new List<object>(columns.Select(c => row[c]));
            WriteCsvLine(writer, items, columnDelimeter, controlCharacters);
        }
    }

    public static void ToCsv(DataTable table, Stream stream, bool includeHeaderRow = true, char columnDelimeter = ',', string recordDelimeter = "\r\n")
    {
        using (var writer = new StreamWriter(stream))
        {
            var textWriter = (TextWriter)writer;
            ToCsv(table, textWriter, includeHeaderRow, columnDelimeter, recordDelimeter);
        }
    }

    public static string ToCsv(DataTable table, bool includeHeaderRow = true, char columnDelimeter = ',', string recordDelimeter = "\r\n")
    {
        var result = new StringBuilder();
        using (var writer = new StringWriter(result))
        {
            var textWriter = (TextWriter)writer;
            ToCsv(table, textWriter, includeHeaderRow, columnDelimeter, recordDelimeter);
        }
        return result.ToString();
    }

    public static void ToCsv<T>(IEnumerable<T> enumerable, TextWriter writer, bool includeHeaderRow = true, char columnDelimeter = ',', string recordDelimeter = "\r\n")
    {
        var controlCharacters = recordDelimeter.ToCharArray().Concat(new char[] { columnDelimeter, '\"' }).ToArray();
        var type = typeof(T);
        var fields = type.GetFields();
        var properties = type.GetProperties();
        if (includeHeaderRow)
        {
            var items = new List<object>();
            items.AddRange(fields.Select(f => f.Name));
            items.AddRange(properties.Select(p => p.Name));
            WriteCsvLine(writer, items, columnDelimeter, controlCharacters);
        }
        var firstRecord = !includeHeaderRow;
        foreach (var obj in enumerable)
        {
            if (!firstRecord)
                writer.Write(recordDelimeter);
            else
                firstRecord = false;
            var items = new List<object>();
            items.AddRange(fields.Select(f => f.GetValue(obj)));
            items.AddRange(properties.Select(p => p.GetGetMethod().Invoke(obj, new object[] { })));
            WriteCsvLine(writer, items, columnDelimeter, controlCharacters);
        }
    }

    public static void ToCsv<T>(IEnumerable<T> enumerable, Stream stream, bool includeHeaderRow = true, char columnDelimeter = ',', string recordDelimeter = "\r\n")
    {
        using (var writer = new StreamWriter(stream))
        {
            var textWriter = (TextWriter)writer;
            ToCsv(enumerable, textWriter, includeHeaderRow, columnDelimeter, recordDelimeter);
        }
    }

    public static string ToCsv<T>(IEnumerable<T> enumerable, bool includeHeaderRow = true, char columnDelimeter = ',', string recordDelimeter = "\r\n")
    {
        var result = new StringBuilder();
        using (var writer = new StringWriter(result))
        {
            var textWriter = (TextWriter)writer;
            ToCsv(enumerable, textWriter, includeHeaderRow, columnDelimeter, recordDelimeter);
        }
        return result.ToString();
    }

    static void WriteCsvLine(TextWriter writer, List<object> items, char columnDelimeter, char[] controlCharacters)
    {
        for (var i = 0; i < items.Count - 1; ++i)
        {
            WriteCsvItem(writer, items[i], controlCharacters);
            writer.Write(columnDelimeter);
        }
        WriteCsvItem(writer, items[items.Count - 1], controlCharacters);
    }

    static void WriteCsvItem(TextWriter writer, object item, char[] controlCharacters)
    {
        var value = Convert.ToString(item);
        if (value.ToCharArray().Any(c => controlCharacters.Contains(c)))
            value = string.Format("\"{0}\"", value.Replace("\"", "\"\""));
        writer.Write(value);
    }
}

class Spreadsheet
{
    static int GetStringIndex(SharedStringTable sharedStringTable, string str)
    {
        var stringIndex = sharedStringTable.Elements<SharedStringItem>().IndexOf(si => si.Text != null && si.Text.Text == str);
        if (stringIndex < 0)
        {
            sharedStringTable.Append(new SharedStringItem { Text = new Text(str) });
            stringIndex = sharedStringTable.Elements<SharedStringItem>().Count();
            sharedStringTable.Count = ToOpenXmlValue(stringIndex);
            sharedStringTable.UniqueCount = ToOpenXmlValue(stringIndex);
            --stringIndex;
        }
        return stringIndex;
    }
    
    static UInt32Value ToOpenXmlValue(int num) => UInt32Value.FromUInt32((uint)num);

    static DoubleValue ToOpenXmlValue(double num) => DoubleValue.FromDouble(num);

    public static void TransformData(DataSet dataSet, string fileName, bool includeHeaders = true)
    {
        var doc = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook);
        var workbookPart = doc.AddWorkbookPart();
        var workbook = new Workbook();
        workbookPart.Workbook = workbook;
        workbookPart.AddNewPart<SharedStringTablePart>();
        var sharedStringTable = new SharedStringTable();
        workbookPart.SharedStringTablePart.SharedStringTable = sharedStringTable;
        var sheets = workbook.AppendChild<Sheets>(new Sheets());
        foreach (var dt in dataSet.Tables.OfType<DataTable>())
            TransformDataTable(dt, workbookPart, sharedStringTable, sheets, includeHeaders);
        workbook.Save();
        doc.Close();
    }

    public static void TransformData(DataTable dataTable, string fileName, bool includeHeaders = true)
    {
        var dataSet = new DataSet();
        dataSet.Tables.Add(dataTable);
        TransformData(dataSet, fileName, includeHeaders);
    }

    private static void TransformDataTable(DataTable dataTable, WorkbookPart workbookPart, SharedStringTable sharedStringTable, Sheets sheets, bool includeHeaders)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        var worksheet = new Worksheet(sheetData);
        worksheetPart.Worksheet = worksheet;
        var sheetNum = workbookPart.WorksheetParts.Count() + 1;
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = ToOpenXmlValue(sheetNum),
            Name = dataTable.TableName == null || dataTable.TableName.Trim() == string.Empty ? string.Format("Sheet{0}", sheetNum) : dataTable.TableName
        });
        var columns = new Columns();
        columns.Append(Enumerable.Range(1, dataTable.Columns.Count).Select(i => new Column { Min = ToOpenXmlValue(i), Max = ToOpenXmlValue(i), Width = ToOpenXmlValue(8.43) }));
        worksheet.InsertBefore(columns, sheetData);
        var rowIndex = 0;
        if (includeHeaders)
        {
            var row = new Row { RowIndex = ToOpenXmlValue(++rowIndex) };
            sheetData.Append(row);
            var columnIndex = 0;
            row.Append(dataTable.Columns.OfType<DataColumn>().Select(dc => new Cell
            {
                CellValue = new CellValue(GetStringIndex(sharedStringTable, dc.ColumnName).ToString()),
                DataType = new EnumValue<CellValues>(CellValues.SharedString),
                CellReference = GetCellReference(rowIndex, ++columnIndex)
            }));
        }
        sheetData.Append(dataTable.Rows.OfType<DataRow>().Select(dr =>
        {
            var row = new Row { RowIndex = ToOpenXmlValue(++rowIndex) };
            var columnIndex = 0;
            row.Append(dataTable.Columns.OfType<DataColumn>().Select(dc => new Cell
            {
                CellValue = IsNumeric(dr[dc]) ? new CellValue(Convert.ToString(dr[dc])) : new CellValue(GetStringIndex(sharedStringTable, Convert.ToString(dr[dc])).ToString()),
                DataType = !IsNumeric(dr[dc]) ? new EnumValue<CellValues>(CellValues.SharedString) : null,
                CellReference = GetCellReference(rowIndex, ++columnIndex)
            }));
            return row;
        }));
    }

    private static bool IsNumeric(object obj)
    {
        double garbage;
        return double.TryParse(Convert.ToString(obj), out garbage);
    }

    private static string GetCellReference(int rowIndex, int columnIndex)
    {
        var colNumerals = new List<char>();
        var colOrder = 26;
        while (columnIndex > 0)
        {
            var order = columnIndex % colOrder;
            colNumerals.Insert(0, Convert.ToChar(Convert.ToInt32('A') + order - 1));
            columnIndex /= colOrder;
        }
        return string.Format("{0}{1}", new string(colNumerals.ToArray()), rowIndex);
    }
}

class SummaryReportWindowDataContext : SyncDisposablePropertyChangeNotifier
{
    static DataTable ConvertFlowDocumentTableToDataTable(System.Windows.Documents.Table flowTable, bool firstRowHeader = true)
    {
        var result = new DataTable();
        var rows = new List<TableRow>();
        foreach (var rg in flowTable.RowGroups)
            rows.AddRange(rg.Rows);
        bool boolValue;
        decimal numValue;
        DateTime dateValue;
        TimeSpan tsValue;
        Guid guidValue;
        for (var columnIndex = 0; columnIndex < flowTable.Columns.Count; ++columnIndex)
        {
            var boolParsable = true;
            var numParsable = true;
            var dateParsable = true;
            var tsParsable = true;
            var guidParsable = true;
            for (var rowIndex = firstRowHeader ? 1 : 0; rowIndex < rows.Count; ++rowIndex)
            {
                if (!boolParsable && !numParsable && !dateParsable && !tsParsable && !guidParsable)
                    break;
                var cellColumnIndex = 0;
                foreach (var c in rows[rowIndex].Cells)
                {
                    var cellText = GetTextElementText(c).Trim();
                    if (boolParsable)
                        boolParsable = bool.TryParse(cellText, out boolValue);
                    if (numParsable)
                        numParsable = decimal.TryParse(cellText, out numValue);
                    if (dateParsable)
                        dateParsable = DateTime.TryParse(cellText, out dateValue);
                    if (tsParsable)
                        tsParsable = TimeSpan.TryParse(cellText, out tsValue);
                    if (guidParsable)
                        guidParsable = Guid.TryParse(cellText, out guidValue);
                    cellColumnIndex += c.ColumnSpan;
                }
            }
            var columnType = typeof(string);
            if (dateParsable)
                columnType = typeof(DateTime);
            else if (guidParsable)
                columnType = typeof(Guid);
            else if (numParsable)
                columnType = typeof(decimal);
            else if (tsParsable)
                columnType = typeof(TimeSpan);
            else if (boolParsable)
                columnType = typeof(bool);
            if (firstRowHeader)
                result.Columns.Add(GetTextElementText(rows[0].Cells[columnIndex]).Trim(), columnType);
            else result.Columns.Add(new DataColumn
            {
                DataType = columnType
            });
        }
        for (var rowIndex = firstRowHeader ? 1 : 0; rowIndex < rows.Count; ++rowIndex)
        {
            var items = new object[result.Columns.Count];
            var cellColumnIndex = 0;
            foreach (var c in rows[rowIndex].Cells)
            {
                var cellText = GetTextElementText(c).Trim();
                var columnType = result.Columns[cellColumnIndex].DataType;
                if (columnType == typeof(bool))
                    items[cellColumnIndex] = bool.Parse(cellText);
                else if (columnType == typeof(decimal))
                    items[cellColumnIndex] = decimal.Parse(cellText);
                else if (columnType == typeof(DateTime))
                    items[cellColumnIndex] = DateTime.Parse(cellText);
                else if (columnType == typeof(TimeSpan))
                    items[cellColumnIndex] = TimeSpan.Parse(cellText);
                else if (columnType == typeof(Guid))
                    items[cellColumnIndex] = Guid.Parse(cellText);
                else
                    items[cellColumnIndex] = cellText;
                cellColumnIndex += c.ColumnSpan;
            }
            result.Rows.Add(items);
        }
        return result;
    }

    static string GetTextElementText(TextElement textElement) => new TextRange(textElement.ContentStart, textElement.ContentEnd).Text;

    static void ResetTheme()
    {
        if (!App.Configuration.InterfaceUseWindows)
            App.SetTheme(App.Configuration.InterfaceDark, App.Configuration.InterfaceHue, App.Configuration.InterfaceSaturation);
        else
            App.SetThemeFromWindows();
    }

    static void SetLightTheme()
    {
        if (!App.Configuration.InterfaceUseWindows)
            App.SetTheme(false, App.Configuration.InterfaceHue, App.Configuration.InterfaceSaturation);
        else
        {
            var windowsAccentColor = App.WindowsTheme.Color;
            App.SetTheme(false, windowsAccentColor.GetHue() / 360D, windowsAccentColor.GetSaturation());
        }
    }

    static string ToDisplay(object value)
    {
        string display;
        if (value is bool boolKey)
            display = boolKey ? "Yes" : "No";
        else if (value is DateTime dtKey)
            display = dtKey.ToLocalTime().ToString("D");
        else if (value is TimeSpan tsKey)
            display = ToHourSpan(tsKey);
        else
            display = Convert.ToString(value);
        return display;
    }

    static string ToHourSpan(TimeSpan ts)
    {
        var culture = CultureInfo.CurrentUICulture;
        var numberFormat = culture.NumberFormat;
        var timeSeparator = culture.DateTimeFormat.TimeSeparator;
        return $"{(ts < TimeSpan.Zero ? numberFormat.NegativeSign : string.Empty)}{Math.Floor(Math.Abs(ts.TotalHours))}{timeSeparator}{Math.Abs(ts.Minutes):00}{timeSeparator}{Math.Abs(ts.Seconds):00}";
    }

    public SummaryReportWindowDataContext(Window summaryReportWindow)
    {
        this.summaryReportWindow = summaryReportWindow;
        Views = App.ClientProxy.AllViews.ActiveWhere(v => v.EntityType == typeof(IViewPeriod));
        if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.SelectedView is IViewBase mainWindowSelectedView && mainWindowSelectedView.EntityType == typeof(IViewPeriod))
            SelectedView = mainWindowSelectedView;
        else if (Views.OrderBy(v => v.Name).FirstOrDefault() is IViewBase firstView)
            SelectedView = firstView;
    }

    FlowDocument document;
    int durationTypeIndex;
    IViewColumn selectedColumn;
    IViewBase selectedView;
    Window summaryReportWindow;

    public FlowDocument Document
    {
        get => document;
        private set => SetBackedProperty(ref document, in value);
    }

    public int DurationTypeIndex
    {
        get => durationTypeIndex;
        set
        {
            if (SetBackedProperty(ref durationTypeIndex, in value))
                RefreshDocument();
        }
    }

    public IViewColumn SelectedColumn
    {
        get => selectedColumn;
        set
        {
            if (SetBackedProperty(ref selectedColumn, in value))
                RefreshDocument();
        }
    }

    public IViewBase SelectedView
    {
        get => selectedView;
        set
        {
            var selectedColumnProperty = selectedColumn?.Property;
            var selectedColumnIndexers = selectedColumn?.Indexers ?? new object[0];
            if (SetBackedProperty(ref selectedView, in value))
            {
                if (selectedColumnProperty == null)
                    SelectedColumn = selectedView?.ViewColumns.Where(c => c.Header == "Date").FirstOrDefault();
                else
                    SelectedColumn = selectedView?.ViewColumns.Where(c => c.Property == selectedColumnProperty && (c.Indexers ?? new object[0]).SequenceEqual(selectedColumnIndexers)).FirstOrDefault();
            }
        }
    }

    public IActiveEnumerable<IViewBase> Views { get; }

    public void CopyToClipboard()
    {
        SetLightTheme();
        var data = new DataObject();
        var range = new TextRange(Document.ContentStart, Document.ContentEnd);
        using (var stream = new MemoryStream())
        {
            range.Save(stream, DataFormats.Text);
            data.SetData(DataFormats.Text, Encoding.UTF8.GetString(stream.ToArray()));
        }
        using (var stream = new MemoryStream())
        {
            range.Save(stream, DataFormats.Rtf);
            data.SetData(DataFormats.Rtf, Encoding.UTF8.GetString(stream.ToArray()));
        }
        Clipboard.SetDataObject(data);
        ResetTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Views.Dispose();
    }

    public void Print()
    {
        var currentMainWindow = Application.Current.MainWindow;
        Application.Current.MainWindow = summaryReportWindow;
        SetLightTheme();
        var copy = new FlowDocument();
        using (var stream = new MemoryStream())
        {
            new TextRange(Document.ContentStart, Document.ContentEnd).Save(stream, DataFormats.Xaml);
            new TextRange(copy.ContentStart, copy.ContentEnd).Load(stream, DataFormats.Xaml);
        }
        ResetTheme();
        PrintDocumentImageableArea area = null;
        XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter("Grindstone Summary Report", ref area);
        if (writer != null && area != null)
        {
            var paginator = ((IDocumentPaginatorSource)copy).DocumentPaginator;
            paginator.PageSize = new Size(area.MediaSizeWidth, area.MediaSizeHeight);
            var minimumMargin = new Thickness(72);
            copy.PagePadding = new Thickness
            (
                Math.Max(area.OriginWidth, minimumMargin.Left),
                Math.Max(area.OriginHeight, minimumMargin.Top),
                Math.Max(area.MediaSizeWidth - (area.OriginWidth + area.ExtentWidth), minimumMargin.Right),
                Math.Max(area.MediaSizeHeight - (area.OriginHeight + area.ExtentHeight), minimumMargin.Bottom)
            );
            copy.ColumnWidth = double.PositiveInfinity;
            writer.Write(paginator);
        }
        Application.Current.MainWindow = currentMainWindow;
    }

    public void RefreshDocument()
    {
        var view = (IView<IViewPeriod>)SelectedView;
        var column = SelectedColumn;
        if (column != null)
        {
            var selectedColumnPropertyName = column.Property.Name;
            var isWorkItemColumn = selectedColumnPropertyName != nameof(IViewPeriod.ItemName);
            var isDateColumn = selectedColumnPropertyName != nameof(IViewPeriod.Date);

            var document = new FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Roboto"),
                FontSize = 12,
                PagePadding = new Thickness(0)
            };
            var table = new System.Windows.Documents.Table
            {
                CellSpacing = 4
            };
            
            TableCell newCell(object content, string styleResourceKey = null)
            {
                var cell = new TableCell(new Paragraph(new System.Windows.Documents.Run(ToDisplay(content))));
                if (!string.IsNullOrEmpty(styleResourceKey))
                    cell.Style = (Style)summaryReportWindow.FindResource(styleResourceKey);
                return cell;
            }

            var rowGroup = new TableRowGroup();
            if (isWorkItemColumn)
                table.Columns.Add(new System.Windows.Documents.TableColumn());
            if (isDateColumn)
                table.Columns.Add(new System.Windows.Documents.TableColumn());
            table.Columns.Add(new System.Windows.Documents.TableColumn());
            table.Columns.Add(new System.Windows.Documents.TableColumn());
            table.Columns.Add(new System.Windows.Documents.TableColumn()
            {
                Style = (Style)summaryReportWindow.FindResource("LastColumn")
            });

            var groups = 0;
            var duration = TimeSpan.Zero;

            foreach (var periodGroup in view.VisibleEntities.GroupBy(e => column.GetValue<IViewPeriod>(e)).OrderBy(g => g.Key))
            {
                var header = new TableRow
                {
                    Style = (Style)summaryReportWindow.FindResource("Header")
                };
                var headerCell = newCell(periodGroup.Key);
                headerCell.ColumnSpan = table.Columns.Count;
                header.Cells.Add(headerCell);
                rowGroup.Rows.Add(header);

                var subHeader = new TableRow
                {
                    Style = (Style)summaryReportWindow.FindResource("Header2")
                };
                if (isWorkItemColumn)
                    subHeader.Cells.Add(newCell("Work Item"));
                if (isDateColumn)
                    subHeader.Cells.Add(newCell("Date", "Quantity"));
                subHeader.Cells.Add(newCell("Start", "Quantity"));
                subHeader.Cells.Add(newCell("End", "Quantity"));
                subHeader.Cells.Add(newCell("Duration", "Quantity"));
                rowGroup.Rows.Add(subHeader);

                var periods = 0;
                var groupDuration = TimeSpan.Zero;

                foreach (var period in periodGroup.OrderBy(p => p.Start))
                {
                    var row = new TableRow();
                    if (isWorkItemColumn)
                        row.Cells.Add(newCell(period.ItemName));
                    if (isDateColumn)
                        row.Cells.Add(newCell(period.Date.ToString("d"), "Quantity"));
                    row.Cells.Add(newCell(period.Start.ToString("T"), "Quantity"));
                    row.Cells.Add(newCell(period.End.ToString("T"), "Quantity"));
                    TimeSpan periodDuration;
                    switch (DurationTypeIndex)
                    {
                        case 1:
                            periodDuration = TimeSpan.FromHours(period.HoursToQuarters);
                            break;
                        case 2:
                            periodDuration = TimeSpan.FromHours(period.HoursToTenths);
                            break;
                        default:
                            periodDuration = period.Duration;
                            break;
                    }
                    row.Cells.Add(newCell(periodDuration, "Quantity"));
                    rowGroup.Rows.Add(row);

                    if (!string.IsNullOrWhiteSpace(period.Notes))
                    {
                        var notesRow = new TableRow();
                        var notesCell = newCell(period.Notes, "Notes");
                        notesCell.ColumnSpan = table.Columns.Count - 1;
                        notesRow.Cells.Add(notesCell);
                        rowGroup.Rows.Add(notesRow);
                    }

                    ++periods;
                    groupDuration += periodDuration;
                }

                var subTotalRow = new TableRow
                {
                    Style = (Style)summaryReportWindow.FindResource("TotalRow2")
                };
                var timeSlicesCell = newCell($"{periods} Time Slice(s)");
                timeSlicesCell.ColumnSpan = table.Columns.Count - 1;
                subTotalRow.Cells.Add(timeSlicesCell);
                subTotalRow.Cells.Add(newCell(groupDuration, "Quantity"));
                rowGroup.Rows.Add(subTotalRow);

                ++groups;
                duration += groupDuration;
            }

            var totalRow = new TableRow
            {
                Style = (Style)summaryReportWindow.FindResource("TotalRow")
            };
            var groupsCell = newCell($"{groups} Group(s)");
            groupsCell.ColumnSpan = table.Columns.Count - 1;
            totalRow.Cells.Add(groupsCell);
            totalRow.Cells.Add(newCell(duration, "Quantity"));
            rowGroup.Rows.Add(totalRow);

            table.RowGroups.Add(rowGroup);
            document.Blocks.Add(table);
            Document = document;
        }
    }

    public void Save()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Comma-Separated Values (CSV)|*.csv|Microsoft Excel 2007 Workbook (XLSX)|*.xlsx|Rich Text Format (RTF)|*.rtf",
            FilterIndex = 1
        };
        if (dlg.ShowDialog(summaryReportWindow) ?? false)
        {
            switch (dlg.FilterIndex)
            {
                case 2:
                    var dt = ConvertFlowDocumentTableToDataTable((System.Windows.Documents.Table)Document.Blocks.First(), false);
                    dt.TableName = "Grindstone Summary Report";
                    Spreadsheet.TransformData(dt, dlg.FileName, false);
                    break;
                case 3:
                    if (File.Exists(dlg.FileName))
                        File.Delete(dlg.FileName);
                    using (var stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        SetLightTheme();
                        new TextRange(Document.ContentStart, Document.ContentEnd).Save(stream, DataFormats.Rtf);
                        ResetTheme();
                    }
                    break;
                default:
                    using (var stream = new StreamWriter(dlg.FileName))
                        Csv.ToCsv(ConvertFlowDocumentTableToDataTable((System.Windows.Documents.Table)Document.Blocks.First(), false), stream, false);
                    break;
            }
        }
    }
}

var summaryReportWindows = new List<Window>();

Extension.App.DatabaseDismounting += async (sender, e) =>
{
    if (summaryReportWindows.Any())
        await Extension.OnUiThreadAsync(() =>
        {
            while (summaryReportWindows.Any())
                summaryReportWindows[0].Close();
        });
};

Extension.PostMessage(Guid.Parse("{27F65593-7235-4108-B5D9-F0DE417D8536}"), new {
    Title = "Summary Report",
    OnClick = (Action)(() =>
    {
        var summaryReportWindow = (Window)Extension.LoadUiElement("SummaryReportWindow.xaml");
        var dataContext = new SummaryReportWindowDataContext(summaryReportWindow);
        summaryReportWindow.DataContext = dataContext;

        var view = (RadComboBox)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "view");
        view.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        view.Items.SortDescriptions.Add(new SortDescription("EntityType.Name", ListSortDirection.Ascending));

        var column = (RadComboBox)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "column");
        column.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));

        var container = (FlowDocumentScrollViewer)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "container");

        var refresh = (Button)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "refresh");
        refresh.Click += (sender, e) => dataContext.RefreshDocument();

        var copy = (Button)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "copy");
        copy.Click += (sender, e) => dataContext.CopyToClipboard();

        var save = (Button)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "save");
        save.Click += (sender, e) => dataContext.Save();

        var print = (Button)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "print");
        print.Click += (sender, e) => dataContext.Print();

        var about = (Button)LogicalTreeHelper.FindLogicalNode(summaryReportWindow, "about");
        about.Click += (sender, e) =>
            MessageDialog.Present(Window.GetWindow((DependencyObject)sender), "This extension fully reimplements the Summary Report from previous versions of Grindstone.", "About Summary Report", MessageBoxImage.Information);

        summaryReportWindows.Add(summaryReportWindow);
        summaryReportWindow.Closed += (sender, e) =>
        {
            summaryReportWindows.Remove(summaryReportWindow);
            dataContext.Dispose();
        };

        summaryReportWindow.Show();
    })
});