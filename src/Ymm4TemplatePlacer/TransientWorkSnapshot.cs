using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

internal sealed record AssignmentWork(
    VoiceSnapshot Target,
    ItemTemplate SourceTemplate,
    TachieFaceItem SourceFace,
    string SourceName,
    string SourceCharacter);

internal sealed record ExpressionDraftWork(ExpressionPreset Saved, PresetDraftState Draft);
internal sealed record SelectionDraftWork(SelectionPreset Saved, SelectionPresetDraftState Draft);
internal sealed record PaletteLayerWork(Guid PaletteId, LayerPolicy Saved, bool UseTemplateLayer, string Minimum, string Maximum, string Preferred);
internal sealed record PaletteNameWork(Guid PaletteId, string SavedName, string DraftName);
internal sealed record PaletteCreationWork(string Name, string? CharacterName);
internal sealed record TemplateAdditionWork(
    Guid? PaletteId,
    VoiceSnapshot? ExpressionTarget,
    Character? ExpressionCharacter,
    string? ExpressionCharacterName,
    string Search,
    IReadOnlyList<ItemTemplate> Sources,
    string DisplayName);
internal sealed record LibraryEditWork(
    string Search,
    Guid? EntryId,
    LibraryEntry? SavedEntry,
    ItemTemplate? Source,
    string DisplayName,
    string? CharacterName,
    bool ManagingTemplates);

internal sealed record TransientWorkSnapshot(
    Timeline? Timeline,
    IReadOnlyList<AssignmentWork> Assignments,
    ExpressionDraftWork? ExpressionDraft,
    SelectionDraftWork? SelectionDraft,
    Guid? SelectionTemplateId,
    LibraryEntry? SelectionTemplateEntry,
    Guid? PaletteId,
    Guid? PaletteEntryId,
    PaletteLayerWork? PaletteLayer,
    PaletteNameWork? PaletteName,
    PaletteCreationWork? PaletteCreation,
    TemplateAdditionWork? TemplateAddition,
    LibraryEditWork LibraryEdit,
    bool AddingTemplate,
    bool ManagingTemplates)
{
    public bool LegacyWorkspace { get; init; }
    public ExpressionResumeWork? DeferredExpressions { get; init; }
}

internal sealed record PresetDraftState(
    string Name,
    ExpressionDuration Duration,
    string MaxGap,
    string StartOffset,
    string EndOffset,
    bool UseTemplateLayer,
    string Minimum,
    string Maximum,
    string Preferred)
{
    public static PresetDraftState Capture(PresetDraft draft) => new(draft.Name, draft.Duration, draft.MaxGap, draft.StartOffset,
        draft.EndOffset, draft.UseTemplateLayer, draft.Minimum, draft.Maximum, draft.Preferred);
    public void Apply(PresetDraft draft)
    {
        draft.Name = Name; draft.Duration = Duration; draft.MaxGap = MaxGap; draft.StartOffset = StartOffset; draft.EndOffset = EndOffset;
        draft.UseTemplateLayer = UseTemplateLayer; draft.Minimum = Minimum; draft.Maximum = Maximum; draft.Preferred = Preferred;
    }
}

internal sealed record SelectionPresetDraftState(
    string Name,
    string StartOffset,
    string EndOffset,
    string Duration,
    string HeadPadding,
    string TailPadding,
    string Tolerance,
    int AnchorPercent,
    bool UseTemplateLayer,
    string Minimum,
    string Maximum,
    string Preferred)
{
    public static SelectionPresetDraftState Capture(SelectionPresetDraft draft) => new(draft.Name, draft.StartOffset, draft.EndOffset,
        draft.Duration, draft.HeadPadding, draft.TailPadding, draft.Tolerance, draft.AnchorPercent, draft.UseTemplateLayer,
        draft.Minimum, draft.Maximum, draft.Preferred);
    public void Apply(SelectionPresetDraft draft)
    {
        draft.Name = Name; draft.StartOffset = StartOffset; draft.EndOffset = EndOffset; draft.Duration = Duration;
        draft.HeadPadding = HeadPadding; draft.TailPadding = TailPadding; draft.Tolerance = Tolerance; draft.AnchorPercent = AnchorPercent;
        draft.UseTemplateLayer = UseTemplateLayer; draft.Minimum = Minimum; draft.Maximum = Maximum; draft.Preferred = Preferred;
    }
}

public sealed partial class PlacerViewModel
{
    private TransientWorkSnapshot? disposedTransientWork;
    private TransientWorkSnapshot? pendingTransientWork;

    internal TransientWorkSnapshot TakeTransientWork()
    {
        if (disposedTransientWork != null) return disposedTransientWork;
        return CaptureTransientWork();
    }

    internal void AcceptTransientWork(TransientWorkSnapshot snapshot)
    {
        pendingTransientWork = snapshot;
        TryRestoreTransientWork();
    }

