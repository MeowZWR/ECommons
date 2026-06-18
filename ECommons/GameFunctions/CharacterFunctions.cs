using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;

namespace ECommons.GameFunctions;

public static unsafe class CharacterFunctions
{
    public static bool HasStatus(this IBattleChara chr, uint id, float? lessThan = null, float? moreThan = null)
    {
        foreach(var x in chr.StatusList)
        {
            if(x.StatusId == id)
            {
                if(lessThan != null && x.RemainingTime > lessThan.Value) continue;
                if(moreThan != null && x.RemainingTime < moreThan.Value) continue;
                return true;
            }
        }
        return false;
    }

    public static bool HasStatus(this IBattleChara chr, uint id, out float time, float? lessThan = null, float? moreThan = null)
    {
        foreach(var x in chr.StatusList)
        {
            if(x.StatusId == id)
            {
                if(lessThan != null && x.RemainingTime > lessThan.Value) continue;
                if(moreThan != null && x.RemainingTime < moreThan.Value) continue;
                time = x.RemainingTime;
                return true;
            }
        }
        time = default;
        return false;
    }

    public static bool HasStatus(this IBattleChara chr, IEnumerable<uint> id, float? lessThan = null, float? moreThan = null)
    {
        foreach(var x in id)
        {
            if(chr.HasStatus(x, lessThan, moreThan))
            {
                return true;
            }
        }
        return false;
    }

    public static bool HasStatus(this IBattleChara chr, IEnumerable<uint> id, out List<(uint ID, float Time)> foundStatus, float? lessThan = null, float? moreThan = null)
    {
        foundStatus = [];
        foreach(var x in id)
        {
            if(chr.HasStatus(x, out var time, lessThan, moreThan))
            {
                foundStatus.Add((x, time));
            }
        }
        return foundStatus.Count > 0;
    }

    public static ushort GetVFXId(void* VfxData)
    {
        if(VfxData == null) return 0;
        return *(ushort*)((IntPtr)(VfxData) + 8);
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Struct(this ICharacter o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)o.Address;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* Struct(this IBattleChara o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)o.Address;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Character(this IBattleChara o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)o.Address;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* GameObject(this IBattleChara o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)o.Address;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* IGameObject(this ICharacter o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)o.Address;
    }

    public static bool IsCharacterVisible(this ICharacter chr)
    {
        var v = (IntPtr)(((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)chr.Address)->GameObject.DrawObject);
        if(v == IntPtr.Zero) return false;
        return Bitmask.IsBitSet(*(byte*)(v + 136), 0);
    }

    public static byte GetTransformationID(this ICharacter chr)
    {
        return chr.Struct()->Timeline.ModelState;
        //return *(byte*)(chr.Address + 2480 + 704);
    }

    public static bool IsInWater(this ICharacter chr)
    {
        return *(byte*)(chr.Address + 1452) == 1;
    }

    public static CombatRole GetRole(this ICharacter c)
    {
        if(c.ClassJob.ValueNullable?.Role == 1) return CombatRole.Tank;
        if(c.ClassJob.ValueNullable?.Role == 2) return CombatRole.DPS;
        if(c.ClassJob.ValueNullable?.Role == 3) return CombatRole.DPS;
        if(c.ClassJob.ValueNullable?.Role == 4) return CombatRole.Healer;
        return CombatRole.NonCombat;
    }

    extension(IBattleChara v)
    {
        public CastInfo CastInfo
        {
            get
            {
                var info = v.Struct()->GetCastInfo();
                if(info == null)
                {
                    return default;
                }
                var ret = *info;
                //ret.ActionType = (*(byte*)(((nint)info) + 1));
                return ret;
            }
        }
    }

    public static bool IsCasting(this IBattleChara c, uint spellId = 0, ActionType? type = null)
    {
        var info = c.CastInfo;
        if(info.ActionId == 0) return false;
        return c.IsCasting && (spellId == 0 || (info.ActionId.EqualsAny(spellId) && (type == null || info.ActionType == (byte)type.Value)));
    }

    public static bool IsCasting(this IBattleChara c, params uint[] spellId)
    {
        var info = c.CastInfo;
        if(info.ActionId == 0) return false;
        return c.IsCasting && info.ActionId.EqualsAny(spellId);
    }

    public static bool IsCasting(this IBattleChara c, IEnumerable<uint> spellId)
    {
        var info = c.CastInfo;
        if(info.ActionId == 0) return false;
        return c.IsCasting && info.ActionId.EqualsAny(spellId);
    }

    extension(IGameObject obj)
    {
        public uint ObjectId => obj.EntityId;
    }

    extension(ICharacter chr)
    {
        public float Health => (float)chr.CurrentHp / (float)chr.MaxHp;
        public uint MissingHp => chr.MaxHp - chr.CurrentHp;
        public uint StatusLoop => chr.Struct()->StatusLoopVfxId;
        public int ModelId => chr.Struct()->ModelContainer.ModelCharaId;
    }

    extension(IBattleChara b)
    {
        public float RemainingCastTime => b.CastInfo.TotalCastTime - b.CastInfo.CurrentCastTime;
    }

    public static List<TetherInfo> GetTethers(this ICharacter c, bool onlySource = false)
    {
        List<TetherInfo> ret = [];
        {
            var t = c.Struct()->Vfx.Tethers;
            for(int i = 0; i < t.Length; i++)
            {
                if(t[i].Id != 0)
                {
                    ret.Add(new(t[i], t[i].TargetId.ObjectId, true));
                }
            }
        }
        if(!onlySource)
        {
            foreach(var obj in Svc.Objects)
            {
                if(obj is ICharacter chr)
                {
                    var t = chr.Struct()->Vfx.Tethers;
                    for(int i = 0; i < t.Length; i++)
                    {
                        if(t[i].Id != 0 && t[i].TargetId == c.GameObjectId)
                        {
                            ret.Add(new(t[i], chr.ObjectId, false));
                        }
                    }
                }
            }
        }
        return ret;
    }
}
