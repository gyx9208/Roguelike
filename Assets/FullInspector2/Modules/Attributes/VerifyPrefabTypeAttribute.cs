using System;

namespace FullInspector {
    /// <summary>
    /// A simple verification attribute that ensures the UnityObject derived
    /// target is a prefab.
    /// </summary>
    // TODO: rename to InspectorVerifyPrefabType
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class VerifyPrefabTypeAttribute : Attribute {
        public VerifyPrefabTypeFlags PrefabType;

        public VerifyPrefabTypeAttribute(VerifyPrefabTypeFlags prefabType) {
            PrefabType = prefabType;
        }
    }

    /// <summary>
    /// The different prefab possibilities an object could be.
    /// </summary>
    [Flags]
    public enum VerifyPrefabTypeFlags {
		NotAPrefab = 1 << 0,
		Connected = 1 << 1,
		Disconnected = 1 << 2,
		MissingAsset = 1 << 3,
    }
}