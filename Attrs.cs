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
    public ColumnIndexAttribute(ColumnIndexAttributeKind type)
    {
        Type = type;
    }

    public ColumnIndexAttributeKind Type { get; }
}
