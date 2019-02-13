using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Fundamental;

namespace UI
{
	public enum UI_LAYER : int
	{
		NORMAL,
		PERMANENT,
		POPUP,   //  弹窗  //
		MASK, //  特殊(如引导) 会在noraml和popup的前面  //
	}

	public enum UI_MODE : int
	{
		NONE,
		BACK,  // 加入界面返回队列 //
		HIDE_OTHER, // 加入界面返回队列 //
	}

	public class UIManager : Singleton<UIManager>
	{
		public static readonly Vector2 DEFAULT_SCREEN_SIZE = new Vector2(2160, 1080);
		public static readonly int UI_RENDERQUEUE = 3000;
		public static int HideLayer;
		public static int AvatarLayer;
		public static int UILayer;
		private static readonly int POPUP_VIEW_SORTINGORDER = 500;
		private static readonly int SPECIAL_VIEW_SOTRINGORDER = 1000;
		private Camera _UICamera = null;

		public bool ShowTouchArea = false;

		private Canvas _rootCanvas;
		private GameObject _hideRoot;
		private GameObject _normalLayerRoot;
		private GameObject _popupLayerRoot;
		private GameObject _specialLayerRoot;
		private Vector3 _uiScale = Vector3.one;
		private Vector2 _uiSizeDelta = DEFAULT_SCREEN_SIZE;

		private Dictionary<UI_TYPE, BaseUIView> _allUIView;
		private List<BaseUIView> _backUIViewQueue;
		private List<BaseUIView> _openUIViewQueue;

		private BaseUIView _topUIView = null;

		public Camera UICamera { get { return _UICamera; } }
		public Canvas RootCanvas { get { return _rootCanvas; } }

		public CommonDispathcer<EventId> UguiEventDispatcher { get; private set; }

		#region UNITY_FUNTION

		public void Update()
		{
			if (_allUIView == null || _allUIView.Count <= 0)
				return;

			float deltaTime = Time.deltaTime;
			for (UI_TYPE type = UI_TYPE.UI_NONE; type < UI_TYPE.UI_MAX; ++type)
			{
				BaseUIView baseUIView = null;
				if (_allUIView.TryGetValue(type, out baseUIView))
				{
					baseUIView.Core(deltaTime);

					if (baseUIView.IsNeedDestroy())
					{
						_allUIView.Remove(type);
						_backUIViewQueue.Remove(baseUIView);
						_openUIViewQueue.Remove(baseUIView);
						baseUIView.Hide();
						baseUIView.Release();
						GameObject.Destroy(baseUIView.gameObject);
					}
				}
			}
		}

		public void Release()
		{
			if (_allUIView == null)
			{
				return;
			}

			for (UI_TYPE i = UI_TYPE.UI_NONE; i < UI_TYPE.UI_MAX; ++i)
			{
				BaseUIView uiView = null;
				if (_allUIView.TryGetValue(i, out uiView))
				{
					if (uiView != null)
					{
						if (!uiView.IsClose()) uiView.Hide();
						uiView.Release();
						GameObject.Destroy(uiView.gameObject);
					}
				}
			}

			_UICamera = null;
			_allUIView.Clear();
			_backUIViewQueue.Clear();
			Resources.UnloadUnusedAssets();
		}

#if UNITY_EDITOR
		private static Vector3[] _fourCorners = new Vector3[4];
		void OnDrawGizmos()
		{
			if (!ShowTouchArea)
			{
				return;
			}
			var touchAreas = GameObject.FindObjectsOfType<MaskableGraphic>();
			foreach (MaskableGraphic g in touchAreas)
			{
				if (g.raycastTarget)
				{
					RectTransform rectTransform = g.transform as RectTransform;
					rectTransform.GetWorldCorners(_fourCorners);
					Gizmos.color = Color.red;
					for (int i = 0; i < 4; ++i)
					{
						Gizmos.DrawLine(_fourCorners[i], _fourCorners[(i + 1) % 4]);
					}
				}
			}
		}

#endif
		#endregion UNITY_FUNTION

