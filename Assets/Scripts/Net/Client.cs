using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using Fundamental;

/*
 *  2015/02/01
 *  xiao.liu@mihoyo.com
 *      实现到服务器的连接管理
 *      
 *  2018.10.9 by yuxiang.geng@mihoyo.com
 *  主要和之前实现的区别是包体用包头2字节定义包长，第二个2字节定义包序列
 */

namespace Net
{
	class ClientDefine
	{
		// 接受缓存大小
		public const int g_recv_buf_size = 65536;
	}

	public class Client : ClientInterface
	{
		private string host_;
		private ushort port_;
		private Socket socket_;
		private ManualResetEvent timeout_event_;
		private int timeout_ms_;
		private Thread client_producer_thread_;
		private Queue<NetPacket> recv_queue_;
		private bool connected_before_;
		private Action disconnect_callback_;
		private Action<NetPacket> doCmd_callback_;

		// 心跳包自动发送配置
		private int keepalive_time_ms_;
		private DateTime last_keepalive_time_;
		private NetPacket keepalive_packet_;

		// 缓存收到的二进制数据
		private byte[] left_buf_;
		private int left_buf_len_;
		private NetPacket _tempPacket;

		public string Host { get { return host_; } }
		public ushort Port { get { return port_; } }

		private EndPoint _remoteEndPoint;

		/*
		 *  构造函数
		 */
		public Client()
		{
			host_ = "";
			port_ = 0;
			socket_ = null;
			timeout_event_ = new ManualResetEvent(false);
			timeout_ms_ = 0;
			client_producer_thread_ = null;
			recv_queue_ = new Queue<NetPacket>();
			connected_before_ = false;
			disconnect_callback_ = null;
			doCmd_callback_ = null;

			keepalive_time_ms_ = 0;
			last_keepalive_time_ = DateTime.Now;    // !!! 用本地的 Now 因为 TimeUtil.Now 会被修改，导致 keepalive 不发
			keepalive_packet_ = null;

			left_buf_ = new byte[ClientDefine.g_recv_buf_size];
			left_buf_len_ = 0;

		}

		~Client()
		{
			Disconnect();
		}
		/*
		 *  建立连接
		 */
		public bool Connect(string host, ushort port, int timeout_ms = 2000) //以毫秒为单位
		{
			try
			{
				// 检查是否已经建立连接
				if (IsConnected())
				{
					SuperDebug.LogWarning(DebugPrefix.Network, "client is already connected to " + host_ + ":" + port_ + ", can not connect to other server now.");
					return false;
				}

				// 赋值
				host_ = host;
				port_ = port;
				timeout_ms_ = timeout_ms;

				// 根据host获取ip列表
				IPAddress[] ip_list = Dns.GetHostAddresses(host_);
				if (0 == ip_list.Length)
				{
					SuperDebug.LogWarning(DebugPrefix.Network, "can not get any ip address from host " + host_);
					return false;
				}

				// 尝试连接每个ip
				socket_ = null;
				_remoteEndPoint = null;
				for (int idx = 0; idx < ip_list.Length; idx++)
				{
					IPAddress ip_tmp = GetIPAddress(ip_list[idx]);

					SuperDebug.Log(DebugPrefix.Network, "try to connect to " + ip_tmp);
					IPEndPoint ipe = new IPEndPoint(ip_tmp, port_);
					Socket socket_tmp = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

					// 初始化socket属性
					socket_tmp.NoDelay = true;
					socket_tmp.Blocking = true;
					socket_tmp.SendTimeout = timeout_ms_;
					socket_tmp.ReceiveTimeout = timeout_ms_;
					socket_tmp.ReceiveBufferSize = ClientDefine.g_recv_buf_size * 2;

					// 连接
					timeout_event_.Reset();
					socket_tmp.BeginConnect(ip_tmp, port_, new AsyncCallback(ConnectCallback), socket_tmp);

					// 超时等待连接建立
					timeout_event_.WaitOne(timeout_ms, false);

					// 检验连接是否成功
					if (socket_tmp.Connected)
					{
						socket_ = socket_tmp;
						_remoteEndPoint = ipe;
						SuperDebug.Log(DebugPrefix.Network, "socket_.ReceiveBufferSize= " + socket_.ReceiveBufferSize);

						break;
					}
					else
					{
						SuperDebug.Log(DebugPrefix.Network, "connect to " + ip_tmp + " timeout.");
						continue;
					}
				}

				// 检查是否成功连接
				if (null == socket_)
				{
					SuperDebug.LogWarning(DebugPrefix.Network, "connect to " + host_ + ":" + port_ + " failed.");
					return false;
				}
				else
				{
					SuperDebug.Log(DebugPrefix.Network, "connect to " + host_ + ":" + port_ + " succ.");
					SuperDebug.Log(DebugPrefix.Network, "_remoteEndPoint= " + _remoteEndPoint);
				}

				// 启动工作线程
				StartClientThread();

				//
				connected_before_ = true;
			}
			catch (System.Exception e)
			{
				SuperDebug.LogError(DebugPrefix.Network, "connect to " + host + ":" + port + " meet exception " + e.ToString());
				return false;
			}

			return true;
		}

