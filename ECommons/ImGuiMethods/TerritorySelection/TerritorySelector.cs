﻿using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ECommons.ImGuiMethods.TerritorySelection;
#nullable disable

public unsafe class TerritorySelector : Window
{
    public static bool Singleton = true;
    public static Dictionary<string, TerritoryIntendedUseEnum[]> Categories = new()
    {
        ["世界"] = [TerritoryIntendedUseEnum.City_Area, TerritoryIntendedUseEnum.Open_World,],
        ["住宅"] = [TerritoryIntendedUseEnum.Housing_Instances, TerritoryIntendedUseEnum.Residential_Area,],
        ["旅店"] = [TerritoryIntendedUseEnum.Inn,],
        ["迷宫挑战"] = [TerritoryIntendedUseEnum.Dungeon, TerritoryIntendedUseEnum.Variant_Dungeon, TerritoryIntendedUseEnum.Criterion_Duty, TerritoryIntendedUseEnum.Criterion_Savage_Duty,],
        ["大型任务"] = [TerritoryIntendedUseEnum.Raid, TerritoryIntendedUseEnum.Raid_2, TerritoryIntendedUseEnum.Alliance_Raid, TerritoryIntendedUseEnum.Large_Scale_Raid, TerritoryIntendedUseEnum.Large_Scale_Savage_Raid,],
        ["讨伐歼灭战"] = [TerritoryIntendedUseEnum.Trial],
        ["深层迷宫"] = [TerritoryIntendedUseEnum.Deep_Dungeon],
    };
    public static readonly List<TerritorySelector> Selectors = [];
    private readonly Dictionary<string, List<TerritoryType>> Cache = [];

    public bool OnlySelected = false;
    public string Filter = "";
    private WindowSystem WindowSystem;
    private HashSet<uint> SelectedTerritories;
    private uint SelectedTerritory;
    private bool IsSingleSelection = false;
    private Action<TerritorySelector, HashSet<uint>> Callback;
    private Action<TerritorySelector, uint> CallbackSingle;
    private static bool? VisibleAction = null;

    public Action<TerritoryType, Vector4?, string> ActionDrawPlaceName = (TerritoryType t, Vector4? col, string name) =>
    {
        ImGuiEx.Text(col, name);
    };

    public TerritorySelector(uint SelectedTerritory, Action<TerritorySelector, uint> Callback, string TitleName = null) : base(TitleName)
    {
        Setup([SelectedTerritory], null, Callback);
    }

    public TerritorySelector(Action<TerritorySelector, uint> Callback, string TitleName = null) : base(TitleName)
    {
        Setup([], null, Callback);
    }

    public TerritorySelector(IEnumerable<uint> SelectedTerritories, Action<TerritorySelector, HashSet<uint>> Callback, string TitleName = null) : base(TitleName)
    {
        Setup(SelectedTerritories?.ToHashSet() ?? [], Callback, null);
    }

    public TerritorySelector(Action<TerritorySelector, HashSet<uint>> Callback, string TitleName = null) : base(TitleName)
    {
        Setup([], Callback, null);
    }

    private void Setup(HashSet<uint> SelectedTerritories, Action<TerritorySelector, HashSet<uint>> Callback, Action<TerritorySelector, uint> CallbackSingle)
    {
        WindowName ??= "选择区域";
        IsSingleSelection = CallbackSingle != null;
        SelectedTerritory = SelectedTerritories.FirstOrDefault();
        if(Singleton)
        {
            Selectors.Each(x => x.Close());
            Selectors.Clear();
        }
        else
        {
            if(Selectors.Any(x => x.WindowName == WindowName))
            {
                Notify.Error("区域选择器已打开");
                return;
            }
        }
        Selectors.Add(this);
        this.SelectedTerritories = SelectedTerritories;
        this.Callback = Callback;
        this.CallbackSingle = CallbackSingle;
        WindowSystem = new($"ECommonsTerritorySelector_{Guid.NewGuid()}");
        WindowSystem.AddWindow(this);
        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        base.IsOpen = true;
        //yes I know it's not optimized but it's one-time call so whatever okay?
        foreach(var c in Categories)
        {
            foreach(var x in Svc.Data.GetExcelSheet<TerritoryType>())
            {
                if(c.Value.Contains((TerritoryIntendedUseEnum)x.TerritoryIntendedUse.RowId) && x.PlaceName.ValueNullable?.Name.ExtractText().IsNullOrEmpty() == false)
                {
                    if(!Cache.TryGetValue(c.Key, out var value))
                    {
                        value = ([]);
                        Cache[c.Key] = value;
                    }

                    value.Add(x);
                }
            }
        }
        Cache["其他"] = [];
        foreach(var x in Svc.Data.GetExcelSheet<TerritoryType>())
        {
            if(!Cache.Values.Any(c => c.Any(z => z.RowId == x.RowId)) && x.PlaceName.ValueNullable?.Name.ExtractText().IsNullOrEmpty() == false)
            {
                Cache["其他"].Add(x);
            }
        }
        Cache["全部"] = [];
        foreach(var x in Svc.Data.GetExcelSheet<TerritoryType>())
        {
            if(x.PlaceName.ValueNullable?.Name.ExtractText().IsNullOrEmpty() == false)
            {
                Cache["全部"].Add(x);
            }
        }
    }

