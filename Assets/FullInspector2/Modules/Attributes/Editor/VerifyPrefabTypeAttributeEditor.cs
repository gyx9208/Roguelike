using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace FullInspector.Modules {
    [CustomAttributePropertyEditor(typeof(VerifyPrefabTypeAttribute), ReplaceOthers = false)]
    public class VerifyPrefabTypeAttributeEditor<T> : AttributePropertyEditor<T, VerifyPrefabTypeAttribute>
        where T : UnityObject {
        private static bool IsFlagSet(VerifyPrefabTypeFlags flags, VerifyPrefabTypeFlags setFlag) {
            if ((flags & setFlag) == 0) {
                return false;
            }

            return true;
        }

        private bool IsValidInstance(T element, VerifyPrefabTypeAttribute attribute) {
            if (element == null) {
                return true;
            }

			PrefabInstanceStatus prefabType = PrefabUtility.GetPrefabInstanceStatus(element);
            switch (prefabType) {
                case PrefabInstanceStatus.NotAPrefab:
                    return IsFlagSet(attribute.PrefabType, VerifyPrefabTypeFlags.NotAPrefab);
                case PrefabInstanceStatus.Connected:
                    return IsFlagSet(attribute.PrefabType, VerifyPrefabTypeFlags.Connected);
                case PrefabInstanceStatus.Disconnected:
                    return IsFlagSet(attribute.PrefabType, VerifyPrefabTypeFlags.Disconnected);
                case PrefabInstanceStatus.MissingAsset:
                    return IsFlagSet(attribute.PrefabType, VerifyPrefabTypeFlags.MissingAsset);
            }

            return false;
        }

        protected override T Edit(Rect region, GUIContent label, T element, VerifyPrefabTypeAttribute attribute, fiGraphMetadata metadata) {
            if (IsValidInstance(element, attribute) == false) {
                region.height -= Margin;

                PrefabInstanceStatus actualPrefabType = PrefabUtility.GetPrefabInstanceStatus(element); ;

                EditorGUI.HelpBox(region, "This property needs to be a " + attribute.PrefabType + ", not a " + actualPrefabType, MessageType.Error);
            }

            return element;
        }

        private const float Margin = 2f;

        protected override float GetElementHeight(GUIContent label, T element, VerifyPrefabTypeAttribute attribute, fiGraphMetadata metadata) {
            if (IsValidInstance(element, attribute) == false) {
                return 33 + Margin;
            }

            return 0;
        }
    }
}