		/*
		 *  连接超时回调函数
		 */
		private void ConnectCallback(IAsyncResult asyncresult)
		{
			try
			{
				Socket socket = asyncresult.AsyncState as Socket;
				socket.EndConnect(asyncresult);
			}
			catch (SystemException)
			{
			}
			finally
			{
			}
			timeout_event_.Set();
		}

		/*
		 *  主动断开连接
		 */
		public void Disconnect()
		{
			connected_before_ = false;
			if (IsConnected())
			{
				SuperDebug.Log(DebugPrefix.Network, "disconnect to " + host_ + ":" + port_);

				/* 多线程可能让socket_为空 */
				if (socket_ != null)
				{
					socket_.Close();
				}

				socket_ = null;

				timeout_event_.Set();
			}
		}

		/*
		 *  判断当前连接是否建立
		 */
		public bool IsConnected()
		{
			if (null == socket_)
			{
				return false;
			}

			// 通过尝试获取服务器地址，判断连接是否断开
			bool res = true;
			try
			{
				if (_remoteEndPoint == null)
				{
					res = false;
				}
			}
			catch (SystemException)
			{
				res = false;
			}

			// 连接意外断开后，调用用户回调函数
			if (!res && connected_before_)
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "connect to " + host_ + ":" + port_ + " break down.");
				connected_before_ = false;
				if (null != disconnect_callback_)
				{
					disconnect_callback_();
				}
			}

