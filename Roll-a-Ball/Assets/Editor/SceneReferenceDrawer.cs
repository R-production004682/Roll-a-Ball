#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Roll_a_Ball.OutGame.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public sealed class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var pathProperty = property.FindPropertyRelative("scenePath");
            var guidProperty = property.FindPropertyRelative("sceneGuid");
            var path = string.IsNullOrEmpty(guidProperty.stringValue)
                ? pathProperty.stringValue : AssetDatabase.GUIDToAssetPath(guidProperty.stringValue);
            var current = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            var selected = (SceneAsset)EditorGUI.ObjectField(position, label, current, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                pathProperty.stringValue = selected == null ? string.Empty : AssetDatabase.GetAssetPath(selected);
                guidProperty.stringValue = selected == null ? string.Empty : AssetDatabase.AssetPathToGUID(pathProperty.stringValue);
            }
            EditorGUI.EndProperty();
        }
    }

    /// <summary>Scene の移動・改名後も、ビルドに含める遷移パスを最新にする。</summary>
    public sealed class SceneReferenceBuildProcessor : UnityEditor.Build.IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, UnityEditor.Build.Reporting.BuildReport report)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var button in root.GetComponentsInChildren<SceneTransitionButton>(true))
                    button.SynchronizeDestination();
        }
    }
}
#endif