		#region PUBLIC_FUNCTION
		public BaseUIView OpenUIView(UI_TYPE uiType, object param = null)
		{
			BaseUIView baseUIView = null;
			if (!_allUIView.TryGetValue(uiType, out baseUIView))
			{
				string uiAssetPath = GetUIAssetPathByUIType(uiType);
				GameObject goUI = GameObject.Instantiate(ResourceManager.GetInstance().GetResource(uiAssetPath, typeof(GameObject), enResourceType.UIPrefab).Content) as GameObject;
				baseUIView = goUI.GetComponent<BaseUIView>();
				_allUIView.Add(uiType, baseUIView);
			}
			else
			{
				if (!baseUIView.IsClose())
				{
					baseUIView.Hide();
				}
			}

			if (baseUIView.IsNeedBack())
			{
				for (int i = 0, len = _backUIViewQueue.Count; i < len; ++i)
				{
					BaseUIView tempUIView = _backUIViewQueue[i];
					if (tempUIView.Equals(baseUIView))
					{
						_backUIViewQueue.RemoveAt(i);
						break;
					}
				}

				_backUIViewQueue.Add(baseUIView);
			}

			if (_openUIViewQueue.Contains(baseUIView))
			{
				_openUIViewQueue.Remove(baseUIView);
			}
			_openUIViewQueue.Add(baseUIView);

			ChangeUIViewParent(baseUIView);

			HideOldView();
			baseUIView.UIType = uiType;
			if (baseUIView.IsShowAfterOpenEffect)
			{
				baseUIView.UIParam = param;
				baseUIView.ShowOpenEffect();
			}
			else
			{
				baseUIView.Show(param);
			}
			baseUIView.ChangeCameraRenderTexture(true);
			baseUIView.ChangeCanvasLayer(UILayer);
			baseUIView.ChangeGraphicRaycasterState(true);
			RefreshUISortingOrder();

			UguiEventOpenView evt = ClassObjPool<UguiEventOpenView>.Get();
			evt.uiType = uiType;
			UguiEventDispatcher.EventDispatch<UguiEventOpenView>(EventId.UGUI_OPEN_VIEW, evt);
			evt.Release();

			DispatchChangeTopViewEvent();

			return baseUIView;
		}

		public void DispatchChangeTopViewEvent()
		{
			if (_backUIViewQueue.Count > 0 && _backUIViewQueue[_backUIViewQueue.Count - 1] != _topUIView)
			{
				_topUIView = _backUIViewQueue[_backUIViewQueue.Count - 1];
				UguiEventChangeTopView evt = ClassObjPool<UguiEventChangeTopView>.Get();
				evt.uiView = _topUIView;
				UguiEventDispatcher.EventDispatch<UguiEventChangeTopView>(EventId.UGUI_CHANGE_TOP_VIEW, evt);
				evt.Release();
			}
		}

		public void PrecacheUIView(UI_TYPE uiType)
		{
			string uiAssetPath = GetUIAssetPathByUIType(uiType);
			LogicGameFramework.Instance.StartCoroutine(ResourceManager.Instance.GetResourceAsync(uiAssetPath, typeof(GameObject), enResourceType.UIPrefab));
		}

		public void PrecacheUIView(UI_TYPE uiType, CustomAsyncOperation async)
		{
			async.AddMission();

			BaseUIView baseUIView = null;

			if (!_allUIView.TryGetValue(uiType, out baseUIView))
			{
				LogicGameFramework.Instance.StartCoroutine(EPrecache(uiType, async.FinishMission));
			}
			else
			{
				async.FinishMission();
			}
		}

		private System.Collections.IEnumerator EPrecache(UI_TYPE uiType, Action onFinish)
		{
			string uiAssetPath = GetUIAssetPathByUIType(uiType);
			yield return LogicGameFramework.Instance.StartCoroutine(ResourceManager.Instance.GetResourceAsync(uiAssetPath, typeof(GameObject), enResourceType.UIPrefab));
			
			BaseUIView baseUIView = null;

			if (!_allUIView.TryGetValue(uiType, out baseUIView))
			{
				GameObject goUI = GameObject.Instantiate(ResourceManager.Instance.GetResource(uiAssetPath, typeof(GameObject), enResourceType.UIPrefab).Content) as GameObject;
				baseUIView = goUI.GetComponent<BaseUIView>();
				_allUIView.Add(uiType, baseUIView);

				ChangeUIViewParent(baseUIView, true);
				baseUIView.PreCache();
			}

			if (onFinish != null)
				onFinish();
		}


