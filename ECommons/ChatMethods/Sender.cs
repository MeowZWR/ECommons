using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.GeneratedSheets;

namespace ECommons.ChatMethods;
#nullable disable

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public struct Sender : IEquatable<Sender>
{
    public string Name;

    public uint HomeWorld;

    public static bool TryParse(string nameWithWorld, out Sender s)
    {
        string[] array = nameWithWorld.Split('@');
        if (array.Length == 2)
        {
            World world = ExcelWorldHelper.Get(array[1]);
            if (world != null)
            {
                s = new Sender(array[0], world.RowId);
                return true;
            }
        }

        s = default(Sender);
        return false;
    }

    public Sender(string Name, uint HomeWorld)
    {
        this.Name = Name;
        this.HomeWorld = HomeWorld;
    }

    public Sender(SeString Name, uint HomeWorld)
        : this(Name.ToString(), HomeWorld)
    {
    }

    public Sender(SeString Name, Dalamud.Game.ClientState.Resolvers.ExcelResolver<World> HomeWorld)
        : this(Name.ToString(), HomeWorld.Id)
    {
    }

    public Sender(string Name, Dalamud.Game.ClientState.Resolvers.ExcelResolver<World> HomeWorld)
        : this(Name, HomeWorld.Id)
    {
    }

    public Sender(IPlayerCharacter pc)
        : this(pc.Name, pc.HomeWorld)
    {
    }

    public override bool Equals(object obj)
    {
        if (obj is Sender other)
        {
            return Equals(other);
        }

        return false;
    }

    public bool Equals(Sender other)
    {
        if (Name == other.Name)
        {
            return HomeWorld == other.HomeWorld;
        }

        return false;
    }

    public IPlayerCharacter? Find()
    {
        foreach (IGameObject @object in Svc.Objects)
        {
            if (@object is IPlayerCharacter playerCharacter && playerCharacter.Name.ToString() == Name && playerCharacter.HomeWorld.Id == HomeWorld)
            {
                return playerCharacter;
            }
        }

        return null;
    }

    public bool TryFind([NotNullWhen(true)] out IPlayerCharacter pc)
    {
        pc = Find();
        return pc != null;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, HomeWorld);
    }

    public override string ToString()
    {
        return $"{Name}@{Svc.Data.GetExcelSheet<World>()?.GetRow(HomeWorld)?.Name}";
    }

    public static bool operator ==(Sender left, Sender right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Sender left, Sender right)
    {
        return !(left == right);
    }
}
