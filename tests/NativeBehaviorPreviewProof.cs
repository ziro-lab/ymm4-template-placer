using System.Windows;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyBehaviorPreviewDescription()
    {
        stage = "BEHAVIOR_PREVIEW P0 common description projection";

        var voiceKey = IntentSelectionContext.TypeKey(typeof(VoiceItem));
        var target = new IntentTargetContext
        {
            ItemTypeKeys = [voiceKey],
            CharacterName = "Preview Character"
        };
        var relation = new IntentRelation
        {
            Anchor = IntentAnchor.SelectedCenter,
            Alignment = IntentAlignment.CenterAtAnchor,
            Duration = IntentDuration.Fixed,
            FixedDuration = 48,
            Layer = new RelativeLayerPolicy
            {
                Mode = LayerPlacementMode.Absolute,
                AbsoluteLayer = 42,
                Direction = RelativeLayerDirection.Down,
                Minimum = 0,
                Maximum = 99
            }
        };
        var palette = new IntentPalette(Guid.NewGuid(), "Preview Set", "preview", target, relation, []);
        var draft = new IntentPaletteDraft(palette, Array.Empty<LibraryEntry>(), new Dictionary<string, string>
        {
            [voiceKey] = "ボイス"
        });

        var targeted = draft.BehaviorDescription;
        Assert(targeted.TargetTypeNames.SequenceEqual(["ボイス"]) &&
            targeted.CharacterRestricted && targeted.CharacterName == "Preview Character" &&
            targeted.Anchor == IntentAnchor.SelectedCenter &&
            targeted.Alignment == IntentAlignment.CenterAtAnchor &&
            targeted.Duration == IntentDuration.Fixed &&
            targeted.FixedDurationFrames == 48 &&
            targeted.LayerMode == LayerPlacementMode.Absolute &&
            targeted.AbsoluteLayer == 42 &&
            targeted.LayerDirection == RelativeLayerDirection.Down &&
            targeted.Summary == draft.Summary &&
            targeted.Summary == "Preview Characterのボイスを選んだとき、演出の中央を選択アイテムの中央に合わせて48フレームで、レイヤー42を基準に配置します。塞がっていればさらに下へ探します。",
            "BEHAVIOR_PREVIEW P0 targeted Draft exposes one typed description and preserves the accepted text summary");

        draft.FixedDuration = "not a number";
        targeted = draft.BehaviorDescription;
        Assert(targeted.FixedDurationFrames == null && targeted.Summary.Contains("指定した長さ", StringComparison.Ordinal),
            "BEHAVIOR_PREVIEW P0 incomplete numeric Draft remains projectable without inventing a value");

        draft.Duration = IntentDuration.UntilRelated;
        draft.Neighbor = IntentNeighbor.NextSameTypeAndCharacter;
        draft.NeighborEdge = IntentNeighborEdge.Start;
        draft.Fallback = IntentFallback.DoNotPlace;
        targeted = draft.BehaviorDescription;
        Assert(targeted.Alignment == IntentAlignment.StartAtAnchor &&
            targeted.Neighbor == IntentNeighbor.NextSameTypeAndCharacter &&
            targeted.Fallback == IntentFallback.DoNotPlace &&
            targeted.Summary.Contains("次の同じ種類・同じキャラのアイテムの開始まで", StringComparison.Ordinal) &&
            targeted.Summary.EndsWith("見つからなければ配置しません。", StringComparison.Ordinal),
            "BEHAVIOR_PREVIEW P0 neighbor/fallback meaning is represented by the same projection used by Summary");

        var style = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "Generic Preview", null, [])
        {
            Layer = new LayerPolicy
            {
                UseTemplateLayer = false,
                SearchMode = LayerSearchMode.SearchDown,
                Minimum = 4,
                Maximum = 12,
                Preferred = 8
            }
        };
        var genericDraft = new GenericSetDraft(style, Array.Empty<LibraryEntry>());
        var generic = genericDraft.BehaviorDescription;
        Assert(!generic.UseTemplateLayer &&
            generic.SearchMode == LayerSearchMode.SearchDown &&
            generic.Minimum == 4 && generic.Maximum == 12 && generic.Preferred == 8 &&
            generic.Summary == genericDraft.Summary &&
            generic.Summary == "現在の再生位置から、テンプレートの長さでレイヤー8へ配置します。塞がっていれば下（大きい番号）の空きを探します。範囲は4〜12です。",
            "BEHAVIOR_PREVIEW P0 Generic Draft uses the same typed description/text projection");

        genericDraft.Minimum = "bad";
        generic = genericDraft.BehaviorDescription;
        Assert(generic.Minimum == null && generic.Summary.Contains("範囲はbad〜12", StringComparison.Ordinal),
            "BEHAVIOR_PREVIEW P0 Generic incomplete numeric Draft stays visible instead of being silently normalized");

        Log("BEHAVIOR_PREVIEW_P0=PASS");

        var preview = new PlacementBehaviorPreview { DataContext = targeted, Width = 340 };
        preview.Measure(new Size(340, double.PositiveInfinity));
        preview.Arrange(new Rect(0, 0, 340, preview.DesiredSize.Height));
        preview.UpdateLayout();
        Assert(!preview.IsHitTestVisible &&
            preview.ContextText.Text == targeted.ContextLabel &&
            preview.AnchorText.Text == targeted.AnchorLabel &&
            preview.SpanText.Text == targeted.SpanLabel &&
            preview.AlignmentText.Text == targeted.AlignmentLabel &&
            preview.LayerText.Text == targeted.LayerLabel &&
            preview.FallbackText.Text == targeted.FallbackLabel &&
            preview.FallbackText.Visibility == Visibility.Visible,
            "BEHAVIOR_PREVIEW P1 targeted schematic renders only the shared description and remains read-only");

        preview.DataContext = generic;
        preview.Measure(new Size(340, double.PositiveInfinity));
        preview.Arrange(new Rect(0, 0, 340, preview.DesiredSize.Height));
        preview.UpdateLayout();
        Assert(preview.ContextText.Text == generic.ContextLabel &&
            preview.AnchorText.Text == generic.AnchorLabel &&
            preview.SpanText.Text == generic.SpanLabel &&
            preview.AlignmentText.Text == generic.AlignmentLabel &&
            preview.LayerText.Text == generic.LayerLabel &&
            preview.FallbackText.Text == "" &&
            preview.FallbackText.Visibility == Visibility.Collapsed,
            "BEHAVIOR_PREVIEW P1 Generic schematic reuses the same bounded visual surface without inventing fallback");

        Log("BEHAVIOR_PREVIEW_P1=PASS");
    }
}