    public override void Draw()
    {
        VisibleAction = null;
        if(ImGui.BeginTabBar("##TerritorySelectorBar"))
        {
            foreach(var x in Cache)
            {
                if(ImGui.BeginTabItem(x.Key))
                {
                    ImGui.SetNextItemWidth(200f);
                    ImGui.InputTextWithHint($"##search", "筛选...", ref Filter, 50);
                    ImGui.SameLine();
                    ImGui.Checkbox("仅显示已选择项", ref OnlySelected);
                    if(Player.Available)
                    {
                        ImGui.SameLine();
                        if(!IsSingleSelection)
                        {
                            if(ImGuiEx.CollectionCheckbox($"当前：{ExcelTerritoryHelper.GetName(Svc.ClientState.TerritoryType)}", Svc.ClientState.TerritoryType, SelectedTerritories))
                            {
                                try
                                {
                                    Callback(this, SelectedTerritories);
                                }
                                catch(Exception e)
                                {
                                    e.Log();
                                }
                            }
                            ImGui.SameLine();
                            if(ImGui.Button("添加所有可见项"))
                            {
                                VisibleAction = true;
                            }
                            ImGui.SameLine();
                            if(ImGui.Button("移除所有可见项"))
                            {
                                VisibleAction = false;
                            }
                        }
                        else
                        {
                            if(ImGui.RadioButton($"Current zone: {ExcelTerritoryHelper.GetName(Svc.ClientState.TerritoryType)}", SelectedTerritory == Svc.ClientState.TerritoryType))
                            {
                                SelectedTerritory = Svc.ClientState.TerritoryType;
                                try
                                {
                                    CallbackSingle(this, SelectedTerritory);
                                }
                                catch(Exception e)
                                {
                                    e.Log();
                                }
                            }
                        }
                    }

                    if(ImGui.BeginChild("##ChildTable"))
                    {
                        if(ImGui.BeginTable("##TSelector", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingFixedFit))
                        {
                            ImGui.TableSetupColumn(" ");
                            ImGui.TableSetupColumn("ID");
                            ImGui.TableSetupColumn("地名", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("任务");
                            ImGui.TableSetupColumn("地区");
                            ImGui.TableSetupColumn("地域");
                            ImGui.TableSetupColumn("预期用途");

                            ImGui.TableHeadersRow();

                            foreach(var t in x.Value)
                            {
                                var cfc = t.ContentFinderCondition.ValueNullable?.Name.ExtractText() ?? "";
                                var questBattle = t.QuestBattle.ValueNullable?.Quest.GetValueOrDefault<Quest>()?.Name.ExtractText() ?? "";
                                var name = t.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
                                var zone = t.PlaceNameZone.ValueNullable?.Name.ExtractText() ?? "";
                                var region = t.PlaceNameRegion.ValueNullable?.Name.ExtractText() ?? "";
                                var intended = ((TerritoryIntendedUseEnum)t.TerritoryIntendedUse.RowId).ToString().Replace("_", " ") ?? "";
                                var col = t.RowId == Svc.ClientState.TerritoryType && Player.Available;

                                if(Filter != ""
                                    && !cfc.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !questBattle.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !name.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !zone.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !region.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !intended.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !x.Key.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                                    && !t.RowId.ToString().Contains(Filter, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if(OnlySelected && !SelectedTerritories.Contains(t.RowId))
                                {
                                    continue;
                                }

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();//checkbox
                                if(!IsSingleSelection)
                                {
                                    if(ImGuiEx.CollectionCheckbox($"##sel{t.RowId}", t.RowId, SelectedTerritories))
                                    {
                                        try
                                        {
                                            Callback(this, SelectedTerritories);
                                        }
                                        catch(Exception e)
                                        {
                                            e.Log();
                                        }
                                    }
                                    if(VisibleAction == true && !SelectedTerritories.Contains(t.RowId))
                                    {
                                        SelectedTerritories.Add(t.RowId);
                                        Callback(this, SelectedTerritories);
                                    }
                                    if(VisibleAction == false && SelectedTerritories.Contains(t.RowId))
                                    {
                                        SelectedTerritories.Remove(t.RowId);
                                        Callback(this, SelectedTerritories);
                                    }
                                }
                                else
                                {
                                    if(ImGui.RadioButton($"##sel{t.RowId}", t.RowId == SelectedTerritory))
                                    {
                                        SelectedTerritory = t.RowId;
                                        try
                                        {
                                            CallbackSingle(this, SelectedTerritory);
                                        }
                                        catch(Exception e)
                                        {
                                            e.Log();
                                        }
                                    }
                                }

                                ImGui.TableNextColumn(); //id
                                ImGuiEx.Text($"{t.RowId}");

                                ImGui.TableNextColumn(); //Place name
                                ActionDrawPlaceName(t, col ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudYellow, name);

                                ImGui.TableNextColumn(); //Duty
                                if(!cfc.IsNullOrEmpty())
                                {
                                    ImGuiEx.Text($"{cfc}");
                                }
                                else if(!questBattle.IsNullOrEmpty())
                                {
                                    ImGuiEx.Text($"{questBattle}");
                                }
                                else
                                {
                                    ImGuiEx.Text("");
                                }

                                ImGui.TableNextColumn(); //zone
                                ImGuiEx.Text($"{zone}");

                                ImGui.TableNextColumn(); //Region
                                ImGuiEx.Text($"{region}");

                                ImGui.TableNextColumn(); //use
                                ImGuiEx.Text($"{intended}");

                            }


                            ImGui.EndTable();
                        }
                    }
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }

    public override void OnClose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        Selectors.Remove(this);
    }

    [Obsolete("Do not reopen existing TerritorySelector. You should create new TerritorySelector every time.", true)]
    private new bool IsOpen => throw new Exception("You should create new TerritorySelector every time.");

    public void Close()
    {
        base.IsOpen = false;
    }
}
