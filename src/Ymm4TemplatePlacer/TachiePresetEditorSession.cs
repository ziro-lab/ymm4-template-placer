#pragma warning disable CS0618
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

internal sealed record TachiePresetEditorRoute(PropertyInfo Property, Type AttributeType, TachiePresetRouteDescriptor Descriptor);
internal sealed record TachiePresetEditorChoice(Selector Selector, object Item, string Label);

// All fields here are temporary and UI-affine. Nothing from a session is cacheable.
internal sealed class TachiePresetEditorSession : IDisposable
{
    private readonly object attribute;
    private readonly TachiePresetCapabilityDiagnostics diagnostics;
    private readonly StackPanel staging = new();
    private FrameworkElement? control;
    private bool disposed;
    public FrameworkElement Control => control ?? throw new InvalidOperationException("一時エディタがありません。");

    private TachiePresetEditorSession(object attribute, TachiePresetCapabilityDiagnostics diagnostics)
    { this.attribute = attribute; this.diagnostics = diagnostics; }

    // Canonical Lab #62 proves these public methods on local preset attributes.
    // A concrete built-in attribute need not inherit the obsolete tachie-aware base.
    private static MethodInfo? PublicMethod(Type type, string name, params Type[] parameters) =>
        type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, parameters, null);
    private static bool HasLegacyContract(Type type)
    {
        var config = type.GetProperty("CharacterParameter", BindingFlags.Instance | BindingFlags.Public);
        var create = PublicMethod(type, "Create");
        return config?.SetMethod?.IsPublic == true && config.PropertyType == typeof(object) &&
            config.GetIndexParameters().Length == 0 && create != null &&
            typeof(FrameworkElement).IsAssignableFrom(create.ReturnType) &&
            PublicMethod(type, "SetBindings", typeof(FrameworkElement), typeof(object), typeof(object), typeof(PropertyInfo))?.ReturnType == typeof(void) &&
            PublicMethod(type, "ClearBindings", typeof(FrameworkElement))?.ReturnType == typeof(void);
    }
    private static void AssignLegacyContext(object editor, object? configuration) =>
        editor.GetType().GetProperty("CharacterParameter", BindingFlags.Instance | BindingFlags.Public)!.SetValue(editor, configuration);

    public static IReadOnlyList<TachiePresetEditorRoute> FindRoutes(object face)
    {
        TachiePresetPublicState.RequireUiThread();
        var result = new List<TachiePresetEditorRoute>();
        var properties = face.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (properties.Length > TachiePresetPublicState.MaxMembers)
            throw new InvalidOperationException("表情のプロパティ数が確認上限を超えています。");
        foreach (var property in properties.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (property.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0) continue;
            var metadata = CustomAttributeData.GetCustomAttributes(property);
            if (metadata.Count > TachiePresetPublicState.MaxMembers)
                throw new InvalidOperationException("表情の属性数が確認上限を超えています。");
            var display = metadata.Where(a => a.AttributeType == typeof(DisplayAttribute))
                .SelectMany(a => a.NamedArguments).Where(a => a.MemberName == nameof(DisplayAttribute.Name))
                .Select(a => a.TypedValue.Value as string).FirstOrDefault() ?? "";
            foreach (var data in metadata)
            {
                var type = data.AttributeType;
                if (!(HasPresetContext(property.Name) || HasPresetContext(display) || HasPresetContext(type.Name))) continue;
                var modern = typeof(PropertyEditorAttribute2).IsAssignableFrom(type) &&
                             typeof(IPropertyEditorForTachieParameterAttribute).IsAssignableFrom(type);
                if (!modern && !HasLegacyContract(type)) continue;
                var descriptor = new TachiePresetRouteDescriptor(
                    modern ? TachiePresetRouteKind.PropertyEditorModern : TachiePresetRouteKind.PropertyEditorLegacy,
                    property.DeclaringType?.FullName + "." + property.Name,
                    type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
                result.Add(new(property, type, descriptor));
                if (result.Count > 8) throw new InvalidOperationException("プリセットエディタの候補経路が多すぎます。");
            }
        }
        return result;
    }

    private static bool HasPresetContext(string value) =>
        value.Contains("Preset", StringComparison.OrdinalIgnoreCase) || value.Contains("プリセット", StringComparison.Ordinal);

    public static TachiePresetEditorSession Open(TachiePresetEditorRoute route, object configuration,
        object freshFace, TachiePresetCapabilityDiagnostics diagnostics)
    {
        TachiePresetPublicState.RequireUiThread();
        var attributes = route.Property.GetCustomAttributes(route.AttributeType, true);
        if (attributes.Length != 1) throw new InvalidOperationException("プリセットエディタの属性を一意に解決できません。");
        var session = new TachiePresetEditorSession(attributes[0], diagnostics);
        try
        {
            if (session.attribute is PropertyEditorAttribute2 modern && session.attribute is IPropertyEditorForTachieParameterAttribute aware)
            {
                aware.CharacterParameter = configuration;
                session.Attach(modern.Create());
                modern.SetBindings(session.Control, [new ItemProperty(freshFace, freshFace, route.Property, new PropertiesCache())]);
            }
            else if (session.attribute is PropertyEditorForTachieParameterAttribute legacy)
            {
                legacy.CharacterParameter = configuration;
                session.Attach(legacy.Create());
                legacy.SetBindings(session.Control, freshFace, freshFace, route.Property);
            }
            else if (HasLegacyContract(session.attribute.GetType()))
            {
                var type = session.attribute.GetType();
                AssignLegacyContext(session.attribute, configuration);
                session.Attach((FrameworkElement)PublicMethod(type, "Create")!.Invoke(session.attribute, null)!);
                PublicMethod(type, "SetBindings", typeof(FrameworkElement), typeof(object), typeof(object), typeof(PropertyInfo))!
                    .Invoke(session.attribute, [session.Control, freshFace, freshFace, route.Property]);
            }
            else throw new InvalidOperationException("対応する公開PropertyEditor契約がありません。");
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    private void Attach(FrameworkElement created)
    {
        control = created ?? throw new InvalidOperationException("プリセットエディタを作成できません。");
        diagnostics.EditorsOpened++;
        diagnostics.ActiveEditors++;
        diagnostics.PeakEditors = Math.Max(diagnostics.PeakEditors, diagnostics.ActiveEditors);
        created.IsHitTestVisible = false;
        staging.Children.Add(created);
    }

    public async Task SettleAsync(Action ensureCurrent, CancellationToken token)
    {
        ensureCurrent();
        token.ThrowIfCancellationRequested();
        Control.ApplyTemplate();
        Control.Measure(new Size(600, 400));
        Control.Arrange(new Rect(0, 0, 600, 400));
        Control.UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.Loaded);
        ensureCurrent();
        await Dispatcher.Yield(DispatcherPriority.Background);
        ensureCurrent();
    }

    public IReadOnlyList<TachiePresetEditorChoice> Choices(CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        var groups = new List<List<TachiePresetEditorChoice>>();
        foreach (var selector in Descendants(Control).OfType<Selector>())
        {
            token.ThrowIfCancellationRequested();
            if (selector.Items.Count > TachiePresetCapabilityCoordinator.MaxCandidates)
                throw new InvalidOperationException("立ち絵プリセットの候補数が128件を超えています。");
            var choices = new List<TachiePresetEditorChoice>();
            foreach (var item in selector.Items)
            {
                token.ThrowIfCancellationRequested();
                var label = ReadLabel(item);
                if (label != null) choices.Add(new(selector, item, label));
            }
            if (choices.Count > 0) groups.Add(choices);
        }
        if (groups.Count > 1) throw new InvalidOperationException("プリセット選択欄が複数あり、一意に特定できません。");
        var result = groups.SingleOrDefault() ?? [];
        if (result.GroupBy(c => c.Label, StringComparer.Ordinal).Any(g => g.Count() != 1))
            throw new InvalidOperationException("同名の立ち絵プリセット候補が複数あります。");
        return result;
    }

    private static string? ReadLabel(object? value)
    {
        if (value == null) return null;
        if (value is string text) return ValidateLabel(text);
        var type = value.GetType();
        var presetProperty = type.GetProperty("Preset", BindingFlags.Instance | BindingFlags.Public);
        object? nested = null;
        if (presetProperty?.GetMethod?.IsPublic == true && presetProperty.GetIndexParameters().Length == 0)
        {
            nested = presetProperty.GetValue(value);
            // A null preset behind a Custom/placeholder item is not a named preset.
            if (nested == null) return null;
        }
        foreach (var name in new[] { "Display", "Name", "Label", "Text", "Content" })
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0 &&
                property.GetValue(value) is string label && !string.IsNullOrWhiteSpace(label))
                return ValidateLabel(label);
        }
        if (nested?.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public) is { } nestedName &&
            nestedName.GetMethod?.IsPublic == true && nestedName.GetIndexParameters().Length == 0 &&
            nestedName.GetValue(nested) is string nestedLabel) return ValidateLabel(nestedLabel);
        return null; // Never use object.ToString(), private state or a DataContext/ViewModel fallback.
    }

    private static string? ValidateLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (text.Length > 512) throw new InvalidOperationException("立ち絵プリセット名が512文字を超えています。");
        return text;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var seen = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<DependencyObject>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!seen.Add(node)) continue;
            if (seen.Count > 256) throw new InvalidOperationException("一時エディタの構造が確認上限を超えています。");
            yield return node;
            if (node is Visual)
            {
                var count = VisualTreeHelper.GetChildrenCount(node);
                if (count > 256) throw new InvalidOperationException("一時エディタの子要素が多すぎます。");
                for (var i = 0; i < count; i++) stack.Push(VisualTreeHelper.GetChild(node, i));
            }
            var logicalCount = 0;
            foreach (var child in LogicalTreeHelper.GetChildren(node))
            {
                if (++logicalCount > 256) throw new InvalidOperationException("一時エディタの子要素が多すぎます。");
                if (child is DependencyObject dependency) stack.Push(dependency);
            }
        }
    }

    public void Dispose()
    {
        TachiePresetPublicState.RequireUiThread();
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            if (control != null)
            {
                if (attribute is PropertyEditorAttribute2 modern && attribute is IPropertyEditorForTachieParameterAttribute) modern.ClearBindings(control);
                else if (attribute is PropertyEditorForTachieParameterAttribute legacy) legacy.ClearBindings(control);
                else PublicMethod(attribute.GetType(), "ClearBindings", typeof(FrameworkElement))!.Invoke(attribute, [control]);
                diagnostics.EditorsCleared++;
            }
        }
        catch (Exception ex) { diagnostics.CleanupFailures++; failure = ex; }
        finally
        {
            if (control != null)
            {
                try
                {
                    foreach (var element in Descendants(control).OfType<FrameworkElement>())
                    { BindingOperations.ClearAllBindings(element); element.DataContext = null; }
                }
                catch (Exception ex) { failure ??= ex; }
                staging.Children.Remove(control);
                control = null;
                diagnostics.ActiveEditors--;
            }
            try
            {
                if (attribute is IPropertyEditorForTachieParameterAttribute aware) aware.CharacterParameter = null;
                else if (attribute is PropertyEditorForTachieParameterAttribute legacy) legacy.CharacterParameter = null!;
                else AssignLegacyContext(attribute, null);
            }
            catch (Exception ex) { failure ??= ex; }
        }
        if (failure != null)
            throw new InvalidOperationException("一時プリセットエディタの後始末を完了できませんでした。この候補は利用しません。", failure);
    }
}
