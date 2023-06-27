namespace SpacetimeDB.Module;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpacetimeDB.SATS;

[SpacetimeDB.Type]
partial struct Typespace
{
    List<AlgebraicType> Types = new();

    public Typespace() { }

    public AlgebraicTypeRef Add(AlgebraicType type)
    {
        var index = Types.Count;
        Types.Add(type);
        return new AlgebraicTypeRef(index);
    }
}

[SpacetimeDB.Type]
public partial struct IndexDef
{
    string Name;
    Runtime.IndexType Type;
    byte[] ColumnIds;

    public IndexDef(string name, Runtime.IndexType type, byte[] columnIds)
    {
        Name = name;
        Type = type;
        ColumnIds = columnIds;
    }
}

[SpacetimeDB.Type]
public partial struct TableDef
{
    string Name;
    AlgebraicTypeRef Data;
    ColumnIndexKind[] ColumnAttrs;
    IndexDef[] Indices;

    public TableDef(
        string name,
        AlgebraicTypeRef type,
        ColumnIndexKind[] columnAttrs,
        IndexDef[] indices
    )
    {
        Name = name;
        Data = type;
        ColumnAttrs = columnAttrs;
        Indices = indices;
    }
}

[SpacetimeDB.Type]
public partial struct ReducerDef
{
    string Name;
    ProductTypeElement[] Args;

    public ReducerDef(string name, params ProductTypeElement[] args)
    {
        Name = name;
        Args = args;
    }
}

[SpacetimeDB.Type]
partial struct TypeAlias
{
    internal string Name;
    internal AlgebraicTypeRef Type;
}

[SpacetimeDB.Type]
partial struct MiscModuleExport : SpacetimeDB.TaggedEnum<(TypeAlias TypeAlias, Unit _Reserved)> { }

[SpacetimeDB.Type]
public partial struct ModuleDef
{
    Typespace Typespace = new();
    List<TableDef> Tables = new();
    List<ReducerDef> Reducers = new();
    List<MiscModuleExport> MiscExports = new();

    public ModuleDef() { }

    public AlgebraicTypeRef AddType(AlgebraicType type)
    {
        return Typespace.Add(type);
    }

    public void Add(TableDef table)
    {
        Tables.Add(table);
    }

    public void Add(ReducerDef reducer)
    {
        Reducers.Add(reducer);
    }

    // TODO: support complex named types (aliases).
}

[SpacetimeDB.Type]
public enum ColumnIndexKind : byte
{
    UnSet,

    /// Unique + AutoInc
    Identity,

    /// Index unique
    Unique,

    ///  Index no unique
    Indexed,

    /// Generate the next [Sequence]
    AutoInc,
}

public static class ReducerKind {
    public const string Init = "__init__";
    public const string Update = "__update__";
}

public interface IReducer
{
    SpacetimeDB.Module.ReducerDef MakeReducerDef();
    void Invoke(System.IO.BinaryReader reader, Runtime.DbEventArgs args);
}

public static class FFI
{
    private readonly static List<IReducer> Reducers = new();
    private static ModuleDef Module = new();
    private static Dictionary<System.Type, object> registeredTypes = new();

    public static void RegisterReducer(IReducer reducer)
    {
        Reducers.Add(reducer);
        Module.Add(reducer.MakeReducerDef());
    }

    public static void RegisterTable(TableDef table) => Module.Add(table);

    public static AlgebraicTypeRef RegisterType(AlgebraicType type) => Module.AddType(type);

    // Note: this is accessed by C bindings.
    private static byte[] DescribeModule() => ModuleDef.GetSatsTypeInfo().ToBytes(Module);

    // Note: this is accessed by C bindings.
    private static string? CallReducer(
        uint id,
        byte[] sender_identity,
        ulong timestamp,
        byte[] args
    )
    {
        try
        {
            using var stream = new MemoryStream(args);
            using var reader = new BinaryReader(stream);
            Reducers[(int)id].Invoke(reader, new(sender_identity, timestamp));
            if (stream.Position != stream.Length)
            {
                throw new Exception("Extra bytes in the input");
            }
            return null;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}
