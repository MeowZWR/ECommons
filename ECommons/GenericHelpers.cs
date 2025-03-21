﻿using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.ExcelServices.Sheets;
using ECommons.Interop;
using ECommons.MathHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Lumina.Text.ReadOnly;
using Newtonsoft.Json;
using PInvoke;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
#nullable disable

namespace ECommons;

public static unsafe partial class GenericHelpers
{
    private static string UidPrefix = $"{Random.Shared.Next(0, 0xFFFF):X4}";
    private static ulong UidCnt = 0;
    public static string GetTemporaryId() => $"{UidPrefix}{UidCnt++:X}";

    public static bool TryGetValue<T>(this T? nullable, out T value) where T : struct
    {
        if(nullable.HasValue)
        {
            value = nullable.Value;
            return true;
        }
        value = default;
        return false;
    }

    public static bool TryGetValue<T>(this RowRef<T> rowRef, out T value) where T:struct, IExcelRow<T>
    {
        if(rowRef.ValueNullable != null)
        {
            value = rowRef.Value;
            return true;
        }
        value = default;
        return false;
    }

    public static TExtension GetExtension<TExtension, TBase>(this TBase row) where TExtension : struct, IExcelRow<TExtension>, IRowExtension<TExtension, TBase> where TBase : struct, IExcelRow<TBase>
    {
        return TExtension.GetExtended(row);
    }

    public static SeString ReadSeString(Utf8String* utf8String)
    {
        if(utf8String != null)
        {
            return SeString.Parse(utf8String->AsSpan());
        }

        return string.Empty;
    }

    public static T? FirstOrNull<T>(this IEnumerable<T> values, Func<T, bool> predicate) where T : struct
    {
        if(values.TryGetFirst(predicate, out var result))
        {
            return result;
        }
        return null;
    }

    public static T? FirstOrNull<T>(this IEnumerable<T> values) where T:struct
    {
        if(values.TryGetFirst(out var result))
        {
            return result;
        }
        return null;
    }

