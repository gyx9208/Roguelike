using System;
using UnityEditor;
using UnityEngine;

namespace Fundamental
{
	[CustomEditor(typeof(DebugConfig))]
	public class DebugConfigEditor : Editor
	{
		private SerializedProperty _enable;
		private SerializedProperty _switch;

		private void OnEnable()
		{
			int count = Enum.GetValues(typeof(DebugPrefix)).Length;
			DebugConfig config = target as DebugConfig;

			if (config != null)
			{
				if (config.Switch == null)
					config.Switch = new bool[0];

				if (config.Switch.Length != count)
				{
					bool[] newlist = new bool[count];
					for (int i = 0; i < config.Switch.Length && i < count; i++)
					{
						newlist[i] = config.Switch[i];
					}
					config.Switch = newlist;
				}
			}

			_enable = serializedObject.FindProperty("Enable");
			_switch = serializedObject.FindProperty("Switch");
		}
		
		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(_enable);
			for(int i = 0; i < _switch.arraySize; i++)
			{
				EditorGUILayout.PropertyField(_switch.GetArrayElementAtIndex(i), new GUIContent(((DebugPrefix)i).ToString()));
			}
			if (EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
		}
	}
}