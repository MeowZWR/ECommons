using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ECommons.GameFunctions;

/// <summary>
/// RenderDisableManager provides cross-plugin synchronization for disabling world rendering.
/// <br></br>How to use:
/// <br></br>- Call PlaceRequest to indicate that you wish to stop rendering from happening
/// <br></br>- Call RemoveRequest to resume rendering
/// <br></br>Rendering will be disabled for as long as any plugin has a request.
/// <br></br>You do not to neither initialize nor dispose it manually, however you can call Init method to initialize it preemptively if you want.
/// </summary>
public unsafe static class RenderDisableManager
{
    private static bool Initialized = false;
    private static uint* FrameCounter;
    private static byte* RenderDisabled;
    private static HashSet<uint> RenderDisableRequests;
    private static uint[] RenderDisableProcessingFramecount;
    /// <summary>
    /// Set to true if you want to output verbose logs. It is advised to just have a checkbox rather than permanently setting it to true. 
    /// </summary>
    public static bool Debug;

    private static readonly string Name_RenderDisableRequests = $"ECommons.RenderDisableRequests";
    private static readonly string Name_RenderDisableProcessingFramecount = $"ECommons.RenderDisableProcessingFramecount";
    private static readonly string Name_RenderDisableTakenIdentifiers = $"ECommons.RenderDisableTakenIdentifiers";

    /// <summary>
    /// Initializes RenderDisableManager. You do not need to call it manually. 
    /// </summary>
    public static void Init()
    {
        if(Initialized)
        {
            PluginLog.Error("RenderDisableManager is already initialized and subsequent initialize call was ignored");
            return;
        }
        Initialized = true;
        FrameCounter = &Framework.Instance()->FrameCounter;
        RenderDisabled = (byte*)(((nint)Manager.Instance()) + 230232);
        RenderDisableRequests = Svc.PluginInterface.GetOrCreateData<HashSet<uint>>(Name_RenderDisableRequests, () => []);
        RenderDisableProcessingFramecount = Svc.PluginInterface.GetOrCreateData<uint[]>(Name_RenderDisableProcessingFramecount, () => [0]);
        Svc.Framework.Update += Framework_Update;
        PluginLog.Information($"Initialized RenderDisableManager");
    }

    /// <summary>
    /// Places request indicating that your plugin wants to disable rendering. If not initialized, initializes RenderDisablerManager. Must only be called inside framework update thread. You can call it every frame, subsequent requests will not be piled up. 
    /// </summary>
    public static void PlaceRequest()
    {
        if(!Initialized) Init();
        if(!Svc.Framework.IsInFrameworkUpdateThread)
        {
            PluginLog.Error($"{nameof(RenderDisableManager)}.{nameof(PlaceRequest)} can only be used in Framework Update thread.");
            return;
        }
        RenderDisableRequests.Add(ECommonsMain.InstanceUniqueId);
    }

    /// <summary>
    /// Places request indicating that your plugin no longer wants to disable rendering. If not initialized, does not initializes RenderDisablerManager. Must only be called inside framework update thread. You can call it every frame, subsequent requests when there are no requests from your plugin will not do anything. 
    /// </summary>
    public static void RemoveRequest()
    {
        if(!Initialized) return;
        if(!Svc.Framework.IsInFrameworkUpdateThread)
        {
            PluginLog.Error($"{nameof(RenderDisableManager)}.{nameof(RemoveRequest)} can only be used in Framework Update thread.");
            return;
        }
        RenderDisableRequests.Remove(ECommonsMain.InstanceUniqueId);
    }

    internal static void Dispose()
    {
        if(Initialized)
        {
            Svc.Framework.RunOnFrameworkThread(() =>
            {
                RemoveRequest();
                Framework_Update(null);
            });
            Svc.Framework.Update -= Framework_Update;
            Svc.PluginInterface.RelinquishData(Name_RenderDisableRequests);
            Svc.PluginInterface.RelinquishData(Name_RenderDisableProcessingFramecount);
        }
    }

    private static void Framework_Update(IFramework framework)
    {
        if(RenderDisableProcessingFramecount[0] == *FrameCounter)
        {
            if(Debug) PluginLog.Verbose($"[RenderDisableManager] Frame {*FrameCounter} was already processed by different instance");
        }
        else
        {
            if(RenderDisableRequests.Count == 0)
            {
                if(*RenderDisabled != 0)
                {
                    if(Debug) PluginLog.Verbose($"[RenderDisableManager] Enabling render because there are no requests");
                    *RenderDisabled = 0;
                }
            }
            else
            {
                if(*RenderDisabled == 0)
                {
                    if(Debug) PluginLog.Verbose($"[RenderDisableManager] Disabling render because there are requests");
                    *RenderDisabled = 1;
                }
            }
            RenderDisableProcessingFramecount[0] = *FrameCounter;
        }
    }
}
