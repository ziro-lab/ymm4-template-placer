using System.ComponentModel;
using System.Text.Json;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal sealed record ManagedIntentExpressionBundle(long Serial, IntentAssociationTag Descriptor, IReadOnlyList<IItem> Members);
internal sealed record ManagedIntentExpressionAssociation(long? Serial, ManagedIntentExpressionBundle? Bundle);

internal static class ManagedIntentExpressionReader
{
    public static ManagedIntentExpressionAssociation Read(Timeline timeline, VoiceItem voice)
    {
        if (!timeline.Items.Contains(voice)) throw new InvalidOperationException("対象音声が現在のシーンにありません。メンテナンスから一覧を読み直してください。");
        var voiceState = AssociationTag.Voice(voice.Remark, out var serial);
        if (voiceState == AssociationTagState.Invalid) throw new InvalidOperationException("対象音声の関連付けタグが不正または重複しています。推測して変更しません。");
        if (voiceState == AssociationTagState.None) return new(null, null);
        var targets = timeline.Items.OfType<VoiceItem>().Where(x => x.CharacterName == voice.CharacterName &&
            AssociationTag.Voice(x.Remark, out var id) == AssociationTagState.Valid && id == serial).Take(2).ToArray();
        if (targets.Length != 1 || !ReferenceEquals(targets[0], voice))
            throw new InvalidOperationException("関連付けIDとキャラクター名が一致する音声を一意に特定できません。推測して変更しません。");
        var related = new List<IItem>();
        foreach (var item in timeline.Items)
            if (AssociationTag.Source(item.Remark, out var source) == AssociationTagState.Valid && source!.Serial == serial) related.Add(item);
        if (related.Count == 0) return new(serial, null);
        var tagged = related.Select(item => (Item: item, State: IntentAssociationTag.Read(item.Remark, out var tag), Tag: tag)).ToArray();
        if (tagged.Any(x => x.State != AssociationTagState.Valid))
            throw new InvalidOperationException("対象音声のPlugin-managed Bundleに欠落した関連付け情報があります。推測して変更しません。");
        var ordered = tagged.OrderBy(x => x.Tag!.Index).ToArray();
        var descriptor = ordered[0].Tag!;
        if (ordered.Length != descriptor.Count || ordered.Any(x => !descriptor.SameGroup(x.Tag!)) ||
            !ordered.Select(x => x.Tag!.Index).SequenceEqual(Enumerable.Range(0, descriptor.Count)))
            throw new InvalidOperationException("対象音声のPlugin-managed Bundleに欠落・重複・コピーまたは設定不一致があります。");
        if (ordered.Any(x => x.Item.Group != 0))
            throw new InvalidOperationException("対象のPlugin-managed BundleはYMM4側でグループ化されています。解除してから表情を変更してください。");
        if (ordered.Any(x => !(x.Item.Remark ?? "").Split('\n').Any(line => line.TrimEnd('\r') == PlacementEngine.Marker)))
            throw new InvalidOperationException("対象音声の関連アイテムをTemplate Placer生成物として確認できません。推測して変更しません。");
        var sameGroup = timeline.Items.Where(item => IntentAssociationTag.Read(item.Remark, out var tag) == AssociationTagState.Valid &&
            tag!.Group == descriptor.Group).ToArray();
        if (sameGroup.Length != ordered.Length || sameGroup.Any(x => ordered.All(y => !ReferenceEquals(y.Item, x))))
            throw new InvalidOperationException("同じBundle IDを持つコピーまたは別アイテムがあります。推測して変更しません。");
        return new(serial, new(serial, descriptor, ordered.Select(x => x.Item).ToArray()));
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
    private readonly List<(IntentGeometry Geometry, Guid PaletteId, string Palette, Guid LibraryId, TemplateLocator Source)> guarded;
    public PlacementPlan Plan { get; }
    public bool Skipped { get; }
    private IntentExpressionMutation(AssignmentRow row, PlacementPlan plan, bool skipped,
        List<(IntentGeometry Geometry, Guid PaletteId, string Palette, Guid LibraryId, TemplateLocator Source)> guarded)
    { this.row = row; Plan = plan; Skipped = skipped; this.guarded = guarded; }
    public static IntentExpressionMutation Create(Timeline timeline, AssignmentRow row, TemplateChoice choice, PlacerSettings settings,
        IntentAssociationSerialAllocator allocator, bool allowSkip)
    {
        ValidateRow(timeline, row);
        var association = ManagedIntentExpressionReader.Read(timeline, row.Target.Voice);
        var removals = association.Bundle?.Members.ToArray() ?? [];
        if (choice.Template == null) return new(row, PlacementPlan.Create(timeline, [], removals: removals), false, []);
        if (!choice.IsAvailable || choice.Template.IntentSource is not { } reference)
            throw new InvalidOperationException("表情の選択元が現在のパレットと一致しません。候補を更新して選び直してください。");
        var current = reference.ResolveCurrent(settings);
        var voice = row.Target.Voice;
        if (current.Bundle.CharacterName != voice.CharacterName) throw new InvalidOperationException("表情テンプレートと対象音声のキャラクター名が一致しません。");
        var currentHash = IntentAssociationTag.Hash(current.Bundle);
        if (association.Bundle is { } existing && existing.Descriptor.Palette == current.Palette.Id &&
            existing.Descriptor.Entry == current.Entry.LibraryEntryId && existing.Descriptor.GeometryHash == currentHash)
            return new(row, PlacementPlan.Create(timeline, []), false, []);
        var own = removals.ToHashSet();
        var context = IntentSelectionContext.ForItems(timeline, [voice]);
        var geometry = IntentPlacementGeometry.Prepare(timeline, context, current.Palette, current.Entry, settings.Library,
            timeline.Items.Where(x => !own.Contains(x)));
        if (geometry.Skipped)
        {
            if (allowSkip) return new(row, PlacementPlan.Create(timeline, []), true, []);
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
        return new(row, PlacementPlan.Create(timeline, geometry.Items, updates, removals), false, guards);
    }
    public void ValidateCurrent(Timeline timeline, PlacerSettings settings)
    {
        ValidateRow(timeline, row);
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
        NavigateExpressionRowCommand = new ActionCommand(x => x is AssignmentRow row && Rows.Contains(row) && timeline != null &&
            timeline.Items.Contains(row.Target.Voice), x => Guard(() => NavigateExpressionRow((AssignmentRow)x!)));
        OnPropertyChanged(nameof(NavigateExpressionRowCommand));
    }
    private void NavigateExpressionRow(AssignmentRow row) => QueueExpressionNavigation(row);
    internal void SetExpressionRowContext(AssignmentRow? row)
    {
        if (expressionTrialSession.IsOpen && !ReferenceEquals(expressionTrialSession.Voice, row?.Target.Voice)) CloseExpressionTrialSession();
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
        Guid paletteId, libraryId;
        if (choice.Template?.IntentSource is { } source)
        {
            paletteId = source.Palette.Id; libraryId = source.Entry.LibraryEntryId;
        }
        else
        {
            var association = ManagedIntentExpressionReader.Read(current, row.Target.Voice);
            if (association.Bundle is not { } bundle) return;
            paletteId = bundle.Descriptor.Palette; libraryId = bundle.Descriptor.Entry;
        }
        if (draft.HasExpressionDependencyChanges(paletteId, libraryId, settings, out var setName))
            throw new InvalidOperationException($"この表情Set「{setName}」に未保存の変更があります。保存または破棄してから表情を変更してください。");
    }
    private void ApplyImmediateExpressionChoice(AssignmentRow row)
    {
        if (suppressExpressionApply || !UsesRelativeExpressions) return;
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
                var changed = expressionTrialSession.ExecuteOwned(() => mutation.CommitWithinOpenRecord(current, settings));
                HasError = false; Status = row.SelectedChoice.Template == null ? "この音声の関連表情を外しました。" : $"「{row.SelectedChoice.DisplayName}」を即時反映しました（{changed}変更）。";
            }
            RestoreExpressionChoiceFromTimeline(row);
        }
        catch (Exception ex)
        {
            HasError = true; Status = "表情を変更できませんでした: " + ex.GetBaseException().Message; RestoreExpressionChoiceFromTimeline(row);
        }
        finally { OnPropertyChanged(nameof(Summary)); UpdateCommands(); }
    }
    private void RestoreExpressionChoiceFromTimeline(AssignmentRow row)
    {
        var previous = suppressExpressionApply; suppressExpressionApply = true;
        try
        {
            try
            {
                var association = ManagedIntentExpressionReader.Read(RequireTimeline(), row.Target.Voice);
                if (association.Bundle == null) { row.RestoreSelectedChoice(row.Choices.FirstOrDefault(x => x.Template == null)); return; }
                var descriptor = association.Bundle.Descriptor; TemplateChoice? match = null;
                foreach (var choice in row.Choices.Where(x => x.Template?.IntentSource != null && x.IsAvailable))
                {
                    var source = choice.Template!.IntentSource!;
                    if (source.Palette.Id != descriptor.Palette || source.Entry.LibraryEntryId != descriptor.Entry) continue;
                    try { source.Bundle.ValidateCurrent(); if (IntentAssociationTag.Hash(source.Bundle) == descriptor.GeometryHash) { match = choice; break; } }
                    catch (InvalidOperationException) { }
                }
                row.RestoreSelectedChoice(match, match == null ? "⚠ 現在の関連表情（選択元を確認）" : null);
            }
            catch (InvalidOperationException) { row.RestoreSelectedChoice(null, "⚠ 関連付けを確認"); }
        }
        finally { suppressExpressionApply = previous; }
    }
    private bool ExpressionAssociationOwnsSelection(AssignmentRow row) => AssociationTag.Voice(row.Target.Voice.Remark, out _) != AssociationTagState.None;
    private bool ExpressionChoiceMatchesTimeline(AssignmentRow row)
    {
        if (row.SelectedChoice.Template?.IntentSource is not { } source) return true;
        try
        {
            var association = ManagedIntentExpressionReader.Read(RequireTimeline(), row.Target.Voice); var descriptor = association.Bundle?.Descriptor;
            if (descriptor == null || descriptor.Palette != source.Palette.Id || descriptor.Entry != source.Entry.LibraryEntryId) return false;
            source.Bundle.ValidateCurrent(); return descriptor.GeometryHash == IntentAssociationTag.Hash(source.Bundle);
        }
        catch (InvalidOperationException) { return false; }
    }
    private bool HasPendingRelativeAssignments() => UsesRelativeExpressions && Rows.Any(row =>
        row.SelectedChoice.Template != null && row.SelectedChoice.IsAvailable && !ExpressionChoiceMatchesTimeline(row));
}
