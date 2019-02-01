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
	public enum NetworkErrorCode
	{
		None,
		GlobalDispatcherConnectError,
		DispatcherConnectError,
		DownloadDataAssetError,
		DownloadEventAssetError,
		DownloadObbError,
		AccountLoginTimeOut,
		GameServerConnectErroreOut,
		OtherError,
		DownloadStreamingAssetBundleError,
	}

	/*	Flag to Mark Req and Rsp State */
	/*  default: None,	Send Req : Hanging Rsp : success : fail */
	public enum NetworkReqRspFlagState
	{
		None,       // Default State or after result
		Hanging,    // sending req waiting rsp
		Success,    // rsp result success
		Fail,       // rsp result fail
	}

	public class NetworkManager : Singleton<NetworkManager>
	{
		//阿里云风控riskdata系列
		//public string rawRiskData = "";
		//public string riskSign = "";

		//private const float DISPATCH_CONNECT_TIMEOUT_SECOND = 3.0f;
		//private short[] requestByte = { 0x71, 0x57, 0x60, 0x18, 0x4f, 0x35, 0x1e, 0x1c, 0x60, 0x17, 0x1b };

		//public readonly ConfigChannel channelConfig;
		//private List<string> _globalDispatchUrlList;

		private Client _client;
		private MonoClientPacketConsumer _clientPacketConsumer;

		//private Dictionary<int, DateTime> _lastRequestTimeDict;
		/*
         *有的request可能会因为玩家误操作，在短时间内重复发多次，需要在网络底层屏蔽掉重复的请求
         *需要首先建立一个需要防止重发的请求的白名单
         *白名单的key是请求的cmd id，value是请求重发的最小interval
         *   
         */
		//private Dictionary<int, float> _requestMinIntervalDict;

		//public int RegionIndex { get; set; }

		public bool AlreadyLogin;

		/* 反代充的时候，login的时候要给服务器uuid, 为了性能所以缓存起来 */
		//private string _uuid;

		/* 发包的缓存队列，用于连接恢复时的重发 */
		private const int QUEUE_CAPACITY = 20;
		private Queue<NetPacket> _packetSendQueue;
		private Dictionary<uint, bool> _CMD_SHOULD_ENQUEUE_MAP;

		/* 把签名和checksum塞到这里来 */
		public string mIPv6string; // 安卓签名
		public string mIPv4string; // apk的checksum
		public string mIPInfostring; // so文件的checksum

		public string gitCommit;

		private string _requestStr;

		/*	Unexpected Disconnect Delegate	*/
		public Action onUnexpectedDisconnect;

		private OneServerInfo _oneServerInfo;

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

			_packetSendQueue = new Queue<NetPacket>();

			InitClientPacketConsumer();

			AlreadyLogin = false;

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
			if (_client.isConnected())
			{
				/* 如果是已经连上了同一个服务器，直接返回true */
				if (_client.Host == host && _client.Port == port)
				{
					return true;
				}
				else
				{
					/* 如果是要连接另外一个服务器，先断开当前连接 */
					_client.disconnect();
				}
			}

			bool success = _client.connect(host, port, timeout_ms);
			if (success)
			{
				// set keeplive pack
				NetPacket pack = new NetPacket();
				pack.setData(new HeartBeat(), GetNewSession((ushort)HeartBeat.CmdId.CmdId));
				_client.setKeepalive(0, pack);

				_client.setDisconnectCallback(UnexceptedDisconnectCallback);

				_clientPacketConsumer.gameObject.SetActive(true);
			}

			return success;
		}

		/* Disconnect */
		public void DisConnect()
		{
			_client.disconnect();
			return;
		}

		/* Send Packet */
		public bool Post<T>(T data)
		{
			ushort cmdId = CommandMap.Instance.GetCmdIDByType(typeof(T));

			//if (!CheckRequestTimeValid(cmdID))
			//{
			//	SuperDebug.Log(SuperDebug.NETWORK, "Request Time Invalid: " + Singleton<CommandMap>.Instance.GetTypeByCmdID(cmdID));
			//	return false;
			//}

			//_lastRequestTimeDict[cmdID] = DateTime.Now;

			NetPacket req_pack = new NetPacket();
			req_pack.setData<T>(data, GetNewSession(cmdId));

			//Type cmdType = _CommandMap.GetTypeByCmdID(cmdID);
			//SuperDebug.Log(SuperDebug.NETWORK, "Send: " + cmdType);

			//TryCacheSendPacket(req_pack);

			_client.send(req_pack);

			//#if NG_HSOD_DEBUG || NG_HSOD_PROFILE
			//			Singleton<TestModule>.Instance.SavePeriodicRequestHistory(data, 1, 1);
			//#endif
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
			packet.setData(data, session);

			if (_client.send(packet))
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
			SuperDebug.Log(SuperDebug.NETWORK, "UnExceptedDisconnectCallback");
			//_clientPacketConsumer.isUnexpectedDisconnect = true;

			return;
		}

		/* Quick Login */
		/* 从Debug的Scene，或者断线重连 */
		//public void QuickLogin()
		//{
		//	LogicGameFramework.Instance.StartCoroutine(ConnectDispatchServer
		//	(
		//		delegate ()
		//		{
		//			if (ConnectGameServer(HOST, PORT))
		//			{
		//				RequestPlayerToken();
		//			}
		//			else
		//			{
		//				SuperDebug.LogWarning(SuperDebug.NETWORK, "Connect Gateway Failed");
		//			}
		//		}
		//	));
		//}

		public void RequestPlayerToken()
		{

		}

		/* Login GameServer */
		/* 从GameEntryScene进来，不需要连接Dispatch */
		public void LoginGameServer()
		{
			if (ConnectGameServer(_oneServerInfo.Ip, _oneServerInfo.Port))
			{
				RequestPlayerToken();
			}
		}

		//public void ProcessWaitStopAnotherLogin()
		//{
		//	float waitTime = 2.0f;

		//	if (alreadyLogin)
		//	{
		//		/* 不转菊花， 等2秒钟后重新发login请求*/
		//		Singleton<ApplicationManager>.Instance.Invoke(waitTime, RequestPlayerLogin);
		//	}
		//	else
		//	{
		//		/* 转菊花，等2秒钟后重新发login请求 */
		//		LoadingWheelWidgetContext loadingWheel = new LoadingWheelWidgetContext();
		//		loadingWheel.SetMaxWaitTime(waitTime);
		//		loadingWheel.timeOutCallBack = RequestPlayerLogin;
		//		Singleton<MainUIManager>.Instance.ShowWidget(loadingWheel);
		//	}
		//}

		#region Global Dispatch
		//public IEnumerator ConnectGlobalDispatchServer(Action successCallback = null)
		//{
		//	string retJsonString = "";
		//	int count = 0;

		//	while (count < _globalDispatchUrlList.Count)
		//	{
		//		float timer = 0.0f;

		//		/* 为了协助服务器更好地分配用户，把上次登录过的uid作为参数发给服务器 */
		//		string time = Miscs.GetTimeStampFromDateTime(DateTime.Now).ToString();
		//		string globalDispatchUrl = GlobalVars.GetGlobalDispatchUrl(_globalDispatchUrlList[count], GetGameVersion(), time);
		//		SuperDebug.Log(SuperDebug.NETWORK, "Try Connect Global Dispatch: " + globalDispatchUrl);

		//		bool timeout = false;
		//		WWW www = new WWW(globalDispatchUrl);
		//		while (!www.isDone)
		//		{
		//			if (timer > DISPATCH_CONNECT_TIMEOUT_SECOND) { timeout = true; break; }
		//			timer += Time.deltaTime;
		//			yield return null;
		//		}

		//		if (!string.IsNullOrEmpty(www.error) || timeout)
		//		{
		//			count++;
		//		}
		//		else
		//		{
		//			retJsonString = www.text;
		//			break;
		//		}
		//	}

		//	//	TODO handle global dispatch fail?
		//	OnConnectGlobalDispatch(retJsonString, successCallback);
		//}

		//private bool OnConnectGlobalDispatch(string retJsonString, Action successCallback = null)
		//{
		//	//retJsonString = ""; // test case
		//	//retJsonString = "{}"; // test case
		//	//retJsonString = "{\"retcode\":a}"; // test case
		//	//retJsonString = "{\"retcode\":1}"; // test case
		//	bool isSuccess = false;
		//	bool isGetRetcodeSuccess = false;
		//	string errorMsg = string.Empty;
		//	int retcode = 0;
		//	JSONNode retJson = null;

		//	string showDesc = LocalizationGeneralLogic.GetText("Menu_Desc_ConnectGlobalDispatchErr");

		//	isGetRetcodeSuccess = TryGetRetCodeFromJsonString(retJsonString, out retJson, out errorMsg, out retcode);
		//	isSuccess = isGetRetcodeSuccess;

		//	if (isGetRetcodeSuccess)
		//	{
		//		if (retcode == (int)GetDispatchRsp.Retcode.SUCC)
		//		{
		//			GlobalDispatchData = new GlobalDispatchDataItem(retJson);

		//			if (GlobalDispatchData.regionList.Count <= 0)
		//			{
		//				isSuccess = false;
		//				errorMsg = "Error: GlobalDispatchData.regionList.Count <= 0";
		//			}
		//			else
		//			{
		//				isSuccess = true;
		//			}
		//		}
		//		else
		//		{
		//			isSuccess = false;
		//			errorMsg = retJson["msg"] + " retcode=" + retcode;
		//			showDesc = errorMsg;
		//		}
		//	}

		//	if (isSuccess)
		//	{
		//		SuperDebug.Log(SuperDebug.NETWORK, "Connect Global Dispatch Success:" + retJsonString);

		//		RegionIndex = 0;

		//		if (successCallback != null)
		//		{
		//			successCallback();
		//		}
		//	}
		//	else
		//	{
		//		Singleton<MainUIManager>.Instance.ShowDialog(new GeneralDialogContext
		//		{
		//			type = GeneralDialogContext.ButtonType.DoubleButton,
		//			title = LocalizationGeneralLogic.GetText("Menu_Tittle_GlobalDispatchUnknownErr"),
		//			cancelBtnText = LocalizationGeneralLogic.GetText("Menu_Retry"),
		//			okBtnText = LocalizationGeneralLogic.GetText("Menu_NetError_OnlineHelp"),
		//			desc = showDesc,
		//			notDestroyAfterTouchBG = true,
		//			hideCloseBtn = true,
		//			buttonCallBack = (confirmed) =>
		//			{
		//				TryReconnectGlobalDispatch();
		//				if (confirmed)
		//				{
		//					UIUtil.OpenUrlForNetworkErrHelp(channelConfig.ChannelName, NetworkErrorCode.GlobalDispatcherConnectError);
		//				}
		//			},
		//		});

		//		UIUtil.TryReportNetworkErrForLogin(NetworkErrorCode.GlobalDispatcherConnectError, extMsg: errorMsg);

		//		SuperDebug.LogWarning(SuperDebug.NETWORK, "Connect Global Dispatch Error msg=" + errorMsg + " retJsonString=" + retJsonString);

		//		/*if (!string.IsNullOrEmpty(retJsonString))
		//		{
		//			SuperDebug.VeryImportantError("Connect Global Dispatch Error msg=" + errorMsg + " retJsonString=" + retJsonString);
		//		}*/
		//	}

		//	return isSuccess;
		//}

		//private bool TryGetRetCodeFromJsonString(string jsonString, out JSONNode retJson, out string errorMsg, out int retCode)
		//{
		//	bool isSuccess = false;
		//	errorMsg = string.Empty;
		//	retCode = 0;
		//	retJson = null;

		//	if (string.IsNullOrEmpty(jsonString))
		//	{
		//		isSuccess = false;
		//		errorMsg = "Error: retJsonString IsNullOrEmpty!";
		//	}
		//	else
		//	{
		//		retJson = JSON.Parse(jsonString);
		//		if (retJson == null || string.IsNullOrEmpty(retJson["retcode"]))
		//		{
		//			isSuccess = false;
		//			errorMsg = "Error: JSON.Parse null!";
		//		}
		//		else if (!int.TryParse(retJson["retcode"].Value, out retCode))
		//		{
		//			isSuccess = false;
		//			errorMsg = "Error: retcode is not integer!";
		//		}
		//		else
		//		{
		//			isSuccess = true;
		//		}
		//	}

		//	return isSuccess;
		//}

		#endregion

		#region Dispatch
		//public IEnumerator ConnectDispatchServer(Action successCallback = null)
		//{
		//	int lastLoginUserId = Singleton<MiHoYoGameData>.Instance.GeneralLocalData.LastLoginUserId;

		//	string time = Miscs.GetTimeStampFromDateTime(DateTime.Now).ToString();

		//	string dispatchUrl = GlobalVars.GetGlobalDispatchUrl(GlobalDispatchData.regionList[RegionIndex].dispatchUrl, GetGameVersion(), time);
		//	dispatchUrl += "&uid=" + lastLoginUserId;
		//	Singleton<MiHoYoGameData>.Instance.GeneralLocalData.LastLoginServer = GlobalDispatchData.regionList[RegionIndex].name;
		//	Singleton<MiHoYoGameData>.Instance.SaveGeneralData();

		//	SuperDebug.Log(SuperDebug.NETWORK, "Try Connect Dispatch: " + dispatchUrl);

		//	WWW www = new WWW(dispatchUrl);
		//	yield return www;

		//	string retString = "";

		//	if (!string.IsNullOrEmpty(www.error))
		//	{
		//		SuperDebug.LogWarning(SuperDebug.NETWORK, string.Format("www error: url={0}, error={1}", dispatchUrl, www.error));
		//	}
		//	else
		//	{
		//		retString = www.text;
		//	}

		//	OnConnectDispatchServer(retString, successCallback);

		//	www.Dispose();
		//}

		//		private bool OnConnectDispatchServer(string retJsonString, Action successCallback = null)
		//		{
		//			//retJsonString = ""; // test case
		//			//retJsonString = "{}"; // test case
		//			//retJsonString = "{\"retcode\":a}"; // test case
		//			//retJsonString = "{\"retcode\":4}"; // test case
		//			bool isGetRetcodeSuccess = false;
		//			string errorMsg = string.Empty;
		//			int retcode = 0;
		//			JSONNode retJson = null;

		//			isGetRetcodeSuccess = TryGetRetCodeFromJsonString(retJsonString, out retJson, out errorMsg, out retcode);

		//			if (!isGetRetcodeSuccess)
		//			{
		//				if (!alreadyLogin)
		//				{
		//					Singleton<MainUIManager>.Instance.ShowDialog(new GeneralDialogContext
		//					{
		//						type = GeneralDialogContext.ButtonType.DoubleButton,
		//						title = LocalizationGeneralLogic.GetText("Menu_NetError"),
		//						desc = LocalizationGeneralLogic.GetText("Menu_Desc_ConnectDispatchErr"),
		//						cancelBtnText = LocalizationGeneralLogic.GetText("Menu_Retry"),
		//						okBtnText = LocalizationGeneralLogic.GetText("Menu_NetError_OnlineHelp"),
		//						notDestroyAfterTouchBG = true,
		//						hideCloseBtn = true,
		//						buttonCallBack = (confirmed) =>
		//						{
		//							TryReconnectDispatch();
		//							if (confirmed)
		//							{
		//								UIUtil.OpenUrlForNetworkErrHelp(channelConfig.ChannelName, NetworkErrorCode.DispatcherConnectError);
		//							}
		//						},
		//					});

		//					UIUtil.TryReportNetworkErrForLogin(NetworkErrorCode.DispatcherConnectError, extMsg: errorMsg);
		//				}
		//				SuperDebug.LogWarning(SuperDebug.NETWORK, "Connect Dispatch Error msg=" + errorMsg + " retJsonString=" + retJsonString);

		//				/*if (!string.IsNullOrEmpty(retJsonString))
		//				{
		//					SuperDebug.VeryImportantError("Connect Dispatch Error msg=" + errorMsg + " retJsonString=" + retJsonString);
		//				}*/

		//				return false;
		//			}

		//			// eg. {"retcode":"0","msg":"succ","gateway":{"ip":"192.168.10.12","port":"24101"},"ext":[{"key1":"a"},{"key2":"b"}]}
		//			SuperDebug.Log(SuperDebug.NETWORK, "Connect Dispatch Server Success:" + retJsonString);

		//			// get gateway success
		//			retcode = retJson["retcode"].AsInt;
		//			if (retcode == (int)GetGameserverRsp.Retcode.SUCC)
		//			{
		//				DispatchSeverData = new DispatchServerDataItem(retJson);
		//				Singleton<AssetBundleManager>.Instance.remoteAssetBundleUrl = DispatchSeverData.assetBundleUrl;
		//				Singleton<StreamingAssetBundleManager>.Instance.SetDownloadUrl(DispatchSeverData.exResServerUrl);

		//				if (DispatchSeverData.deleteStreamingAsb)
		//				{
		//					Singleton<StreamingAssetBundleManager>.Instance.DeleteDownloadFolder();
		//					Singleton<StreamingAssetBundleManager>.Instance.Init();
		//				}

		//				GlobalVars.UpdateStreamingAsb = DispatchSeverData.updateStreamingAsb;
		//#if UNITY_EDITOR
		//				GlobalVars.UpdateStreamingAsb &= !GlobalVars.SimulateAssetBundleInEditor;
		//#endif

		//				/* 为了防止审核的时候被拒，这边可以从服务器那边强制不使用asb */
		//				if (DispatchSeverData.dataUseAssetBundleUseSever)
		//				{
		//					GlobalVars.DataUseAssetBundle = DispatchSeverData.dataUseAssetBundle;

		//				}
		//				if (DispatchSeverData.resUseAssetBundleUseSever)
		//				{
		//					GlobalVars.ResourceUseAssetBundle = DispatchSeverData.resUseAssetBundle;
		//				}

		//				if (successCallback != null)
		//				{
		//					successCallback();
		//				}
		//				if (!DispatchSeverData.forbidRiskDataSign)
		//				{
		//					if (null != Singleton<ApplicationManager>.GetInstance().GetRiskSigner())
		//					{
		//						Singleton<ApplicationManager>.GetInstance().GetRiskSigner().Init();
		//					}
		//				}

		//				return true;
		//			}
		//			else if (retcode == (int)GetGameserverRsp.Retcode.FORCE_UPDATE)
		//			{
		//				/* 版本需要强更 */
		//				string forceUupdateUrl = retJson["force_update_url"];
		//				bool showHelpBtn = channelConfig.AccountBranch == ConfigAccount.AccountBranch.Original;
		//				Singleton<MainUIManager>.Instance.ShowDialog(
		//					new GeneralDialogContext
		//					{
		//						type = showHelpBtn ? GeneralDialogContext.ButtonType.DoubleButton : GeneralDialogContext.ButtonType.SingleButton,
		//						title = LocalizationGeneralLogic.GetText("Menu_Title_NewVersion"),
		//						cancelBtnText = LocalizationGeneralLogic.GetText("Menu_NewVersion_OnlineHelp"),
		//						desc = retJson["msg"],
		//						notDestroyAfterTouchBG = true,
		//						hideCloseBtn = true,
		//						buttonCallBack = (confirmed) =>
		//						{
		//							if (confirmed)
		//							{
		//								/* 尝试跳转taptap */
		//								bool enableTaptapRedirect = !string.IsNullOrEmpty(retJson["ext"]["taptap_redirect"]) && retJson["ext"]["taptap_redirect"].AsInt == 1;
		//								bool redirectTaptap = enableTaptapRedirect && OpeUtil.TryRedirectToTaptap();
		//								if (!redirectTaptap)
		//								{
		//									UIUtil.OpenUrl(forceUupdateUrl);
		//								}

		//								GeneralLogicManager.QuitGame();
		//							}
		//							else
		//							{
		//								UIUtil.OpenUrlForNewVersionHelp();
		//							}
		//						}
		//					});
		//			}
		//			else if (retcode == (int)GetGameserverRsp.Retcode.SERVER_STOP)
		//			{
		//				/* 停服维护 */
		//				DateTime beginTime = Miscs.GetDateTimeFromTimeStamp((uint)retJson["stop_begin_time"].AsInt);
		//				DateTime endTime = Miscs.GetDateTimeFromTimeStamp((uint)retJson["stop_end_time"].AsInt);

		//				string descMsg = LocalizationGeneralLogic.GetCompiledText(retJson["msg"], beginTime.ToString("MM-dd HH:mm"), endTime.ToString("MM-dd HH:mm"));

		//				Singleton<MainUIManager>.Instance.ShowDialog(
		//					new GeneralDialogContext
		//					{
		//						type = GeneralDialogContext.ButtonType.SingleButton,
		//						title = LocalizationGeneralLogic.GetText("Menu_Title_ServerStop"),
		//						desc = descMsg,
		//						notDestroyAfterTouchBG = true,
		//						hideCloseBtn = true,
		//						buttonCallBack = (confirmed) => { TryReconnectDispatch(); },
		//					});
		//			}
		//			else
		//			{
		//				/* 其他错误 */
		//				Singleton<MainUIManager>.Instance.ShowDialog(
		//					new GeneralDialogContext
		//					{
		//						type = GeneralDialogContext.ButtonType.DoubleButton,
		//						title = LocalizationGeneralLogic.GetText("Menu_Tittle_DispatchUnknownErr"),
		//						desc = retJson["msg"] + " retcode=" + retcode,
		//						cancelBtnText = LocalizationGeneralLogic.GetText("Menu_Retry"),
		//						okBtnText = LocalizationGeneralLogic.GetText("Menu_NetError_OnlineHelp"),
		//						notDestroyAfterTouchBG = true,
		//						hideCloseBtn = true,
		//						buttonCallBack = (confirmed) =>
		//						{
		//							TryReconnectDispatch();
		//							if (confirmed)
		//							{
		//								UIUtil.OpenUrlForNetworkErrHelp(channelConfig.ChannelName, NetworkErrorCode.OtherError);
		//							}
		//						},
		//					});
		//			}

		//			return false;
		//		}

		//private void TryReconnectDispatch()
		//{
		//	if (alreadyLogin)
		//	{
		//		QuickLogin();
		//	}
		//	else
		//	{
		//		MonoGameEntry gameEntry = Singleton<MainUIManager>.Instance.SceneCanvas as MonoGameEntry;
		//		if (gameEntry != null)
		//		{
		//			gameEntry.ConnectDispatch();
		//		}
		//	}
		//}

		//private void TryReconnectGlobalDispatch()
		//{
		//	if (alreadyLogin)
		//	{
		//		SuperDebug.LogWarning("TryReconnectGlobalDispatch when alreadyLogin!");
		//	}
		//	else
		//	{
		//		MonoGameEntry gameEntry = Singleton<MainUIManager>.Instance.SceneCanvas as MonoGameEntry;
		//		if (gameEntry != null)
		//		{
		//			gameEntry.ConnentGlobalDispatch();
		//		}
		//	}
		//}
		#endregion

		//private proto.GetPlayerTokenReq GetTestPlayerTokenReq()
		//{
		//	string token = GetPersistentUUID();
		//	SuperDebug.Log(SuperDebug.NETWORK, "UUID=" + token);

		//	return new proto.GetPlayerTokenReq
		//	{
		//		account_type = (uint)proto.AccountType.ACCOUNT_NONE,
		//		account_uid = "",
		//		account_token = "",
		//		account_ext = "",
		//		token = token,
		//	};
		//}

		//private proto.GetPlayerTokenReq GetJapanTransferPlayerTokenReq()
		//{
		//	string token = GetPersistentUUID();
		//	SuperDebug.Log(SuperDebug.NETWORK, "UUID=" + token);

		//	TheJapanAccountManager manager = Singleton<AccountManager>.Instance.manager as TheJapanAccountManager;

		//	SuperDebug.Assert(manager != null, "GetJapanTransferPlayerTokenReq only use for Japan account!");
		//	SuperDebug.Assert(!string.IsNullOrEmpty(manager.inputTransferCode), "Error: inputTransferCode is empty!");
		//	SuperDebug.Assert(!string.IsNullOrEmpty(manager.inputTransferPassword), "Error: inputTransferPassword is empty!");

		//	return new proto.GetPlayerTokenReq
		//	{
		//		account_type = (uint)proto.AccountType.ACCOUNT_NONE,
		//		account_uid = "",
		//		account_token = "",
		//		account_ext = "",
		//		token = token,
		//		transfer_code = manager.inputTransferCode,
		//		transfer_pwd = manager.inputTransferPassword,
		//	};
		//}

		public static string GetPersistentUUID()
		{
			return SystemInfo.deviceUniqueIdentifier;
//			string retUUID = string.Empty;

//#if UNITY_IPHONE
//			/* 检查是不是合法的机器码 */
//			if (!CheckDeviceUniqueIdentifier())
//			{
//				retUUID = string.Empty;
//			}
//			else
//			{
//				// if there's uuid stored in keychain, use it as global uuid
//				// if there's not, use System.deviceUniqueIdentifier and save it into keychain
//				KeychainAccess.KeychainInitWithService(GlobalVars.BUNDLE_IDENTIFIER, null);
//				string stringInKeyChain = KeychainAccess.KeychainFind(GlobalVars.BUNDLE_IDENTIFIER);
//				if (stringInKeyChain == null)
//				{
//					stringInKeyChain = SystemInfo.deviceUniqueIdentifier;
//					KeychainAccess.KeychainInsert(GlobalVars.BUNDLE_IDENTIFIER, stringInKeyChain);
//				}

//				retUUID = stringInKeyChain;
//			}
//#elif UNITY_EDITOR
//			var id = SystemInfo.deviceUniqueIdentifier;
//			if (!string.IsNullOrEmpty(EditorData.Config.CustomDeviceID))
//			{
//				id += "-" + EditorData.Config.CustomDeviceID;
//			}
//			retUUID = id;
//#elif UNITY_ANDROID

//			if (!CheckDeviceUniqueIdentifier())
//			{
//				retUUID = string.Empty;
//			}
//			else
//			{
//				retUUID = AndroidUniqueIdentifierLogic.GenerateDeviceUniqueIdentifier();

//				/* 日服&台服：把机器ID放到本地缓存，可以减少机器ID发生改变对账号的影响 */
//				if (Singleton<AccountManager>.Instance.IsCachePersistentUUID())
//				{
//					if (!string.IsNullOrEmpty(Singleton<MiHoYoGameData>.Instance.GeneralLocalData.CachePersistentUUID))
//					{
//						retUUID = Singleton<MiHoYoGameData>.Instance.GeneralLocalData.CachePersistentUUID;
//					}
//					else
//					{
//						Singleton<MiHoYoGameData>.Instance.GeneralLocalData.CachePersistentUUID = retUUID;
//						Singleton<MiHoYoGameData>.Instance.SaveGeneralData();
//					}
//				}
//			}
//#else
//			retUUID = SystemInfo.deviceUniqueIdentifier;
//#endif
//			return retUUID;
		}

