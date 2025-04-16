using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class WireframeGenerator : MonoBehaviour
{
  [MenuItem("TomsStuff/Generate Wireframe")]
  public static void GenerateWireframe()
  {
    var (_, assetPath) = GetSelectedPrefab();
    using (var scope = new PrefabUtility.EditPrefabContentsScope(assetPath))
    {
      var obj = scope.prefabContentsRoot;
      var wireframe = obj.GetComponent<Wireframe>();
      var gen = new Assets.Scripts.UI.WireframeGenerator(wireframe.transform);
      wireframe.WireframeEdges = gen.Edges;
      wireframe.ShowTransformArrow = false;
      wireframe.BlueprintTransform = obj.transform;
      wireframe.BlueprintMeshFilter = obj.GetComponent<MeshFilter>();
      wireframe.BlueprintRenderer = obj.GetComponent<MeshRenderer>();
    }
    AssetDatabase.Refresh(ImportAssetOptions.Default);
  }

  [MenuItem("TomsStuff/Generate Wireframe", validate = true)]
  public static bool ValidateGenerateWireframe()
  {
    var (go, _) = GetSelectedPrefab();
    return go != null && go.GetComponent<Wireframe>() != null;
  }

  public static (GameObject, string) GetSelectedPrefab() {
    if (Selection.activeGameObject == null) {
      return default;
    }
    var obj = Selection.activeGameObject;
    var path = AssetDatabase.GetAssetPath(obj);
    if (!string.IsNullOrEmpty(path)) {
      return (obj, path);
    }

    var stage = PrefabStageUtility.GetCurrentPrefabStage();
    if (stage == null) {
      return default;
    }

    return (stage.prefabContentsRoot, stage.assetPath);
  }
}
