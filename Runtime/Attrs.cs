namespace SpacetimeDB;

using System;
using SpacetimeDB.Module;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ReducerAttribute : Attribute {
    public ReducerAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; set; }

    // Special reserved reducer names.
    public const string Init = "__init__";
    public const string Update = "__update__";
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class TypeAttribute : Attribute { }

public interface TaggedEnum<Variants>
    where Variants : struct { }

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ColumnAttribute : Attribute
{
    public ColumnAttribute(ColumnAttrs type)
    {
        Type = type;
    }

    public ColumnAttrs Type { get; }
}
