﻿#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	public sealed class ServerRequestSocketAsyncEventArgs : ServerSocketAsyncEventArgs, ILeaseable<ServerRequestSocketAsyncEventArgs>
	{
		/// <summary>
		///		The initial process of the deserialization pipeline.
		/// </summary>
		private Func<ServerRequestSocketAsyncEventArgs, bool> _initialProcess;

		/// <summary>
		///		Next (that is, resuming) process on the deserialization pipeline.
		/// </summary>
		internal Func<ServerRequestSocketAsyncEventArgs, bool> NextProcess;

		/// <summary>
		///		Buffer that stores unpacking binaries received.
		/// </summary>
		internal ByteArraySegmentStream UnpackingBuffer;


		/// <summary>
		///		<see cref="Unpacker"/> to unpack entire request/notification message.
		/// </summary>
		internal Unpacker RootUnpacker;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to unpack request/notification message as array.
		/// </summary>
		internal Unpacker HeaderUnpacker;


		/// <summary>
		///		Buffer to store binaries for arguments array for subsequent deserialization.
		/// </summary>
		internal readonly MemoryStream ArgumentsBuffer;

		/// <summary>
		///		<see cref="Packer"/> to re-pack to binaries of arguments for subsequent deserialization.
		/// </summary>
		internal Packer ArgumentsBufferPacker;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse arguments array as opaque sequence.
		/// </summary>
		internal Unpacker ArgumentsBufferUnpacker;

		/// <summary>
		///		The count of declared method arguments.
		/// </summary>
		internal int ArgumentsCount;

		/// <summary>
		///		The count of unpacked method arguments.
		/// </summary>
		internal int UnpackedArgumentsCount;


		/// <summary>
		///		Unpacked Message Type part value.
		/// </summary>
		internal MessageType MessageType;

		/// <summary>
		///		Unpacked Method Name part value.
		/// </summary>
		internal string MethodName;

		/// <summary>
		///		<see cref="Unpacker"/> to deserialize arguments on the dispatcher.
		/// </summary>
		internal Unpacker ArgumentsUnpacker;

		public ServerRequestSocketAsyncEventArgs()
		{
			// TODO: Configurable
			this.ArgumentsBuffer = new MemoryStream( 65536 );
			this.State = ServerProcessingState.Uninitialized;
		}

		internal void SetTransport( ServerTransport transport )
		{
			this._initialProcess = transport.UnpackRequestHeader;
			this.NextProcess = transport.UnpackRequestHeader;
		}

		private static bool InvalidFlow( ServerSocketAsyncEventArgs context )
		{
			throw new InvalidOperationException( "Invalid state transition." );
		}

		internal sealed override void Clear()
		{
			this.ClearBuffers();
			this.ClearDispatchContext();
			base.Clear();
		}

		/// <summary>
		///		Clears the buffers to deserialize message, which is not required to dispatch and invoke server method.
		/// </summary>
		internal void ClearBuffers()
		{
			this.NextProcess = InvalidFlow;

			if ( this.ArgumentsBufferUnpacker != null )
			{
				this.ArgumentsBufferUnpacker.Dispose();
				this.ArgumentsBufferUnpacker = null;
			}

			if ( this.ArgumentsBufferPacker != null )
			{
				this.ArgumentsBufferPacker.Dispose();
				this.ArgumentsBufferPacker = null;
			}

			this.ArgumentsCount = 0;
			this.UnpackedArgumentsCount = 0;
			if ( this.HeaderUnpacker != null )
			{
				this.HeaderUnpacker.Dispose();
				this.HeaderUnpacker = null;
			}

			if ( this.RootUnpacker != null )
			{
				this.RootUnpacker.Dispose();
				this.RootUnpacker = null;
			}

			if ( this.UnpackingBuffer != null )
			{
				this.TruncateUsedReceivedData();
				this.UnpackingBuffer.Dispose();
				this.UnpackingBuffer = null;
			}
		}

		/// <summary>
		///		Truncates the used segments from the received data.
		/// </summary>
		private void TruncateUsedReceivedData()
		{
			long remaining = this.UnpackingBuffer.Position;
			var segments = this.UnpackingBuffer.GetBuffer();
			while ( segments.Any() && 0 < remaining )
			{
				if ( segments[ 0 ].Count < remaining )
				{
					remaining -= segments[ 0 ].Count;
					segments.RemoveAt( 0 );
				}
				else
				{
					int newCount = segments[ 0 ].Count - unchecked( ( int )remaining );
					int newOffset = segments[ 0 ].Offset + unchecked( ( int )remaining );
					segments[ 0 ] = new ArraySegment<byte>( segments[ 0 ].Array, newOffset, newCount );
					remaining -= newCount;
				}
			}
		}

		/// <summary>
		///		Clears the dispatch context information.
		/// </summary>
		internal void ClearDispatchContext()
		{
			this.NextProcess = this._initialProcess;

			this.MethodName = null;
			this.MessageType = MessageType.Response; // Invalid value.
			if ( this.ArgumentsUnpacker != null )
			{
				this.ArgumentsUnpacker.Dispose();
				this.ArgumentsUnpacker = null;
			}

			this.ArgumentsBuffer.SetLength( 0 );
			base.ReturnLease();
			this.State = ServerProcessingState.Uninitialized;
		}

		void ILeaseable<ServerRequestSocketAsyncEventArgs>.SetLease( ILease<ServerRequestSocketAsyncEventArgs> lease )
		{
			base.SetLease( lease );
		}
	}
}