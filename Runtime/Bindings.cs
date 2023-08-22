namespace SpacetimeDB;

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using static System.Text.Encoding;

static class Bindings
{

	[StructLayout(LayoutKind.Sequential)]
	private struct Buffer
	{
		private uint handle;

		[DllImport("spacetime")]
		static extern uint _buffer_len(
		Buffer buf_handle
		);

		[DllImport("spacetime")]
		static extern void _buffer_consume(
		Buffer buf_handle,
		byte[] into,
		uint len
		);

		public byte[] Consume()
		{
			var len = _buffer_len(this);
			// todo: use `out` param?
			var result = new byte[len];
			_buffer_consume(this, result, len);
			return result;
		}

		public byte[]? ConsumeOrNull() => handle == uint.MaxValue ? null : Consume();

		[DllImport("spacetime")]
		static extern uint _buffer_alloc(
		byte[] data,
		uint data_len
		);

        public static readonly Buffer INVALID = new() { handle = uint.MaxValue };

		public Buffer(byte[] data)
		{
			handle = _buffer_alloc(data, (uint)data.Length);
		}
	}

	internal class BufferIter : IEnumerator<byte[]>, IDisposable
	{
		[DllImport("spacetime")]
		static extern ushort _iter_start(
		uint table_id,
		out uint iter_handle
		);

		[DllImport("spacetime")]
		static extern ushort _iter_start_filtered(
		uint table_id,
		byte[] filter,
		uint filter_len,
		out uint iter_handle
		);

		[DllImport("spacetime")]
		static extern ushort _iter_next(
		uint iter_handle,
		out Buffer buf
		);

		[DllImport("spacetime")]
		static extern void _iter_drop(
		uint iter_handle
		);

		private uint handle = uint.MaxValue;
		public byte[] Current { get; private set; } = new byte[0];

		object IEnumerator.Current => Current;

		public BufferIter(uint table_id, byte[]? filterBytes)
		{
			CheckResult(filterBytes switch
			{
				null => _iter_start(table_id, out handle),
				_ => _iter_start_filtered(table_id, filterBytes, (uint)filterBytes.Length, out handle)
			});
		}

		public bool MoveNext()
		{
			Current = new byte[0];
			CheckResult(_iter_next(handle, out var buf));
			var next = buf.ConsumeOrNull();
			if (next is not null)
			{
				Current = next;
			}
			return next is not null;
		}

		public void Dispose()
		{
			if (handle == uint.MaxValue)
			{
				return;
			}
			_iter_drop(handle);
			handle = uint.MaxValue;
		}