		public void CloseUIView()
		{
			if (_backUIViewQueue == null || _backUIViewQueue.Count <= 0)
				return;

			int lastIndex = _backUIViewQueue.Count - 1;
			BaseUIView closeUIView = _backUIViewQueue[lastIndex];
			UI_TYPE closeUIType = closeUIView.UIType;
			_backUIViewQueue.RemoveAt(lastIndex);
			CloseUIView(closeUIView);

			if (lastIndex > 0)
			{
				BaseUIView showUIView = _backUIViewQueue[lastIndex - 1];
				ChangeUIViewParent(showUIView);
				showUIView.ChangeCameraRenderTexture(true);
				showUIView.ChangeCanvasLayer(UILayer);
				showUIView.ChangeGraphicRaycasterState(true);
				showUIView.Back();
			}

			UguiEventCloseView evt = ClassObjPool<UguiEventCloseView>.Get();
			evt.uiType = closeUIType;
			UguiEventDispatcher.EventDispatch<UguiEventCloseView>(EventId.UGUI_CLOSE_VIEW, evt);
			evt.Release();

			DispatchChangeTopViewEvent();
		}

		public void CloseUIView(UI_TYPE uiType)
		{
			BaseUIView baseUIView = null;
			if (_allUIView.TryGetValue(uiType, out baseUIView))
			{
				CloseUIView(baseUIView);
			}
		}

		public void CloseUIView(BaseUIView closeUIView)
		{
			if (closeUIView == null)
				return;

			if (closeUIView.DeleteOnHide)
			{
				_allUIView.Remove(closeUIView.UIType);
				_backUIViewQueue.Remove(closeUIView);
				_openUIViewQueue.Remove(closeUIView);
				closeUIView.Hide();
				closeUIView.Release();
				GameObject.Destroy(closeUIView.gameObject);
			}
			else
			{
				ChangeUIViewParent(closeUIView, true);
				closeUIView.ChangeCameraRenderTexture(false);
				closeUIView.ChangeCanvasLayer(HideLayer);
				closeUIView.ChangeGraphicRaycasterState(false);
				_openUIViewQueue.Remove(closeUIView);
				closeUIView.Hide();
			}
		}

		public bool IsUIOpen(UI_TYPE uiType)
		{
			BaseUIView baseUIView = null;
			if (_allUIView.TryGetValue(uiType, out baseUIView))
			{
				return !baseUIView.IsClose();
			}

			return false;
		}

		public bool IsUIBackHide(UI_TYPE uiType)
		{
			BaseUIView baseUIView = null;
			if (_allUIView.TryGetValue(uiType, out baseUIView))
			{
				int index = _backUIViewQueue.LastIndexOf(baseUIView);
				return index < (_backUIViewQueue.Count - 1);
			}

			return false;
		}

		public void RefreshUISortingOrder()
		{
			int maxSortingOrder = 0;
			int popMaxSortingOrder = POPUP_VIEW_SORTINGORDER;
			int specialMaxSotringOrder = SPECIAL_VIEW_SOTRINGORDER;
			for (int i = 0; i < _openUIViewQueue.Count; ++i)
			{
				BaseUIView baseUIView = _openUIViewQueue[i];
				if (baseUIView == null || baseUIView.gameObject == null)
					continue;

				if (baseUIView.UILayer == UI_LAYER.NORMAL)
				{
					maxSortingOrder = baseUIView.UpdateSortingOrder(maxSortingOrder);
				}
				else if (baseUIView.UILayer == UI_LAYER.POPUP)
				{
					popMaxSortingOrder = baseUIView.UpdateSortingOrder(popMaxSortingOrder);
				}
				else if (baseUIView.UILayer == UI_LAYER.MASK)
				{
					specialMaxSotringOrder = baseUIView.UpdateSortingOrder(specialMaxSotringOrder);
				}
			}
		}

