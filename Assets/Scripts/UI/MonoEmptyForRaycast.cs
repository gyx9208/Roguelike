using UnityEngine;
using UnityEngine.UI;

/*
 *	
 *	For Mask Touch
 *
 *	by Xuanyi
 *
 */

namespace UI
{
	public class MonoEmptyForRaycast : MaskableGraphic
	{
#if UNITY_EDITOR
		public bool ShowRect = false;
#endif
		protected MonoEmptyForRaycast()
		{
			useLegacyMeshGeneration = false;
		}

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			toFill.Clear();
		}
#if UNITY_EDITOR
		private static Vector3[] _fourCorners = new Vector3[4];
		void OnDrawGizmos()
		{
			if (ShowRect && raycastTarget)
			{
				RectTransform rectTransform = transform as RectTransform;
				rectTransform.GetWorldCorners(_fourCorners);
				Gizmos.color = Color.red;
				for (int i = 0; i < _fourCorners.Length; ++i)
				{
					Gizmos.DrawLine(_fourCorners[i], _fourCorners[(i + 1) % 4]);
				}
			}
		}
#endif
	}
}
