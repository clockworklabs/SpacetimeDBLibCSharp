namespace SpacetimeDB;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class Runtime
{
    [SpacetimeDB.Type]
    public enum IndexType : byte
    {
        BTree,
        Hash,
    }

    public class RawTableIter : IEnumerable<byte[]>
    {
        public readonly byte[] Schema;

        private readonly IEnumerator<byte[]> iter;

        public RawTableIter(uint tableId, byte[]? filterBytes = null)
        {
            iter = new Bindings.BufferIter(tableId, filterBytes);
            iter.MoveNext();
            Schema = iter.Current;
        }

        public IEnumerator<byte[]> GetEnumerator()
        {
            return iter;
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

    public static void Log(
        string text,
        LogLevel level = LogLevel.Info,
        [CallerMemberName] string target = "",
        [CallerFilePath] string filename = "",
        [CallerLineNumber] uint lineNumber = 0
    ) => Bindings.ConsoleLog((byte)level, target, filename, lineNumber, text);

    public struct Identity : IEquatable<Identity>
    {
        private readonly byte[] bytes;

        public Identity(byte[] bytes) => this.bytes = bytes;

        public bool Equals(Identity other) =>
            StructuralComparisons.StructuralEqualityComparer.Equals(bytes, other.bytes);

        public override bool Equals(object? obj) => obj is Identity other && Equals(other);

        public static bool operator ==(Identity left, Identity right) => left.Equals(right);

        public static bool operator !=(Identity left, Identity right) => !left.Equals(right);

        public override int GetHashCode() =>
            StructuralComparisons.StructuralEqualityComparer.GetHashCode(bytes);

        public override string ToString() => BitConverter.ToString(bytes);

        private static SpacetimeDB.SATS.TypeInfo<Identity> satsTypeInfo =
            new(
                // We need to set type info to inlined identity type as `generate` CLI currently can't recognise type references for built-ins.
                new SpacetimeDB.SATS.ProductType
                {
                    { "__identity_bytes", SpacetimeDB.SATS.BuiltinType.BytesTypeInfo.AlgebraicType }
                },
                reader => new(SpacetimeDB.SATS.BuiltinType.BytesTypeInfo.Read(reader)),
                (writer, value) =>
                    SpacetimeDB.SATS.BuiltinType.BytesTypeInfo.Write(writer, value.bytes)
            );

        public static SpacetimeDB.SATS.TypeInfo<Identity> GetSatsTypeInfo() => satsTypeInfo;
    }

    public class DbEventArgs : EventArgs
    {
        public readonly Identity Sender;
        public readonly DateTimeOffset Time;

        public DbEventArgs(byte[] senderIdentity, ulong timestamp_us)
        {
            Sender = new Identity(senderIdentity);
            // timestamp is in microseconds; the easiest way to convert those w/o losing precision is to get Unix origin and add ticks which are 0.1ms each.
            Time = DateTimeOffset.UnixEpoch.AddTicks(10 * (long)timestamp_us);
        }
    }

    public class ScheduleToken {
        private Bindings.ScheduleToken inner;

        public ScheduleToken(string name, byte[] args, DateTimeOffset time) {
            inner = new(name, args, time);
        }

        public void Cancel() => inner.Cancel();
    }

    public static event Action<DbEventArgs>? OnConnect;
    public static event Action<DbEventArgs>? OnDisconnect;

    // Note: this is accessed by C bindings.
    internal static string? IdentityConnected(byte[] sender_identity, ulong timestamp)
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
    internal static string? IdentityDisconnected(byte[] sender_identity, ulong timestamp)
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
}
