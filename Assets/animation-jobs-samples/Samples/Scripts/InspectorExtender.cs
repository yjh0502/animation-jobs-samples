using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class InspectorExtender : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var properties = target
            .GetType()
            .GetProperties()
            .Where(p => p.CanRead && p.DeclaringType.IsSubclassOf(typeof(MonoBehaviour)))
            .ToArray();

        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };


        if (properties.Length != 0) {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Properties", style);
        }
        foreach (var p in properties) {
            var type = p.PropertyType;
            try {
                object value = p.GetValue(target);
                if (type == typeof(int)) {
                    value = EditorGUILayout.IntField(p.Name, (int)value);
                }
                else if (type == typeof(float)) {
                    value = EditorGUILayout.FloatField(p.Name, (float)value);
                }
                else if (type == typeof(bool)) {
                    value = EditorGUILayout.Toggle(p.Name, (bool)value);
                }
                else if (type == typeof(string)) {
                    value = EditorGUILayout.TextField(p.Name, (string)value);
                }
                else if (type.IsEnum) {
                    value = EditorGUILayout.EnumPopup(p.Name, (System.Enum)value);
                }
                else if (type == typeof(Vector2Int)) {
                    value = EditorGUILayout.Vector2IntField(p.Name, (Vector2Int)value);
                }
                else if (type.IsSubclassOf(typeof(Object))) {
                    value = EditorGUILayout.ObjectField(p.Name, (Object)value, p.PropertyType, false);
                }
                else if (type.IsInterface) {
                    var v = EditorGUILayout.ObjectField(p.Name, (Object)value, p.PropertyType, false);
                    if (v.GetType().IsAssignableFrom(type)) {
                        value = v;
                    }
                }
                else {
                    continue;
                }

                if (p.CanWrite) {
                    p.SetValue(target, value);
                }
            }
            catch {
                continue;
            }
        }

        var methods = target
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0 && !m.IsSpecialName)
            .ToArray();

        if (methods.Length != 0) {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Methods", style);
        }

        var go = ((MonoBehaviour)target).gameObject;

        foreach (var m in methods) {
            if (GUILayout.Button($"{m.ReturnType.Name} {m.Name}()")) {
                m.Invoke(target, new object[0]);

                if (!EditorApplication.isPlaying) {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }
        }
    }
}
