namespace SpacetimeDB;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class Runtime
{
    private static void ThrowForResult(ushort result)
    {
        throw new Exception($"SpacetimeDB error code: {result}");
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern uint CreateTable(string name, byte[] schema);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern uint GetTableId(string name);

    public enum IndexType : byte
    {
        BTree = 0,
        Hash = 1,
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void CreateIndex(
        string index_name,
        uint table_id,
        IndexType index_type,
        byte[] col_ids
    );

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern byte[] SeekEq(uint table_id, uint col_id, byte[] value);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern static void Insert(uint tableId, byte[] row);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void DeletePk(uint table_id, byte[] pk);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void DeleteValue(uint table_id, byte[] row);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern uint DeleteEq(uint tableId, uint colId, byte[] value);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern uint DeleteRange(
        uint tableId,
        uint colId,
        byte[] rangeStart,
        byte[] rangeEnd
    );

    // Ideally these methods would be scoped under BufferIter,
    // but Mono bindings don't seem to work correctly with nested
    // classes.

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void BufferIterStart(uint table_id, out uint handle);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern byte[]? BufferIterNext(uint handle);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private extern static void BufferIterDrop(ref uint handle);

    private class BufferIter : IEnumerator<byte[]>, IDisposable
    {
        private uint handle;
        public byte[] Current { get; private set; } = new byte[0];

        object IEnumerator.Current => Current;

        public BufferIter(uint table_id)
        {
            BufferIterStart(table_id, out handle);
        }

        public bool MoveNext()
        {
            Current = new byte[0];
            var next = BufferIterNext(handle);
            if (next is not null)
            {
                Current = next;
            }
            return next is not null;
        }

        public void Dispose()
        {
            BufferIterDrop(ref handle);
        }

        // Free unmanaged resource just in case user hasn't disposed for some reason.
        ~BufferIter()
        {
            // we already guard against double-free in stdb_iter_drop.
            Dispose();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    public class RawTableIter : IEnumerable<byte[]>
    {
        public readonly byte[] Schema;

        private readonly IEnumerator<byte[]> _iter;

        public RawTableIter(uint tableId)
        {
            _iter = new BufferIter(tableId);
            _iter.MoveNext();
            Schema = _iter.Current;
        }

        public IEnumerator<byte[]> GetEnumerator()
        {
            return _iter;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public enum LogLevel : byte
    {
        Error,
        Warn,
        Info,
        Debug,
        Trace,
        Panic
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern static void Log(
        string text,
        LogLevel level = LogLevel.Info,
        [CallerMemberName] string target = "",
        [CallerFilePath] string filename = "",
        [CallerLineNumber] uint lineNumber = 0
    );
}
