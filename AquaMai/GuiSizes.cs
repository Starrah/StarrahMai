using HarmonyLib;

namespace StarrahMai.AquaMai;

public static class GuiSizes
{
    private static Traverse _t = Traverse.CreateWithType("AquaMai.Core.Helpers.GuiSizes");
    public static float PlayerCenter => _t.Property("PlayerCenter").GetValue<float>();
    public static int FontSize => _t.Property("FontSize").GetValue<int>();
    public static float LabelHeight => _t.Property("LabelHeight").GetValue<float>();
}