using System.ComponentModel;
using System.Text.Json;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal sealed record ManagedIntentExpressionBundle(long Serial, IntentAssociationTag Descriptor, IReadOnlyList<IItem> Members);
internal sealed record ManagedIntentExpressionAssociation(long? Serial, ManagedIntentExpressionBundle? Bundle);

// Compatibility wrapper for the existing Template mutation path. P6 can read both
// source kinds, but Template mutation must not consume a Tachie-Preset bundle yet.
internal static class ManagedIntentExpressionReader
{
    public static ManagedIntentExpressionAssociation Read(Timeline timeline, VoiceItem voice)
    {
        var association = ManagedExpressionReader.Read(timeline, voice);
        if (association.Bundle == null) return new(association.Serial, null);
        if (association.Bundle.Descriptor.Kind != ManagedExpressionSourceKind.Template)
            throw new InvalidOperationException("現在の関連表情は立ち絵プリセット由来です。対応する置換経路が有効になるまで変更しません。");
        return new(association.Serial, new(
            association.Bundle.Serial,
            association.Bundle.Descriptor.Template!,
            association.Bundle.Members));
    }
}

internal sealed class IntentAssociationSerialAllocator
{
    private long next;
    public long NextSerial => next;
    public IntentAssociationSerialAllocator(Timeline timeline, long configured)
    {
        if (configured < 1) throw new InvalidOperationException("関連付け連番の設定が不正です。");
        next = configured;
        foreach (var item in timeline.Items)
        {
            if (AssociationTag.Voice(item.Remark, out var target) == AssociationTagState.Valid) Observe(target);
            if (AssociationTag.Source(item.Remark, out var source) == AssociationTagState.Valid) Observe(source!.Serial);
        }
    }
    private void Observe(long value)
    {
        if (value < next) return;
        if (value == long.MaxValue) throw new InvalidOperationException("関連付けIDを新規発行できません。上限に達しています。");
        next = value + 1;
    }
    public long Allocate()
    {
        if (next == long.MaxValue) throw new InvalidOperationException("関連付けIDを新規発行できません。上限に達しています。");
        return next++;
    }
}

