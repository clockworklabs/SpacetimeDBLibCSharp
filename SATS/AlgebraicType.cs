using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpacetimeDB.SATS
{
    [SpacetimeDB.Type]
    public partial struct Unit { }

    public class TypeInfo<T>
    {
        public readonly AlgebraicType algebraicType;
        public readonly Func<BinaryReader, T> read;
        public readonly Action<BinaryWriter, T> write;

        public TypeInfo(
            AlgebraicType algebraicType,
            Func<BinaryReader, T> read,
            Action<BinaryWriter, T> write
        )
        {
            this.algebraicType = algebraicType;
            this.read = read;
            this.write = write;
        }

        public T ReadBytes(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            var value = read(reader);
            if (stream.Position != stream.Length)
            {
                throw new Exception("Extra bytes in the input");
            }
            return value;
        }

        public byte[] ToBytes(T value)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer, value);
            return stream.ToArray();
        }
    }

    public static class Typespace
    {
        private static Dictionary<System.Type, object> registeredTypes = new();

        public static TypeInfo<T> RegisterType<T>(Func<TypeInfo<T>> createType)
        {
            if (registeredTypes.TryGetValue(typeof(T), out var typeInfoObj))
            {
                return (TypeInfo<T>)typeInfoObj;
            }
            var typeInfo = createType();
            registeredTypes.Add(typeof(T), typeInfo);
            return typeInfo;
        }
    }

    [SpacetimeDB.Type]
    partial struct Option<T> : SpacetimeDB.TaggedEnum<(T Some, Unit None)> { }

    [SpacetimeDB.Type]
    public partial struct SumType : IEnumerable<SumTypeVariant>
    {
        public List<SumTypeVariant> variants = new();

        public SumType() { }

        public IEnumerator<SumTypeVariant> GetEnumerator()
        {
            return variants.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string name, AlgebraicType algebraicType)
        {
            variants.Add(new SumTypeVariant(name, algebraicType));
        }

        public static TypeInfo<T?> MakeOption<T>(TypeInfo<T> typeInfo)
            where T : class
        {
            var reprTypeInfo = Option<T>.GetSatsTypeInfo(typeInfo);

            return new TypeInfo<T?>(
                reprTypeInfo.algebraicType,
                (reader) =>
                {
                    var repr = reprTypeInfo.read(reader);
                    return repr.IsSome ? repr.Some : null;
                },
                (writer, value) =>
                {
                    var repr = value switch
                    {
                        null => new Option<T> { None = new Unit() },
                        _ => new Option<T> { Some = value }
                    };
                    reprTypeInfo.write(writer, repr);
                }
            );
        }
    }

    [SpacetimeDB.Type]
    public partial struct SumTypeVariant
    {
        public string? name;
        public AlgebraicTypeBox algebraicType;

        public SumTypeVariant(string name, AlgebraicType algebraicType)
        {
            this.name = name;
            this.algebraicType = new AlgebraicTypeBox(algebraicType);
        }
    }

    [SpacetimeDB.Type]
    public partial struct ProductType : IEnumerable<ProductTypeElement>
    {
        public List<ProductTypeElement> elements = new();

        public ProductType() { }

        public IEnumerator<ProductTypeElement> GetEnumerator()
        {
            return elements.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string name, AlgebraicType algebraicType)
        {
            elements.Add(new ProductTypeElement(name, algebraicType));
        }
    }

    [SpacetimeDB.Type]
    public partial struct ProductTypeElement
    {
        public string? name;
        public AlgebraicTypeBox algebraicType;

        public ProductTypeElement(string name, AlgebraicType algebraicType)
        {
            this.name = name;
            this.algebraicType = new AlgebraicTypeBox(algebraicType);
        }
    }

    [SpacetimeDB.Type]
    public partial struct MapType
    {
        public AlgebraicTypeBox key;
        public AlgebraicTypeBox value;

        public MapType(AlgebraicType key, AlgebraicType value)
        {
            this.key = new AlgebraicTypeBox(key);
            this.value = new AlgebraicTypeBox(value);
        }
    }

    [SpacetimeDB.Type]
    public partial struct BuiltinType
        : SpacetimeDB.TaggedEnum<(
            Unit Bool,
            Unit I8,
            Unit U8,
            Unit I16,
            Unit U16,
            Unit I32,
            Unit U32,
            Unit I64,
            Unit U64,
            Unit I128,
            Unit U128,
            Unit F32,
            Unit F64,
            Unit String,
            AlgebraicTypeBox Array,
            MapType Map
        )>
    {
        public static readonly TypeInfo<bool> BoolTypeInfo = new TypeInfo<bool>(
            new BuiltinType { Bool = default },
            (reader) => reader.ReadBoolean(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<sbyte> I8TypeInfo = new TypeInfo<sbyte>(
            new BuiltinType { I8 = default },
            (reader) => reader.ReadSByte(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<byte> U8TypeInfo = new TypeInfo<byte>(
            new BuiltinType { U8 = default },
            (reader) => reader.ReadByte(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<short> I16TypeInfo = new TypeInfo<short>(
            new BuiltinType { I16 = default },
            (reader) => reader.ReadInt16(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<ushort> U16TypeInfo = new TypeInfo<ushort>(
            new BuiltinType { U16 = default },
            (reader) => reader.ReadUInt16(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<int> I32TypeInfo = new TypeInfo<int>(
            new BuiltinType { I32 = default },
            (reader) => reader.ReadInt32(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<uint> U32TypeInfo = new TypeInfo<uint>(
            new BuiltinType { U32 = default },
            (reader) => reader.ReadUInt32(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<long> I64TypeInfo = new TypeInfo<long>(
            new BuiltinType { I64 = default },
            (reader) => reader.ReadInt64(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<ulong> U64TypeInfo = new TypeInfo<ulong>(
            new BuiltinType { U64 = default },
            (reader) => reader.ReadUInt64(),
            (writer, value) => writer.Write(value)
        );

#if NET7_0_OR_GREATER
        public static readonly TypeInfo<Int128> I128TypeInfo = new TypeInfo<Int128>(
            new BuiltinType { I128 = default },
            (reader) => new Int128(reader.ReadUInt64(), reader.ReadUInt64()),
            (writer, value) =>
            {
                writer.Write((ulong)(value >> 64));
                writer.Write((ulong)value);
            }
        );

        public static readonly TypeInfo<UInt128> U128TypeInfo = new TypeInfo<UInt128>(
            new BuiltinType { U128 = default },
            (reader) => new UInt128(reader.ReadUInt64(), reader.ReadUInt64()),
            (writer, value) =>
            {
                writer.Write((ulong)(value >> 64));
                writer.Write((ulong)value);
            }
        );
#endif

        public static readonly TypeInfo<float> F32TypeInfo = new TypeInfo<float>(
            new BuiltinType { F32 = default },
            (reader) => reader.ReadSingle(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<double> F64TypeInfo = new TypeInfo<double>(
            new BuiltinType { F64 = default },
            (reader) => reader.ReadDouble(),
            (writer, value) => writer.Write(value)
        );

        public static readonly TypeInfo<string> StringTypeInfo = new TypeInfo<string>(
            new BuiltinType { String = default },
            (reader) => reader.ReadString(),
            (writer, value) => writer.Write(value)
        );

        private static IEnumerable<T> ReadEnumerable<T>(
            BinaryReader reader,
            Func<BinaryReader, T> readElement
        )
        {
            var length = reader.ReadInt32();
            return Enumerable.Range(0, length).Select((_) => readElement(reader));
        }

        private static void WriteEnumerable<T>(
            BinaryWriter writer,
            IEnumerable<T> enumerable,
            Action<BinaryWriter, T> writeElement
        )
        {
            writer.Write(enumerable.Count());
            foreach (var element in enumerable)
            {
                writeElement(writer, element);
            }
        }

        public static TypeInfo<T[]> MakeArray<T>(TypeInfo<T> elementTypeInfo)
        {
            return new TypeInfo<T[]>(
                new BuiltinType { Array = new AlgebraicTypeBox(elementTypeInfo.algebraicType) },
                (reader) => ReadEnumerable(reader, elementTypeInfo.read).ToArray(),
                (writer, array) => WriteEnumerable(writer, array, elementTypeInfo.write)
            );
        }

        public static TypeInfo<List<T>> MakeList<T>(TypeInfo<T> elementTypeInfo)
        {
            return new TypeInfo<List<T>>(
                new BuiltinType { Array = new AlgebraicTypeBox(elementTypeInfo.algebraicType) },
                (reader) => ReadEnumerable(reader, elementTypeInfo.read).ToList(),
                (writer, list) => WriteEnumerable(writer, list, elementTypeInfo.write)
            );
        }

        public static TypeInfo<Dictionary<K, V>> MakeMap<K, V>(TypeInfo<K> key, TypeInfo<V> value)
            where K : notnull
        {
            return new TypeInfo<Dictionary<K, V>>(
                new BuiltinType { Map = new MapType(key.algebraicType, value.algebraicType) },
                (reader) =>
                    ReadEnumerable(
                            reader,
                            (reader) => (Key: key.read(reader), Value: value.read(reader))
                        )
                        .ToDictionary((pair) => pair.Key, (pair) => pair.Value),
                (writer, map) =>
                    WriteEnumerable(
                        writer,
                        map,
                        (w, pair) =>
                        {
                            key.write(w, pair.Key);
                            value.write(w, pair.Value);
                        }
                    )
            );
        }
    }

    [SpacetimeDB.Type]
    public partial struct AlgebraicType
        : SpacetimeDB.TaggedEnum<(
            SumType Sum,
            ProductType Product,
            BuiltinType Builtin,
            AlgebraicTypeRef TypeRef
        )>
    {
        public static implicit operator AlgebraicType(SumType sum)
        {
            return new AlgebraicType { Sum = sum };
        }

        public static implicit operator AlgebraicType(ProductType product)
        {
            return new AlgebraicType { Product = product };
        }

        public static implicit operator AlgebraicType(BuiltinType builtin)
        {
            return new AlgebraicType { Builtin = builtin };
        }

        public static implicit operator AlgebraicType(AlgebraicTypeRef typeRef)
        {
            return new AlgebraicType { TypeRef = typeRef };
        }
    }

    [SpacetimeDB.Type]
    public partial struct AlgebraicTypeRef
    {
        public int TypeRef;

        public AlgebraicTypeRef(int typeRef)
        {
            TypeRef = typeRef;
        }
    }

    // This is escape hatch for recursive references to AlgebraicTypeDecl.
    // It's nice to have it as separate class anyway because it needs to be
    // represented differently than AlgebraicTypeDecl itself.
    public class AlgebraicTypeBox
    {
        public AlgebraicType deref;

        public static TypeInfo<AlgebraicTypeBox> GetSatsTypeInfo()
        {
            // Note: AlgebraicType.GetSatsTypeInfo() is intentionally not stored
            // in a variable - it would cause an infinite recursion during
            // AlgebraicType.GetSatsTypeInfo() initialization.
            return Typespace.RegisterType(
                () =>
                    new TypeInfo<AlgebraicTypeBox>(
                        new AlgebraicTypeRef(0),
                        (reader) =>
                            new AlgebraicTypeBox(AlgebraicType.GetSatsTypeInfo().read(reader)),
                        (writer, value) =>
                            AlgebraicType.GetSatsTypeInfo().write(writer, value.deref)
                    )
            );
        }

        public AlgebraicTypeBox(AlgebraicType deref)
        {
            this.deref = deref;
        }
    }
}