		// Free unmanaged resource just in case user hasn't disposed for some reason.
		~BufferIter()
		{
			// we already guard against double-free in Dispose.
			Dispose();
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ScheduleToken
	{
		private readonly ulong handle;

		[DllImport("spacetime")]
		static extern void _schedule_reducer(
		byte[] name,
		uint name_len,
		byte[] args,
		uint args_len,
		ulong time,
		out ulong schedule_token_handle
		);

		[DllImport("spacetime")]
		static extern void _cancel_reducer(
		ulong schedule_token_handle
		);

		public ScheduleToken(string name, byte[] args, DateTimeOffset time)
		{
			var name_bytes = UTF8.GetBytes(name);
			_schedule_reducer(name_bytes, (uint)name_bytes.Length, args, (uint)args.Length, (ulong)((time - DateTimeOffset.UnixEpoch).Ticks / 10), out handle);
		}

		public void Cancel() => _cancel_reducer(handle);
	}

	static void CheckResult(ushort result)
	{
		if (result != 0)
		{
			throw new System.Exception($"SpacetimeDB error code: {result}");
		}
	}

	[DllImport("spacetime")]
	static extern ushort _get_table_id(
	byte[] name,
	uint name_len,
	out uint out_
	);

	public static uint GetTableId(string name)
	{
		var name_bytes = UTF8.GetBytes(name);
		CheckResult(_get_table_id(name_bytes, (uint)name_bytes.Length, out var out_));
		return out_;
	}

	[DllImport("spacetime")]
	static extern ushort _create_index(
	byte[] index_name,
	uint index_name_len,
	uint table_id,
	byte index_type,
	byte[] col_ids,
	uint col_len
	);

	public static void CreateIndex(string index_name, uint table_id, byte index_type, byte[] col_ids)
	{
		var index_name_bytes = UTF8.GetBytes(index_name);
		CheckResult(_create_index(index_name_bytes, (uint)index_name_bytes.Length, table_id, index_type, col_ids, (uint)col_ids.Length));
	}

	[DllImport("spacetime")]
	static extern ushort _iter_by_col_eq(
	uint table_id,
	uint col_id,
	byte[] value,
	uint value_len,
	out Buffer out_
	);

	public static byte[] IterByColEq(uint table_id, uint col_id, byte[] value)
	{
		CheckResult(_iter_by_col_eq(table_id, col_id, value, (uint)value.Length, out var buf));
		return buf.Consume();
	}

	[DllImport("spacetime")]
	static extern ushort _insert(
	uint table_id,
	byte[] row,
	uint row_len
	);

	public static void Insert(uint table_id, byte[] row)
	{
		CheckResult(_insert(table_id, row, (uint)row.Length));
	}

	[DllImport("spacetime")]
	static extern ushort _delete_by_col_eq(
	uint table_id,
	uint col_id,
	byte[] value,
	uint value_len,
	out uint out_
	);

	public static uint DeleteByColEq(uint table_id, uint col_id, byte[] value)
	{
		CheckResult(_delete_by_col_eq(table_id, col_id, value, (uint)value.Length, out var out_));
		return out_;
	}

	public static bool UpdateByColEq(uint tableId, uint colId, byte[] value, byte[] row)
	{
		// Just like in Rust bindings, updating is just deleting and inserting for now.
		if (DeleteByColEq(tableId, colId, value) > 0)
		{
			Insert(tableId, row);
			return true;
		}
		else
		{
			return false;
		}
	}

	[DllImport("spacetime")]
	static extern void _console_log(
	byte level,
	byte[] target,
	uint target_len,
	byte[] filename,
	uint filename_len,
	uint line_number,
	byte[] text,
	uint text_len
	);

	internal static void ConsoleLog(byte level, string target, string filename, uint line_number, string text)
	{
		var target_bytes = UTF8.GetBytes(target);
		var filename_bytes = UTF8.GetBytes(filename);
		var text_bytes = UTF8.GetBytes(text);
		_console_log(level, target_bytes, (uint)target_bytes.Length, filename_bytes, (uint)filename_bytes.Length, line_number, text_bytes, (uint)text_bytes.Length);
	}

	[UnmanagedCallersOnly(EntryPoint = "__describe_module__")]
	static Buffer DescribeModule()
	{
		var bytes = Module.FFI.DescribeModule();
		return new(bytes);
	}

	private static Buffer ReturnResultBuf(string? str)
	{
		if (str == null)
		{
			return Buffer.INVALID;
		}
		var bytes = UTF8.GetBytes(str);
		return new(bytes);
	}

	[UnmanagedCallersOnly(EntryPoint = "__call_reducer__")]
	static Buffer CallReducer(uint id, Buffer sender, ulong timestamp, Buffer args)
	{
		return ReturnResultBuf(Module.FFI.CallReducer(id, sender.Consume(), timestamp, args.Consume()));
	}

	[UnmanagedCallersOnly(EntryPoint = "__identity_connected__")]
	static Buffer IdentityConnected(Buffer sender, ulong timestamp)
	{
		return ReturnResultBuf(Runtime.IdentityConnected(sender.Consume(), timestamp));
	}

	[UnmanagedCallersOnly(EntryPoint = "__identity_disconnected__")]
	static Buffer IdentityDisconnected(Buffer sender, ulong timestamp)
	{
		return ReturnResultBuf(Runtime.IdentityDisconnected(sender.Consume(), timestamp));
	}

}
