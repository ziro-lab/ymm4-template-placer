using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

// Bounded vocabulary proved by the exact 4.55.1.1 Japanese host (P0).
// Plugin-defined / unavailable types never fall back to CLR identifiers.
public static class ItemDisplayNames
{
    public static string For(Type type)
    {
        if (type.Assembly != typeof(VoiceItem).Assembly) return "追加アイテム";
        return type.Name switch
        {
            "VoiceItem" => "ボイス", "TachieFaceItem" => "表情", "TachieItem" => "立ち絵",
            "TextItem" => "テキスト", "VideoItem" => "動画", "AudioItem" => "音声",
            "ShapeItem" => "図形", "ImageItem" => "画像", "TransitionItem" => "場面切り替え",
            "FrameBufferItem" => "画面の複製", "EffectItem" => "エフェクト", _ => "追加アイテム"
        };
    }
}
