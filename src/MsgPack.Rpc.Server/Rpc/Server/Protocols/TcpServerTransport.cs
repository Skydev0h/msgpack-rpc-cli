#region -- License Terms --
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
using System.Net;
using System.Net.Sockets;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransport"/> implementation for TCP/IP.
	/// </summary>
	public sealed class TcpServerTransport : ServerTransport
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="TcpServerTransport"/> class.
		/// </summary>
		/// <param name="context">The context information.</param>
		/// <exception cref="ArgumentNullException">
		///   <paramref name="context"/> is <c>null</c>.
		///   </exception>
		public TcpServerTransport( ServerSocketAsyncEventArgs context )
			: base( context ) { }

		protected sealed override void InitializeCore( ServerSocketAsyncEventArgs context, EndPoint bindingEndPoint )
		{
			// TODO: Pooling
			// TODO: IPv6
			// TODO: BackLog-Configuration
			var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			socket.Bind( bindingEndPoint );
			socket.Listen( 10 );
			context.ListeningSocket = socket;
			Tracer.Protocols.TraceEvent( Tracer.EventType.StartListen, Tracer.EventId.StartListen, "Start listen. [ \"endPoint\" : \"{0}\", \"backLog\" : {1} ]", bindingEndPoint, 10 );
			this.Accept( context );
		}

		protected sealed override void OnAcceptted( ServerSocketAsyncEventArgs e )
		{
			this.Receive( e );
		}

		protected sealed override void ReceiveCore( ServerSocketAsyncEventArgs context )
		{
			if ( !context.AcceptSocket.ReceiveAsync( context ) )
			{
				this.OnReceived( context );
			}
		}

		protected sealed override void SendCore( ServerSocketAsyncEventArgs context )
		{
			if ( !context.AcceptSocket.SendAsync( context ) )
			{
				this.OnSent( context );
			}
		}

		protected sealed override void OnSent( ServerSocketAsyncEventArgs e )
		{
			e.AcceptSocket.Close();
			e.AcceptSocket = null;
			base.OnSent( e );
			this.Accept( e );
		}

		private void Accept( ServerSocketAsyncEventArgs e )
		{
			if ( !e.ListeningSocket.AcceptAsync( e ) )
			{
				this.OnAcceptted( e );
			}
		}
	}
}