//		private static bool CheckDeviceUniqueIdentifier()
//		{
//			// 参考:https://developer.apple.com/reference/adsupport/asidentifiermanager
//			/* Get the advertising identifier using the advertisingIdentifier property (note that when ad tracking is limited, the value of the advertising identifier is 00000000-0000-0000-0000-000000000000). */

//#if UNITY_ANDROID
//			string deviceUniqueIdentifier = AndroidUniqueIdentifierLogic.GenerateDeviceUniqueIdentifier();
//#else
//			string deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
//#endif
//			//deviceUniqueIdentifier = ""; // test case
//			//deviceUniqueIdentifier = "00000000-0000-0000-0000-000000000000"; // test case

//			string allZeroCode =
//#if UNITY_IPHONE
//				"00000000-0000-0000-0000-000000000000";
//#elif UNITY_ANDROID
//				"cd9e459ea708a948d5c2f5a6ca8838cf";
//#else
//				string.Empty;
//#endif
//			/* 加个开关来控制安卓客户端，是否检查全零的机器码 */
//			bool needCheck = true;
//#if UNITY_ANDROID
//			needCheck = MiscData.Config.BasicConfig.EnableAndroidAllZeroTokenCheck;
//#endif
//			bool isAllZero = string.IsNullOrEmpty(deviceUniqueIdentifier) || deviceUniqueIdentifier == allZeroCode;
//			return !(needCheck && isAllZero);
//		}

		//public string GetGameVersion()
		//{
		//	return GlobalVars.VERSION + "_" + channelConfig.ChannelName;
		//}

		//private uint GeneralLoginRandomNum()
		//{
		//	return (uint)(DateTime.Now.Ticks >> 13);
		//}

		//public bool CheckRequestTimeValid(int cmdId)
		//{
		//	if (!_requestMinIntervalDict.ContainsKey(cmdId))
		//	{
		//		return true;
		//	}

		//	bool canRequest = false;
		//	if (_lastRequestTimeDict.ContainsKey(cmdId))
		//	{
		//		if (_lastRequestTimeDict[cmdId].AddSeconds(_requestMinIntervalDict[cmdId]) < TimeUtil.Now)
		//		{
		//			canRequest = true;
		//		}
		//	}
		//	else
		//	{
		//		canRequest = true;
		//	}

		//	return canRequest;
		//}

		//private void SetupRequestMinIntervalDict()
		//{
		//	_requestMinIntervalDict = new Dictionary<int, float>()
		//	{

		//	};

		//	return;
		//}

		//public float GetRequestMinInterval(int cmdId)
		//{
		//	var time = 0.0f;
		//	if (_requestMinIntervalDict != null)
		//		_requestMinIntervalDict.TryGetValue(cmdId, out time);
		//	return time;
		//}

		//private void BuildCmdShouldEnqueueMap()
		//{
		//	/* 重连之后，如果没有发成功会重新发送这些包
		//	 * 关卡结算，扭蛋，关卡复活 会锁死UI保证发送成功前不会点到其他按钮
		//	 * 其他的多为客户端默默发给服务器的包，如果丢了就会有bug，比方说任务更新 */
		//	_CMD_SHOULD_ENQUEUE_MAP = new Dictionary<uint, bool>
		//	{
		//	};
		//}

		//private void TryCacheSendPacket(NetPacketV1 reqPack)
		//{
		//	if (_CMD_SHOULD_ENQUEUE_MAP == null)
		//		BuildCmdShouldEnqueueMap();

		//	/* 白名单 */
		//	if (!_CMD_SHOULD_ENQUEUE_MAP.ContainsKey(reqPack.getCmdId()))
		//	{
		//		return;
		//	}

		//	if (_packetSendQueue.Count >= QUEUE_CAPACITY)
		//	{
		//		_packetSendQueue.Dequeue();
		//	}
		//	_packetSendQueue.Enqueue(reqPack);

		//	return;
		//}

		//private void SendQueuePacketWhenReconnected(uint serverProcessedPacketId)
		//{
		//	SuperDebug.Log(SuperDebug.NETWORK, "SendQueuePacketWhenReconnected Start! serverProcessedPacketId=" + serverProcessedPacketId);

		//	foreach (NetPacketV1 packet in _packetSendQueue)
		//	{
		//		if (packet.getTime() > serverProcessedPacketId)
		//		{
		//			Type cmdType = Singleton<CommandMap>.Instance.GetTypeByCmdID(packet.getCmdId());
		//			SuperDebug.Log(SuperDebug.NETWORK, "ReSend: " + cmdType);
		//			packet.reSetSign();
		//			_client.send(packet);
		//		}
		//	}

		//	SuperDebug.Log(SuperDebug.NETWORK, "SendQueuePacketWhenReconnected End!");
		//}

		public void Destroy()
		{
			DisConnect();
			onUnexpectedDisconnect = null;
			GameObject.Destroy(_clientPacketConsumer.gameObject);
			return;
		}

		public void SetRepeatLogin()
		{
			_clientPacketConsumer.SetRepeatLogin();
		}

		CommonDispathcer<ushort> _NetPackDispathcer;
		public void DispatchPacket(NetPacket pkt)
		{
			pkt.ReadData();
			ushort cmdid = pkt.GetCmdId();
			PacketSession session = pkt.GetSession();
			object data = pkt.getData();

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
					SuperDebug.Error(e.ToString());
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