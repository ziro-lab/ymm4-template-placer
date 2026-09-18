using System.Reflection;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    // Keep this proof native-relevant: release packaging must never promote a fidelity-gate edit on a docs-only CI path.
    private static async Task VerifyTemplateFidelity(Timeline timeline, UndoRedoManager undo)
    {
        stage = "v0.4.2 template Character/effect fidelity";
        var vm = ViewModel!; var view = View!;
        var settingsField = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var settingsStore = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var originalSettings = (PlacerSettings)settingsField.GetValue(vm)!;
        var originalItems = timeline.Items; var originalSelection = timeline.SelectedItems; var originalMode = vm.UseLegacyWorkspace;
        var canonical = new Character { Name = "Fidelity Character" };
        var detached = new Character { Name = canonical.Name };
        var voice = new VoiceItem(canonical) { Frame = 120, Length = 60, Layer = 20, Serif = "fidelity" };

        var effectType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(FidelityTypes)
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
            var fixture = PlacerSettingsStore.Copy(originalSettings);
            fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true; fixture.LegacyWorkspace = false;
            fixture.IntentPalettes = [palette]; fixture.Library = [entry];

            settingsField.SetValue(vm, fixture);
            timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.Refresh(); view.ExpressionTab.IsSelected = true; await Idle();

            var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            var choice = row.Choices.Single(x => x.Template?.Name == template.Name);
            Assert(!ReferenceEquals(choice.Template!.Face.Character, canonical) && ReferenceEquals(choice.Template.Face.Character, detached),
                "Template fidelity fixture reaches the expression dropdown with a detached same-name source Character");
            var baseline = Signature(timeline);
            await SelectInDropdown(view, row, template.Name); await Idle();
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            Assert(row.SelectedChoice.Template?.Name == template.Name && !vm.PlaceCommand.CanExecute(null) && !vm.HasError,
                "Template fidelity actual expression ComboBox selection immediately applies the managed expression: " + vm.Status);

            var placedFace = timeline.Items.OfType<TachieFaceItem>().Single();
            var placedEffect = placedFace.TachieFaceEffects.Single();
            Assert(ReferenceEquals(placedFace.Character, canonical) && !ReferenceEquals(placedFace.Character, detached),
                "Template fidelity expression-list placement rebinds a detached same-name Face to the target Voice Character object");
            Assert(ReferenceEquals(sourceFace.Character, detached) && !ReferenceEquals(sourceFace.Character, canonical),
                "Template fidelity never mutates the live source Template Character while rebinding the clone");
            Assert(!ReferenceEquals(sourceFace.TachieFaceEffects, placedFace.TachieFaceEffects) && !ReferenceEquals(effect, placedEffect),
                "Template fidelity keeps the cloned effect list/effect object independent after Character rebind");
            Assert(Equals(modeProperty.GetValue(effect), marker) && Equals(modeProperty.GetValue(placedEffect), marker),
                "Template fidelity preserves a non-default effect parameter value across clone, Character rebind and actual expression placement");
            Assert(sourceFace.Frame == 30 && sourceFace.Layer == 5 && sourceFace.Length == 30 && sourceFace.Remark == "fidelity-source",
                "Template fidelity leaves source geometry and user content unchanged");
            vm.CloseExpressionTrialSession();
            await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == baseline, "Template fidelity immediate expression trial remains one native Undo");
            Log("TEMPLATE_FIDELITY=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(template);
            settingsStore.Save(originalSettings); settingsField.SetValue(vm, originalSettings);
            timeline.Items = originalItems; timeline.SelectedItems = originalSelection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(originalMode); vm.Refresh(); vm.ResetIntentSettings();
        }
    }

    private static Type[] FidelityTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(x => x != null).Cast<Type>().ToArray(); }
    }
}