		public BaseUIView FindUIViewByUIType(UI_TYPE uiType)
		{
			BaseUIView baseUIView = null;
			if (_allUIView.TryGetValue(uiType, out baseUIView))
			{
				return baseUIView;
			}

			return null;
		}

		public Vector2 UISizeDelta { get { return _uiSizeDelta; } }

		public Vector3 UIScale { get { return _uiScale; } }

		internal void InitOnEnterBattle()
		{
			RefreshUI(SceneManager.GetActiveScene(), LoadSceneMode.Single);
		}

		internal void ClearOnExitBattle()
		{
			Release();
		}

		internal void ClearOnExitMainUI()
		{
			Release();
		}

		/* main camera to ugui pos */
		public Vector3 WorldToUIPoint(Camera worldCamera, Vector3 pos, bool clampZ = true)
		{
			Vector3 screenPos = worldCamera.WorldToScreenPoint(pos);
			/* Change To Ugui Coordinates */
			// screen center is ugui's (0,0)
			var inLevelUICamera = UIManager.Instance.UICamera;
			/*
			 * 首先使用main camera的world to screen point得到screen point；
			 * 但是screen point到UGUI的坐标还需要转换a
			 * 因为in level ui camera和main camera来说，screen point是相同的，但是对于in level ui camera,它的world space就是UGUI的world space
			 * 所以转换方法是使用in level ui camera的screen to world point，得到和最初pos对应的UGUI上的位置
			 * 需要注意的是，只有在in level ui camera的near，far plane之间的点才是可见的，所以需要把screen point的z轴的值限定在near，far plane之间
			 */
			if (clampZ)
				screenPos.z = Mathf.Clamp(screenPos.z, inLevelUICamera.nearClipPlane, inLevelUICamera.farClipPlane);
			Vector3 uiPos = inLevelUICamera.ScreenToWorldPoint(screenPos);
			return uiPos;
		}

		public void SaveOpenUIViewParams(UI_TYPE[] exceptUITypes)
		{
			foreach (var uiView in _backUIViewQueue)
			{
				if (exceptUITypes == null || System.Array.IndexOf(exceptUITypes, uiView.UIType) < 0)
				{
					PlayerDataManager.Instance.SaveOpenUIViewParam(new OpenUIViewParam() { UIType = uiView.UIType, UIParam = uiView.UIParam });
				}
			}
		}

		public void SetChangeLocationStr(int index, string locationStr)
		{
			UguiEventChangeLocationStr evt = ClassObjPool<UguiEventChangeLocationStr>.Get();
			evt.index = index;
			evt.locationStr = locationStr;
			UIManager.Instance.UguiEventDispatcher.EventDispatch(EventId.UGUI_CHANGE_LOCATION_STR, evt);
			evt.Release();
		}

		#endregion PUBLIC_FUNCTION

		#region PRIVATE_FUNCTION
		private void HideOldView()
		{
			if (_backUIViewQueue.Count <= 0)
			{
				return;
			}

			BaseUIView baseUIView = _backUIViewQueue[_backUIViewQueue.Count - 1];
			if (baseUIView.UIMode == UI_MODE.HIDE_OTHER)
			{
				for (int i = _backUIViewQueue.Count - 2; i >= 0; --i)
				{
					_backUIViewQueue[i].ChangeCameraRenderTexture(false);
					_backUIViewQueue[i].ChangeCanvasLayer(HideLayer);
					_backUIViewQueue[i].ChangeGraphicRaycasterState(false);
					ChangeUIViewParent(_backUIViewQueue[i], true);
				}
			}
		}

		private string GetUIAssetPathByUIType(UI_TYPE uiType)
		{
			string assetPath = string.Empty;
			if (!CommonUIData.DICT_UI_PATHS.TryGetValue(uiType, out assetPath))
			{
				throw new System.IndexOutOfRangeException(uiType.ToString());
			}
			return assetPath;
		}

