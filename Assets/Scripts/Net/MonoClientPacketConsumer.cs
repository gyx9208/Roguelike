using UnityEngine;
using Fundamental;

namespace Net
{
	public class MonoClientPacketConsumer : MonoBehaviour
	{
		private Client _client = null;

		private const float RECONNECT_INTERVAL = 5.0f; /* 如果断线状态下，每隔5秒重新连接 */
		private float _timer;
		private int _reconnectTimes;

		public bool netReachAlreadyInit;
		public NetworkReachability netReach;
		private float _nextCheckReachTime;
		public const float CHECK_REACH_TIME_INTERVAL = 1.0f;

		public enum Status
		{
			Normal,
			WaitingConnect,
			RepeatLogin,
		}

		private Status _status;

		/*	Last Connected	*/
		private bool _lastIsConnected;

		public void Init(Client client)
		{
			_client = client;
			return;
		}

		void Awake()
		{
			GameObject.DontDestroyOnLoad(this);
			return;
		}

		// Use this for initialization
		void Start()
		{
			_status = Status.Normal;

			return;
		}

		// Update is called once per frame
		void Update()
		{
			while (_client.GetPacktNumInQueue() > 0)
				ConsumePacket();
		}

		void ConsumePacket()
		{
			NetPacket packet = _client.Recv(0);

			if (packet != null)
			{
				DispatchPacket(packet);
			}
			else
			{
				/* 检查网络连接状态 */
				if (CheckReachabilityChange())
				{
					SuperDebug.LogWarning(SuperDebug.NETWORK, "=========== internetReachability changed to " + netReach);
					if (_client.IsConnected())
					{
						SuperDebug.LogWarning(SuperDebug.NETWORK, "=========== disconnect by MonoClientPacketConsumer");
						_client.Disconnect();
					}
				}

				//	disconnected check
				bool isConnected = _client.IsConnected();
				if (!isConnected && _lastIsConnected
					&& NetworkManager.Instance.onUnexpectedDisconnect != null)
				{
					NetworkManager.Instance.onUnexpectedDisconnect();
				}
				_lastIsConnected = isConnected;

				if (!isConnected)
				{
					if (_status == Status.Normal)
					{
						_status = Status.WaitingConnect;
						Reconnect();
					}
					else if (_status == Status.WaitingConnect)
					{
						_timer += Time.deltaTime;
						if (_timer > RECONNECT_INTERVAL)
						{
							ShowLoadingWheel();
							Reconnect();
						}
					}
					else if (_status == Status.RepeatLogin)
					{
						TryShowErrorDialog();
					}
				}
			}

			return;
		}

		private bool CheckReachabilityChange()
		{
			if (_nextCheckReachTime == 0)
			{
				_nextCheckReachTime = Time.unscaledTime;
			}

			if (Time.unscaledTime < _nextCheckReachTime)
			{
				return false;
			}

			_nextCheckReachTime = Time.unscaledTime + CHECK_REACH_TIME_INTERVAL;

			NetworkReachability curNetReach = Application.internetReachability;
			if (!netReachAlreadyInit)
			{
				netReachAlreadyInit = true;
				netReach = curNetReach;
			}
			else if (netReach != curNetReach)
			{
				netReach = curNetReach;

				return true;
			}
			return false;
		}

		void DispatchPacket(NetPacket pkt)
		{
			NetworkManager.Instance.DispatchPacket(pkt);
		}

		public void OnApplicationQuit()
		{
			_client.Disconnect();
			return;
		}

		private void Reconnect()
		{
			_reconnectTimes += 1;
			_timer = 0;
			_status = Status.WaitingConnect;

			SuperDebug.LogWarning(SuperDebug.NETWORK, "===========Reconnect");

			NetworkManager.Instance.LoginGameServer();
		}

		private void ShowLoadingWheel()
		{

		}

		public void TryShowErrorDialog()
		{

		}
	}
}
