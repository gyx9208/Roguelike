using UnityEngine;
using UnityEditor;

/*
 * 
 * 
 * chenchanghong
 */

namespace UI
{
    [CustomEditor(typeof(BaseUIView), true)]
    public class BaseUIViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
			base.OnInspectorGUI();

            BaseUIView baseUIView = target as BaseUIView;
            if (baseUIView == null)
                return;

			if(baseUIView.gameObject.activeInHierarchy && baseUIView.ContentCanvas == null)
			{
				baseUIView.CreateUIContent();
			}

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("UI_TYPE : ", GUILayout.Width(60));
			string uiType = UIPostprocessor.GetUITypeByUIPrefabName(baseUIView.gameObject.name);
			if (System.Enum.IsDefined(typeof(UI_TYPE), uiType))
			{
				GUILayout.Label(uiType + " = " + (int)(UI_TYPE)System.Enum.Parse(typeof(UI_TYPE), uiType));
			}
			GUILayout.EndHorizontal();
        }
    }
}