    public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> values) where T : struct
    {
        return values.Cast<T?>();
    }

    public static bool ContainsNullable<T>(this IEnumerable<T> values, T? value) where T : struct
    {
        if(value == null) return false;
        return System.Linq.Enumerable.Contains(values, value.Value);
    }

    /// <summary>
    /// Adds all <paramref name="values"/> to the <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="values"></param>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> values)
    {
        foreach(var x in values)
        {
            collection.Add(x);
        }
    }

    /// <summary>
    /// Generates range of numbers with step = 1.
    /// </summary>
    /// <param name="inclusiveStart"></param>
    /// <param name="inclusiveEnd"></param>
    /// <returns></returns>
    public static uint[] Range(uint inclusiveStart, uint inclusiveEnd)
    {
        var ret = new uint[inclusiveEnd - inclusiveStart + 1];
        for(var i = 0; i < ret.Length; i++)
        {
            ret[i] = (uint)(inclusiveStart + i);
        }
        return ret;
    }

    /// <summary>
    /// Generates range of numbers with step = 1.
    /// </summary>
    /// <param name="inclusiveStart"></param>
    /// <param name="inclusiveEnd"></param>
    /// <returns></returns>
    public static int[] Range(int inclusiveStart, int inclusiveEnd)
    {
        var ret = new int[inclusiveEnd - inclusiveStart + 1];
        for(var i = 0; i < ret.Length; i++)
        {
            ret[i] = (int)(inclusiveStart + i);
        }
        return ret;
    }

    /// <summary>
    /// Reads SeString.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static SeString Read(this Utf8String str)
    {
        return GenericHelpers.ReadSeString(&str);
    }

    /// <summary>
    /// Reads Span of bytes into <see langword="string"/>.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string Read(this Span<byte> bytes)
    {
        for(var i = 0; i < bytes.Length; i++)
        {
            if(bytes[i] == 0)
            {
                fixed(byte* ptr = bytes)
                {
                    return Marshal.PtrToStringUTF8((nint)ptr, i);
                }
            }
        }
        fixed(byte* ptr = bytes)
        {
            return Marshal.PtrToStringUTF8((nint)ptr, bytes.Length);
        }
    }

    /// <summary>
    /// Returns random element from <paramref name="enumerable"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static T GetRandom<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.ElementAt(Random.Shared.Next(enumerable.Count()));
    }

    /// <summary>
    /// Returns <see langword="true"/> if screen isn't faded. 
    /// </summary>
    /// <returns></returns>
    public static bool IsScreenReady()
    {
        { if(TryGetAddonByName<AtkUnitBase>("NowLoading", out var addon) && addon->IsVisible) return false; }
        { if(TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var addon) && addon->IsVisible) return false; }
        { if(TryGetAddonByName<AtkUnitBase>("FadeBack", out var addon) && addon->IsVisible) return false; }
        return true;
    }

    public static bool AddressEquals(this IGameObject obj, IGameObject other)
    {
        return obj?.Address == other?.Address;
    }

    /// <inheritdoc cref="SafeSelect{K, V}(IReadOnlyDictionary{K, V}, K, V)"/>
    public static V? SafeSelect<K, V>(this IReadOnlyDictionary<K, V> dictionary, K? key) => SafeSelect(dictionary, key, default);

    /// <summary>
    /// Safely selects a value from a <paramref name="dictionary"/>. Does not throws exceptions under any circumstances.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="dictionary"></param>
    /// <param name="key"></param>
    /// <param name="defaultValue">Returns if <paramref name="dictionary"/> is <see langword="null"/> or <paramref name="key"/> is <see langword="null"/> or <paramref name="key"/> is not found in <paramref name="dictionary"/></param>
    /// <returns></returns>
    public static V? SafeSelect<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key, V defaultValue)
    {
        if(dictionary == null) return default;
        if(key == null) return default;
        if(dictionary.TryGetValue(key, out var ret))
        {
            return ret;
        }
        return defaultValue;
    }

    public static T CircularSelect<T>(this IList<T> list, int index) => list[MathHelper.Mod(index, list.Count)];

    public static T CircularSelect<T>(this T[] list, int index) => list[MathHelper.Mod(index, list.Length)];

    /// <summary>
    /// Safely selects an entry of the <paramref name="list"/> at a specified <paramref name="index"/>, returning default value if index is out of range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T SafeSelect<T>(this IReadOnlyList<T> list, int index)
    {
        if(list == null) return default;
        if(index < 0 || index >= list.Count) return default;
        return list[index];
    }

    /// <summary>
    /// Safely selects an entry of the <paramref name="array"/> at a specified <paramref name="index"/>, returning <see langword="default"/> value if index is out of range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T SafeSelect<T>(this T[] array, int index)
    {
        if(index < 0 || index >= array.Length) return default;
        return array[index];
    }

    /// <summary>
    /// Attempts to parse byte array string separated by specified character.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static bool TryParseByteArray(string input, out byte[] output, char separator = ' ')
    {
        var str = input.Split(separator);
        output = new byte[str.Length];
        for(var i = 0; i < str.Length; i++)
        {
            if(!byte.TryParse(str[i], NumberStyles.HexNumber, null, out output[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Retrieves entries from call stack in a form of single string. <b>Expensive.</b>
    /// </summary>
    /// <param name="maxFrames"></param>
    /// <returns></returns>
    public static string GetCallStackID(int maxFrames = 3)
    {
        try
        {
            if(maxFrames == 0)
            {
                maxFrames = int.MaxValue;
            }
            else
            {
                maxFrames--;
            }
            var stack = new StackTrace().GetFrames();
            if(stack.Length > 1)
            {
                return stack[1..Math.Min(stack.Length, maxFrames)].Select(x => x.GetMethod() == null ? "<unknown>" : $"{x.GetMethod().DeclaringType?.FullName}.{x.GetMethod().Name}").Join(" <- ");
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return "";
    }

    /// <summary>
    /// Converts byte array to hex string where bytes are separated by a specified character
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string ToHexString(this IEnumerable<byte> bytes, char separator = ' ')
    {
        var first = true;
        var sb = new StringBuilder();
        foreach(var x in bytes)
        {
            if(first)
            {
                first = false;
            }
            else
            {
                sb.Append(separator);
            }
            sb.Append($"{x:X2}");
        }
        return sb.ToString();
    }

    [Obsolete($"Use {nameof(SafeSelect)}")]
    public static T GetOrDefault<T>(this IReadOnlyList<T> List, int index) => SafeSelect(List, index);

    [Obsolete($"Use {nameof(SafeSelect)}")]
    public static T GetOrDefault<T>(this T[] Array, int index) => SafeSelect(Array, index);

    /// <summary>
    /// Treats list as a queue, removing and returning element at index 0.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="List"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryDequeue<T>(this IList<T> List, out T result)
    {
        if(List.Count > 0)
        {
            result = List[0];
            List.RemoveAt(0);
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Treats list as a queue, removing and returning element at index 0.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="List"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T Dequeue<T>(this IList<T> List)
    {
        if(List.TryDequeue(out var ret))
        {
            return ret;
        }
        throw new InvalidOperationException("Sequence contains no elements");
    }

    public static bool TryDequeueLast<T>(this IList<T> List, out T result)
    {
        if(List.Count > 0)
        {
            result = List[List.Count - 1];
            List.RemoveAt(List.Count - 1);
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

    public static T DequeueLast<T>(this IList<T> List)
    {
        if(List.TryDequeueLast(out var ret))
        {
            return ret;
        }
        throw new InvalidOperationException("Sequence contains no elements");
    }

    /// <summary>
    /// Treats list as a queue, removing and returning element at index 0 or default value if there's nothing to dequeue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="List"></param>
    /// <returns></returns>
    public static T DequeueOrDefault<T>(this IList<T> List)
    {
        if(List.Count > 0)
        {
            var ret = List[0];
            List.RemoveAt(0);
            return ret;
        }
        else
        {
            return default;
        }
    }

    /// <summary>
    /// Dequeues element from queue or returns default value if there's nothing to dequeue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Queue"></param>
    /// <returns></returns>
    public static T DequeueOrDefault<T>(this Queue<T> Queue)
    {
        if(Queue.Count > 0)
        {
            return Queue.Dequeue();
        }
        return default;
    }

    /// <summary>
    /// Searches index of first element in IEnumerable that matches the predicate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static int IndexOf<T>(this IEnumerable<T> values, Predicate<T> predicate)
    {
        var ret = -1;
        foreach(var v in values)
        {
            ret++;
            if(predicate(v))
            {
                return ret;
            }
        }
        return -1;
    }

    /// <summary>
    /// Searches index of first element in IEnumerable that matches the predicate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int IndexOf<T>(this IEnumerable<T> values, T value)
    {
        var ret = -1;
        foreach(var v in values)
        {
            ret++;
            if(v.Equals(value))
            {
                return ret;
            }
        }
        return -1;
    }

    public static bool ContainsIgnoreCase(this IEnumerable<string> haystack, string needle)
    {
        foreach(var x in haystack)
        {
            if(x.EqualsIgnoreCase(needle)) return true;
        }
        return false;
    }

    public static T[] Together<T>(this T[] array, params T[] additionalValues)
    {
        return array.Union(additionalValues).ToArray();
    }

    /// <summary>
    /// Returns <paramref name="s"/> when <paramref name="b"/> is <see langword="true"/>, <see langword="null"/> otherwise
    /// </summary>
    /// <param name="s"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static string NullWhenFalse(this string s, bool b)
    {
        return b ? s : null;
    }

    /// <summary>
    /// Returns <see cref="UInt32"/> representation of <see cref="Single"/>.
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static uint AsUInt32(this float f)
    {
        return *(uint*)&f;
    }

    /// <summary>
    /// Converts <see cref="UInt32"/> representation of <see cref="Single"/> into <see cref="Single"/>.
    /// </summary>
    /// <param name="u"></param>
    /// <returns></returns>
    public static float AsFloat(this uint u)
    {
        return *(float*)&u;
    }

    /// <summary>
    /// Tries to add multiple items to collection
    /// </summary>
    /// <typeparam name="T">Collection type</typeparam>
    /// <param name="collection">Collection</param>
    /// <param name="values">Items</param>
    public static void Add<T>(this ICollection<T> collection, params T[] values)
    {
        foreach(var x in values)
        {
            collection.Add(x);
        }
    }

    /// <summary>
    /// Tries to remove multiple items to collection. In case if few of the same values are present in the collection, only first will be removed.
    /// </summary>
    /// <typeparam name="T">Collection type</typeparam>
    /// <param name="collection">Collection</param>
    /// <param name="values">Items</param>
    public static void Remove<T>(this ICollection<T> collection, params T[] values)
    {
        foreach(var x in values)
        {
            collection.Remove(x);
        }
    }

#pragma warning disable
    /// <summary>
    /// Sets whether <see cref="User32.GetKeyState"/> or <see cref="User32.GetAsyncKeyState"/> will be used when calling <see cref="IsKeyPressed(Keys)"/> or <see cref="IsKeyPressed(LimitedKeys)"/>
    /// </summary>
#pragma warning restore
    public static bool UseAsyncKeyCheck = false;

    /// <summary>
    /// Checks if a key is pressed via winapi.
    /// </summary>
    /// <param name="key">Key</param>
    /// <returns>Whether the key is currently pressed</returns>
    public static bool IsKeyPressed(int key)
    {
        if(key == 0) return false;
        if(UseAsyncKeyCheck)
        {
            return Bitmask.IsBitSet(User32.GetKeyState(key), 15);
        }
        else
        {
            return Bitmask.IsBitSet(User32.GetAsyncKeyState(key), 15);
        }
    }

    /// <summary>
    /// Checks if a key is pressed via winapi.
    /// </summary>
    /// <param name="key">Key</param>
    /// <returns>Whether the key is currently pressed</returns>
    public static bool IsKeyPressed(LimitedKeys key) => IsKeyPressed((int)key);

    public static bool IsAnyKeyPressed(IEnumerable<LimitedKeys> keys) => keys.Any(IsKeyPressed);

    public static bool IsKeyPressed(IEnumerable<LimitedKeys> keys)
    {
        foreach(var x in keys)
        {
            if(IsKeyPressed(x)) return true;
        }
        return false;
    }

    public static bool IsKeyPressed(IEnumerable<int> keys)
    {
        foreach(var x in keys)
        {
            if(IsKeyPressed(x)) return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if you are targeting object <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">Object to check</param>
    /// <returns>Whether you are targeting object <paramref name="obj"/>; <see langword="false"/> if <paramref name="obj"/> is <see langword="null"/></returns>
    public static bool IsTarget(this IGameObject obj)
    {
        return Svc.Targets.Target != null && obj != null && Svc.Targets.Target.Address == obj.Address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOr<T>(this T source, Predicate<T> testFunction)
    {
        return source == null || testFunction(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CreateArray<T>(this T o, uint num)
    {
        var arr = new T[num];
        for(var i = 0; i < arr.Length; i++)
        {
            arr[i] = o;
        }
        return arr;
    }

    public static V GetOrDefault<K, V>(this IDictionary<K, V> dic, K key)
    {
        if(dic.TryGetValue(key, out var value)) return value;
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IncrementOrSet<K>(this IDictionary<K, int> dic, K key, int increment = 1)
    {
        if(dic.ContainsKey(key))
        {
            dic[key] += increment;
        }
        else
        {
            dic[key] = increment;
        }
        return dic[key];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveOtherChars(this string s, string charsToKeep)
    {
        return new string(s.ToArray().Where(charsToKeep.Contains).ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceByChar(this string s, string replaceWhat, string replaceWith, bool replaceWithWhole = false)
    {
        if(replaceWhat.Length != replaceWith.Length && !replaceWithWhole)
        {
            PluginLog.Error($"{nameof(replaceWhat)} and {nameof(replaceWith)} must be same length");
            return s;
        }
        for(var i = 0; i < replaceWhat.Length; i++)
        {
            if(replaceWithWhole)
            {
                s = s.Replace(replaceWhat[i].ToString(), replaceWith);
            }
            else
            {
                s = s.Replace(replaceWhat[i], replaceWith[i]);
            }
        }
        return s;
    }

    /// <summary>
    /// Serializes and then deserializes object, returning result of deserialization using <see cref="Newtonsoft.Json"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns>Deserialized copy of <paramref name="obj"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T JSONClone<T>(this T obj)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
    }

    /// <summary>
    /// Attempts to parse integer
    /// </summary>
    /// <param name="number">Input string</param>
    /// <returns>Integer if parsing was successful, <see langword="null"/> if failed</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? ParseInt(this string number)
    {
        if(int.TryParse(number, out var result))
        {
            return result;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Print<T>(this IEnumerable<T> x, string separator = ", ")
    {
        return x.Select(x => (x?.ToString() ?? "")).Join(separator);
    }

    public static void DeleteFileToRecycleBin(string path)
    {
        try
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }
        catch(Exception e)
        {
            e.LogWarning();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V GetSafe<K, V>(this IDictionary<K, V> dic, K key, V Default = default)
    {
        if(dic?.TryGetValue(key, out var value) == true)
        {
            return value;
        }
        return Default;
    }

    /// <summary>
    /// Retrieves a value from dictionary, adding it first if it doesn't exists yet.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="dictionary"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key)
    {
        if(dictionary.TryGetValue(key, out var result))
        {
            return result;
        }
        V newValue;
        if(typeof(V).FullName == typeof(string).FullName)
        {
            newValue = (V)(object)"";
        }
        else
        {
            try
            {
                newValue = (V)Activator.CreateInstance(typeof(V));
            }
            catch(Exception)
            {
                newValue = default;
            }
        }
        dictionary.Add(key, newValue);
        return newValue;
    }

    /// <summary>
    /// Executes action for each element of collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="function"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Each<T>(this IEnumerable<T> collection, Action<T> function)
    {
        foreach(var x in collection)
        {
            function(x);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool If<T>(this T obj, Func<T, bool> func)
    {
        return func(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NotNull<T>(this T obj, [NotNullWhen(true)] out T outobj)
    {
        outobj = obj;
        return obj != null;
    }

    public static bool IsOccupied()
    {
        return Svc.Condition[ConditionFlag.Occupied]
               || Svc.Condition[ConditionFlag.Occupied30]
               || Svc.Condition[ConditionFlag.Occupied33]
               || Svc.Condition[ConditionFlag.Occupied38]
               || Svc.Condition[ConditionFlag.Occupied39]
               || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
               || Svc.Condition[ConditionFlag.OccupiedInEvent]
               || Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
               || Svc.Condition[ConditionFlag.OccupiedSummoningBell]
               || Svc.Condition[ConditionFlag.WatchingCutscene]
               || Svc.Condition[ConditionFlag.WatchingCutscene78]
               || Svc.Condition[ConditionFlag.BetweenAreas]
               || Svc.Condition[ConditionFlag.BetweenAreas51]
               || Svc.Condition[ConditionFlag.InThatPosition]
               //|| Svc.Condition[ConditionFlag.TradeOpen]
               || Svc.Condition[ConditionFlag.Crafting]
               || Svc.Condition[ConditionFlag.Crafting40]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.InThatPosition]
               || Svc.Condition[ConditionFlag.Unconscious]
               || Svc.Condition[ConditionFlag.MeldingMateria]
               || Svc.Condition[ConditionFlag.Gathering]
               || Svc.Condition[ConditionFlag.OperatingSiegeMachine]
               || Svc.Condition[ConditionFlag.CarryingItem]
               || Svc.Condition[ConditionFlag.CarryingObject]
               || Svc.Condition[ConditionFlag.BeingMoved]
               || Svc.Condition[ConditionFlag.Mounted2]
               || Svc.Condition[ConditionFlag.Mounting]
               || Svc.Condition[ConditionFlag.Mounting71]
               || Svc.Condition[ConditionFlag.ParticipatingInCustomMatch]
               || Svc.Condition[ConditionFlag.PlayingLordOfVerminion]
               || Svc.Condition[ConditionFlag.ChocoboRacing]
               || Svc.Condition[ConditionFlag.PlayingMiniGame]
               || Svc.Condition[ConditionFlag.Performing]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.Fishing]
               || Svc.Condition[ConditionFlag.Transformed]
               || Svc.Condition[ConditionFlag.UsingHousingFunctions]
               || Svc.ClientState.LocalPlayer?.IsTargetable != true;
    }

    public static string ReplaceFirst(this string text, string search, string replace)
    {
        var pos = text.IndexOf(search);
        if(pos < 0)
        {
            return text;
        }
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    /// <summary>
    /// Attempts to parse player in a <see cref="SeString"/>. 
    /// </summary>
    /// <param name="sender"><see cref="SeString"/> from which to read player</param>
    /// <param name="senderStruct">Resulting player data</param>
    /// <returns>Whether operation succeeded</returns>
    public static bool TryDecodeSender(SeString sender, out Sender senderStruct)
    {
        if(sender == null)
        {
            senderStruct = default;
            return false;
        }
        foreach(var x in sender.Payloads)
        {
            if(x is PlayerPayload p)
            {
                senderStruct = new(p.PlayerName, p.World.RowId);
                return true;
            }
        }
        senderStruct = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkUnitBase* Addon)
    {
        return Addon->IsVisible && Addon->UldManager.LoadedState == AtkLoadState.Loaded && Addon->IsFullyLoaded();
    }

    public static bool IsReady(this AtkUnitBase Addon) => Addon.IsVisible && Addon.UldManager.LoadedState == AtkLoadState.Loaded && Addon.IsFullyLoaded();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkComponentNode* Addon)
    {
        return Addon->AtkResNode.IsVisible() && Addon->Component->UldManager.LoadedState == AtkLoadState.Loaded;
    }

    /// <summary>
    /// Gets a node given a chain of node IDs
    /// </summary>
    /// <param name="node">Root node of the addon</param>
    /// <param name="ids">Node IDs (starting from root) to the desired node</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params int[] ids)
    {
        if (node == null || ids.Length <= 0)
            return null;

        if (node->NodeId == ids[0])
        {
            if (ids.Length == 1)
                return node;

            var newList = new List<int>(ids);
            newList.RemoveAt(0);

            var childNode = node->ChildNode;
            if (childNode != null)
                return GetNodeByIDChain(childNode, [.. newList]);

            if ((int)node->Type >= 1000)
            {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;
                childNode = uldManager.NodeList[0];
                return childNode == null ? null : GetNodeByIDChain(childNode, [.. newList]);
            }

            return null;
        }

        //check siblings
        var sibNode = node->PrevSiblingNode;
        return sibNode != null ? GetNodeByIDChain(sibNode, ids) : null;
    }

    /// <summary>
    /// Recursively gets the root node of an addon
    /// </summary>
    /// <param name="node">Starting node to search from</param>
    /// <returns></returns>
    public static unsafe AtkResNode* GetRootNode(AtkResNode* node)
    {
        var parent = node->ParentNode;
        return parent == null ? node : GetRootNode(parent);
    }
    
    /// <summary>
    /// Removes whitespaces, line breaks, tabs, etc from string.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string Cleanup(this string s)
    {
        StringBuilder sb = new(s.Length);
        foreach(var c in s)
        {
            if(c == ' ' || c == '\n' || c == '\r' || c == '\t') continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    [Obsolete("Dalamud has added their own ExtractText method for Lumina strings that is not compatible with ECommons. Therefore, extension method can not be used on Lumina strings anymore. For the consistency, ExtractText method is renamed to GetText.")]
    public static string ExtractText(this ReadOnlySeString s, bool onlyFirst = false) => s.GetText(onlyFirst);
    /// <summary>
    /// Discards any non-text payloads from <see cref="SeString"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="onlyFirst">Whether to find first text payload and only return it</param>
    /// <returns>String that only includes text payloads</returns>
    public static string GetText(this ReadOnlySeString s, bool onlyFirst = false)
    {
        return s.ToDalamudString().GetText(onlyFirst);
    }

    [Obsolete("Dalamud has added their own ExtractText method for Lumina strings that is not compatible with ECommons. Therefore, extension method can not be used on Lumina strings anymore. For the consistency, ExtractText method is renamed to GetText.")]
    public static string ExtractText(this Utf8String s, bool onlyFirst = false) => s.GetText(onlyFirst);
    /// <summary>
    /// Reads SeString from unmanaged memory and discards any non-text payloads from <see cref="SeString"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="onlyFirst">Whether to find first text payload and only return it</param>
    /// <returns>String that only includes text payloads</returns>
    public static string GetText(this Utf8String s, bool onlyFirst = false)
    {
        var str = GenericHelpers.ReadSeString(&s);
        return str.GetText(false);
    }

    [Obsolete("Dalamud has added their own ExtractText method for Lumina strings that is not compatible with ECommons. Therefore, extension method can not be used on Lumina strings anymore. For the consistency, ExtractText method is renamed to GetText.")]
    public static string ExtractText(this SeString seStr, bool onlyFirst = false) => seStr.GetText(onlyFirst);
    /// <summary>
    /// Discards any non-text payloads from <see cref="SeString"/>
    /// </summary>
    /// <param name="seStr"></param>
    /// <param name="onlyFirst">Whether to find first text payload and only return it</param>
    /// <returns>String that only includes text payloads</returns>
    public static string GetText(this SeString seStr, bool onlyFirst = false)
    {
        StringBuilder sb = new();
        foreach(var x in seStr.Payloads)
        {
            if(x is TextPayload tp)
            {
                sb.Append(tp.Text);
                if(onlyFirst) break;
            }
            if(x.Type == PayloadType.Unknown && x.Encode().SequenceEqual<byte>([0x02, 0x1d, 0x01, 0x03]))
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithAny(this string source, params string[] values)
    {
        return source.StartsWithAny(values, StringComparison.Ordinal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithAny(this string source, StringComparison stringComparison = StringComparison.Ordinal, params string[] values)
    {
        return source.StartsWithAny(values, stringComparison);
    }

    public static bool StartsWithAny(this string source, IEnumerable<string> compareTo, StringComparison stringComparison = StringComparison.Ordinal)
    {
        foreach(var x in compareTo)
        {
            if(source.StartsWith(x, stringComparison)) return true;
        }
        return false;
    }

    public static SeStringBuilder Add(this SeStringBuilder b, IEnumerable<Payload> payloads)
    {
        foreach(var x in payloads)
        {
            b = b.Add(x);
        }
        return b;
    }

    /// <summary>
    /// Adds <paramref name="value"/> into HashSet if it doesn't exists yet or removes if it exists.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hashSet"></param>
    /// <param name="value"></param>
    /// <returns>Whether <paramref name="hashSet"/> contains <paramref name="value"/> after function has been executed.</returns>
    public static bool Toggle<T>(this HashSet<T> hashSet, T value)
    {
        if(hashSet.Contains(value))
        {
            hashSet.Remove(value);
            return false;
        }
        else
        {
            hashSet.Add(value);
            return true;
        }
    }

    public static bool Toggle<T>(this List<T> list, T value)
    {
        if(list.Contains(value))
        {
            list.RemoveAll(x => x.Equals(value));
            return false;
        }
        else
        {
            list.Add(value);
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> Split(this string str, int chunkSize)
    {
        return Enumerable.Range(0, str.Length / chunkSize)
            .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

    public static string GetTerritoryName(this Number terr)
    {
        var t = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(terr);
        return $"{terr} | {t?.ContentFinderCondition.ValueNullable?.Name.ToString().Default(t?.PlaceName.ValueNullable?.Name.ToString())}";
    }

    public static T FirstOr0<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
    {
        foreach(var x in collection)
        {
            if(predicate(x))
            {
                return x;
            }
        }
        return collection.First();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Default(this string s, string defaultValue)
    {
        if(string.IsNullOrEmpty(s)) return defaultValue;
        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCase(this string s, string other)
    {
        return s.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NullWhenEmpty(this string s)
    {
        return s == string.Empty ? null : s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(this string s)
    {
        return string.IsNullOrEmpty(s);
    }

    public static IEnumerable<R> SelectMulti<T, R>(this IEnumerable<T> values, params Func<T, R>[] funcs)
    {
        foreach(var v in values)
            foreach(var x in funcs)
            {
                yield return x(v);
            }
    }

    [Obsolete($"Please use ExcelWorldHelper.TryGetWorldByName")]
    public static bool TryGetWorldByName(string world, out Lumina.Excel.Sheets.World worldId) => ExcelWorldHelper.TryGetWorldByName(world, out worldId);

    public static Vector4 Invert(this Vector4 v)
    {
        return v with { X = 1f - v.X, Y = 1f - v.Y, Z = 1f - v.Z };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(this Vector4 color)
    {
        return ImGui.ColorConvertFloat4ToU32(color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVector4(this uint color)
    {
        return ImGui.ColorConvertU32ToFloat4(color);
    }

    public static ref int ValidateRange(this ref int i, int min, int max)
    {
        if(i > max) i = max;
        if(i < min) i = min;
        return ref i;
    }

    public static ref float ValidateRange(this ref float i, float min, float max)
    {
        if(i > max) i = max;
        if(i < min) i = min;
        return ref i;
    }

    public static string ToStringFull(this Exception e)
    {
        var str = new StringBuilder($"{e.Message}\n{e.StackTrace}");
        var inner = e.InnerException;
        for(var i = 1; inner != null; i++)
        {
            str.Append($"\nAn inner exception ({i}) was thrown: {e.Message}\n{e.StackTrace}");
            inner = inner.InnerException;
        }
        return str.ToString();
    }

    public static void Log(this Exception e, Action<string> exceptionFunc)
    {
        exceptionFunc(e.ToStringFull());
    }

    public static void Log(this Exception e) => e.Log(PluginLog.Error);
    public static void Log(this Exception e, string ErrorMessage)
    {
        PluginLog.Error($"{ErrorMessage}");
        e.Log(PluginLog.Error);
    }

    public static void LogFatal(this Exception e) => e.Log(PluginLog.Fatal);
    public static void LogFatal(this Exception e, string ErrorMessage)
    {
        PluginLog.Fatal($"{ErrorMessage}");
        e.Log(PluginLog.Fatal);
    }

    public static void LogWarning(this Exception e) => e.Log(PluginLog.Warning);
    public static void LogWarning(this Exception e, string errorMessage)
    {
        PluginLog.Warning(errorMessage);
        e.Log(PluginLog.Warning);
    }

    public static void LogVerbose(this Exception e) => e.Log(PluginLog.Verbose);
    public static void LogVerbose(this Exception e, string ErrorMessage)
    {
        PluginLog.Verbose(ErrorMessage);
        e.Log(PluginLog.Verbose);
    }

    public static void LogInternal(this Exception e) => e.Log(InternalLog.Error);
    public static void LogInternal(this Exception e, string ErrorMessage)
    {
        InternalLog.Error(ErrorMessage);
        e.Log(InternalLog.Error);
    }

    public static void LogDebug(this Exception e) => e.Log(PluginLog.Debug);
    public static void LogDebug(this Exception e, string ErrorMessage)
    {
        PluginLog.Debug(ErrorMessage);
        e.Log(PluginLog.Debug);
    }

    public static void LogInfo(this Exception e) => e.Log(PluginLog.Information);
    public static void LogInfo(this Exception e, string ErrorMessage)
    {
        PluginLog.Information(ErrorMessage);
        e.Log(PluginLog.Information);
    }
    public static void LogDuo(this Exception e) => e.Log(DuoLog.Error);

    public static bool IsNoConditions()
    {
        if(!Svc.Condition[ConditionFlag.NormalConditions]) return false;
        for(var i = 2; i < 100; i++)
        {
            if(i == (int)ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance) continue;
            if(Svc.Condition[i]) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Invert(this bool b, bool invert)
    {
        return invert ? !b : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> values)
    {
        foreach(var x in values)
        {
            if(!source.Contains(x)) return false;
        }
        return true;
    }

    public static void ShellStart(string s)
    {
        Safe(delegate
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = s,
                UseShellExecute = true
            });
        }, (e) =>
        {
            Notify.Error($"Could not open {s.Cut(60)}\n{e}");
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Cut(this string s, int num)
    {
        if(s.Length <= num) return s;
        return s[0..num] + "...";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetParsedSeSetingColor(int percent)
    {
        if(percent < 25)
        {
            return 3;
        }
        else if(percent < 50)
        {
            return 45;
        }
        else if(percent < 75)
        {
            return 37;
        }
        else if(percent < 95)
        {
            return 541;
        }
        else if(percent < 99)
        {
            return 500;
        }
        else if(percent == 99)
        {
            return 561;
        }
        else if(percent == 100)
        {
            return 573;
        }
        else
        {
            return 518;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Repeat(this string s, int num)
    {
        StringBuilder str = new();
        for(var i = 0; i < num; i++)
        {
            str.Append(s);
        }
        return str.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IEnumerable<string> e, string separator)
    {
        return string.Join(separator, e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Safe(System.Action a, bool suppressErrors = false)
    {
        try
        {
            a();
        }
        catch(Exception e)
        {
            if(!suppressErrors) PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Safe(System.Action a, Action<string, object[]> logAction)
    {
        try
        {
            a();
        }
        catch(Exception e)
        {
            logAction($"{e.Message}\n{e.StackTrace ?? ""}", Array.Empty<object>());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Safe(System.Action a, Action<string> fail, bool suppressErrors = false)
    {
        try
        {
            a();
        }
        catch(Exception e)
        {
            try
            {
                fail(e.Message);
            }
            catch(Exception ex)
            {
                PluginLog.Error("Error while trying to process error handler:");
                PluginLog.Error($"{ex.Message}\n{ex.StackTrace ?? ""}");
                suppressErrors = false;
            }
            if(!suppressErrors) PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }

    public static bool TryExecute(System.Action a)
    {
        try
        {
            a();
            return true;
        }
        catch(Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
            return false;
        }
    }

    public static bool TryExecute<T>(Func<T> a, out T result)
    {
        try
        {
            result = a();
            return true;
        }
        catch(Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
            result = default;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAny<T>(this IEnumerable<T> obj, params T[] values)
    {
        foreach(var x in values)
        {
            if(obj.Contains(x))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAny<T>(this IEnumerable<T> obj, IEnumerable<T> values)
    {
        foreach(var x in values)
        {
            if(obj.Contains(x))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAny(this string obj, IEnumerable<string> values)
    {
        foreach(var x in values)
        {
            if(obj.Contains(x))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAny(this string obj, params string[] values)
    {
        foreach(var x in values)
        {
            if(obj.Contains(x))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAny(this string obj, StringComparison comp, params string[] values)
    {
        foreach(var x in values)
        {
            if(obj.Contains(x, comp))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsAny<T>(this T obj, params T[] values)
    {
        return values.Any(x => x.Equals(obj));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCaseAny(this string obj, params string[] values)
    {
        return values.Any(x => x.Equals(obj, StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCaseAny(this string obj, IEnumerable<string> values)
    {
        return values.Any(x => x.Equals(obj, StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsAny<T>(this T obj, IEnumerable<T> values)
    {
        return values.Any(x => x.Equals(obj));
    }

    public static uint ToUInt(this ushort value) => value;
    public static uint ToUInt(this byte value) => value;
    public static uint ToUInt(this int value) => (uint)value;
    public static int ToInt(this byte value) => value;
    public static int ToInt(this ushort value) => value;
    public static int ToInt(this uint value) => (int)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AllNull(params object[] objects) => objects.All(s => s == null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AnyNull(params object[] objects) => objects.Any(s => s == null);

    public static IEnumerable<K> FindKeysByValue<K, V>(this IDictionary<K, V> dictionary, V value)
    {
        foreach(var x in dictionary)
        {
            if(value.Equals(x.Value))
            {
                yield return x.Key;
            }
        }
    }

    public static bool TryGetFirst<K, V>(this IDictionary<K, V> dictionary, Func<KeyValuePair<K, V>, bool> predicate, out KeyValuePair<K, V> keyValuePair)
    {
        try
        {
            keyValuePair = dictionary.First(predicate);
            return true;
        }
        catch(Exception)
        {
            keyValuePair = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get first element of <see cref="IEnumerable"/>.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetFirst<TSource>(this IEnumerable<TSource> source, out TSource value)
    {
        if(source == null)
        {
            value = default;
            return false;
        }
        var list = source as IList<TSource>;
        if(list != null)
        {
            if(list.Count > 0)
            {
                value = list[0];
                return true;
            }
        }
        else
        {
            using(var e = source.GetEnumerator())
            {
                if(e.MoveNext())
                {
                    value = e.Current;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get first element of IEnumerable
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate">Function to test elements.</param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource value)
    {
        if(source == null)
        {
            value = default;
            return false;
        }
        if(predicate == null)
        {
            value = default;
            return false;
        }
        foreach(var element in source)
        {
            if(predicate(element))
            {
                value = element;
                return true;
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get last element of <see cref="IEnumerable"/>.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="predicate">Function to test elements.</param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetLast<K>(this IEnumerable<K> enumerable, Func<K, bool> predicate, out K value)
    {
        try
        {
            value = enumerable.Last(predicate);
            return true;
        }
        catch(Exception)
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Slower than <see cref="TryGetAddonByName"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="addon"></param>
    /// <param name="addonMaster"></param>
    /// <returns></returns>
    public static bool TryGetAddonMaster<T>(string addon, out T addonMaster) where T : IAddonMasterBase
    {
        if(TryGetAddonByName<AtkUnitBase>(addon, out var ptr))
        {
            addonMaster = (T)Activator.CreateInstance(typeof(T), (nint)ptr);
            return true;
        }
        addonMaster = default;
        return false;
    }

    public static bool TryGetAddonMaster<T>(out T addonMaster) where T : IAddonMasterBase
    {
        if(TryGetAddonByName<AtkUnitBase>(typeof(T).Name.Split(".")[^1], out var ptr))
        {
            addonMaster = (T)Activator.CreateInstance(typeof(T), (nint)ptr);
            return true;
        }
        addonMaster = default;
        return false;
    }

    /// <summary>
    /// Attempts to get first instance of addon by name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Addon"></param>
    /// <param name="AddonPtr"></param>
    /// <returns></returns>
    public static bool TryGetAddonByName<T>(string Addon, out T* AddonPtr) where T : unmanaged
    {
        var a = Svc.GameGui.GetAddonByName(Addon, 1);
        if(a == IntPtr.Zero)
        {
            AddonPtr = null;
            return false;
        }
        else
        {
            AddonPtr = (T*)a;
            return true;
        }
    }

    /// <summary>
    /// Attempts to find out whether SelectString entry is enabled based on text color. 
    /// </summary>
    /// <param name="textNodePtr"></param>
    /// <returns></returns>
    [Obsolete("Incompatible with UI mods, use other methods")]
    public static bool IsSelectItemEnabled(AtkTextNode* textNodePtr)
    {
        var col = textNodePtr->TextColor;
        //EEE1C5FF
        return (col.A == 0xFF && col.R == 0xEE && col.G == 0xE1 && col.B == 0xC5)
            //7D523BFF
            || (col.A == 0xFF && col.R == 0x7D && col.G == 0x52 && col.B == 0x3B)
            || (col.A == 0xFF && col.R == 0xFF && col.G == 0xFF && col.B == 0xFF)
            // EEE1C5FF
            || (col.A == 0xFF && col.R == 0xEE && col.G == 0xE1 && col.B == 0xC5);
    }

    public static void MoveItemToPosition<T>(IList<T> list, Func<T, bool> sourceItemSelector, int targetedIndex)
    {
        var sourceIndex = -1;
        for(var i = 0; i < list.Count; i++)
        {
            if(sourceItemSelector(list[i]))
            {
                sourceIndex = i;
                break;
            }
        }
        if(sourceIndex == targetedIndex) return;
        var item = list[sourceIndex];
        list.RemoveAt(sourceIndex);
        list.Insert(targetedIndex, item);
    }

    public static void SetMinSize(this Window window, float width = 100, float height = 100) => SetMinSize(window, new Vector2(width, height));

    public static void SetMinSize(this Window window, Vector2 minSize)
    {
        window.SizeConstraints = new()
        {
            MinimumSize = minSize,
            MaximumSize = new Vector2(float.MaxValue)
        };
    }

    public static void SetSizeConstraints(this Window window, Vector2 minSize, Vector2 maxSize)
    {
        window.SizeConstraints = new()
        {
            MinimumSize = minSize,
            MaximumSize = maxSize
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete($"Use MemoryHelper.ReadSeString")]
    public static SeString ReadSeString(IntPtr memoryAddress, int maxLength) => GenericHelpers.ReadSeString(memoryAddress, maxLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete($"Use MemoryHelper.ReadRaw")]
    public static void ReadRaw(IntPtr memoryAddress, int length, out byte[] value) => value = MemoryHelper.ReadRaw(memoryAddress, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete($"Use MemoryHelper.ReadRaw")]
    public static byte[] ReadRaw(IntPtr memoryAddress, int length) => MemoryHelper.ReadRaw(memoryAddress, length);

    public static ExcelSheet<T> GetSheet<T>(ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => Svc.Data.GetExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage);

    public static SubrowExcelSheet<T> GetSubrowSheet<T>(ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => Svc.Data.GetSubrowExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage);

    public static int GetRowCount<T>() where T : struct, IExcelRow<T>
        => GetSheet<T>().Count;

    public static T? GetRow<T>(uint rowId, ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => GetSheet<T>(language).GetRowOrDefault(rowId);

    public static T? GetRow<T>(uint rowId, ushort subRowId, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>(language).GetSubrowOrDefault(rowId, subRowId);

    public static SubrowCollection<T>? GetSubRow<T>(uint rowId, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>(language).GetRowOrDefault(rowId);

    public static T? FindRow<T>(Func<T, bool> predicate) where T : struct, IExcelRow<T>
         => GetSheet<T>().FirstOrNull(predicate);

    public static T? FindRow<T>(Func<T, bool> predicate, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>(language).SelectMany(m => m).Cast<T?>().FirstOrDefault(t => predicate(t.Value), null);

    public static T[] FindRows<T>(Func<T, bool> predicate) where T : struct, IExcelRow<T>
        => GetSheet<T>().Where(predicate).ToArray();

    public static bool TryGetRow<T>(uint rowId, [NotNullWhen(returnValue: true)] out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>().TryGetRow(rowId, out row);

    public static bool TryGetRow<T>(uint rowId, ClientLanguage? language, [NotNullWhen(returnValue: true)] out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>(language).TryGetRow(rowId, out row);

    public static bool TryGetRow<T>(uint rowId, ushort subRowId, [NotNullWhen(returnValue: true)] out T row) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>().TryGetSubrow(rowId, subRowId, out row);

    public static bool TryGetRow<T>(uint rowId, ushort subRowId, ClientLanguage? language, [NotNullWhen(returnValue: true)] out T row) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>(language).TryGetSubrow(rowId, subRowId, out row);

    public static bool TryFindRow<T>(Predicate<T> predicate, out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>().TryGetFirst(predicate, out row);

    public static bool TryFindRow<T>(Predicate<T> predicate, ClientLanguage? language, out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>(language).TryGetFirst(predicate, out row);

    public static IEnumerable<T> AllRows<T>(this SubrowExcelSheet<T> subrowSheet) where T:struct, IExcelSubrow<T>
    {
        foreach(var x in subrowSheet)
        {
            foreach(var z in x)
            {
                yield return z;
            }
        }
    }
}
