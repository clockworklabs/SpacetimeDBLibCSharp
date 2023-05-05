# SpacetimeDB.Runtime

This project contains the core SpacetimeDB SATS typesystem, attributes for the codegen as well as runtime bindings for SpacetimeDB WebAssembly modules.

The runtime bindings are currently implementing via `Wasi.Sdk` package, which is a .NET implementation of the [WASI](https://wasi.dev/) standard. This is likely to change in the future.

While not really documented, it allows to build raw WebAssembly modules with custom bindings as well, which is what we're using here. The process is somewhat complicated, but here are the steps:

- `bindings.c` declares raw C bindings to the SpacetimeDB FFI _imports_ and marks them with attributes like `__attribute__((import_module("spacetime"), import_name("_insert")))` that make them WebAssembly imports. (unfortunately, function name duplication is currently unavoidable)
- `bindings.c` implements a bunch of Mono-compatible wrappers that convert between Mono types and raw types expected by the SpacetimeDB FFI and invoke corresponding raw bindings.
- `Runtime.cs` declares corresponding functions with compatible signatures for Mono-compatible wrappers to attach to. It marks them all with `[MethodImpl(MethodImplOptions.InternalCall)]`.
- `bindings.c` attaches all those Mono-compatible wrappers to their C# declarations in a `mono_stdb_attach_bindings` function.
- `bindings.c` adds FFI-compatible _exports_ that search for a method by assembly name, namespace, class name and a method name in the Mono runtime and invoke it. Those exports are marked with attributes like `__attribute__((export_name("__call_reducer__")))` so that they're exported from Wasm by the linker.
- Finally, `bindings.c` implements no-op shims for all the WASI APIs so that they're linked internally and not attempted to be imported from the runtime itself.

The result is a WebAssembly module FFI-compatible with SpacetimeDB and with no WASI imports, which is what we need.

Since those things can't be forwarded from our `csproj` to the application, the user's project must have following references and properties:

```xml
<ItemGroup>
  <ProjectReference Include="SpacetimeSharpSATS/Codegen/Codegen.csproj" OutputItemType="Analyzer" />
  <ProjectReference Include="SpacetimeSharpSATS/Runtime/Runtime.csproj" />
</ItemGroup>

<ItemGroup>
  <!-- Wasi.Sdk must be a dependency of application itself as it does all the bundling magic to turn .NET DLLs into a single Wasm -->
  <PackageReference Include="Wasi.Sdk" Version="0.1.4-preview.10020" />
  <!-- Include bindings.c in the native compilation so that it can implement all the necessary imports/exports. -->
  <WasiNativeFileReference Include="SpacetimeSharpSATS/Runtime/bindings.c" />
  <!-- Unlike functions, static variables can't be marked with __attribute__((export_name(...))), so they must be referenced on the command line. -->
  <!-- Oh, and `WasiNativeFileReference` seems to be the only way to pass extra compiler flags even though it's not the intended usage. -->
  <WasiNativeFileReference Include="-Wl,--export=SPACETIME_ABI_VERSION,--export=SPACETIME_ABI_VERSION_IS_ADDR" />
  <!-- Make sure `mono_stdb_attach_bindings` attaches the C functions to C# declarations after the runtime has loaded but before any user code has executed. -->
  <WasiAfterRuntimeLoaded Include="mono_stdb_attach_bindings" />
</ItemGroup>
```