    private TransientWorkSnapshot CaptureTransientWork()
    {
        var assignments = Rows.Where(x => x.SelectedChoice.Template != null).Select(x =>
        {
            var selected = x.SelectedChoice.Template!;
            return new AssignmentWork(x.Target, selected.Template, selected.Face, selected.Name, selected.Character);
        }).ToArray();
        var expression = settingsAvailable && ExpressionPresetDirty
            ? new ExpressionDraftWork(CurrentExpressionPreset, PresetDraftState.Capture(ExpressionDraft)) : null;
        var selection = settingsAvailable && SelectionPresetDirty
            ? new SelectionDraftWork(CurrentSelectionPreset, SelectionPresetDraftState.Capture(SelectionDraft)) : null;
        var selectionEntry = SelectionTemplate?.Entry;
        var palette = CurrentPalette;
        var layer = palette != null && PaletteLayerDirty
            ? new PaletteLayerWork(palette.Id, palette.Layer, PaletteUseTemplateLayer, PaletteMinimumText, PaletteMaximumText, PalettePreferredText) : null;
        var paletteName = palette != null && paletteNameDraftId == palette.Id && PaletteNameDraft != savedPaletteName
            ? new PaletteNameWork(palette.Id, savedPaletteName, PaletteNameDraft) : null;
        var creating = IsCreatingPalette ? new PaletteCreationWork(NewPaletteName, NewPaletteCharacter?.Name) : null;
        VoiceSnapshot? expressionTarget = expressionAdditionTarget?.Target;
        var addition = IsAddingTemplate ? new TemplateAdditionWork(addingPaletteId, expressionTarget, expressionAdditionCharacter,
            expressionAdditionCharacterName, AddTemplateSearch, SelectedAddTemplateSources.ToArray(), AddTemplateDisplayName) : null;
        var selectedLibrary = SelectedLibraryEntry?.Entry;
        var libraryEdit = new LibraryEditWork(LibrarySearch, selectedLibrary?.Id, selectedLibrary, SelectedSourceTemplate,
            LibraryDisplayName, SelectedLibraryCharacter?.Name, IsManagingTemplates);
        return new TransientWorkSnapshot(timeline, assignments, expression, selection,
            selectionEntry?.Id, selectionEntry, palette?.Id, SelectedPaletteEntry?.LibraryEntryId,
            layer, paletteName, creating, addition, libraryEdit, IsAddingTemplate, IsManagingTemplates)
        { LegacyWorkspace = UseLegacyWorkspace, DeferredExpressions = deferredExpressionResume };
    }

    private static bool SameVoice(VoiceSnapshot left, VoiceSnapshot right) => ReferenceEquals(left.Voice, right.Voice) &&
        left.Character == right.Character && left.Frame == right.Frame && left.Length == right.Length &&
        left.Serif == right.Serif && left.Layer == right.Layer;

