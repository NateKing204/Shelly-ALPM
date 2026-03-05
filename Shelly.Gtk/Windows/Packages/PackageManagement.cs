using Gtk;
using Shelly.Gtk.Helpers;
using Shelly.Gtk.Services;
using Shelly.Gtk.UiModels.PackageManagerObjects;
using Shelly.Gtk.UiModels.PackageManagerObjects.GObjects;

namespace Shelly.Gtk.Windows.Packages;

public class PackageManagement(IPrivilegedOperationService privilegedOperationService) : IShellyWindow
{
    private Box _box = null!;
    private ColumnView _columnView = null!;
    private Gio.ListStore _listStore = null!;
    private List<AlpmPackageDto> _allPackages = [];
    private string _searchText = string.Empty;

    public Box CreateWindow()
    {
        var builder = Builder.NewFromFile("UiFiles/Package/PackageManagement.ui");
        _box = (Box)builder.GetObject("PackageManagement")!;
        _columnView = (ColumnView)builder.GetObject("package_grid")!;
        var searchEntry = (SearchEntry)builder.GetObject("search_entry")!;

        var checkColumn = (ColumnViewColumn)builder.GetObject("check_column")!;
        var nameColumn = (ColumnViewColumn)builder.GetObject("name_column")!;
        var sizeColumn = (ColumnViewColumn)builder.GetObject("size_column")!;
        var versionColumn = (ColumnViewColumn)builder.GetObject("version_column")!;

        _listStore = Gio.ListStore.New(AlpmPackageGObject.GetGType());
        var selectionModel = SingleSelection.New(_listStore);
        _columnView.SetModel(selectionModel);

        SetupColumns(checkColumn, nameColumn, sizeColumn, versionColumn);

        _columnView.OnRealize += (_, _) => { _ = LoadDataAsync(); };
        searchEntry.OnSearchChanged += (_, _) =>
        {
            _searchText = searchEntry.GetText();
            ApplyFilter();
        };

        return _box;
    }

    private static void SetupColumns(ColumnViewColumn checkColumn, ColumnViewColumn nameColumn, ColumnViewColumn sizeColumn, ColumnViewColumn versionColumn)
    {
        var checkFactory = SignalListItemFactory.New();
        checkFactory.OnSetup += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var check = new CheckButton { MarginStart = 10, MarginEnd = 10 };
            listItem.SetChild(check);
        };
        checkFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is not AlpmPackageGObject pkgObj ||
                listItem.GetChild() is not CheckButton checkButton) return;

            checkButton.OnToggled -= OnCheckToggled;
            checkButton.SetActive(pkgObj.IsSelected);
            checkButton.OnToggled += OnCheckToggled;
            return;

            void OnCheckToggled(object? s, EventArgs e)
            {
                pkgObj.IsSelected = checkButton.GetActive();
            }
        };
        checkColumn.SetFactory(checkFactory);
        
        var nameFactory = SignalListItemFactory.New();
        nameFactory.OnSetup += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            listItem.SetChild(Label.New(string.Empty));
        };
        nameFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is not AlpmPackageGObject { Package: { } pkg } ||
                listItem.GetChild() is not Label label) return;
            label.SetText(pkg.Name);
            label.Halign = Align.Start;
        };
        nameColumn.SetFactory(nameFactory);
        
        var sizeFactory = SignalListItemFactory.New();
        sizeFactory.OnSetup += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            listItem.SetChild(Label.New(string.Empty));
        };
        sizeFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is not AlpmPackageGObject { Package: { } pkg } ||
                listItem.GetChild() is not Label label) return;
            label.SetText(SizeHelpers.FormatSize(pkg.InstalledSize));
            label.Halign = Align.End;
        };
        sizeColumn.SetFactory(sizeFactory);
        
        var versionFactory = SignalListItemFactory.New();
        versionFactory.OnSetup += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            listItem.SetChild(Label.New(string.Empty));
        };
        versionFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is not AlpmPackageGObject { Package: { } pkg } ||
                listItem.GetChild() is not Label label) return;
            label.SetText(pkg.Version);
            label.Halign = Align.End;
        };
        versionColumn.SetFactory(versionFactory);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _allPackages = await privilegedOperationService.GetInstalledPackagesAsync();
            GLib.Functions.IdleAdd(0, () =>
            {
                ApplyFilter();
                return false;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load packages: {e.Message}");
        }
    }

    private void ApplyFilter()
    {
        _listStore.RemoveAll();
        var filtered = _allPackages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var package in filtered)
        {
            var obj = new AlpmPackageGObject { Package = package };
            _listStore.Append(obj);
        }
    }
}