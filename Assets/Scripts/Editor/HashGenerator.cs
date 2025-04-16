using UnityEditor;
using UnityEngine;

public class HashGenerator : EditorWindow
{
  static HashGenerator()
  {
    EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
  }

  static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
  {
    string pathPrefix;

    if (property.type == "int" && property.propertyPath.EndsWith("Hash"))
    {
      pathPrefix = property.propertyPath[..^4];
    }
    else if (property.type == "string" && property.propertyPath.EndsWith("Name"))
    {
      pathPrefix = property.propertyPath[..^4];
    }
    else if (property.type == "string" && property.propertyPath.EndsWith("Key"))
    {
      pathPrefix = property.propertyPath[..^3];
    }
    else
    {
      return;
    }

    var obj = property.serializedObject;
    var nameProp = obj.FindProperty($"{pathPrefix}Key") ?? obj.FindProperty($"{pathPrefix}Name");
    var hashProp = obj.FindProperty($"{pathPrefix}Hash");

    if (nameProp != null && hashProp != null)
    {
      menu.AddItem(new GUIContent("Generate Hash"), false, () =>
      {
        hashProp.intValue = Animator.StringToHash(nameProp.stringValue);
        obj.ApplyModifiedProperties();
      });
    }
  }
}
