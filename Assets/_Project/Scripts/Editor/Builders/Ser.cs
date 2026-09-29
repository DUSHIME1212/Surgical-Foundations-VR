using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>Sets private [SerializeField]s from builders without making runtime fields public.</summary>
    public static class Ser
    {
        public static void Set(Object target, string field, Object value) => Edit(target, field, p => p.objectReferenceValue = value);
        public static void Set(Object target, string field, int value) => Edit(target, field, p => p.intValue = value);
        public static void Set(Object target, string field, float value) => Edit(target, field, p => p.floatValue = value);
        public static void Set(Object target, string field, bool value) => Edit(target, field, p => p.boolValue = value);
        public static void Set(Object target, string field, string value) => Edit(target, field, p => p.stringValue = value);

        public static void SetArray(Object target, string field, Object[] values) => Edit(target, field, p =>
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });

        public static void SetIntArray(Object target, string field, int[] values) => Edit(target, field, p =>
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).intValue = values[i];
        });

        static void Edit(Object target, string field, System.Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Ser] {target.GetType().Name} has no serialized field '{field}'"); return; }
            apply(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
