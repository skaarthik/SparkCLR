﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Spark.CSharp.Configuration;
using Microsoft.Spark.CSharp.Services;

namespace Microsoft.Spark.CSharp.Network
{
	/// <summary>
	/// A simple wrapper of System.Net.Sockets.Socket class.
	/// </summary>
	internal class DefaultSocketWrapper : ISocketWrapper
	{
		private readonly ILoggerService logger = LoggerServiceFactory.GetLogger(typeof(DefaultSocketWrapper));
		private readonly Socket innerSocket;

		/// <summary>
		/// Default constructor that creates a new instance of DefaultSocket class which represents
		/// a traditional socket (System.Net.Socket.Socket).
		/// 
		/// This socket is bound to Loopback with port 0.
		/// </summary>
		public DefaultSocketWrapper()
		{
			innerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
			innerSocket.Bind(localEndPoint);
		}

		/// <summary>
		/// Initializes a instance of DefaultSocket class using the specified System.Net.Socket.Socket object.
		/// </summary>
		/// <param name="socket">The existing socket</param>
		private DefaultSocketWrapper(Socket socket)
		{
			innerSocket = socket;
		}

		/// <summary>
		/// Accepts a incoming connection request.
		/// </summary>
		/// <returns>A DefaultSocket instance used to send and receive data</returns>
		public ISocketWrapper Accept()
		{
			var socket = innerSocket.Accept();
			return new DefaultSocketWrapper(socket);
		}

		/// <summary>
		/// Close the socket connections and releases all associated resources.
		/// </summary>
		public void Close()
		{
			innerSocket.Close();
		}

		/// <summary>
		/// Establishes a connection to a remote host that is specified by an IP address and a port number
		/// </summary>
		/// <param name="remoteaddr">The IP address of the remote host</param>
		/// <param name="port">The port number of the remote host</param>
		public void Connect(IPAddress remoteaddr, int port, string secret)
		{
			var remoteEndPoint = new IPEndPoint(remoteaddr, port);
			innerSocket.Connect(remoteEndPoint);
		}

		private static byte[] ReceiveAll(Socket socket, int len)
		{
			var buffer = new List<byte>();

			while (socket.Available > 0 && buffer.Count < len)
			{
				var currByte = new Byte[1];
				var byteCounter = socket.Receive(currByte, currByte.Length, SocketFlags.None);

				if (byteCounter.Equals(1))
				{
					buffer.Add(currByte[0]);
				}
			}

			return buffer.ToArray();
		}

		/// <summary>
		/// Returns the NetworkStream used to send and receive data.
		/// </summary>
		/// <returns>The underlying Stream instance that be used to send and receive data</returns>
		/// <remarks>
		/// GetStream returns a NetworkStream that you can use to send and receive data. You must close/dispose
		/// the NetworkStream by yourself. Closing DefaultSocketWrapper does not release the NetworkStream
		/// </remarks>
		public Stream GetStream()
		{
			return new NetworkStream(innerSocket);
		}

		/// <summary>
		/// Returns a stream used to receive data only.
		/// </summary>
		/// <returns>The underlying Stream instance that be used to receive data</returns>
		public Stream GetInputStream()
		{
			// The default buffer size is 64K, PythonRDD also use 64K as default buffer size.
			var readBufferSize = int.Parse(Environment.GetEnvironmentVariable(ConfigurationService.CSharpWorkerReadBufferSizeEnvName) ?? "65536");
			logger.LogDebug("Input stream buffer size: [{0}]", readBufferSize);
			return readBufferSize > 0 ? new BufferedStream(GetStream(), readBufferSize) : GetStream();
		}

		/// <summary>
		/// Returns a stream used to send data only.
		/// </summary>
		/// <returns>The underlying Stream instance that be used to send data</returns>
		public Stream GetOutputStream()
		{
			// The default buffer size is 64K, PythonRDD also use 64K as default buffer size.
			var writeBufferSize = int.Parse(Environment.GetEnvironmentVariable(ConfigurationService.CSharpWorkerWriteBufferSizeEnvName) ?? "65536");
			logger.LogDebug("Output stream buffer size: [{0}]", writeBufferSize);
			return writeBufferSize > 0 ? new BufferedStream(GetStream(), writeBufferSize) : GetStream();
		}

		/// <summary>
		/// Starts listening for incoming connections requests
		/// </summary>
		/// <param name="backlog">The maximum length of the pending connections queue. </param>
		public void Listen(int backlog = 16)
		{
			innerSocket.Listen(backlog);
		}

		/// <summary>
		/// Receives network data from this socket, and returns a ByteBuf that contains the received data.
		/// 
		/// The DefaultSocketWrapper does not support this function.
		/// </summary>
		/// <returns>A ByteBuf object that contains received data.</returns>
		public ByteBuf Receive()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sends data to this socket with a ByteBuf object that contains data to be sent.
		/// 
		/// The DefaultSocketWrapper does not support this function.
		/// </summary>
		/// <param name="data">A ByteBuf object that contains data to be sent</param>
		public void Send(ByteBuf data)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Disposes the resources used by this instance of the DefaultSocket class.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				innerSocket.Dispose();
			}
		}

		/// <summary>
		/// Releases all resources used by the current instance of the DefaultSocket class.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Frees resources used by DefaultSocket class
		/// </summary>
		~DefaultSocketWrapper()
		{
			Dispose(false);
		}

		/// <summary>
		/// Indicates whether there are data that has been received from the network and is available to be read.
		/// </summary>
		public bool HasData { get { return innerSocket.Available > 0; } }

		/// <summary>
		/// Returns the local endpoint.
		/// </summary>
		public EndPoint LocalEndPoint { get { return innerSocket.LocalEndPoint; } }

		/// <summary>
		/// Returns the remote endpoint if it has one.
		/// </summary>
		public EndPoint RemoteEndPoint { get { return innerSocket.RemoteEndPoint; } }
	}
}
