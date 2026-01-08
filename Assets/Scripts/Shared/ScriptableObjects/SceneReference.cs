using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Shared.ScriptableObjects
{
    [System.Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        public SceneAsset sceneAsset;
#endif
        [SerializeField] private string sceneName;
        [SerializeField] private string scenePath;

        public string SceneName => sceneName;
        public string ScenePath => scenePath;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            try
            {
                if (sceneAsset != null)
                {
                    sceneName = sceneAsset.name;
                    scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                }
            }
            catch { }
#endif
        }

        public void OnAfterDeserialize() { }
        
        // Implicit conversion to string (returns Name)
        public static implicit operator string(SceneReference s) => s != null ? s.SceneName : "";
    }
}
