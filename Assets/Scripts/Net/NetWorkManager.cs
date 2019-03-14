/*
 * Author: yuxiang.geng@mihoyo.com 
 * Date: 2018-10-09 11:31:37 
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Fundamental;

namespace Net
{
	public class NetworkManager : Singleton<NetworkManager>
	{
		private const int KEEP_ALIVE_INTERVAL = 10000; //10s

		private Client _client;
		private MonoClientPacketConsumer _clientPacketConsumer;

		/*	Unexpected Disconnect Delegate	*/
		public Action onUnexpectedDisconnect;

		private OneServerInfo _oneServerInfo;

		/// <summary>
		/// every packet needs a session index
		/// </summary>
		private byte _sessionIndex = 0;
		/// <summary>
		/// Cache packets not sent successful, key is cmd id
		/// </summary>
		private Dictionary<ushort, NetPacket> _cachePackets;
		/// <summary>
		/// Cache cmd packet which is wating response
		/// </summary>
		private HashSet<ushort> _waitResponse;
		/// <summary>
		/// Cache request's callback, key is session id
		/// </summary>
		private Dictionary<int, System.Action<object>> _cacheCallback;

		public NetworkManager()
		{
		}

		public override void Init()
		{
			base.Init();

			_client = new Client();

			InitClientPacketConsumer();

			//_lastRequestTimeDict = new Dictionary<int, DateTime>();
			_NetPackDispathcer = new CommonDispathcer<ushort>();

			var ServerInfo = Resources.Load<ServerInfo>("Data/ServerInfo");
			_oneServerInfo = ServerInfo.Servers[ServerInfo.Index];

			_cacheCallback = new Dictionary<int, Action<object>>();
			_waitResponse = new HashSet<ushort>();
			_cachePackets = new Dictionary<ushort, NetPacket>();
		}

		private void InitClientPacketConsumer()
		{
			if (_clientPacketConsumer == null)
			{
				GameObject go = new GameObject();
				go.name = "NetPacketConsumer";
				_clientPacketConsumer = go.AddComponent<MonoClientPacketConsumer>();
			}

			_clientPacketConsumer.Init(_client);
			_clientPacketConsumer.gameObject.SetActive(false);

			return;
		}

		private bool ConnectGameServer(string host, ushort port, int timeout_ms = 3000)
		{
			if (_client.IsConnected())
			{
				/* 如果是已经连上了同一个服务器，直接返回true */
				if (_client.Host == host && _client.Port == port)
				{
					return true;
				}
				else
				{
					/* 如果是要连接另外一个服务器，先断开当前连接 */
					_client.Disconnect();
				}
			}

			bool success = _client.Connect(host, port, timeout_ms);
			if (success)
			{
				// set keeplive pack
				NetPacket pack = new NetPacket();
				pack.SetData(new HeartBeat(), GetNewSession((ushort)HeartBeat.CmdId.CmdId));
				_client.SetKeepalive(KEEP_ALIVE_INTERVAL, pack);

				_client.SetDisconnectCallback(UnexceptedDisconnectCallback);

				_clientPacketConsumer.gameObject.SetActive(true);
			}

			return success;
		}

		/* Disconnect */
		public void DisConnect()
		{
			_client.Disconnect();
			return;
		}

		/* Send Packet */
		public bool Post<T>(T data)
		{
			ushort cmdId = CommandMap.Instance.GetCmdIDByType(typeof(T));

			NetPacket req_pack = new NetPacket();
			req_pack.SetData<T>(data, GetNewSession(cmdId));

			_client.Send(req_pack);
			return true;
		}

		public bool Request<T>(T data, System.Action<object> callback)
		{
			ushort cmdId = CommandMap.Instance.GetCmdIDByType(typeof(T));

			if (_cachePackets.ContainsKey(cmdId) || _waitResponse.Contains(cmdId))
			{//  last packet wasn't sent or waiting response, drop
				return false;
			}

			var session = GetNewSession(cmdId);
			session.PType = PacketSession.PacketType.Request;

			NetPacket packet = new NetPacket();
			packet.SetData(data, session);

			if (_client.Send(packet))
			{
				_waitResponse.Add(packet.GetCmdId());
				_cacheCallback[session.SessionId] = callback;
			}
			else
			{
				_cachePackets[cmdId] = packet;
			}

			return true;
		}

		private PacketSession GetNewSession(ushort cmdId)
		{
			_sessionIndex = (byte)(_sessionIndex + 1);
			PacketSession session = new PacketSession();
			session.SessionId = _sessionIndex;
			session.CmdId = cmdId;

			return session;
		}


		public void UnexceptedDisconnectCallback()
		{
			SuperDebug.Log(DebugPrefix.Network, "UnExceptedDisconnectCallback");
		}

		/* Login GameServer */
		/* 从GameEntryScene进来，不需要连接Dispatch */
		public bool LoginGameServer()
		{
			return (ConnectGameServer(_oneServerInfo.Ip, _oneServerInfo.Port));
		}

		public void Destroy()
		{
			DisConnect();
			onUnexpectedDisconnect = null;
			GameObject.Destroy(_clientPacketConsumer.gameObject);
			return;
		}

		CommonDispathcer<ushort> _NetPackDispathcer;
		public void DispatchPacket(NetPacket pkt)
		{
			pkt.ReadData();
			ushort cmdid = pkt.GetCmdId();
			PacketSession session = pkt.GetSession();
			object data = pkt.GetData();

			_NetPackDispathcer.EventDispatch(cmdid, data);

#if HKMM_DEV
			if( data is CommonRsp)
			{
				var rsp = data as CommonRsp;
				if (!rsp.Success)
					Debug.Log(rsp.ErrorStr);
			}
#endif

			if (session.PType == PacketSession.PacketType.Response)
			{
				try
				{
					_waitResponse.Remove((ushort)session.CmdId);
					_cacheCallback[session.SessionId](data);
					_cacheCallback.Remove(session.SessionId);
				}
				catch (Exception e)
				{
					SuperDebug.LogError(e.ToString());
				}
			}
		}

		public void AddListener(ushort cmdid, Action<object> action)
		{
			_NetPackDispathcer.AddEventListener(cmdid, action);
		}

		public void RemoveListener(ushort cmdid, Action<object> action)
		{
			_NetPackDispathcer.RemoveEventListener(cmdid, action);
		}
	}
}