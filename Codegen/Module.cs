namespace SpacetimeDB.Codegen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Utils;

[Generator]
public class Module : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tables = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "SpacetimeDB.TableAttribute",
            predicate: (node, ct) => true, // already covered by attribute restrictions
            transform: (context, ct) =>
            {
                var table = (TypeDeclarationSyntax)context.TargetNode;

                var fields = table.Members
                    .OfType<FieldDeclarationSyntax>()
                    .Where(f => !f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                    .SelectMany(f =>
                    {
                        var attrVariantName =
                            f.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Where(
                                    a =>
                                        context.SemanticModel.GetSymbolInfo(a).Symbol
                                            is IMethodSymbol method
                                        && method.ContainingType.ToDisplayString()
                                            == "SpacetimeDB.ColumnIndexAttribute"
                                )
                                .Select(
                                    a =>
                                        a.ArgumentList?.Arguments[0].Expression switch
                                        {
                                            MemberAccessExpressionSyntax m
                                                // Assume object part is already checked by compiler anyway.
                                                => m.Name.Identifier.Text,
                                            // In case someone did `using static SpacetimeDB.Module.ColumnIndexAttributeKind;`
                                            IdentifierNameSyntax i => i.Identifier.Text,
                                            null => null,
                                            var expr
                                                => throw new System.Exception(
                                                    $"Unexpected expression {expr}"
                                                )
                                        }
                                )
                                .SingleOrDefault() ?? "UnSet";

                        var type = context.SemanticModel.GetTypeInfo(f.Declaration.Type).Type!;

                        return f.Declaration.Variables.Select(
                            v =>
                                (
                                    Name: v.Identifier.Text,
                                    Type: SymbolToName(type),
                                    TypeInfo: GetTypeInfo(type),
                                    IndexKind: attrVariantName
                                )
                        );
                    })
                    .ToArray();

                return new
                {
                    Scope = new Scope(table),
                    Name = table.Identifier.Text,
                    FullName = SymbolToName(context.SemanticModel.GetDeclaredSymbol(table)!),
                    Fields = fields,
                };
            }
        );

        tables
            .Select(
                (t, ct) =>
                {
                    var extensions =
                        $@"
                            private static Lazy<uint> tableId = new (() => SpacetimeDB.Runtime.GetTableId(nameof({t.Name})));

                            public static IEnumerable<{t.Name}> Iter() =>
                                new SpacetimeDB.Runtime.RawTableIter(tableId.Value)
                                .Select(GetSatsTypeInfo().ReadBytes);

                            public void Insert() => SpacetimeDB.Runtime.Insert(
                                tableId.Value,
                                GetSatsTypeInfo().ToBytes(this)
                            );
                        ";

                    foreach (var (f, index) in t.Fields.Select((f, i) => (f, i)))
                    {
                        if (f.IndexKind == "Unique" || f.IndexKind == "Identity")
                        {
                            extensions +=
                                $@"
                                    public static {t.Name}? FindBy{f.Name}({f.Type} {f.Name}) {{
                                        var raw = SpacetimeDB.Runtime.SeekEq(tableId.Value, {index}, {f.TypeInfo}.ToBytes({f.Name}));
                                        return raw.Length == 0 ? null : GetSatsTypeInfo().ReadBytes(raw);
                                    }}

                                    public static bool DeleteBy{f.Name}({f.Type} {f.Name}) =>
                                        SpacetimeDB.Runtime.DeleteEq(tableId.Value, {index}, {f.TypeInfo}.ToBytes({f.Name})) > 0;

                                    public static void UpdateBy{f.Name}({f.Type} {f.Name}, {t.Name} value) =>
                                        SpacetimeDB.Runtime.UpdateEq(tableId.Value, {index}, {f.TypeInfo}.ToBytes({f.Name}), GetSatsTypeInfo().ToBytes(value));
                                ";
                        }
                        else
                        {
                            // TODO: add extensions for non-unique fields.
                            // For now not adding as Rust does this filtering on Wasm side and
                            // users can already do that via normal LINQ methods anyway.
                        }
                    }

                    return new KeyValuePair<string, string>(
                        t.FullName,
                        t.Scope.GenerateExtensions(extensions)
                    );
                }
            )
            .RegisterSourceOutputs(context);

        var addTables = tables
            .Select(
                (t, ct) =>
                    $@"
                FFI.RegisterTable(new SpacetimeDB.Module.TableDef(
                    nameof({t.FullName}),
                    FFI.RegisterType({t.FullName}.GetSatsTypeInfo().algebraicType),
                    new SpacetimeDB.Module.ColumnIndexAttributeKind[] {{ {string.Join(", ", t.Fields.Select(f => $"SpacetimeDB.Module.ColumnIndexAttributeKind.{f.IndexKind}"))} }},
                    new SpacetimeDB.Module.IndexDef[] {{ }}
                ));
            "
            )
            .Collect();

        var reducers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "SpacetimeDB.ReducerAttribute",
                predicate: (node, ct) => true, // already covered by attribute restrictions
                transform: (context, ct) =>
                {
                    var method = (IMethodSymbol)
                        context.SemanticModel.GetDeclaredSymbol(context.TargetNode)!;

                    if (!method.ReturnsVoid)
                    {
                        throw new System.Exception($"Reducer {method} must return void");
                    }

                    return new
                    {
                        Name = method.Name,
                        FullName = SymbolToName(method),
                        Args = method.Parameters.Select(p => (p.Name, p.Type)).ToArray(),
                    };
                }
            )
            .Select(
                (r, ct) =>
                    (
                        r.Name,
                        Class: $@"
                            class {r.Name}: IReducer {{
                                {string.Join("\n", r.Args.Select(a => $"SpacetimeDB.SATS.TypeInfo<{a.Type}> {a.Name} = {GetTypeInfo(a.Type)};"))}

                                SpacetimeDB.Module.ReducerDef IReducer.MakeReducerDef() {{
                                    return new (
                                        nameof({r.FullName})
                                        {string.Join("", r.Args.Select(a => $",\nnew SpacetimeDB.SATS.ProductTypeElement(nameof({a.Name}), {a.Name}.algebraicType)"))}
                                    );
                                }}

                                void IReducer.Invoke(BinaryReader reader) {{
                                    {r.FullName}({string.Join(", ", r.Args.Select(a => $"{a.Name}.read(reader)"))});
                                }}
                            }}
                        "
                    )
            )
            .Collect();

        context.RegisterSourceOutput(
            addTables.Combine(reducers),
            (context, tuple) =>
            {
                var addTables = tuple.Left;
                var reducers = tuple.Right;
                if (addTables.IsEmpty && reducers.IsEmpty)
                    return;
                context.AddSource(
                    "FFI.cs",
                    $@"
            using SpacetimeDB.Module;
            using System.Runtime.CompilerServices;
            using static SpacetimeDB.Runtime;

            static class ModuleRegistration {{
                {string.Join("\n", reducers.Select(r => r.Class))}

#pragma warning disable CA2255
                // [ModuleInitializer] - doesn't work because assemblies are loaded lazily;
                // might make use of it later down the line, but for now assume there is only one
                // module so we can use `Main` instead.
                public static void Main() {{
                    {string.Join("\n", reducers.Select(r => $"FFI.RegisterReducer(new {r.Name}());"))}
                    {string.Join("\n", addTables)}
                }}
#pragma warning restore CA2255
            }}
            "
                );
            }
        );
    }
}
