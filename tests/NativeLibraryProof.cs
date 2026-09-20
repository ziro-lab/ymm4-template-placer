using System.IO;
using System.Text.Json;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyLibrary(Timeline timeline)
    {
        stage = "W3 Library";
        var vm = ViewModel!; var view = View!;
        var before = Signature(timeline);
        vm.RefreshLibraryCommand.Execute(null); ShowTask(view, "library"); await Idle();
        var panel = view.LibrarySurface;
        Assert(panel.IsLoaded && ReferenceEquals(panel.DataContext, vm), "W3 Library tab is hosted in the real plugin UI");
        var source = vm.SourceTemplates.Single(x => x.Name == "TestA/Smile");
        panel.SourceCombo.SelectedItem = source; await Idle();
        panel.DisplayNameBox.Text = "どや"; panel.DisplayNameBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
        Assert(vm.LibraryDisplayName == "どや" && panel.RegisterButton.IsEnabled, "W3 short Library display name is two-way bound");
        ((IInvokeProvider)new ButtonAutomationPeer(panel.RegisterButton).GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
        var entry = vm.SelectedLibraryEntry?.Entry ?? throw new InvalidOperationException(vm.Status);
        Assert(!vm.HasError && entry.DisplayName == "どや" && source.Name == "TestA/Smile", "W3 Library registration does not rename the YMM4 Template");
        Assert(TemplateResolver.Resolve(entry).State == TemplateReferenceState.Resolved, "W3 exactly one strict source resolves");
        var clone = TemplateResolver.Clone(entry);
        Assert(!ReferenceEquals(clone, source.Items[0]) && clone.Length == source.Items[0].Length, "W3 generic singleton source clone preserves intrinsic Length");
        var copy = Template(source.Name, [new TachieFaceItem(ItemCharacters.Get(source.Items[0])!) { Length = 22 }]);
        ItemSettings.Default.Templates.Add(copy);
        try
        {
            Assert(TemplateLocator.Capture(copy) == entry.Source, "W3 duplicate fixture shares Name Path and SceneId");
            Assert(TemplateResolver.Resolve(entry).State == TemplateReferenceState.Ambiguous, "W3 duplicate strict locator is ambiguous; no first-match repair");
            RejectWithoutMutation(timeline, () => TemplateResolver.Clone(entry), "W3 ambiguous clone is rejected without Timeline mutation");
        }
        finally { ItemSettings.Default.Templates.Remove(copy); }
        ItemSettings.Default.Templates.Remove(source);
        try
        {
            vm.RefreshLibraryCommand.Execute(null);
            Assert(TemplateResolver.Resolve(entry).State == TemplateReferenceState.Missing && vm.LibraryEntries.Single(x => x.Id == entry.Id).Status.Contains("見つかりません", StringComparison.Ordinal), "W3 missing source remains visible with relink guidance");
            RejectWithoutMutation(timeline, () => TemplateResolver.Clone(entry), "W3 missing source is never fuzzy-resolved");
            panel.SourceCombo.SelectedItem = vm.SourceTemplates.Single(x => x.Name == "TestA/Neutral"); await Idle();
            panel.DisplayNameBox.Text = "どや"; panel.DisplayNameBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
            ((IInvokeProvider)new ButtonAutomationPeer(panel.RelinkButton).GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
            Assert(!vm.HasError && vm.SelectedLibraryEntry?.Id == entry.Id && vm.SelectedLibraryEntry.Entry.Source.Name == "TestA/Neutral", "W3 explicit UI relink preserves the stable Library ID");
        }
        finally { ItemSettings.Default.Templates.Add(source); }
        var persisted = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(persisted.Library.Single(x => x.Id == entry.Id).DisplayName == "どや", "W3 Library reference and display name survive settings reload");
        using (var json = JsonDocument.Parse(File.ReadAllText(PlacerSettingsStore.DefaultPath)))
            Assert(json.RootElement.GetProperty("Library").EnumerateArray().All(x => !x.TryGetProperty("Items", out _) && !x.TryGetProperty("Template", out _)), "W3 settings contain no copied Template bodies");
        var damaged = Path.Combine(output, "invalid-plugin-settings.json"); File.WriteAllText(damaged, "{");
        var badStore = new PlacerSettingsStore(damaged); var rejected = false;
        try { badStore.Load(); } catch (JsonException) { rejected = true; }
        Assert(rejected && File.ReadAllText(damaged) == "{", "W3 corrupt settings are reported and preserved");
        RejectWithoutMutation(timeline, () => badStore.Save(new()), "W3 failed settings load cannot overwrite the original");
        Assert(Signature(timeline) == before, "W3 all Library operations leave Timeline unchanged");
        SaveView(view); ShowTask(view, "expression"); await Idle();
        Log("W3=PASS");
    }
}
