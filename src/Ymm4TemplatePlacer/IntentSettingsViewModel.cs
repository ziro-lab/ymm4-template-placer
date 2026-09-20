using System.Text.Json;
using System.Windows;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    private IntentSettingsSession? intentSettings;
    public IntentSettingsSession? IntentSettings { get => intentSettings; private set { intentSettings = value; OnPropertyChanged(); UpdateIntentSettingsCommands(); } }
    public ActionCommand SelectSettingsContextCommand { get; private set; } = null!;
    public ActionCommand SaveIntentSettingsCommand { get; private set; } = null!;
    public ActionCommand DiscardIntentSettingsCommand { get; private set; } = null!;
    public ActionCommand CreateIntentPaletteCommand { get; private set; } = null!;
    public ActionCommand DuplicateIntentPaletteCommand { get; private set; } = null!;
    public ActionCommand DeleteIntentPaletteCommand { get; private set; } = null!;
    public ActionCommand MoveIntentPaletteCommand { get; private set; } = null!;
    public ActionCommand MoveIntentEntryCommand { get; private set; } = null!;
    public ActionCommand RemoveIntentEntryCommand { get; private set; } = null!;
    public ActionCommand AddIntentSourcesCommand { get; private set; } = null!;
    public ActionCommand RescanIntentExpressionsCommand { get; private set; } = null!;
    public void BeginIntentSettings()
    {
        CloseExpressionTrialSession();
        if (SaveIntentSettingsCommand == null)
        {
            SelectSettingsContextCommand = new ActionCommand(x => x is IntentSettingsItemContext, x =>
                { if (x is IntentSettingsItemContext context && IntentSettings != null) IntentSettings.SelectedItemContext = context; });
            OnPropertyChanged(nameof(SelectSettingsContextCommand));
            SaveIntentSettingsCommand = new ActionCommand(_ => settingsAvailable && IntentSettings?.HasChanges == true, _ => Guard(SaveIntentSettings));
            DiscardIntentSettingsCommand = new ActionCommand(_ => IntentSettings != null, _ => ResetIntentSettings());
            CreateIntentPaletteCommand = new ActionCommand(_ => settingsAvailable && IntentSettings?.CanCreateForContext == true, _ => Guard(() => IntentSettings!.CreateForContext()));
            DuplicateIntentPaletteCommand = new ActionCommand(_ => IntentSettings?.HasSelectedSet == true, _ => Guard(() => IntentSettings!.Duplicate()));
            DeleteIntentPaletteCommand = new ActionCommand(_ => IntentSettings?.HasSelectedSet == true, _ => Guard(() =>
            {
                var session = IntentSettings!;
                if (MessageBox.Show($"「{session.SelectedSetName}」と、そのセット内の演出{session.SelectedSetEntryCount}件を外します。\n元テンプレート・登録・タイムラインは削除しません。", Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    session.RemoveSelected();
            }));
            MoveIntentPaletteCommand = new ActionCommand(_ => IntentSettings?.HasSelectedSet == true, x => Guard(() => IntentSettings!.MovePalette(Convert.ToInt32(x, System.Globalization.CultureInfo.InvariantCulture))));
            MoveIntentEntryCommand = new ActionCommand(_ => IntentSettings?.HasSelectedSetEntry == true, x => Guard(() => IntentSettings!.MoveEntry(Convert.ToInt32(x, System.Globalization.CultureInfo.InvariantCulture))));
            RemoveIntentEntryCommand = new ActionCommand(_ => IntentSettings?.HasSelectedSetEntry == true, _ => Guard(() =>
            {
                IntentSettings!.RemoveSelectedSetEntry();
            }));
            AddIntentSourcesCommand = new ActionCommand(_ => IntentSettings?.HasSelectedSet == true, _ => Guard(() =>
            {
                var count = IntentSettings!.AddSelectedSources(); HasError = false; Status = $"{count}件を追加しました。設定を確認し、自動で反映します。";
            }));
            RescanIntentExpressionsCommand = new ActionCommand(_ => IntentSettings != null, _ => Guard(() =>
            {
                var count = IntentSettings!.ImportNewExpressions(); HasError = false; Status = $"新しい表情{count}件を取り込みました。設定を確認し、自動で反映します。";
            }));
            foreach (var property in new[] { nameof(SaveIntentSettingsCommand), nameof(DiscardIntentSettingsCommand),
                nameof(CreateIntentPaletteCommand), nameof(DuplicateIntentPaletteCommand), nameof(DeleteIntentPaletteCommand),
                nameof(MoveIntentPaletteCommand), nameof(MoveIntentEntryCommand), nameof(RemoveIntentEntryCommand),
                nameof(AddIntentSourcesCommand), nameof(RescanIntentExpressionsCommand) }) OnPropertyChanged(property);
        }
        if (IntentSettings == null || (settingsSessionClosed && !IntentSettings.HasChanges)) ResetIntentSettings();
        else IntentSettings.UpdateSelectionContext(timeline?.SelectedItems.ToArray() ?? []);
    }
    private void IntentSettingsEdited(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, IntentSettings)) return;
        UpdateIntentSettingsCommands(); RequestSettingsAutoCommit();
    }
    public void ResetIntentSettings()
    {
        if (intentSettings != null) intentSettings.Edited -= IntentSettingsEdited;
        var types = (timeline?.Items.Select(x => x.GetType()) ?? []).Concat(ItemSettings.Default.Templates.SelectMany(x => x.Items).Select(x => x.GetType()));
        IntentSettings = new(settings, types, timeline?.SelectedItems.ToArray() ?? []); IntentSettings.Edited += IntentSettingsEdited;
        ResetSettingsTransaction();
    }
    public void SaveIntentSettings()
    {
        if (!settingsAvailable || IntentSettings == null) throw new InvalidOperationException("設定を保存できません。");
        var next = IntentSettings.Build();
        settings = RequireSettingsTransaction().Commit(settings, next);
        IntentSettings.AcceptCommitted(settings);
        SettingsCommitCompleted();
        RefreshV04(); RefreshIntentWorkspace(); RefreshExpressionVocabulary();
        UpdateIntentSettingsCommands();
        HasError = false; Status = "パレット設定を保存しました。タイムラインと元テンプレートは変更していません。";
    }
    private void UpdateIntentSettingsCommands()
    {
        rollbackIntentSettingsCommand?.RaiseCanExecuteChanged();
        setSettingsShapeCommand?.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanEditExpressionRowHeight)); OnPropertyChanged(nameof(ExpressionRowHeight));
        ReorderIntentTileCommand?.RaiseCanExecuteChanged(); UpdateIntentTileEditingCommands();
        SaveIntentSettingsCommand?.RaiseCanExecuteChanged(); DiscardIntentSettingsCommand?.RaiseCanExecuteChanged();
        CreateIntentPaletteCommand?.RaiseCanExecuteChanged(); DuplicateIntentPaletteCommand?.RaiseCanExecuteChanged(); DeleteIntentPaletteCommand?.RaiseCanExecuteChanged();
        MoveIntentPaletteCommand?.RaiseCanExecuteChanged(); MoveIntentEntryCommand?.RaiseCanExecuteChanged(); RemoveIntentEntryCommand?.RaiseCanExecuteChanged();
        AddIntentSourcesCommand?.RaiseCanExecuteChanged(); RescanIntentExpressionsCommand?.RaiseCanExecuteChanged();
    }
    public IReadOnlyList<IntentOption<IntentTileShape>> IntentTileShapes { get; } = Enum.GetValues<IntentTileShape>().Select(x => new IntentOption<IntentTileShape>(x, IntentTileAppearance.ShapeName(x))).ToArray();
    public IReadOnlyList<IntentOption<IntentTileColor>> IntentTileColors { get; } = Enum.GetValues<IntentTileColor>().Select(x => new IntentOption<IntentTileColor>(x, IntentTileAppearance.ColorName(x))).ToArray();
    public IReadOnlyList<IntentOption<IntentTypeMatch>> IntentTypeModes { get; } = [new(IntentTypeMatch.UniformType,"選択した種類のどれか・全件同じ種類"), new(IntentTypeMatch.ExactMixedTypes,"指定した種類の組み合わせだけ")];
    public IReadOnlyList<IntentOption<IntentAnchor>> IntentAnchors { get; } = [new(IntentAnchor.SelectedStart,"選択アイテムの開始"),new(IntentAnchor.SelectedEnd,"選択アイテムの終了"),new(IntentAnchor.SelectedCenter,"選択アイテムの中央"),new(IntentAnchor.SelectionRangeStart,"選択範囲の開始"),new(IntentAnchor.SelectionRangeEnd,"選択範囲の終了"),new(IntentAnchor.PairBoundary,"選択した2件の境界"),new(IntentAnchor.RelatedStart,"周囲アイテムの開始"),new(IntentAnchor.RelatedEnd,"周囲アイテムの終了")];
    public IReadOnlyList<IntentOption<IntentAlignment>> IntentAlignments { get; } = [new(IntentAlignment.StartAtAnchor,"ここから開始"),new(IntentAlignment.EndAtAnchor,"ここで終了")];
    public IReadOnlyList<IntentOption<IntentDuration>> IntentDurations { get; } = [new(IntentDuration.TargetSpan,"選択対象と同じ長さ"),new(IntentDuration.UntilRelated,"周囲アイテムまで"),new(IntentDuration.Fixed,"固定の長さ"),new(IntentDuration.Template,"テンプレートの長さ")];
    public IReadOnlyList<IntentOption<IntentNeighbor>> IntentNeighbors { get; } = [new(IntentNeighbor.None,"参照しない"),new(IntentNeighbor.NextSameType,"次の同種類"),new(IntentNeighbor.PreviousSameType,"前の同種類"),new(IntentNeighbor.NextSameCharacter,"次の同キャラ"),new(IntentNeighbor.PreviousSameCharacter,"前の同キャラ"),new(IntentNeighbor.NextSameTypeAndCharacter,"次の同種類・同キャラ"),new(IntentNeighbor.PreviousSameTypeAndCharacter,"前の同種類・同キャラ")];
    public IReadOnlyList<IntentOption<IntentNeighborEdge>> IntentNeighborEdges { get; } = [new(IntentNeighborEdge.Start,"開始まで"),new(IntentNeighborEdge.End,"終了まで")];
    public IReadOnlyList<IntentOption<IntentFallback>> IntentFallbacks { get; } = [new(IntentFallback.CurrentTargetEnd,"現在の対象の終了まで"),new(IntentFallback.FixedDuration,"固定の長さを使う"),new(IntentFallback.TargetSpan,"現在の対象と同じ範囲"),new(IntentFallback.DoNotPlace,"配置しない")];
    public IReadOnlyList<IntentOption<RelativeLayerDirection>> IntentDirections { get; } = [new(RelativeLayerDirection.Up,"上"),new(RelativeLayerDirection.Down,"下")];
}