internal sealed class IntentExpressionMutation
{
    private readonly AssignmentRow row;
    private readonly ManagedExpressionAssociation existing;
    private readonly List<(IntentGeometry Geometry, Guid PaletteId, string Palette, Guid LibraryId, TemplateLocator Source)> guarded;
    public PlacementPlan Plan { get; }
    public bool Skipped { get; }
    internal AssignmentRow Row => row;
    private IntentExpressionMutation(AssignmentRow row, ManagedExpressionAssociation existing,
        PlacementPlan plan, bool skipped,
        List<(IntentGeometry Geometry, Guid PaletteId, string Palette, Guid LibraryId, TemplateLocator Source)> guarded)
    { this.row = row; this.existing = existing; Plan = plan; Skipped = skipped; this.guarded = guarded; }
    public static IntentExpressionMutation Create(Timeline timeline, AssignmentRow row, TemplateChoice choice, PlacerSettings settings,
        IntentAssociationSerialAllocator allocator, bool allowSkip)
    {
        ValidateRow(timeline, row);
        if (choice.TachiePreset != null || choice.IsCurrentOtherSource)
            throw new InvalidOperationException("立ち絵プリセットの候補をテンプレート配置として実行できません。");
        var association = ManagedExpressionReader.Read(timeline, row.Target.Voice);
        ManagedExpressionSafety.ValidatePresetState(association.Bundle);
        var removals = association.Bundle?.Members.ToArray() ?? [];
        if (choice.Template == null)
            return new(row, association, PlacementPlan.Create(timeline, [], removals: removals), false, []);
        if (!choice.IsAvailable || choice.Template.IntentSource is not { } reference)
            throw new InvalidOperationException("表情の選択元が現在のパレットと一致しません。候補を更新して選び直してください。");
        var current = reference.ResolveCurrent(settings);
        var voice = row.Target.Voice;
        if (current.Bundle.CharacterName != voice.CharacterName) throw new InvalidOperationException("表情テンプレートと対象音声のキャラクター名が一致しません。");
        var currentHash = IntentAssociationTag.Hash(current.Bundle);
        if (association.Bundle?.Descriptor is
            { Kind: ManagedExpressionSourceKind.Template, Template: { } existingTemplate } &&
            existingTemplate.Palette == current.Palette.Id &&
            existingTemplate.Entry == current.Entry.LibraryEntryId &&
            existingTemplate.GeometryHash == currentHash)
            return new(row, association, PlacementPlan.Create(timeline, []), false, []);
        var own = removals.ToHashSet();
        var context = IntentSelectionContext.ForItems(timeline, [voice]);
        var geometry = IntentPlacementGeometry.Prepare(timeline, context, current.Palette, current.Entry, settings.Library,
            timeline.Items.Where(x => !own.Contains(x)));
        if (geometry.Skipped)
        {
            if (allowSkip) return new(row, association, PlacementPlan.Create(timeline, []), true, []);
            throw new InvalidOperationException("この表情の配置条件では配置先を決められません。現在の表情は変更していません。");
        }
        var serial = association.Serial ?? allocator.Allocate();
        PlannedItemUpdate[] updates = association.Serial == null
            ? [PlannedItemUpdate.RemarkOnly(voice, PluginRemarks.Append(voice.Remark, AssociationTag.TargetLine(serial)))] : [];
        var group = Guid.NewGuid(); var hash = IntentAssociationTag.Hash(geometry.Source);
        for (var i = 0; i < geometry.Items.Count; i++)
        {
            var item = geometry.Items[i];
            var tag = new IntentAssociationTag(group, current.Palette.Id, current.Entry.LibraryEntryId, i, geometry.Items.Count, hash);
            item.Remark = PluginRemarks.Append(PluginRemarks.Append(PluginRemarks.Append(PluginRemarks.WithoutAssociation(item.Remark),
                PlacementEngine.Marker), AssociationTag.SourceLine(serial)), tag.Line);
        }
        var guards = new List<(IntentGeometry, Guid, string, Guid, TemplateLocator)>
        { (geometry, current.Palette.Id, JsonSerializer.Serialize(current.Palette), current.Entry.LibraryEntryId, geometry.Source.Entry.Source) };
        return new(row, association, PlacementPlan.Create(timeline, geometry.Items, updates, removals), false, guards);
    }
    public void ValidateCurrent(Timeline timeline, PlacerSettings settings)
    {
        ValidateRow(timeline, row);
        var currentAssociation = ManagedExpressionReader.Read(timeline, row.Target.Voice);
        if (!ManagedExpressionSafety.Same(existing, currentAssociation))
            throw new InvalidOperationException("計画後に現在の関連表情が変わりました。配置していません。");
        ManagedExpressionSafety.ValidatePresetState(currentAssociation.Bundle);
        foreach (var check in guarded)
        {
            check.Geometry.Context.ValidateCurrent(timeline, false); check.Geometry.Source.ValidateCurrent();
            if (JsonSerializer.Serialize(settings.IntentPalettes.SingleOrDefault(x => x.Id == check.PaletteId)) != check.Palette ||
                settings.Library.SingleOrDefault(x => x.Id == check.LibraryId)?.Source != check.Source)
                throw new InvalidOperationException("計画後に表情パレットが変更されました。配置していません。");
            IntentPlacementGeometry.ValidateCharacters(check.Geometry.Context, check.Geometry.Source);
        }
    }
    public int CommitWithinOpenRecord(Timeline timeline, PlacerSettings settings)
    { ValidateCurrent(timeline, settings); return Plan.CommitWithinOpenRecord(timeline); }
    private static void ValidateRow(Timeline timeline, AssignmentRow row)
    {
        var voice = row.Target.Voice;
        if (!timeline.Items.Contains(voice) || voice.CharacterName != row.Target.Character || voice.Frame != row.Target.Frame ||
            voice.Length != row.Target.Length || voice.Layer != row.Target.Layer || (voice.Serif ?? "") != row.Target.Serif)
            throw new InvalidOperationException("対象音声が「表情をまとめて」を開いた時点から変更されています。メンテナンスから一覧を読み直してください。");
    }
}

