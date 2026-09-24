#if YMM4_PROOF
using System.Text.Json.Serialization;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

// Temporary proof-only bridge while historical Native tests are consolidated.
// Distribution builds contain no LegacyWorkspace settings/runtime path.
public sealed partial class PlacerSettings
{
    [JsonIgnore]
    internal bool LegacyWorkspace { get; set; }
}

public sealed partial class PlacerViewModel
{
    internal bool UseLegacyWorkspace => false;
    internal ActionCommand OpenLegacyWorkspaceCommand { get; } = new(_ => true, _ => { });
    internal ActionCommand CloseLegacyWorkspaceCommand { get; } = new(_ => true, _ => { });
    internal void SetLegacyWorkspace(bool value) { }
}
#endif
