using System.Reflection;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyTemplateFidelity(Timeline timeline, UndoRedoManager undo)
    {
        stage = "v0.4.2 template Character/effect fidelity";
        var originalItems = timeline.Items; var originalSelection = timeline.SelectedItems;
        var canonical = new Character { Name = "Fidelity Character" };
        var detached = new Character { Name = canonical.Name };
        var voice = new VoiceItem(canonical) { Frame = 120, Length = 60, Layer = 20, Serif = "fidelity" };

        var effectType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes)
            .FirstOrDefault(x => x.FullName == "YukkuriMovieMaker.Plugin.Community.Effect.Video.AfterImage.AfterImageEffect")
            ?? throw new InvalidOperationException("Pinned YMM4 host no longer exposes the built-in AfterImageEffect fidelity fixture.");
        var effect = Activator.CreateInstance(effectType) as IVideoEffect
            ?? throw new InvalidOperationException("Could not instantiate the built-in effect fidelity fixture.");
        var modeProperty = effectType.GetProperty("Mode", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Built-in effect fixture no longer exposes Mode.");
        var marker = Enum.Parse(modeProperty.PropertyType, "Back");
        modeProperty.SetValue(effect, marker);

        var sourceFace = new TachieFaceItem(detached) { Frame = 30, Length = 30, Layer = 5, Remark = "fidelity-source" };
        sourceFace.TachieFaceEffects = sourceFace.TachieFaceEffects.Add(effect);
        var template = Template("Fidelity/Expression", [sourceFace]);
        ItemSettings.Default.Templates.Add(template);
        try
        {
            var entry = TemplateResolver.Reference(template, "Fidelity", canonical.Name);
            var tile = new IntentEntry(entry.Id);
            var target = new IntentTargetContext
            {
                ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
                CharacterName = canonical.Name
            };
            var palette = new IntentPalette(Guid.NewGuid(), "Fidelity", "表情", target, new IntentRelation(), [tile])
            {
                ExpressionCandidates = true
            };

            timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            var context = IntentSelectionContext.ForItems(timeline, [voice]);
            var geometry = IntentPlacementGeometry.Prepare(timeline, context, palette, tile, [entry], timeline.Items);
            var placedFace = geometry.Items.OfType<TachieFaceItem>().Single();
            var placedEffect = placedFace.TachieFaceEffects.Single();

            Assert(ReferenceEquals(placedFace.Character, canonical) && !ReferenceEquals(placedFace.Character, detached),
                "Template fidelity rebinds a detached same-name Face to the selected Voice Character object");
            Assert(ReferenceEquals(sourceFace.Character, detached) && !ReferenceEquals(sourceFace.Character, canonical),
                "Template fidelity never mutates the live source Template Character while rebinding the clone");
            Assert(!ReferenceEquals(sourceFace.TachieFaceEffects, placedFace.TachieFaceEffects) && !ReferenceEquals(effect, placedEffect),
                "Template fidelity keeps the cloned effect list/effect object independent after Character rebind");
            Assert(Equals(modeProperty.GetValue(effect), marker) && Equals(modeProperty.GetValue(placedEffect), marker),
                "Template fidelity preserves a non-default effect parameter value across clone and Character rebind");
            Assert(sourceFace.Frame == 30 && sourceFace.Layer == 5 && sourceFace.Length == 30 && sourceFace.Remark == "fidelity-source",
                "Template fidelity leaves source geometry and user content unchanged");
            Log("TEMPLATE_FIDELITY=PASS");
            await Idle();
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(template);
            timeline.Items = originalItems; timeline.SelectedItems = originalSelection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        }
    }

    private static Type[] SafeTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(x => x != null).Cast<Type>().ToArray(); }
    }
}