internal sealed class ExpressionTrialSession : IDisposable
{
    private Timeline? timeline; private UndoRedoManager? undo; private VoiceItem? voice;
    private readonly HashSet<INotifyPropertyChanging> itemSubscriptions = [];
    private bool open, ownedMutation, closing;
    public bool IsOpen => open; public VoiceItem? Voice => voice;
    public void Begin(Timeline targetTimeline, UndoRedoManager manager, VoiceItem targetVoice)
    {
        if (open && ReferenceEquals(timeline, targetTimeline) && ReferenceEquals(undo, manager) && ReferenceEquals(voice, targetVoice)) return;
        Close(); timeline = targetTimeline; undo = manager; voice = targetVoice; Subscribe();
        try { manager.Record(); open = true; }
        catch { Unsubscribe(); timeline = null; undo = null; voice = null; throw; }
    }
    public T ExecuteOwned<T>(Func<T> action)
    {
        if (!open) throw new InvalidOperationException("表情試用のUndo sessionが開始されていません。");
        ownedMutation = true;
        try { return action(); }
        finally { ownedMutation = false; RewireItems(); }
    }
    public void Close()
    {
        if (!open) { Unsubscribe(); timeline = null; undo = null; voice = null; return; }
        closing = true;
        try { undo!.Record(); open = false; Unsubscribe(); timeline = null; undo = null; voice = null; }
        finally { closing = false; }
    }
    private void Subscribe()
    {
        timeline!.PropertyChanging += ExternalChanging; undo!.Recorded += ManagerRecorded; undo.Undoed += ManagerUndoRedo; undo.Redoed += ManagerUndoRedo; RewireItems();
    }
    private void RewireItems()
    {
        foreach (var item in itemSubscriptions) item.PropertyChanging -= ExternalChanging;
        itemSubscriptions.Clear();
        if (timeline == null) return;
        foreach (var item in timeline.Items.OfType<INotifyPropertyChanging>())
            if (itemSubscriptions.Add(item)) item.PropertyChanging += ExternalChanging;
    }
    private void ExternalChanging(object? sender, PropertyChangingEventArgs e) { if (open && !ownedMutation && !closing) Close(); }
    private void ManagerRecorded(object? sender, EventArgs e) { if (open && !closing) EndWithoutRecord(); }
    private void ManagerUndoRedo(object? sender, EventArgs e) { if (open) EndWithoutRecord(); }
    private void EndWithoutRecord() { open = false; Unsubscribe(); timeline = null; undo = null; voice = null; }
    private void Unsubscribe()
    {
        if (timeline != null) timeline.PropertyChanging -= ExternalChanging;
        if (undo != null) { undo.Recorded -= ManagerRecorded; undo.Undoed -= ManagerUndoRedo; undo.Redoed -= ManagerUndoRedo; }
        foreach (var item in itemSubscriptions) item.PropertyChanging -= ExternalChanging;
        itemSubscriptions.Clear();
    }
    public void Dispose() => Close();
}

public sealed partial class PlacerViewModel
{
    private readonly ExpressionTrialSession expressionTrialSession = new();
    private bool suppressExpressionApply;
    private long expressionAssociationNextSerial;
    public ActionCommand NavigateExpressionRowCommand { get; private set; } = null!;
    private void InitializeExpressionImmediate()
    {
        NavigateExpressionRowCommand = new ActionCommand(x => ExpressionRowsMatchSource && x is AssignmentRow row && Rows.Contains(row) && timeline != null &&
            timeline.Items.Contains(row.Target.Voice), x => Guard(() => NavigateExpressionRow((AssignmentRow)x!)));
        OnPropertyChanged(nameof(NavigateExpressionRowCommand));
        InitializeTachiePresetCalibration();
        InitializeTachiePresetSourceRegistration();
    }
    private void NavigateExpressionRow(AssignmentRow row) => QueueExpressionNavigation(row);
    internal void SetExpressionRowContext(AssignmentRow? row)
    {
        if (!ReferenceEquals(expressionTrialSession.Voice, row?.Target.Voice))
        {
            CancelTachiePresetApply();
            if (expressionTrialSession.IsOpen) CloseExpressionTrialSession();
        }
    }
    internal void CloseExpressionTrialSession() => expressionTrialSession.Close();
    private IntentAssociationSerialAllocator CreateExpressionSerialAllocator(Timeline current)
    {
        var seed = Math.Max(settings.NextAssociationId, Math.Max(1, expressionAssociationNextSerial));
        var allocator = new IntentAssociationSerialAllocator(current, seed);
        expressionAssociationNextSerial = Math.Max(expressionAssociationNextSerial, allocator.NextSerial);
        return allocator;
    }
    private long ExpressionSerialSeed(Timeline current)
    {
        var allocator = CreateExpressionSerialAllocator(current);
        return allocator.NextSerial;
    }
    private void ObserveExpressionSerial(long nextSerial) => expressionAssociationNextSerial = Math.Max(expressionAssociationNextSerial, nextSerial);
    private void RequireExpressionDraftCompatible(Timeline current, AssignmentRow row, TemplateChoice choice)
    {
        var draft = IntentSettings;
        if (draft?.HasChanges != true) return;
        Guid paletteId, sourceId;
        if (choice.Template?.IntentSource is { } source)
        {
            paletteId = source.Palette.Id;
            sourceId = source.Entry.SourceId;
        }
        else if (choice.RegisteredPreset is { } registered)
        {
            paletteId = registered.PaletteId;
            sourceId = registered.SourceId;
        }
        else
        {
            var association = ManagedExpressionReader.Read(current, row.Target.Voice);
            if (association.Bundle?.Descriptor is
                { Kind: ManagedExpressionSourceKind.Template, Template: { } templateDescriptor })
            {
                paletteId = templateDescriptor.Palette;
                sourceId = templateDescriptor.Entry;
            }
            else if (association.Bundle?.Descriptor is
                { Kind: ManagedExpressionSourceKind.RegisteredTachiePreset, RegisteredTachiePreset: { } presetDescriptor })
            {
                paletteId = presetDescriptor.Palette;
                sourceId = presetDescriptor.Source;
            }
            else return;
        }
        if (draft.HasExpressionDependencyChanges(paletteId, sourceId, settings, out var setName))
            throw new InvalidOperationException(
                $"この表情Set「{setName}」に未保存の変更があります。保存または破棄してから表情を変更してください。");
    }

