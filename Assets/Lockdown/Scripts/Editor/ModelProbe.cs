using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lockdown.EditorTools
{
    // prints the bounds of every imported model so placement can be tuned
    public static class ModelProbe
    {
        // total triangles and renderers per scene, a rough quest budget check
        [MenuItem("Lockdown/Check/Scene Budget")]
        public static void Budget()
        {
            foreach (var path in new[] { MenuBuilder.ScenePath, LevelBuilder.ScenePath })
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                long tris = 0; int renderers = 0, lights = 0, texts = 0;
                var per = new System.Collections.Generic.Dictionary<string, long>();
                foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!mf.sharedMesh) continue;
                    long n = mf.sharedMesh.triangles.Length / 3;
                    tris += n; renderers++;
                    string key = mf.sharedMesh.name;
                    per[key] = (per.TryGetValue(key, out var v) ? v : 0) + n;
                }
                lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
                texts = Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                var top = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>(per);
                top.Sort((a, b) => b.Value.CompareTo(a.Value));
                string heavy = "";
                for (int i = 0; i < Mathf.Min(6, top.Count); i++) heavy += top[i].Key + "=" + top[i].Value + " ";
                Debug.Log("[check] " + Path.GetFileNameWithoutExtension(path) + " tris=" + tris + " meshes=" + renderers + " lights=" + lights + " labels=" + texts + " heaviest: " + heavy);
            }
        }

        [MenuItem("Lockdown/Setup/Probe Models")]
        public static void Run()
        {
            foreach (var dir in Directory.GetDirectories("Assets/Lockdown/Art/Models/PolyHaven"))
            {
                foreach (var fbx in Directory.GetFiles(dir, "*.fbx"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx.Replace('\\', '/'));
                    if (!asset) { Debug.Log("[check] cannot load " + fbx); continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                    var rs = go.GetComponentsInChildren<Renderer>();
                    Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    var mats = new System.Text.StringBuilder();
                    foreach (var r in rs) foreach (var m in r.sharedMaterials) mats.Append(m ? m.name : "null").Append(',');
                    Debug.Log("[check] " + Path.GetFileName(dir) + " size=" + b.size.ToString("F3") + " center=" + b.center.ToString("F3") +
                              " rootRot=" + go.transform.localEulerAngles + " renderers=" + rs.Length + " mats=" + mats);
                    Object.DestroyImmediate(go);
                }
            }
        }
    }
}
