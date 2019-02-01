/*
 * Author: yuxiang.geng@mihoyo.com 
 * Date: 2018-10-09 11:31:51 
*/
using Fundamental;
using UnityEngine;

namespace Net
{
	public class MonoClientPacketConsumer : MonoBehaviour
	{
		private Client _client = null;

		//public bool isUnexpectedDisconnect = false;

		// for debug
		public string host;
		public ushort port;

		private const float RECONNECT_INTERVAL = 5.0f; /* 如果断线状态下，每隔8秒重新连接 */
private float _timer;
		private int _reconnectTimes;

		public uint lastServerPacketId;
		public uint clientPacketId;

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
		public Status _status;

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
			host = _client.Host;
			port = _client.Port;

			//_loadingWheelDialogContext = new LoadingWheelWidgetContext();

			_status = Status.Normal;

			return;
		}

		// Update is called once per frame
		void Update()
		{
			//if (Singleton<NetworkManager>.Instance == null || MiscData.Config == null)
			//{
			//	ConsumePacket();
			//	return;
			//}

			////MiscData里有两个开关，分别控制 Login前和Login后，是否一帧处理掉队列里所有的Packet
			//bool consume_all_before_login = !Singleton<NetworkManager>.Instance.alreadyLogin && MiscData.Config.PacketConsumeAllOneFrameBeforeLogin;
			//bool consume_all_after_login = Singleton<NetworkManager>.Instance.alreadyLogin && MiscData.Config.PacketConsumeAllOneFrameAfterLogin;

			//if (consume_all_before_login || consume_all_after_login)
			//{
			while (_client.GetPacktNumInQueue() > 0)
				ConsumePacket();
			//}
			//else
			//{
			//	ConsumePacket();
			//}
		}

		void ConsumePacket()
		{
			NetPacket packet = _client.recv(0);

			if (packet != null)
			{
				//OnConnectNormal();


				//Type cmdType = Singleton<CommandMap>.Instance.GetTypeByCmdID(packet.getCmdId());
				//string packetInfo = "Recv: " + cmdType + "cmdID : " + packet.getCmdId();

//#if UNITY_EDITOR
//				var data = packet.getData(cmdType);
//				System.Reflection.MemberInfo[] memberInfos = cmdType.GetMembers();
//				foreach (System.Reflection.MemberInfo memberInfo in memberInfos)
//				{
//					if (memberInfo.Name == "retcode")
//					{
//						System.Reflection.PropertyInfo pi = (System.Reflection.PropertyInfo)memberInfo;
//						packetInfo += " retcode : " + pi.GetValue(data, null);
//						break;
//					}
//				}
//#endif
				//SuperDebug.Log(SuperDebug.NETWORK, packetInfo);

				///* 只有收到了客户端能识别的包才会分发处理 */
				//if (cmdType != null)
				//{
					DispatchPacket(packet);
				//}
			}
			else
			{
				/* 重连只在登录完成进入游戏之后进行 */
				if (!NetworkManager.Instance.AlreadyLogin)
				{
					return;
				}

				/* 检查网络连接状态 */
				if (CheckReachabilityChange())
				{
					SuperDebug.LogWarning(SuperDebug.NETWORK, "=========== internetReachability changed to " + netReach);
					if (_client.isConnected())
					{
						SuperDebug.LogWarning(SuperDebug.NETWORK, "=========== disconnect by MonoClientPacketConsumer");
						_client.disconnect();
					}
				}

				//	disconnected check
				bool isConnected = _client.isConnected();
				if (!isConnected && _lastIsConnected
					&& NetworkManager.Instance.onUnexpectedDisconnect != null)
				{
					NetworkManager.Instance.onUnexpectedDisconnect();
				}
				_lastIsConnected = isConnected;

				if (!isConnected)
				{
					///* 如果UI是空，不进行重连 */
					//if (Singleton<MainUIManager>.Instance == null || Singleton<MainUIManager>.Instance.SceneCanvas == null)
					//{
					//	SuperDebug.LogWarning(SuperDebug.NETWORK, "MainUIManager is not ready");
					//	return;
					//}

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

		#region Packet Dispatcher
		void DispatchPacket(NetPacket pkt)
		{
			//if (pkt.getTime() > 0)
			//{
			//	lastServerPacketId = pkt.getTime();
			//}
			//if (_syncTimeMgr != null)
			//{
			//	_syncTimeMgr.OnPacket(pkt);
			//}

			NetworkManager.Instance.DispatchPacket(pkt);
		}

		#endregion

		public void OnApplicationQuit()
		{
			_client.disconnect();
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

		private void OnConnectNormal()
		{
			if (_status != Status.Normal)
			{
				_timer = 0;
				_reconnectTimes = 0;
				_status = Status.Normal;

				//if (ShouldShowLoadingWheelWhenDisconnect())
				//{
				//	Singleton<NotifyManager>.Instance.FireNotify(new Notify(NotifyTypes.OnSocketConnect));
				//}
			}

			//if (_loadingWheelDialogContext != null && _loadingWheelDialogContext.view != null)
			//{
			//	_loadingWheelDialogContext.Finish();
			//}
		}

		private void ShowLoadingWheel()
		{
			//if (ShouldShowLoadingWheelWhenDisconnect())
			//{
			//	/* 为了让关卡内暂停活动 */
			//	Singleton<NotifyManager>.Instance.FireNotify(new Notify(NotifyTypes.OnSocketDisconnect));
			//	Singleton<MainUIManager>.Instance.ShowWidget(_loadingWheelDialogContext);
			//}
		}

		//private bool ShouldShowLoadingWheelWhenDisconnect()
		//{
		//	/* 如果是关卡内的话，不显示菊花 */
		//	bool isInLevel = Singleton<MainUIManager>.Instance.SceneCanvas is MonoInLevelUICanvas;
		//	return !isInLevel || ConstValueDataReaderExtend.ShouldShowLoadingWheelWhenDisconnectInLevel;
		//}

		public void SetRepeatLogin()
		{
			_status = Status.RepeatLogin;
		}

		public void TryShowErrorDialog()
		{
			//if (_errorDialogContext != null && _errorDialogContext.view != null)
			//{
			//	return;
			//}

			//_errorDialogContext = new GeneralDialogContext
			//{
			//	type = GeneralDialogContext.ButtonType.SingleButton,
			//	title = LocalizationGeneralLogic.GetText("Menu_Title_Tips"),
			//	desc = LocalizationGeneralLogic.GetText("Err_PlayerRepeatLogin"),
			//	notDestroyAfterTouchBG = true,
			//	hideCloseBtn = true,
			//	buttonCallBack = (confirmed) => { GeneralLogicManager.RestartGame(); }
			//};

			//Singleton<MainUIManager>.Instance.ShowDialog(_errorDialogContext);
		}
	}

}
