using UnityEngine;
using System.Collections.Generic;


namespace UI
{
	public class BaseUIComponent : MonoBehaviour
	{
		#region UNITY_FUNCTION
		private void Awake()
		{

		}

		private void Start()
		{

		}
		#endregion UNITY_FUNCTION

		#region VIRTUAL_FUNCTION
		#region UNITY_FUNCTION
		protected virtual void OnDestroy()
		{

		}

		protected virtual void OnEnable()
		{

		}

		protected virtual void OnDisable()
		{

		}
		#endregion UNITY_FUNCTION

		public virtual void Show(object param = null)
		{

		}

		public virtual void Back()
		{

		}

		public virtual void Core(float deltaTime)
		{

		}

		public virtual void PreCache()
		{

		}

		/// <summary>
		/// 初始化本地化文字 
		/// </summary>
		public virtual void InitLocalizationText()
		{

		}

		/// <summary>
		/// 隐藏界面
		/// </summary>
		public virtual void Hide()
		{

		}
		#endregion VIRTUAL_FUNCTION

		public static T SpawnOneUIItem<T>(string assetPath, List<T> lt, Transform parent) where T : BaseUIComponent
		{
			GameObject go = Resources.Load<GameObject>(assetPath);
			T item = go.GetComponent<T>();
			lt.Add(item);
			go.transform.SetParent(parent, false);
			return item;
		}

		public static void ReleaseAllUIItem<T>(string assetPath, List<T> lt) where T : BaseUIComponent
		{
			if (lt == null)
			{
				return;
			}

			if (ResourceManager.HasInstance())
			{
				for (int i = 0; i < lt.Count; ++i)
				{
					T item = lt[i];
					if (item == null)
					{
						continue;
					}
					item.Hide();
					CGameObjectPool.Instance.RecycleGameObject(lt[i].gameObject);
				}
			}
			lt.Clear();
		}

		public static void ReleaseOneUIItem<T>(string assetPath, T item) where T : BaseUIComponent
		{
			if (item == null)
			{
				return;
			}

			if (ResourceManager.HasInstance())
			{
				item.Hide();
				CGameObjectPool.Instance.RecycleGameObject(item.gameObject);
			}
		}

		public static UIWrapper AddWrapperObject(GameObject go, Transform parent, int offsetSortingOrder = 0, int siblingIndex = -1)
		{
			GameObject wrapRoot = GameObject.Instantiate(ResourceManager.Instance.GetGameObject(UIWrapper.GetAssetPath())) as GameObject;
			wrapRoot.transform.SetParent(parent, false);
			wrapRoot.transform.localScale = Vector3.one;
			if (siblingIndex >= 0) wrapRoot.transform.SetSiblingIndex(siblingIndex);
			UIWrapper uiWrapper = wrapRoot.GetComponent<UIWrapper>();
			uiWrapper.WrapTarget = go;
			uiWrapper.OffsetSortingOrder = offsetSortingOrder;
			UIManager.Instance.RefreshUISortingOrder();
			return uiWrapper;
		}
	}
}
