using System.Reflection;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal sealed record TachiePresetCalibrationMatch(
    TachiePresetRouteDescriptor Route,
    string CandidateIdentity);

internal static class TachiePresetCalibrationEngine
{
    public static async Task<TachiePresetCalibrationMatch> MatchAsync(
        TachiePresetProbeTarget target,
        string demonstratedStateHash,
        CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        if (string.IsNullOrWhiteSpace(demonstratedStateHash))
            throw new InvalidOperationException("変更後の表情状態を確認できません。");

        void EnsureCurrent()
        {
            token.ThrowIfCancellationRequested();
            if (!target.IsCurrent(token))
                throw new OperationCanceledException("認識中に立ち絵設定が変わりました。", token);
        }

        var sample = target.CreateFreshFace(token);
        var matches = new List<TachiePresetCalibrationMatch>();
        var diagnostics = new TachiePresetCapabilityDiagnostics();

        foreach (var route in TachiePresetEditorSession.FindCalibrationRoutes(sample))
        {
            EnsureCurrent();
            string[] names;
            try
            {
                using var editor = TachiePresetEditorSession.Open(
                    route, target.Configuration, target.CreateFreshFace(token), diagnostics);
                await editor.SettleAsync(EnsureCurrent, token);
                names = editor.Choices(token).Select(x => x.Label).Distinct(StringComparer.Ordinal).ToArray();
            }
            catch (OperationCanceledException) { throw; }
            catch { continue; }

            foreach (var name in names)
            {
                EnsureCurrent();
                try
                {
                    var candidate = new TachiePresetCandidateDescriptor(
                        target.Fingerprint, route.Descriptor, name, name, TachiePresetCapabilityLevel.Strong);
                    var applied = await TachiePresetCandidateApplier.ApplyAsync(target, candidate, EnsureCurrent, token);
                    if (applied.StateHash == demonstratedStateHash)
                        matches.Add(new(route.Descriptor, name));
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }
        }

        // Direct named state is a fallback. Its candidate list still comes from public Character settings.
        if (sample.GetType().GetProperty("Preset", BindingFlags.Instance | BindingFlags.Public) is { } direct &&
            direct.PropertyType == typeof(string) && direct.GetMethod?.IsPublic == true &&
            direct.SetMethod?.IsPublic == true && direct.GetIndexParameters().Length == 0)
        {
            var descriptor = new TachiePresetRouteDescriptor(
                TachiePresetRouteKind.DirectNamedProperty,
                direct.DeclaringType?.FullName + "." + direct.Name);
            foreach (var name in TachiePresetDiscovery.DirectNames(target.Configuration, token))
            {
                EnsureCurrent();
                try
                {
                    var candidate = new TachiePresetCandidateDescriptor(
                        target.Fingerprint, descriptor, name, name, TachiePresetCapabilityLevel.Strong);
                    var applied = await TachiePresetCandidateApplier.ApplyAsync(target, candidate, EnsureCurrent, token);
                    if (applied.StateHash == demonstratedStateHash)
                        matches.Add(new(descriptor, name));
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }
        }

        EnsureCurrent();
        var exact = matches.Distinct().ToArray();
        if (exact.Length == 0)
            throw new InvalidOperationException("変更後の状態を再現できる表情プリセット経路を見つけられませんでした。別のプリセットで試すか、テンプレートを利用してください。");
        if (exact.Length != 1)
            throw new InvalidOperationException("変更後の状態を複数の編集経路で再現できるため、一意に認識できませんでした。別のプリセットで試してください。");
        return exact[0];
    }
}

internal sealed record TachiePresetCalibrationSession(
    Timeline Timeline,
    VoiceItem Voice,
    TachiePresetProbeTarget Target,
    TachieFaceItem Item,
    string Marker,
    string BeforeStateHash);

public sealed partial class PlacerViewModel
{
    private const string TachiePresetCalibrationMarkerPrefix = "CWT_TPL:CAL=";
    private TachiePresetCalibrationSession? tachiePresetCalibration;
    private CancellationTokenSource? tachiePresetCalibrationCancellation;
    private long tachiePresetCalibrationGeneration;

    public ActionCommand TachiePresetCalibrationCommand { get; private set; } = null!;
    public string TachiePresetCalibrationActionText =>
        tachiePresetCalibration == null ? "表情プリセットを認識させる" : "変更を読み取る";
    public string TachiePresetCalibrationNotice => tachiePresetCalibration == null
        ? "自動で候補が出ない場合だけ使います。1回の操作から、この立ち絵Pluginの表情プリセット経路を認識します。"
        : "確認用の表情アイテムで、表情プリセットだけを別のものへ変更してから［変更を読み取る］を押してください。";
    public bool IsTachiePresetCalibrationActive => tachiePresetCalibration != null;
    internal Task TachiePresetCalibrationCompletion { get; private set; } = Task.CompletedTask;

#if YMM4_PROOF
    internal TachieFaceItem? TachiePresetCalibrationItemForProof => tachiePresetCalibration?.Item;
#endif

    private void InitializeTachiePresetCalibration()
    {
        TachiePresetCalibrationCommand = new ActionCommand(
            x => IsTachiePresetExpressionSource && ExpressionRowsMatchSource && !IsExpressionLoading &&
                timeline != null && undo != null && settingsAvailable &&
                (tachiePresetCalibration != null ||
                 x is AssignmentRow row && Rows.Contains(row) && row.Target.Voice.Character != null),
            x =>
            {
                if (tachiePresetCalibration == null)
                    Guard(() => BeginTachiePresetCalibration((AssignmentRow)x!));
                else
                    RequestCompleteTachiePresetCalibration();
            });
        OnPropertyChanged(nameof(TachiePresetCalibrationCommand));
    }

    private void PublishTachiePresetCalibrationState()
    {
        OnPropertyChanged(nameof(TachiePresetCalibrationActionText));
        OnPropertyChanged(nameof(TachiePresetCalibrationNotice));
        OnPropertyChanged(nameof(IsTachiePresetCalibrationActive));
        TachiePresetCalibrationCommand?.RaiseCanExecuteChanged();
    }

    private void BeginTachiePresetCalibration(AssignmentRow row)
    {
        if (!IsTachiePresetExpressionSource || !ExpressionRowsMatchSource || IsExpressionLoading ||
            !Rows.Contains(row) || timeline == null || undo == null)
            throw new InvalidOperationException("現在の表情一覧から認識対象を選び直してください。");

        var character = row.Target.Voice.Character
            ?? throw new InvalidOperationException("対象音声のキャラクターを取得できません。");
        CancelTachiePresetApply();
        CloseExpressionTrialSession();
        CancelTachiePresetCalibration();

        var target = PresetTargetResolver(character);
        var item = target.CreateFreshItem(CancellationToken.None);
        var face = item.TachieFaceParameter
            ?? throw new InvalidOperationException("確認用の表情アイテムを作成できません。");
        var before = TachiePresetPublicState.TryHash(face)
            ?? throw new InvalidOperationException("この立ち絵Pluginでは確認用の表情状態を安全に読み取れません。テンプレートを利用してください。");

        PlacementEngine.ValidateSnapshot(timeline, Rows.Select(x => x.Target).ToArray());
        var preset = RequireExpressionPreset();
        var orderedVoices = Rows.OrderBy(x => x.Frame).ThenBy(x => x.Target.Layer).ThenBy(x => x.No)
            .Select(x => x.Target).ToArray();
        var span = CharacterExpressionProfile.Span(row.Target, orderedVoices, preset);
        item.Frame = span.Frame;
        item.Length = span.Length;
        item.Layer = LayerPlanner.Find(
            item.Frame, item.Length, item.Layer, preset.Layer, CharacterLayerMode.Base,
            character, timeline.Items);

        var marker = TachiePresetCalibrationMarkerPrefix + Guid.NewGuid().ToString("N");
        item.Remark = PluginRemarks.Append(item.Remark, marker);
        PlacementPlan.Create(timeline, [item]).Commit(timeline, undo);

        tachiePresetCalibration = new(timeline, row.Target.Voice, target, item, marker, before);
        HasError = false;
        Status = "確認用の表情アイテムを置きました。そのアイテムの表情プリセットだけを別のものへ変更し、［変更を読み取る］を押してください。";
        PublishTachiePresetCalibrationState();
        QueueExpressionNavigation(row, refreshCurrentContent: true);
    }

    private void RequestCompleteTachiePresetCalibration()
    {
        var session = tachiePresetCalibration;
        if (session == null) return;
        tachiePresetCalibrationGeneration++;
        tachiePresetCalibrationCancellation?.Cancel();
        tachiePresetCalibrationCancellation?.Dispose();
        var own = new CancellationTokenSource();
        tachiePresetCalibrationCancellation = own;
        var request = tachiePresetCalibrationGeneration;
        TachiePresetCalibrationCompletion = CompleteTachiePresetCalibrationAsync(session, request, own);
    }

    private async Task CompleteTachiePresetCalibrationAsync(
        TachiePresetCalibrationSession session,
        long request,
        CancellationTokenSource own)
    {
        bool Current() =>
            !own.IsCancellationRequested &&
            request == tachiePresetCalibrationGeneration &&
            ReferenceEquals(tachiePresetCalibrationCancellation, own) &&
            ReferenceEquals(tachiePresetCalibration, session);
        try
        {
            var token = own.Token;
            if (!ReferenceEquals(timeline, session.Timeline) ||
                !session.Timeline.Items.Contains(session.Item) ||
                !(session.Item.Remark ?? "").Split('\n').Select(x => x.TrimEnd('\r')).Contains(session.Marker, StringComparer.Ordinal))
            {
                if (Current())
                {
                    CancelTachiePresetCalibration();
                    HasError = false;
                    Status = "確認用の表情アイテムが見つからないため、表情プリセットの認識を終了しました。";
                }
                return;
            }

            var face = session.Item.TachieFaceParameter
                ?? throw new InvalidOperationException("確認用アイテムの表情状態を読み取れません。");
            var demonstrated = TachiePresetPublicState.TryHash(face, token)
                ?? throw new InvalidOperationException("変更後の表情状態を安全に読み取れません。");
            if (demonstrated == session.BeforeStateHash)
                throw new InvalidOperationException("表情プリセットの変更を確認できません。確認用アイテムで別の表情プリセットを選んでください。");

            var match = await TachiePresetCalibrationEngine.MatchAsync(session.Target, demonstrated, token);
            if (!Current()) return;
            var still = session.Item.TachieFaceParameter;
            if (still == null || TachiePresetPublicState.TryHash(still, token) != demonstrated)
                throw new InvalidOperationException("認識中に確認用アイテムの表情が変わりました。もう一度読み取ってください。");

            EditSettings(next => TachiePresetLearnedAdapterSettings.Upsert(next, session.Target, match.Route));
            if (!Current()) return;
            tachiePresetCalibration = null;
            InvalidatePresetCapabilityCache();
            expressionCacheDirty = true;
            if (activeTask == "expression") RequestExpressionLoad(true, true);
            HasError = false;
            Status = $"この立ち絵Pluginの表情プリセット経路を認識しました（確認候補: {match.CandidateIdentity}）。候補一覧を読み直しています。確認用アイテムは不要なら削除できます。";
            PublishTachiePresetCalibrationState();
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception ex)
        {
            if (!Current()) return;
            HasError = true;
            Status = "表情プリセットを認識できませんでした: " + ex.GetBaseException().Message;
        }
        finally
        {
            if (ReferenceEquals(tachiePresetCalibrationCancellation, own))
                tachiePresetCalibrationCancellation = null;
            own.Dispose();
            PublishTachiePresetCalibrationState();
        }
    }

    private void CancelTachiePresetCalibration()
    {
        tachiePresetCalibrationGeneration++;
        tachiePresetCalibrationCancellation?.Cancel();
        tachiePresetCalibrationCancellation?.Dispose();
        tachiePresetCalibrationCancellation = null;
        tachiePresetCalibration = null;
        PublishTachiePresetCalibrationState();
    }
}