		public override void Init()
		{
			UguiEventDispatcher = new CommonDispathcer<EventId>();
			SceneManager.sceneLoaded += RefreshUI;

			RefreshUI(SceneManager.GetActiveScene(), LoadSceneMode.Single);


		}

		public override void UnInit()
		{
			base.UnInit();

			UguiEventDispatcher = null;
			Release();
		}

		void RefreshUI(Scene scene, LoadSceneMode mode)
		{
			HideLayer = LayerMask.NameToLayer("Hidden");
			UILayer = LayerMask.NameToLayer("UI");
			AvatarLayer = LayerMask.NameToLayer("Avatar");

			if (_allUIView == null)
			{
				_allUIView = new Dictionary<UI_TYPE, BaseUIView>((int)UI_TYPE.UI_MAX, new UITypeComparer());
			}

			if (_backUIViewQueue == null)
			{
				_backUIViewQueue = new List<BaseUIView>((int)UI_TYPE.UI_MAX);
			}

			if (_openUIViewQueue == null)
			{
				_openUIViewQueue = new List<BaseUIView>((int)UI_TYPE.UI_MAX);
			}

			_UICamera = GameObject.FindGameObjectWithTag("HUDCamera").GetComponent<Camera>();
			
			InitRoot();
		}

		private void ChangeUIViewParent(BaseUIView baseUIView, bool isHide = false)
		{
			if (baseUIView == null || baseUIView.transform == null)
				return;

			if (isHide && _hideRoot != null)
			{
				baseUIView.transform.SetParent(_hideRoot.transform, false);
			}
			else if (baseUIView.UILayer == UI_LAYER.NORMAL && _normalLayerRoot != null)
			{
				baseUIView.transform.SetParent(_normalLayerRoot.transform, false);
				baseUIView.transform.localPosition = Vector3.zero;
			}
			else if (baseUIView.UILayer == UI_LAYER.POPUP && _popupLayerRoot != null)
			{
				baseUIView.transform.SetParent(_popupLayerRoot.transform, false);
				baseUIView.transform.localPosition = Vector3.zero;
			}
			else if (baseUIView.UILayer == UI_LAYER.MASK && _specialLayerRoot != null)
			{
				baseUIView.transform.SetParent(_specialLayerRoot.transform, false);
				baseUIView.transform.localPosition = Vector3.zero;
			}
		}

		private void InitRoot()
		{
			_rootCanvas = _UICamera.transform.Find("UguiCanvas").GetComponent<Canvas>();

			_hideRoot = CreateOrGetSubRoot("HideRoot");
			_hideRoot.transform.localScale = _uiScale;
			_hideRoot.transform.localPosition = new Vector3(0, 10000, 0);

			_normalLayerRoot = CreateOrGetSubRoot("NormalLayerRoot");
			_normalLayerRoot.transform.localScale = _uiScale;
			_normalLayerRoot.transform.localPosition = Vector3.zero;

			_popupLayerRoot = CreateOrGetSubRoot("PopupLayerRoot");
			_popupLayerRoot.transform.localScale = _uiScale;
			_popupLayerRoot.transform.localPosition = Vector3.zero;

			_specialLayerRoot = CreateOrGetSubRoot("SpecialLayerRoot");
			_specialLayerRoot.transform.localScale = _uiScale;
			_specialLayerRoot.transform.localPosition = Vector3.zero;
		}

		private GameObject CreateOrGetSubRoot(string name)
		{
			Transform findTrans = _rootCanvas.transform.Find(name);
			if (findTrans != null)
			{
				return findTrans.gameObject;
			}

			GameObject go = new GameObject(name);
			go.transform.parent = _rootCanvas.transform;
			go.layer = UILayer;

			RectTransform rect = go.AddComponent<RectTransform>();
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(1, 1);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(0, 0);
			rect.offsetMax = new Vector2(0, 0);

			return go;
		}

		#endregion PRIVATE_FUNCTION
	}
}