using System.Collections.Immutable;
using System.IO;
using System.Windows;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyResumeContinuity(Timeline timeline, UndoRedoManager undo)
    {
        stage = "WUX8 resume continuity";
        var vm = ViewModel!; var view = View!;
        var root = Application.Current.Windows.Cast<Window>().Select(x => x.DataContext)
            .First(x => x?.GetType().FullName == "YukkuriMovieMaker.ViewModels.MainViewModel")!;
        var settingsBefore = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var originalSignature = Signature(timeline);
        var neutralSource = Template("WUX8/neutral", [new TextItem { Length = 22, Layer = 151 }]);
        var addSource = Template("WUX8/add-source", [new TextItem { Length = 18, Layer = 152 }]);
        ItemSettings.Default.Templates.Add(neutralSource); ItemSettings.Default.Templates.Add(addSource); vm.Refresh();

        vm.SelectedSourceTemplate = neutralSource; vm.LibraryDisplayName = "WUX8 neutral";
        vm.SelectedLibraryCharacter = vm.LibraryCharacters.First(x => x.Name == null);
        var entry = vm.RegisterLibrary();
        vm.NewPaletteName = "WUX8 palette"; vm.NewPaletteCharacter = vm.LibraryCharacters.First(x => x.Name == null);
        var palette = vm.CreatePalette();
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
        var target = timeline.Items.OfType<VoiceItem>().First();
        timeline.SelectedItems = ImmutableList.Create<IItem>(target); vm.SelectedSelectionProfile = vm.SelectionProfiles.First(x => x.Value == SelectionProfile.TargetCompanion);
        vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id);

        // Set material unfinished work only after all setup writes are complete.
        var assignment = vm.Rows.First(x => x.HasCandidates); var assignedChoice = assignment.Choices.First(x => x.Template != null);
        assignment.SelectedChoice = assignedChoice;
        vm.ExpressionDraft.StartOffset = "17";
        vm.SelectionDraft.StartOffset = "11";
        vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        vm.PaletteUseTemplateLayer = false; vm.PaletteMinimumText = "140"; vm.PaletteMaximumText = "160"; vm.PalettePreferredText = "153";
        vm.PaletteNameDraft = "WUX8 renamed draft";
        vm.BeginCreatePaletteCommand.Execute(null); vm.NewPaletteName = "WUX8 new palette draft";
        var settingsBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        var beforeReopen = Signature(timeline);
        var oldVm = vm;
        await SetToolVisible(root, false); await SetToolVisible(root, true); await Idle();
        vm = ViewModel ?? throw new InvalidOperationException("WUX8 reopened ViewModel missing.");
        view = View ?? throw new InvalidOperationException("WUX8 reopened View missing.");
        Assert(!ReferenceEquals(vm, oldVm), "WUX8 actual native close/reopen replaces the ViewModel for resume proof");
        var restoredRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, assignment.Target.Voice));
        Assert(restoredRow.SelectedChoice.Template != null && ReferenceEquals(restoredRow.SelectedChoice.Template.Template, assignedChoice.Template!.Template),
            "WUX8 exact unchanged expression assignment resumes across native ViewModel replacement");
        Assert(vm.ExpressionPresetDirty && vm.ExpressionDraft.StartOffset == "17" && vm.SelectionPresetDirty && vm.SelectionDraft.StartOffset == "11",
            "WUX8 unsaved Expression and Selection drafts resume without being committed");
        Assert(vm.SelectionTemplate?.Id == entry.Id && vm.CurrentPalette?.Id == palette.Id && vm.SelectedPaletteEntry?.LibraryEntryId == entry.Id,
            "WUX8 Selection Template and Palette working selection resume by exact stable identity");
        Assert(vm.PaletteLayerDirty && !vm.PaletteUseTemplateLayer && vm.PaletteMinimumText == "140" && vm.PaletteMaximumText == "160" && vm.PalettePreferredText == "153" &&
            vm.PaletteNameDraft == "WUX8 renamed draft" && vm.IsCreatingPalette && vm.NewPaletteName == "WUX8 new palette draft",
            "WUX8 Palette layer/name/new-Palette drafts resume without settings writes");
        Assert(Signature(timeline) == beforeReopen && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(settingsBytes),
            "WUX8 resume itself writes neither Timeline nor settings");

        // A nested add -> management workflow must also survive the same native replacement.
        vm.CancelCreatePaletteCommand.Execute(null);
        vm.RevertExpressionPresetCommand.Execute(null); vm.RevertSelectionPresetCommand.Execute(null);
        vm.PaletteNameDraft = vm.CurrentPalette!.Name;
        vm.PaletteUseTemplateLayer = vm.CurrentPalette.Layer.UseTemplateLayer;
        vm.PaletteMinimumText = vm.CurrentPalette.Layer.Minimum.ToString(); vm.PaletteMaximumText = vm.CurrentPalette.Layer.Maximum.ToString();
        vm.PalettePreferredText = vm.CurrentPalette.Layer.Preferred.ToString();
        restoredRow.SelectedChoice = restoredRow.Choices[0];
        vm.BeginTemplateAddition(); vm.AddTemplateSearch = "WUX8/add"; vm.AddTemplateSource = addSource; vm.AddTemplateDisplayName = "WUX8 add draft";
        vm.OpenTemplateManagementCommand.Execute(null);
        vm.LibrarySearch = "WUX8"; vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id);
        vm.SelectedSourceTemplate = addSource; vm.LibraryDisplayName = "WUX8 management draft";
        settingsBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath); beforeReopen = Signature(timeline);
        await SetToolVisible(root, false); await SetToolVisible(root, true); await Idle();
        vm = ViewModel ?? throw new InvalidOperationException("WUX8 second reopened ViewModel missing.");
        Assert(vm.IsAddingTemplate && vm.IsManagingTemplates && vm.AddTemplateSearch == "WUX8/add" && ReferenceEquals(vm.AddTemplateSource, addSource) &&
            vm.AddTemplateDisplayName == "WUX8 add draft",
            "WUX8 add-task search/source/name and nested management state resume together");
        Assert(vm.LibrarySearch == "WUX8" && ReferenceEquals(vm.SelectedSourceTemplate, addSource) && vm.LibraryDisplayName == "WUX8 management draft",
            "WUX8 unfinished Template-management inputs resume without overwriting the registered entry");
        Assert(Signature(timeline) == beforeReopen && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(settingsBytes),
            "WUX8 nested-task resume remains zero-write");

        // If a target changes while the Tool is suspended, do not guess or restore its old assignment.
        vm.CloseTemplateManagementCommand.Execute(null); vm.CancelAddTemplateCommand.Execute(null);
        var changedRow = vm.Rows.First(x => x.HasCandidates); var changedChoice = changedRow.Choices.First(x => x.Template != null);
        changedRow.SelectedChoice = changedChoice; var voice = changedRow.Target.Voice; var originalSerif = voice.Serif;
        settingsBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        await SetToolVisible(root, false); voice.Serif = (originalSerif ?? "") + " / changed while suspended"; var changedSignature = Signature(timeline);
        await SetToolVisible(root, true); await Idle();
        vm = ViewModel ?? throw new InvalidOperationException("WUX8 changed-target reopened ViewModel missing.");
        var currentRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
        Assert(currentRow.SelectedChoice.Template == null && vm.Status.Contains("一部を復元できません", StringComparison.Ordinal) &&
            vm.Status.Contains("推測せず", StringComparison.Ordinal),
            "WUX8 changed Voice is not guessed back into an old assignment and the partial resume is truthful");
        Assert(Signature(timeline) == changedSignature && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(settingsBytes),
            "WUX8 rejected stale resume does not mutate Timeline or settings");
        voice.Serif = originalSerif ?? ""; vm.Refresh();

        // Restore persistent setup to the exact pre-proof configuration.
        timeline.SelectedItems = [];
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == palette.Id); vm.DeleteCurrentPalette();
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == settingsBefore.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == settingsBefore.ManualStylePaletteId); vm.ActivePaletteKind = settingsBefore.PaletteMode;
        var originalSelection = settingsBefore.SelectionPresets.Single(x => x.Id == settingsBefore.CurrentSelectionPresetId);
        var count = originalSelection.Profile is SelectionProfile.SelectionRange or SelectionProfile.Boundary ? 2 : 1;
        timeline.SelectedItems = timeline.Items.Take(count).ToImmutableList();
        vm.SelectedSelectionProfile = vm.SelectionProfiles.First(x => x.Value == originalSelection.Profile);
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == originalSelection.Id);
        timeline.SelectedItems = [];
        ItemSettings.Default.Templates.Remove(neutralSource); ItemSettings.Default.Templates.Remove(addSource); vm.Refresh();
        Assert(Signature(timeline) == originalSignature, "WUX8 fixture cleanup restores the original native Timeline");
        Log("WUX8=PASS");
    }
}
