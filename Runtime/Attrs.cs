namespace SpacetimeDB;

using System;
using SpacetimeDB.Module;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ReducerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TypeAttribute : Attribute { }

public interface TaggedEnum<Variants>
    where Variants : struct { }

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ColumnIndexAttribute : Attribute
{
    public ColumnIndexAttribute(ColumnIndexKind type)
    {
        Type = type;
    }

    public ColumnIndexKind Type { get; }
}