    private void ApplyImmediateExpressionChoice(AssignmentRow row)
    {
        if (suppressExpressionApply || !IsTemplateExpressionSource || !ExpressionRowsMatchSource || !UsesRelativeExpressions || row.SelectedChoice.TachiePreset != null || row.SelectedChoice.RegisteredPreset != null || row.SelectedChoice.IsCurrentOtherSource) return;
        try
        {
            var current = RequireTimeline();
            RequireExpressionDraftCompatible(current, row, row.SelectedChoice);
            if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
            var allocator = CreateExpressionSerialAllocator(current);
            var mutation = IntentExpressionMutation.Create(current, row, row.SelectedChoice, settings, allocator, false);
            ObserveExpressionSerial(allocator.NextSerial);
            if (mutation.Plan.ChangeCount != 0)
            {
                expressionTrialSession.Begin(current, undo, row.Target.Voice);
                var changed = expressionTrialSession.ExecuteOwned(() =>
                    ExecuteOwnedExpressionTimelineMutation(() => mutation.CommitWithinOpenRecord(current, settings)));
                HasError = false; Status = row.SelectedChoice.Template == null ? "この音声の関連表情を外しました。" : $"「{row.SelectedChoice.DisplayName}」を即時反映しました（{changed}変更）。";
            }
            RestoreExpressionChoiceFromTimeline(row);
            if (mutation.Plan.ChangeCount != 0) QueueExpressionNavigation(row, refreshCurrentContent: true);
        }
        catch (Exception ex)
        {
            HasError = true; Status = "表情を変更できませんでした: " + ex.GetBaseException().Message; RestoreExpressionChoiceFromTimeline(row);
        }
        finally { RegisterTachiePresetSourceCommand?.RaiseCanExecuteChanged(); OnPropertyChanged(nameof(Summary)); UpdateCommands(); }
    }
    private void RestoreExpressionChoiceFromTimeline(AssignmentRow row)
    {
        var previous = suppressExpressionApply;
        suppressExpressionApply = true;
        try
        {
            try
            {
                var association = ManagedExpressionReader.Read(RequireTimeline(), row.Target.Voice);
                ManagedExpressionSafety.ValidatePresetState(association.Bundle);
                row.SetSourceNotice("");
                if (association.Bundle == null)
                {
                    row.RestoreSelectedChoice(row.Choices.FirstOrDefault(x =>
                        !x.HasCandidate && x.IsAvailable &&
                        !x.IsCurrentOtherSource && !x.IsInvalidAssociation));
                    row.SetAssociationMatch(true);
                    return;
                }
                if (association.Bundle.Descriptor is
                    { Kind: ManagedExpressionSourceKind.Template, Template: { } templateDescriptor })
                {
                    if (IsTachiePresetExpressionSource)
                    {
                        row.RestoreSelectedChoice(new TemplateChoice(null, "現在：テンプレート由来の表情")
                            { IsCurrentOtherSource = true });
                        row.SetSourceNotice("現在の表情はテンプレートから配置されています。表示切替だけでは変更しません。");
                        row.SetAssociationMatch(true);
                        return;
                    }
                    TemplateChoice? match = null;
                    foreach (var choice in row.Choices.Where(x => x.Template?.IntentSource != null && x.IsAvailable))
                    {
                        var source = choice.Template!.IntentSource!;
                        if (source.Palette.Id != templateDescriptor.Palette ||
                            source.Entry.LibraryEntryId != templateDescriptor.Entry) continue;
                        try
                        {
                            source.Bundle.ValidateCurrent();
                            if (IntentAssociationTag.Hash(source.Bundle) == templateDescriptor.GeometryHash)
                            { match = choice; break; }
                        }
                        catch (InvalidOperationException) { }
                    }
                    row.RestoreSelectedChoice(match,
                        match == null ? "⚠ 現在の関連表情（選択元を確認）" : null);
                    row.SetAssociationMatch(true);
                    return;
                }
                if (association.Bundle.Descriptor is
                    { Kind: ManagedExpressionSourceKind.RegisteredTachiePreset, RegisteredTachiePreset: { } registeredDescriptor })
                {
                    if (IsTemplateExpressionSource)
                    {
                        row.RestoreSelectedChoice(new TemplateChoice(null, "現在：登録済み立ち絵プリセット由来の表情")
                            { IsCurrentOtherSource = true });
                        row.SetSourceNotice("現在の表情は登録済み立ち絵プリセットから配置されています。表示切替だけでは変更しません。");
                        row.SetAssociationMatch(true);
                        return;
                    }
                    var registeredMatch = row.Choices.FirstOrDefault(x =>
                        x.IsAvailable && x.RegisteredPreset is { } registered &&
                        registered.PaletteId == registeredDescriptor.Palette &&
                        registered.SourceId == registeredDescriptor.Source &&
                        registered.SourceSemanticHash == registeredDescriptor.SourceHash);
                    if (registeredMatch != null)
                    {
                        row.RestoreSelectedChoice(registeredMatch);
                        row.SetSourceNotice("現在の表情は登録済み立ち絵プリセットSourceからSetの配置ルールで配置されています。");
                    }
                    else
                    {
                        row.RestoreSelectedChoice(null, "⚠ 現在の登録済み立ち絵プリセット（Set/Sourceを確認）");
                        row.SetSourceNotice("現在の登録済み立ち絵プリセットSourceを候補から一意に再確認できません。");
                    }
                    row.SetAssociationMatch(true);
                    return;
                }
                var presetDescriptor = association.Bundle.Descriptor.TachiePreset!;
                if (IsTemplateExpressionSource)
                {
                    row.RestoreSelectedChoice(new TemplateChoice(null, "現在：立ち絵プリセット由来の表情")
                        { IsCurrentOtherSource = true });
                    row.SetSourceNotice("現在の表情は立ち絵プリセットから配置されています。表示切替だけでは変更しません。");
                    row.SetAssociationMatch(true);
                    return;
                }
                var presetMatch = row.Choices.FirstOrDefault(x =>
                    x.IsAvailable && x.TachiePreset is { } candidate &&
                    TachiePresetAssociationTag.CapabilityIdentity(candidate.Fingerprint) == presetDescriptor.CapabilityHash &&
                    TachiePresetAssociationTag.CandidateIdentity(candidate) == presetDescriptor.CandidateHash);
                if (presetMatch != null)
                {
                    row.RestoreSelectedChoice(presetMatch);
                    row.SetSourceNotice("現在の表情は立ち絵プリセットから配置されています。");
                }
                else
                {
                    row.RestoreSelectedChoice(null, "⚠ 現在の立ち絵プリセット（候補・立ち絵設定を確認）");
                    row.SetSourceNotice("現在の立ち絵プリセットを候補から一意に再確認できません。");
                }
                row.SetAssociationMatch(true);
            }
            catch (InvalidOperationException)
            {
                row.RestoreSelectedChoice(new TemplateChoice(null, "⚠ 関連付けを確認", null, false)
                    { IsInvalidAssociation = true });
                row.SetSourceNotice("現在の関連表情を安全に確認できません。推測して変更しません。");
                row.SetAssociationMatch(true);
            }
        }
        finally { suppressExpressionApply = previous; }
    }
    private bool ExpressionAssociationOwnsSelection(AssignmentRow row) => AssociationTag.Voice(row.Target.Voice.Remark, out _) != AssociationTagState.None;
    private bool ExpressionChoiceMatchesTimeline(AssignmentRow row)
    {
        if (row.SelectedChoice.Template?.IntentSource is not { } source) return true;
        try
        {
            var association = ManagedExpressionReader.Read(RequireTimeline(), row.Target.Voice);
            ManagedExpressionSafety.ValidatePresetState(association.Bundle);
            if (association.Bundle?.Descriptor is not
                { Kind: ManagedExpressionSourceKind.Template, Template: { } descriptor } ||
                descriptor.Palette != source.Palette.Id ||
                descriptor.Entry != source.Entry.LibraryEntryId) return false;
            source.Bundle.ValidateCurrent();
            return descriptor.GeometryHash == IntentAssociationTag.Hash(source.Bundle);
        }
        catch (InvalidOperationException) { return false; }
    }
    private bool HasPendingRelativeAssignments() => UsesRelativeExpressions && PendingRelativeExpressionCount > 0;
}
