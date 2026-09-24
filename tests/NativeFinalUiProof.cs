using System.IO;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void SaveNamedView(PlacerView view, string filename)
    {
        SaveView(view);
        File.Copy(Path.Combine(output, "native-plugin-ui.png"), Path.Combine(output, filename), true);
    }

    private static async Task VerifyFinalUi(Timeline timeline)
    {
        stage = "W12 current UI smoke";
        var vm = ViewModel!; var view = View!;
        timeline.SelectedItems = []; vm.Refresh();

        var before = Signature(timeline);
        var settingsBefore = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var nextId = settingsBefore.NextAssociationId;

        Assert(ReferenceEquals(view.PaletteTab.Content, view.RelativePaletteSurface) &&
            ReferenceEquals(view.SelectionTab.Content, view.RelativeSettingsSurface) &&
            view.PaletteTab.Header?.ToString() == "配置" &&
            view.ExpressionTab.Header?.ToString() == "表情をまとめて" &&
            view.SelectionTab.Header?.ToString() == "設定",
            "W12 current Tool exposes only the accepted placement/expression/settings primary surfaces");

        var source = new TextItem { Frame = 0, Length = 20, Layer = 100, Remark = "current UI fixture" };
        var template = Template("W12/CurrentOverview", [source]);
        ItemSettings.Default.Templates.Add(template);

        vm.Refresh();
        vm.SelectedSourceTemplate = template;
        vm.LibraryDisplayName = "強調";
        var entry = vm.RegisterLibrary();

        ShowTask(view, "library"); await Idle();
        Assert(TaskIsVisible(view, "library") && view.BackFromManagementButton.IsVisible,
            "W12 current Template management remains a bounded secondary task with an explicit return action");

        var search = view.LibrarySurface.LibrarySearchBox;
        search.Text = "W12/CurrentOverview";
        search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        await Idle();
        Assert(vm.LibraryEntries.Count == 1 && vm.LibraryEntries[0].Id == entry.Id,
            "W12 current Library search matches the exact registered source without changing identity");

        search.Text = "no-result-79fba0";
        search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        await Idle();
        view.LibrarySurface.LibraryEmptyNotice.BringIntoView(); await Idle();
        Assert(vm.LibraryEntries.Count == 0 && view.LibrarySurface.LibraryEmptyNotice.IsVisible,
            "W12 empty current Library search shows recovery guidance instead of a silent blank list");
        SaveNamedView(view, "ui-current-library-empty.png");

        search.Text = "";
        search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        await Idle();
        Assert(vm.LibraryEntries.Any(x => x.Id == entry.Id),
            "W12 clearing current Library search restores the same registered source");

        var tasks = new[] { "palette", "expression", "settings", "library" };
        foreach (var task in tasks)
        {
            ShowTask(view, task); await Idle(); view.UpdateLayout();
            Assert(view.IsLoaded && TaskIsVisible(view, task) && view.Background != null && view.Foreground != null,
                "W12 current task has a loaded themed native surface: " + task);
        }

        var width = view.Width; var height = view.Height;
        try
        {
            view.Width = 360; view.Height = 320;
            foreach (var task in tasks)
            {
                ShowTask(view, task); await Idle(); view.UpdateLayout();
                Assert(view.ActualWidth >= 360 && view.ActualHeight >= 280,
                    "W12 current narrow Tool remains measurable without reviving a legacy workspace: " + task);
            }
            SaveNamedView(view, "ui-current-settings-narrow.png");
        }
        finally
        {
            view.Width = width; view.Height = height; await Idle();
        }

        timeline.SelectedItems = [];
        ShowTask(view, "settings"); await Idle();
        Assert(view.RelativeSettingsSurface.IsVisible && ReferenceEquals(view.SelectionTab.Content, view.RelativeSettingsSurface),
            "W12 Settings navigation resolves to the accepted current Settings surface");

        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id);
        vm.UnregisterLibrary();
        ItemSettings.Default.Templates.Remove(template);
        vm.Refresh();
        ShowTask(view, "expression"); await Idle();

        var settingsAfter = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(Signature(timeline) == before && settingsAfter.NextAssociationId == nextId,
            "W12 current UI smoke and Library organization never mutate Timeline Items or allocate expression associations");

        Log("W12_UI=PASS");
    }
}
