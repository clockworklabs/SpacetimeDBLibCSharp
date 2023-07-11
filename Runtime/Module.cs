namespace SpacetimeDB.Module;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpacetimeDB.SATS;

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
    // "system" | "user"
    string TableType;
    // "public" | "private"
    string TableAccess;

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
        TableType = "user";
        TableAccess = name.StartsWith('_') ? "private" : "public";
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
    List<AlgebraicType> Types = new();
    List<TableDef> Tables = new();
    List<ReducerDef> Reducers = new();
    List<MiscModuleExport> MiscExports = new();

    public ModuleDef() { }

    public AlgebraicTypeRef AllocTypeRef() {
        var index = Types.Count;
        var typeRef = new AlgebraicTypeRef(index);
        // uninhabited type, to be replaced by a real type
        Types.Add(new SumType());
        return typeRef;
    }

    // Note: this intends to generate a valid identifier, but it's not guaranteed to be unique as it's not proper mangling.
    // Fix it up to a different mangling scheme if it causes problems.
    private static string GetFriendlyName(Type type) =>
        type.IsGenericType
            ? $"{type.Name.Remove(type.Name.IndexOf('`'))}_{string.Join("_", type.GetGenericArguments().Select(GetFriendlyName))}"
            : type.Name;

    public void SetTypeRef<T>(AlgebraicTypeRef typeRef, AlgebraicType type)
    {
        Types[typeRef.TypeRef] = type;
        MiscExports.Add(
            new MiscModuleExport
            {
                TypeAlias = new TypeAlias { Name = GetFriendlyName(typeof(T)), Type = typeRef }
            }
        );
    }

    public void Add(TableDef table)
    {
        Tables.Add(table);
    }

    public void Add(ReducerDef reducer)
    {
        Reducers.Add(reducer);
    }
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

    /// Primary key column (implies Unique)
    PrimaryKey = 5,

    /// PrimaryKey + AutoInc
    PrimaryKeyAuto = 6,
}

public static class ReducerKind
{
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
    private static List<IReducer> reducers = new();
    private static ModuleDef module = new();

    public static void RegisterReducer(IReducer reducer)
    {
        reducers.Add(reducer);
        module.Add(reducer.MakeReducerDef());
    }

    public static void RegisterTable(TableDef table) => module.Add(table);

    public static AlgebraicTypeRef AllocTypeRef() => module.AllocTypeRef();

    public static void SetTypeRef<T>(AlgebraicTypeRef typeRef, AlgebraicType type) => module.SetTypeRef<T>(typeRef, type);

    // Note: this is accessed by C bindings.
    private static byte[] DescribeModule() {
        // replace `module` with a temporary internal module that will register ModuleDef, AlgebraicType and other internal types
        // during the ModuleDef.GetSatsTypeInfo() instead of exposing them via user's module.
        var userModule = module;
        try {
            module = new();
            return ModuleDef.GetSatsTypeInfo().ToBytes(userModule);
        } finally {
            module = userModule;
        }
    }

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
            reducers[(int)id].Invoke(reader, new(sender_identity, timestamp));
            if (stream.Position != stream.Length)
            {
                throw new Exception("Unrecognised extra bytes in the reducer arguments");
            }
            return null;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}