    private void TryRestoreTransientWork()
    {
        var snapshot = pendingTransientWork;
        if (snapshot == null || timeline == null) return;
        pendingTransientWork = null;
        if (!ReferenceEquals(snapshot.Timeline, timeline)) return;

        var skipped = 0;
        RestoreOrDeferExpressionWork(snapshot, ref skipped);

        if (snapshot.ExpressionDraft is { } expression)
        {
            var current = settings.ExpressionPresets.SingleOrDefault(x => x.Id == expression.Saved.Id);
            if (current == expression.Saved && settings.CurrentExpressionPresetId == expression.Saved.Id)
            {
                loadingPresetDraft = true;
                try { loadedExpressionPreset = current; expression.Draft.Apply(ExpressionDraft); }
                finally { loadingPresetDraft = false; }
                ExpressionDraftChanged(null, new System.ComponentModel.PropertyChangedEventArgs(null));
            }
            else skipped++;
        }

        if (snapshot.SelectionDraft is { } selection)
        {
            var current = settings.SelectionPresets.SingleOrDefault(x => x.Id == selection.Saved.Id);
            if (current == selection.Saved && settings.CurrentSelectionPresetId == selection.Saved.Id)
            {
                loadingSelectionDraft = true;
                try { loadedSelectionPreset = current; selection.Draft.Apply(SelectionDraft); }
                finally { loadingSelectionDraft = false; }
                SelectionDraftChanged(null, new System.ComponentModel.PropertyChangedEventArgs(null));
            }
            else skipped++;
        }

        if (snapshot.SelectionTemplateId is Guid selectionId && snapshot.SelectionTemplateEntry is { } selectionBase)
        {
            var current = settings.Library.SingleOrDefault(x => x.Id == selectionId);
            if (current == selectionBase) SelectionTemplate = SelectionTemplates.FirstOrDefault(x => x.Id == selectionId);
            else skipped++;
        }

        if (snapshot.PaletteId is Guid paletteId && CurrentPalette?.Id == paletteId)
        {
            if (snapshot.PaletteEntryId is Guid entryId)
            {
                var selected = PaletteEntries.FirstOrDefault(x => x.LibraryEntryId == entryId);
                if (selected != null) SelectedPaletteEntry = selected;
                else skipped++;
            }
            if (snapshot.PaletteLayer is { } layer)
            {
                if (CurrentPalette.Layer == layer.Saved)
                {
                    PaletteUseTemplateLayer = layer.UseTemplateLayer; PaletteMinimumText = layer.Minimum;
                    PaletteMaximumText = layer.Maximum; PalettePreferredText = layer.Preferred;
                }
                else skipped++;
            }
            if (snapshot.PaletteName is { } name)
            {
                if (CurrentPalette.Name == name.SavedName)
                {
                    paletteNameDraftId = paletteId; savedPaletteName = name.SavedName; PaletteNameDraft = name.DraftName;
                }
                else skipped++;
            }
        }
        else if (snapshot.PaletteLayer != null || snapshot.PaletteName != null || snapshot.PaletteEntryId != null) skipped++;

        if (snapshot.PaletteCreation is { } creation)
        {
            var character = LibraryCharacters.FirstOrDefault(x => x.Name == creation.CharacterName);
            if (creation.CharacterName == null || character != null)
            {
                NewPaletteName = creation.Name; NewPaletteCharacter = character; IsCreatingPalette = true;
            }
            else skipped++;
        }

        RestoreLibraryEdit(snapshot.LibraryEdit, ref skipped);
        if (snapshot.TemplateAddition is { } addition)
            RestoreTemplateAddition(addition, ref skipped);
        IsManagingTemplates = snapshot.ManagingTemplates;

        OnPropertyChanged(nameof(Summary));
        UpdateCommands(); UpdatePaletteCommands(); UpdateLibraryCommands(); RaiseSelectionCommands();
        if (skipped > 0)
        {
            HasError = false;
            Status = $"途中作業の一部を復元できませんでした（{skipped}件）。変更された対象は推測せず復元していません。";
            keepPartialStatus = true;
        }
        else if (deferredExpressionResume != null)
        {
            Status = "旧workspaceの未配置選択を保持しています。現在の配置へ自動変換していません。";
            keepPartialStatus = true;
        }
    }

    private void RestoreLibraryEdit(LibraryEditWork work, ref int skipped)
    {
        LibrarySearch = work.Search;
        LibraryEntryView? selected = null;
        if (work.EntryId is Guid id && work.SavedEntry is { } saved)
        {
            var current = settings.Library.SingleOrDefault(x => x.Id == id);
            if (current == saved) selected = LibraryEntries.FirstOrDefault(x => x.Id == id);
            else skipped++;
        }
        SelectedLibraryEntry = selected;
        if (work.Source != null)
        {
            if (SourceTemplates.Contains(work.Source)) SelectedSourceTemplate = work.Source;
            else skipped++;
        }
        else SelectedSourceTemplate = null;
        LibraryDisplayName = work.DisplayName;
        var character = LibraryCharacters.FirstOrDefault(x => x.Name == work.CharacterName);
        if (work.CharacterName == null || character != null) SelectedLibraryCharacter = character ?? LibraryCharacters.FirstOrDefault();
        else skipped++;
    }

    private void RestoreTemplateAddition(TemplateAdditionWork work, ref int skipped)
    {
        if (work.PaletteId is Guid paletteId && !settings.Palettes.Any(x => x.Id == paletteId)) { skipped++; return; }
        AssignmentRow? target = null;
        if (work.ExpressionTarget is { } expressionTarget)
        {
            target = Rows.SingleOrDefault(x => SameVoice(x.Target, expressionTarget));
            if (target == null || work.ExpressionCharacter == null || expressionTarget.Voice.Character == null ||
                !ReferenceEquals(expressionTarget.Voice.Character, work.ExpressionCharacter) ||
                !ReferenceEquals(ItemCharacters.ResolveUnique(timeline, work.ExpressionCharacterName ?? ""), work.ExpressionCharacter))
            { skipped++; return; }
        }
        addingPaletteId = work.PaletteId; expressionAdditionTarget = target; expressionAdditionCharacter = work.ExpressionCharacter;
        expressionAdditionCharacterName = work.ExpressionCharacterName;
        addTemplateSearch = work.Search; OnPropertyChanged(nameof(AddTemplateSearch));
        IsAddingTemplate = true; RefreshAddTemplateSources();
        var restorable = new List<ItemTemplate>();
        foreach (var source in work.Sources)
        {
            if (ItemSettings.Default.Templates.Contains(source) && AddSourceAllowed(source)) restorable.Add(source);
            else skipped++;
        }
        RestoreAddTemplateSelections(restorable, work.DisplayName);
    }
}
