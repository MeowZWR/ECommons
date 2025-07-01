using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using ECommons.EzSharedDataManager;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;

namespace ECommons.Funding;
public static class PatreonBanner
{
    public static Func<bool> IsOfficialPlugin = () => false;
    public static string Text = "♥ Patreon/KoFi";
    public static string DonateLink => "https://www.patreon.com/NightmareXIV";
    public static void DrawRaw()
    {
        DrawButton();
    }

    private static uint ColorNormal
    {
        get
        {
            var vector1 = ImGuiEx.Vector4FromRGB(0x022594);
            var vector2 = ImGuiEx.Vector4FromRGB(0x940238);

            var gen = GradientColor.Get(vector1, vector2).ToUint();
            var data = EzSharedData.GetOrCreate<uint[]>("ECommonsPatreonBannerRandomColor", [gen]);
            if(!GradientColor.IsColorInRange(data[0].ToVector4(), vector1, vector2))
            {
                data[0] = gen;
            }
            return data[0];
        }
    }

    private static uint ColorHovered => ColorNormal;

    private static uint ColorActive => ColorNormal;

    private static readonly uint ColorText = 0xFFFFFFFF;

    public static void DrawButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ColorNormal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorActive);
        ImGui.PushStyleColor(ImGuiCol.Text, ColorText);
        if(ImGui.Button(Text))
        {
            GenericHelpers.ShellStart(DonateLink);
        }
        Popup();
        if(ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        ImGui.PopStyleColor(4);
    }

    public static void RightTransparentTab(string? text = null)
    {
        text ??= Text;
        var textWidth = ImGui.CalcTextSize(text).X;
        var spaceWidth = ImGui.CalcTextSize(" ").X;
        ImGui.BeginDisabled();
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0f);
        if(ImGuiEx.BeginTabItem(" ".Repeat((int)MathF.Ceiling(textWidth / spaceWidth)), ImGuiTabItemFlags.Trailing))
        {
            ImGui.EndTabItem();
        }
        ImGui.PopStyleVar();
        ImGui.EndDisabled();
    }

    public static void DrawRight()
    {
        var cur = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(cur.X + ImGui.GetContentRegionAvail().X - ImGuiHelpers.GetButtonSize(Text).X);
        DrawRaw();
        ImGui.SetCursorPos(cur);
    }

    private static string PatreonButtonTooltip => $"""
				如果你喜欢 {Svc.PluginInterface.Manifest.Name}，请考虑通过 Patreon 或其他方式支持它的开发者！ 
				
				这将帮助开发者更新插件，同时为你提供优先功能请求、优先支持、早期插件版本、参与功能投票等更多权益。

				左键点击 - 前往 Patreon;
				右键点击 - Ko-Fi 和其他选项。
				""";

    private static string SmallPatreonButtonTooltip => $"""
				如果你喜欢 {Svc.PluginInterface.Manifest.Name}，请考虑通过 Patreon 支持它的开发者。

				左键点击 - 前往 Patreon;
				右键点击 - Ko-Fi 和其他选项。
				""";

    private static void Popup()
    {
        if(ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGuiEx.Text(IsOfficialPlugin() ? SmallPatreonButtonTooltip : PatreonButtonTooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if(ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("NXPS");
            }
        }
        if(ImGui.BeginPopup("NXPS"))
        {
            if(ImGui.Selectable("在 Patreon 订阅"))
            {
                GenericHelpers.ShellStart("https://subscribe.nightmarexiv.com");
            }
            if(ImGui.Selectable("Donate one-time via Ko-Fi"))
            {
                GenericHelpers.ShellStart("https://donate.nightmarexiv.com");
            }
            if(ImGui.Selectable("通过加密货币捐赠"))
            {
                GenericHelpers.ShellStart($"https://crypto.nightmarexiv.com/{(IsOfficialPlugin() ? "?" + Svc.PluginInterface.Manifest.Name : "")}");
            }
            if(!IsOfficialPlugin())
            {
                if(ImGui.Selectable("加入 NightmareXIV Discord 服务器"))
                {
                    GenericHelpers.ShellStart("https://discord.nightmarexiv.com");
                }
                if(ImGui.Selectable("浏览 NightmareXIV 的其他插件"))
                {
                    GenericHelpers.ShellStart("https://explore.nightmarexiv.com");
                }
            }
            ImGui.EndPopup();
        }
    }
}
