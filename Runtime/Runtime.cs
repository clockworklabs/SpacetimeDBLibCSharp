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

    [SpacetimeDB.Type]
    public enum IndexType : byte
    {
        BTree,
        Hash,
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

    public static bool UpdateEq(uint tableId, uint colId, byte[] value, byte[] row)
    {
        // Just like in Rust bindings, updating is just deleting and inserting for now.
        if (DeleteEq(tableId, colId, value) > 0)
        {
            Insert(tableId, row);
            return true;
        }
        else
        {
            return false;
        }
    }

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

    public class DbEventArgs : EventArgs
    {
        public readonly byte[] Sender;
        public readonly DateTimeOffset Time;

        public DbEventArgs(byte[] senderIdentity, ulong timestamp_us)
        {
            Sender = senderIdentity;
            // timestamp is in microseconds; the easiest way to convert those w/o losing precision is to get Unix origin and add ticks which are 0.1ms each.
            Time = DateTimeOffset.UnixEpoch.AddTicks(10 * (long)timestamp_us);
        }
    }

    public static event Action<DbEventArgs>? OnConnect;
    public static event Action<DbEventArgs>? OnDisconnect;

    // Note: this is accessed by C bindings.
    private static string? IdentityConnected(byte[] sender_identity, ulong timestamp)
    {
        try
        {
            OnConnect?.Invoke(new(sender_identity, timestamp));
            return null;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    // Note: this is accessed by C bindings.
    private static string? IdentityDisconnected(byte[] sender_identity, ulong timestamp)
    {
        try
        {
            OnDisconnect?.Invoke(new(sender_identity, timestamp));
            return null;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private extern static void ScheduleReducer(
        string name,
        byte[] args,
        ulong time,
        out ulong handle
    );

    [MethodImpl(MethodImplOptions.InternalCall)]
    private extern static void CancelReducer(ulong handle);

    public class ScheduleToken
    {
        private readonly ulong handle;

        public ScheduleToken(string name, byte[] args, DateTimeOffset time) =>
            ScheduleReducer(name, args, (ulong)(time - DateTimeOffset.UnixEpoch).Ticks, out handle);

        public void Cancel() => CancelReducer(handle);
    }
}