			return res;
		}

		/*
		 *  同步方式发送一个标准消息包到服务器
		 */
		public bool Send(NetPacket packet)
		{
			if (!IsConnected())
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "client is not connected, can not send now.");
				return false;
			}

			// 序列化
			MemoryStream ms = new MemoryStream();
			packet.Serialize(ref ms);

			// 发送
			/* fix: ObjectDisposedException: The object was used after being disposed. */
			// 因为另一个线程会close掉socket，在close的同时send，就会导致上面的报错，加了Try-Catch */
			try
			{
				SocketError error = new SocketError();
				int send_len = socket_.Send(ms.GetBuffer(), 0, (int)ms.Length, SocketFlags.None, out error);
				if (error != SocketError.Success)
				{
					SuperDebug.LogWarning(DebugPrefix.Network, "send failed: " + error);
					return false;
				}
				if (send_len != ms.Length)
				{//???does this happen?
					SuperDebug.LogWarning(DebugPrefix.Network, "packet_len=" + ms.Length + ", but only send " + send_len);
					return false;
				}
			}
			catch (Exception e)
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "exception e=" + e.ToString());
				return false;
			}

			return true;
		}

		/*
		 *  同步方式接受多个消息
		 *      如果当前有可读消息，则立刻返回，否则超时等待设置的时间
		 */
		private List<NetPacket> RecvPacketList()
		{
			// 检查连接状态
			if (!IsConnected())
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "client is not connected, can not recv now.");
				return null;
			}

			// 开始接受数据
			List<NetPacket> list = new List<NetPacket>();
			try
			{
				// 接受到缓存
				byte[] recv_buf = new byte[ClientDefine.g_recv_buf_size - left_buf_len_];
				SocketError error = new SocketError();
				int recv_len = socket_.Receive(recv_buf, 0, recv_buf.Length, SocketFlags.None, out error);

				// 接受超时立刻返回
				if (error == SocketError.TimedOut
					|| error == SocketError.WouldBlock
					|| error == SocketError.IOPending)
				{
					return list;
				}

				// 如果接受数据长度为0，则表示连接出现异常，需要立刻使用回调函数通知使用方
				if (error != SocketError.Success || 0 == recv_len)
				{
					SuperDebug.LogWarning(DebugPrefix.Network, "recv failed with recv_len=" + recv_len + ", error=" + error);
					socket_.Close();
					socket_ = null;
					return list;
				}

				// 合并上次剩余、本次收到的数据
				byte[] total_buf = new byte[ClientDefine.g_recv_buf_size];
				Array.Copy(left_buf_, 0, total_buf, 0, left_buf_len_);
				Array.Copy(recv_buf, 0, total_buf, left_buf_len_, recv_len);
				int total_len = recv_len + left_buf_len_;
				left_buf_len_ = 0;

				// 开始处理
				int used = 0;

				// 一次可能recv多个packet，循环反序列化每个packet,并加入list
				while (used < total_len)
				{
					//缓存之前的有效数据
					if (_tempPacket == null)
						_tempPacket = new NetPacket();

					PacketStatus packet_status = _tempPacket.Deserialize(ref total_buf, ref used, total_len);

					if (PacketStatus.PACKET_CORRECT != packet_status)
					{
						// 存储残缺的数据
						if (PacketStatus.PACKET_NOT_COMPLETE == packet_status)
						{
							left_buf_len_ = total_len - used;
							Array.Copy(total_buf, used, left_buf_, 0, left_buf_len_);
						}
						else
						{
							SuperDebug.LogWarning(DebugPrefix.Network, "deserialize packet failed. " + packet_status);
						}

						break;
					}
					else
					{
						list.Add(_tempPacket);
						_tempPacket = null;
					}
				}
			}
			catch (SystemException e)
			{
				if (IsConnected())
				{
					SuperDebug.LogError(DebugPrefix.Network, "recv failed: " + e);
				}
			}
			return list;
		}

		/*
		 *  循环接受数据的线程,将收到的packet写入队列
		 */
		private void ClientProducerThreadHandler()
		{
			SuperDebug.Log(DebugPrefix.Network, "clientProducer thread start.");
			while (IsConnected())
			{
				try
				{
					List<NetPacket> list = RecvPacketList();
					//MiLog.Log("recv " + list.Count + " packet");
					if (null != list && 0 != list.Count)
					{
						foreach (NetPacket packet in list)
						{
#if NG_HSOD_DEBUG
							//Thread.Sleep(100);
#endif
							lock (recv_queue_)
							{
								recv_queue_.Enqueue(packet);
							}
						}
						timeout_event_.Set();
					}

					// 发送心跳包
					KeepAlive();
				}
				catch (SystemException e)
				{
					SuperDebug.LogError(DebugPrefix.Network, e.ToString());
				}
			}
			// 断开连接
			Disconnect();
			SuperDebug.Log(DebugPrefix.Network, "clientProducer thread stop.");
		}

		/*
		 *  循环读取数据的线程, 逐个处理包
		 *  目前读取在主线程中，而不在网络线程中
		 */
		private void ClientConsumerThreadHandler()
		{
			SuperDebug.Log(DebugPrefix.Network, "clientConsumer thread start.");
			while (IsConnected() || 0 < recv_queue_.Count)
			{
				// 等待的毫秒数，为 Timeout.Infinite，表示无限期等待。
				NetPacket packet = Recv(Timeout.Infinite);

				if (packet != null)
				{
					SuperDebug.Log(DebugPrefix.Network, "clientConsumer recv: CmdId=" + packet.GetCmdId());
					doCmd_callback_(packet);
				}
				else
				{
					SuperDebug.LogWarning(DebugPrefix.Network, "packet = null in clientConsumerThreadHandler");
				}
			}

			SuperDebug.Log(DebugPrefix.Network, "clientConsumer thread stop.");
		}

		public int GetPacktNumInQueue()
		{
			return recv_queue_.Count;
		}

		/*
		 *   从消息队列中读取一个packet
		 */
		public NetPacket Recv(int timeout_ms = 0)
		{
			// 如果队列中有数据，立刻返回，即使连接已经断开
			lock (recv_queue_)
			{
				if (0 < recv_queue_.Count)
				{
					return recv_queue_.Dequeue();
				}
			}

			// 连接状态校验
			if (!IsConnected())
			{
				//SuperDebug.LogWarning(DebugPrefix.Network, "client is not connected, can not recv now.");
				return null;
			}

			// 当前队列为空，如果timeout=0，立刻返回
			if (0 == timeout_ms)
			{
				return null;
			}

			// 阻塞超时等待
			timeout_event_.Reset();
			timeout_event_.WaitOne(timeout_ms, false);

			// 返回队列头部packet
			lock (recv_queue_)
			{
				if (0 == recv_queue_.Count)
				{
					return null;
				}
				return recv_queue_.Dequeue();
			}
		}

		/*
		 *  判断自动接受数据线程是否启动
		 */
		private bool IsClientThreadRun()
		{
			return (null != client_producer_thread_ && client_producer_thread_.IsAlive);
		}

		/*
		 *  启动接受数据的线程
		 */
		private bool StartClientThread()
		{
			if (IsClientThreadRun())
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "recv thread is already running now, can not restart.");
				return false;
			}
			client_producer_thread_ = new Thread(ClientProducerThreadHandler);
			client_producer_thread_.Start();

			return true;
		}

		/*
		 *  设置连接断开时的回调
		 */
		public void SetDisconnectCallback(Action callback)
		{
			disconnect_callback_ = callback;
		}


		/*
		 *  设置信息处理的回调
		 */
		public void SetCmdCallBack(Action<NetPacket> callback)
		{
			doCmd_callback_ = callback;
		}

		/*
		 *  设置心跳包
		 */
		public bool SetKeepalive(int time_ms, NetPacket packet)
		{
			if (time_ms <= 0 || null == packet)
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "time_ms<=0 or packet==null");
				return false;
			}

			keepalive_time_ms_ = time_ms;
			keepalive_packet_ = packet;
			return true;
		}

		/*
		 *  发送心跳包
		 */
		private void KeepAlive()
		{
			if (0 == keepalive_time_ms_ || null == keepalive_packet_)
			{
				return;
			}
			TimeSpan time_span = DateTime.Now - last_keepalive_time_;
			if (time_span.TotalMilliseconds >= keepalive_time_ms_)
			{
				Send(keepalive_packet_);
				last_keepalive_time_ = DateTime.Now;
			}

		}

		public static bool isIPv6 { get; private set; }

		public static bool IsIPV6()
		{
			/*
#if UNITY_IPHONE
			isIPv6 = Dns.GetHostAddresses("www.bh3.com")[0].AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
#endif
*/
			return isIPv6;
		}

		public static IPAddress GetIPAddress(IPAddress ip)
		{
#if UNITY_IPHONE
			bool isIPV4Format = ip.AddressFamily == AddressFamily.InterNetwork;
			bool isIPV6Environment = IsIPV6();
			if (isIPV4Format && isIPV6Environment)
			{
				string ipv6 = IPV6Access.ConvertIPv4ToIPv6(ip.ToString());
				SuperDebug.Log(DebugPrefix.Network, string.Format("convert ipv4={0} ipv6={1}", ip, ipv6));

				ip = IPAddress.Parse(ipv6);
			}
#endif
			return ip;
		}
	}
}
