using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UI
{
	public class OpenUIViewParam
	{
		public UI_TYPE UIType;
		public object UIParam;
	}

	public class BaseUIView : BaseUIComponent
	{
		[HideInInspector]
		public UI_LAYER UILayer = UI_LAYER.NORMAL;
		[HideInInspector]
		public UI_MODE UIMode = UI_MODE.BACK;
		[HideInInspector]
		public UI_MODAL UIModal = UI_MODAL.NONE;
		[HideInInspector]
		[Range(0f, 1f)]
		public float BackGroundAlpha = 0.75f;
		[HideInInspector]
		public bool TouchModalBgClose = true;
		[HideInInspector]
		public float DestroyExistTime = 300f;
		[HideInInspector]
		public Canvas ContentCanvas = null;
		[HideInInspector]
		public bool IsShowAfterOpenEffect = false;
		[HideInInspector]
		public bool DeleteOnHide = false;

		protected static readonly string OPEN_EFFECT = "open";
		private Animator _UIEffectAnimator = null;

		private GameObject _backGround;
		private Color BlackBGColor = new Color(0f, 0f, 0f, 0.5f);

		protected UI_TYPE _uiType = UI_TYPE.UI_NONE;
		protected float _destroyTime = 0f;
		protected bool _isClose = false;
		protected bool _isInit = false;
		protected object _param = null;
		protected bool _isInOpenEffect = false;

		public delegate void UIHideDelegate();
		public UIHideDelegate onUIHide;
		public delegate void UIShowDelegate();
		public UIShowDelegate onUIShow;

		#region Comment
		/*
					  |
				uiView = UIManager.Instance.OpenUIView(uiType, param)
					  |
					  |                                                                                       ---
				uiView.IsShowAfterOpenEffect                                                                    |
			          |                                                                                         |
		 False                   True                                                                           |
		   |                       |                                                                            |
	  uiView.Show              uiView.ShowOpenEffect                                                            |
 uiView.ShowOpenEffect         uiView.InitUISizeDelta                                                          Init
 uiView.InitUISizeDelta        uiView.CreateBackGround                                                         Start
 uiView.CreateBackGround       uiView.InitLocalizationText                                                      |
 uiView.InitLocalizationText   uiView.AddEventListener                                                          |
 uiView.AddEventListener       uiView.OpenEffectCallback                                                        |
					           uiView.Show                                                                      |
			          |                                                                                       ---
					  |                                                                                         |
					  |                                                                                         |
					  |                                                                                         |
					  |                                                                                       ---
					  |                                                                                         |
				uiView.Core                                                                                    Core
					  |                                                                                         |
					  |                                                                                       ---
					  |                                                                                         |
				uiView.Close                                                                                   Close
				uiView.Hide                                                                                     |
				uiView.RemoveEventListener                                                                      |
			          |                                                                                       ---
				      |                                                                                         |
			    uiView.Release                                                                                 Destroy
			          |                                                                                         |
					  |                                                                                       ---
		*/
		#endregion

		#region OVERRIDE_FUNCTION
		public override void Show(object param = null)
		{
			_param = param;

			Init();

			// 已经播放过就不播了 //
			if (!IsShowAfterOpenEffect)
			{
				ShowOpenEffect();
			}

			_destroyTime = 0f;
			if (onUIShow != null)
			{
				onUIShow();
			}
		}

		public override void Core(float deltaTime)
		{
			if (_isClose)
			{
				_destroyTime += deltaTime;
			}
		}

		public override void InitLocalizationText()
		{
		}

		/// <summary>
		/// 关闭界面后调用的
		/// </summary>
		public override void Hide()
		{
			_destroyTime = 0f;
			_isClose = true;
			_isInit = false;
			RemoveEventListener();
			//EventRouter.Instance.EventDispatch<UI_TYPE>(EventId.UI_CLOSE, _uiType);
			if (onUIHide != null)
			{
				onUIHide();
			}
		}

		public override void PreCache()
		{
			base.PreCache();

			_destroyTime = 0f;
			_isClose = true;
			_isInit = false;
			ChangeCameraRenderTexture(false);
			ChangeCanvasLayer(UIManager.HideLayer);
			ChangeGraphicRaycasterState(false);
		}

		#endregion OVERRIDE_FUNCTION

		#region VIRTUAL_FUNCTION
		public virtual void Init()
		{
			if (!_isInit)
			{
				// 设置content大小 //
				//InitUISizeDelta();

				// 创建modal //
				CreateBackGround();

				// 初始化本地化文本 //
				InitLocalizationText();

				// 添加监听 //
				AddEventListener();
				_isClose = false;
				_isInit = true;
			}
		}

		public virtual void AddEventListener()
		{
		}

		public virtual void RemoveEventListener()
		{
		}

		public virtual void ShowOpenEffect()
		{
			Init();

			_UIEffectAnimator = ContentCanvas.GetComponent<Animator>();
			if (_UIEffectAnimator != null)
			{
				_UIEffectAnimator.SetTrigger(OPEN_EFFECT);
				_isInOpenEffect = true;
			}
		}

		public virtual void OpenEffectCallback()
		{
			if (IsShowAfterOpenEffect)
			{
				Show(UIParam);
			}
			_isInOpenEffect = false;
		}

		/*public virtual void InitUISizeDelta()
		{
			RectTransform rootCanvasTrans = UIManager.Instance.RootCanvas.GetComponent<RectTransform>();
			ContentCanvas.GetComponent<RectTransform>().sizeDelta = rootCanvasTrans.sizeDelta;
		}*/

		/// <summary>
		/// 关闭界面
		/// </summary>
		public virtual void Close()
		{
			if (IsNeedBack())
			{
				UIManager.Instance.CloseUIView();
			}
			else
			{
				UIManager.Instance.CloseUIView(this);
			}
		}

		/// <summary>
		/// 释放界面
		/// </summary>
		public virtual void Release()
		{

		}

		public virtual UI.FrontInfoConfigItem GetFrontInfoConfigItem()
		{
			return new UI.FrontInfoConfigItem()
			{
				enableFunctionButtonView = true,
				functionButtonViewNames = new string[] { "ButtonBack", "ButtonHome" },
				enableCoinView = true,
				coinViewNames = new string[] { "Strength", "FreeCrystal", "PayCrystal" },
				enableLocationView = true,
				bgObjName = string.Empty,
			};
		}

		#endregion VIRTUAL_FUNCTION

		/// <summary>
		/// 是否需要放入backqueue
		/// </summary>
		/// <returns></returns>
		public bool IsNeedBack()
		{
			// 弹窗view 不需要加入backqueue //
			if (UILayer == UI_LAYER.POPUP || UILayer == UI_LAYER.SPECIAL)
			{
				return false;
			}
			else if (UIMode == UI_MODE.NONE)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 是否需要删除
		/// </summary>
		/// <returns></returns>
		public bool IsNeedDestroy()
		{
			return _isClose && _destroyTime >= DestroyExistTime;
		}

		public bool IsClose()
		{
			return _isClose;
		}

		public object UIParam
		{
			get
			{
				return _param;
			}

			set
			{
				_param = value;
			}
		}

		public UI_TYPE UIType
		{
			get
			{
				return _uiType;
			}
			set
			{
				_uiType = value;
			}
		}

		/// <summary>
		/// GetCanvasPos
		/// </summary>
		/// <param name="worldPos">world Pos</param>
		/// <returns></returns>
		public Vector2 GetCanvasPos(Camera camera, Vector3 worldPos)
		{
			return RectTransformUtility.WorldToScreenPoint(camera, worldPos) - ContentCanvas.GetComponent<RectTransform>().sizeDelta / 2f;
		}

		/// <summary>
		/// 改变canvas的layer
		/// </summary>
		/// <param name="layer"></param>
		public void ChangeCanvasLayer(int layer)
		{
			List<Canvas> ltCanvas = ListPool<Canvas>.Get();
			transform.GetComponentsInChildren<Canvas>(true, ltCanvas);
			for (int i = 0; i < ltCanvas.Count; ++i)
			{
				ltCanvas[i].gameObject.layer = layer;
			}
			ListPool<Canvas>.Release(ltCanvas);
		}

		/// <summary>
		/// 改变GraphicRaycaster的enable状态
		/// 界面打开或者关闭的时候调用
		/// 禁用能减少Graphic.GetDepth()的消耗
		/// </summary>
		/// <param name="enable"></param>
		public void ChangeGraphicRaycasterState(bool enable)
		{
			List<GraphicRaycaster> lt = ListPool<GraphicRaycaster>.Get();
			GetComponentsInChildren(true, lt);
			for (int i = 0; i < lt.Count; ++i)
			{
				lt[i].enabled = enabled;
			}

			ListPool<GraphicRaycaster>.Release(lt);
		}

		/// <summary>
		/// Change Camera Render Texture
		/// </summary>
		/// <param name="enable"></param>
		public void ChangeCameraRenderTexture(bool enable)
		{
			List<MonoAvatarRenderTexture> ltAvatarRenderTexture = ListPool<MonoAvatarRenderTexture>.Get();
			transform.GetComponentsInChildren<MonoAvatarRenderTexture>(true, ltAvatarRenderTexture);
			for (int i = 0; i < ltAvatarRenderTexture.Count; ++i)
			{
				Camera camera = ltAvatarRenderTexture[i].GetRTCamera();
				if (camera != null)
				{
					camera.enabled = enable;
				}
			}
			ListPool<MonoAvatarRenderTexture>.Release(ltAvatarRenderTexture);

			List<MonoMiscShowManager> ltMiscShowManager = ListPool<MonoMiscShowManager>.Get();
			transform.GetComponentsInChildren<MonoMiscShowManager>(true, ltMiscShowManager);
			for (int i = 0; i < ltMiscShowManager.Count; ++i)
			{
				Camera camera = ltMiscShowManager[i].GetRTCamera();
				if (camera != null)
				{
					camera.enabled = enable;
				}
			}
			ListPool<MonoMiscShowManager>.Release(ltMiscShowManager);
		}

		#region MODAL BACKGROUND
		/// <summary>
		/// 创建模态底
		/// </summary>
		private void CreateBackGround()
		{
			if (_backGround != null || UIModal == UI_MODAL.NONE)
				return;

			_backGround = new GameObject("imgModalBg");
			_backGround.layer = UIManager.UILayer;
			RectTransform rectTrans = _backGround.AddComponent<RectTransform>();
			rectTrans.sizeDelta = UIManager.Instance.UISizeDelta + new Vector2(Mathf.Abs(_rightBackgroundStretch - _leftBackgroundStretch), Mathf.Abs(_topBackgroundStretch - _bottomBackgroundStretch));
			_backGround.transform.SetParent(transform, false);
			_backGround.transform.SetAsFirstSibling();
			_backGround.AddComponent<Canvas>();
			_backGround.AddComponent<GraphicRaycaster>();
			if (UIModal == UI_MODAL.BACKGROUND)
			{
				BlackBGColor.a = BackGroundAlpha;
				Image imgBG = _backGround.AddComponent<Image>();
				imgBG.color = BlackBGColor;
			}
			else if (UIModal == UI_MODAL.NORMAL)
			{
				_backGround.AddComponent<MonoEmptyForRaycast>();

			}

			EventTriggerListener.Get(_backGround).onClick = OnClickBackGround;
		}

		protected virtual void OnClickBackGround(UIEvent uiEvent)
		{
			if (TouchModalBgClose && !_isInOpenEffect)
			{
				Close();
			}
		}
		#endregion MODAL BACKGROUND

		protected T LoadUIComponent<T>(string assetPath, Transform trans = null) where T : BaseUIComponent
		{
			GameObject go = Instantiate(ResourceManager.GetInstance().GetResource(assetPath, typeof(GameObject), enResourceType.UIPrefab).Content) as GameObject;
			if (trans == null)
			{
				go.transform.SetParent(ContentCanvas.transform);
			}
			else
			{
				go.transform.SetParent(trans);
			}
			go.transform.localScale = Vector3.one;
			return go.GetComponent<T>();
		}

		#region SortingOrderWrapper_Function
		public int UpdateSortingOrder(int startOrder)
		{
			List<Canvas> ltCanvas = ListPool<Canvas>.Get();
			transform.GetComponentsInChildren<Canvas>(true, ltCanvas);
			for (int i = 0; i < ltCanvas.Count; ++i)
			{
				Canvas canvas = ltCanvas[i];
				if (canvas != null && canvas.gameObject != null)
				{
					canvas.overrideSorting = true;
					UIWrapper wrapper = canvas.GetComponent<UIWrapper>();
					if (wrapper != null && wrapper.gameObject != null)
					{
						wrapper.SortingOrder = startOrder;
						canvas.sortingOrder = wrapper.SortingOrder;
					}
					else
					{
						canvas.sortingOrder = startOrder;
					}
					++startOrder;
				}
			}
			ListPool<Canvas>.Release(ltCanvas);
			return startOrder;
		}
		#endregion

		#region Special Background
		protected static float _leftBackgroundStretch = 0;
		protected static float _rightBackgroundStretch = 0;
		protected static float _topBackgroundStretch = 0;
		protected static float _bottomBackgroundStretch = 0;

		protected static void ResetBackgroundStretch(Texture tex)
		{
			_leftBackgroundStretch = (UIManager.DEFAULT_SCREEN_SIZE.x - tex.width) * 0.5f;
			_rightBackgroundStretch = (tex.width - UIManager.DEFAULT_SCREEN_SIZE.x) * 0.5f;
		}

		protected static void ChangeBackgroundStretch(RectTransform rectTrans)
		{
			rectTrans.offsetMin = new Vector2(_leftBackgroundStretch, _bottomBackgroundStretch);
			rectTrans.offsetMax = new Vector2(_rightBackgroundStretch, _topBackgroundStretch);
		}

		public static void SetTopAndBottomBackgroundStretch(float top, float bottom)
		{
			_topBackgroundStretch = top;
			_bottomBackgroundStretch = bottom;
		}
		#endregion

		#region EDITOR
#if UNITY_EDITOR
		public void CreateUIContent()
		{
			if (ContentCanvas != null)
			{
				return;
			}

			GameObject go = new GameObject("content");
			go.transform.parent = transform;
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = Vector3.one;
			go.layer = LayerMask.NameToLayer("UI");

			ContentCanvas = go.AddComponent<Canvas>();
			go.AddComponent<GraphicRaycaster>();
			go.AddComponent<CanvasGroup>();
			go.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
			go.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
			go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
			go.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
		}

		public void OnValidate()
		{
			if (_backGround != null)
			{
				BlackBGColor.a = BackGroundAlpha;
				Image imgBG = _backGround.GetComponent<Image>();
				if (imgBG != null)
				{
					imgBG.color = BlackBGColor;
				}
			}
		}
#endif
		#endregion
	}
}
