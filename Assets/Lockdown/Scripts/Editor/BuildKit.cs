using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lockdown.EditorTools
{
    // helpers shared by the scene builders: materials, boxes, models, text, lights, particles
    public static class BuildKit
    {
        public const string MatDir = "Assets/Lockdown/Materials";
        public const string TexDir = "Assets/Lockdown/Art/Textures/ambientcg";
        public const string ModelDir = "Assets/Lockdown/Art/Models/PolyHaven";
        public const string GenTexDir = "Assets/Lockdown/Art/Textures/Generated";
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        public static void Reset() => cache.Clear();

        // ---------- textures

        public static Texture2D Tex(string path, bool normal = false)
        {
            if (!File.Exists(path)) return null;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp)
            {
                bool dirty = false;
                if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (imp.maxTextureSize > 1024) { imp.maxTextureSize = 1024; dirty = true; }
                if (dirty) imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Texture2D Acg(string id, string map)
        {
            string p = TexDir + "/" + id + "/" + id + "_" + map + ".jpg";
            return Tex(p, map == "nor_gl");
        }

        static string Save(Material m, string name)
        {
            ProjectSetup.EnsureFolder(MatDir);
            string path = MatDir + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing)
            {
                existing.shader = m.shader;
                existing.CopyPropertiesFromMaterial(m);
                existing.shaderKeywords = m.shaderKeywords;
                existing.renderQueue = m.renderQueue;
                EditorUtility.SetDirty(existing);
                cache[name] = existing;
                return path;
            }
            AssetDatabase.CreateAsset(m, path);
            cache[name] = m;
            return path;
        }

        public static Material Get(string name) => cache.TryGetValue(name, out var m) ? m : null;

        // ---------- materials

        public static Material Surface(string acgId, Vector2 tiling, float smooth = 0.3f, float metal = 0f, Color? tint = null)
        {
            string name = acgId + "_" + tiling.x.ToString("0.#") + "x" + tiling.y.ToString("0.#") + (tint.HasValue ? "_t" + ColorUtility.ToHtmlStringRGB(tint.Value) : "");
            if (cache.TryGetValue(name, out var c)) return c;
            var m = new Material(Lit);
            var diff = Acg(acgId, "diff");
            var nrm = Acg(acgId, "nor_gl");
            if (diff) m.SetTexture("_BaseMap", diff);
            if (nrm) { m.SetTexture("_BumpMap", nrm); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            m.SetTextureScale("_BaseMap", tiling);
            m.SetTextureScale("_BumpMap", tiling);
            m.SetColor("_BaseColor", tint ?? Color.white);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            Save(m, name);
            return cache[name];
        }

        public static Material Paint(string name, Color color, float smooth = 0.4f, float metal = 0f, Color? emission = null)
        {
            if (cache.TryGetValue(name, out var c)) return c;
            var m = new Material(Lit);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            Save(m, name);
            return cache[name];
        }

        // emissive-ready lit material so scripts can drive _EmissionColor
        public static Material Glow(string name, Color baseColor, Color emission) => Paint(name, baseColor, 0.6f, 0f, emission);

        public static Material Flat(string name, Color color, bool transparent, string folder = null)
        {
            string key = (folder ?? "") + name;
            if (cache.TryGetValue(key, out var c)) return c;
            var m = new Material(Unlit);
            m.SetColor("_BaseColor", color);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetOverrideTag("RenderType", "Transparent");
            }
            if (folder != null)
            {
                ProjectSetup.EnsureFolder(folder);
                string path = folder + "/" + name + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing) { existing.shader = m.shader; existing.CopyPropertiesFromMaterial(m); existing.shaderKeywords = m.shaderKeywords; existing.renderQueue = m.renderQueue; existing.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque"); cache[key] = existing; return existing; }
                AssetDatabase.CreateAsset(m, path);
                cache[key] = m;
                return m;
            }
            Save(m, name);
            return cache[name];
        }

        public static Material Custom(string name, string shader, System.Action<Material> setup)
        {
            if (cache.TryGetValue(name, out var c)) return c;
            var sh = Shader.Find(shader);
            if (!sh) Debug.LogError("[build] shader missing " + shader);
            var m = new Material(sh);
            setup?.Invoke(m);
            Save(m, name);
            return cache[name];
        }

        public static Material Glass(string name, Color tint)
        {
            if (cache.TryGetValue(name, out var c)) return c;
            var m = new Material(Lit);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Smoothness", 0.95f);
            Save(m, name);
            return cache[name];
        }

        // ---------- geometry

        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true, bool teleport = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (teleport) go.AddComponent<TeleportSurface>();
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        // box with tiling worked out from its size in meters
        public static GameObject Slab(Transform parent, string name, Vector3 center, Vector3 size, string acg, float tileMeters, float smooth = 0.3f, float metal = 0f, bool teleport = false, Color? tint = null)
        {
            // choose the two largest dims for tiling
            var dims = new[] { size.x, size.y, size.z }.OrderByDescending(v => v).ToArray();
            var tiling = new Vector2(Mathf.Max(1f, Mathf.Round(dims[0] / tileMeters * 2f) / 2f), Mathf.Max(1f, Mathf.Round(dims[1] / tileMeters * 2f) / 2f));
            return Box(parent, name, center, size, Surface(acg, tiling, smooth, metal, tint), true, teleport);
        }

        public static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, bool collider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static Transform Node(Transform parent, string name, Vector3 pos, float yaw = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = new Vector3(0f, yaw, 0f);
            return t;
        }

        // ---------- poly haven models

        public static GameObject Model(Transform parent, string id, Vector3 pos, float yaw, float scale = 1f, bool collider = true)
        {
            string dir = ModelDir + "/" + id;
            string fbx = Directory.GetFiles(dir, "*.fbx").FirstOrDefault();
            if (fbx == null) { Debug.LogError("[build] no fbx for " + id); return new GameObject(id); }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx.Replace('\\', '/'));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = id;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.localRotation;
            go.transform.localScale *= scale;
            ApplyModelMaterials(go, id, dir);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
            if (collider)
            {
                var b = WorldBounds(go);
                var col = go.AddComponent<BoxCollider>();
                col.center = go.transform.InverseTransformPoint(b.center);
                Vector3 s = go.transform.InverseTransformVector(b.size);
                col.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            }
            return go;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void ApplyModelMaterials(GameObject go, string id, string dir)
        {
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/')).ToArray();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string mname = mats[i] ? mats[i].name : id;
                    mats[i] = ModelMaterial(id, mname, files);
                }
                r.sharedMaterials = mats;
            }
        }

        static Material ModelMaterial(string id, string matName, string[] files)
        {
            string key = "ph_" + matName;
            if (cache.TryGetValue(key, out var c)) return c;
            string lower = matName.ToLowerInvariant();
            if (lower.Contains("glass"))
                return Glass("ph_" + matName, new Color(0.75f, 0.85f, 0.9f, 0.25f));
            // pick the texture set whose prefix best matches the material name
            string prefix = matName;
            string diff = files.Where(f => Path.GetFileName(f).Contains("_diff")).OrderByDescending(f => Common(Path.GetFileName(f), prefix)).FirstOrDefault();
            string stem = diff != null ? Path.GetFileName(diff).Substring(0, Path.GetFileName(diff).IndexOf("_diff")) : id;
            string nor = files.FirstOrDefault(f => Path.GetFileName(f).StartsWith(stem + "_nor_gl"));
            var m = new Material(Lit);
            if (diff != null) m.SetTexture("_BaseMap", Tex(diff));
            if (nor != null) { m.SetTexture("_BumpMap", Tex(nor, true)); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", lower.Contains("metal") || id.Contains("shelves") || id.Contains("ladder") || id.Contains("power_box") ? 0.45f : 0.3f);
            if (lower.Contains("leaves")) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0f); m.renderQueue = (int)RenderQueue.AlphaTest; }
            if (lower.Contains("screen"))
                return Custom("ph_" + matName, "Lockdown/MonitorNoise", s => { s.SetColor("_Tint", new Color(1f, 0.25f, 0.2f)); s.SetFloat("_Static", 0.3f); });
            if (lower.EndsWith("_light") || lower.Contains("emission"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 1.5f);
            }
            Save(m, key);
            return cache[key];
        }

        static int Common(string a, string b)
        {
            int n = 0;
            while (n < a.Length && n < b.Length && a[n] == b[n]) n++;
            return n;
        }

        // ---------- text

        public static TextMeshPro Label(Transform parent, string text, Vector3 pos, Vector3 euler, Vector2 size, float heightMeters, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = heightMeters * 10f;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = size;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return t;
        }

        // paper note with text, readable from the front
        public static TextMeshPro Note(Transform parent, string name, Vector3 pos, Vector3 euler, Vector2 size, string text, float textHeight = 0.012f)
        {
            var root = Node(parent, name, pos);
            root.localEulerAngles = euler;
            var paper = Prim(root, PrimitiveType.Quad, "Paper", Vector3.zero, new Vector3(size.x, size.y, 1f), Paint("paper", new Color(0.93f, 0.92f, 0.86f), 0.1f));
            paper.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return Label(root, text, new Vector3(0f, 0f, -0.001f), Vector3.zero, size * 0.9f, textHeight, new Color(0.1f, 0.1f, 0.12f), TextAlignmentOptions.TopLeft);
        }

        // ---------- lights

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            return l;
        }

        public static Light SpotLight(Transform parent, string name, Vector3 pos, Vector3 euler, Color color, float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = angle;
            l.shadows = LightShadows.None;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            return l;
        }

        // ---------- generated textures

        public static Texture2D SoftDot()
        {
            string path = GenTexDir + "/soft_dot.png";
            if (!File.Exists(path))
            {
                ProjectSetup.EnsureFolder(GenTexDir);
                const int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        a = a * a * (3f - 2f * a);
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // procedural night skyline seen through the windows
        public static Texture2D Skyline()
        {
            string path = GenTexDir + "/skyline.png";
            if (!File.Exists(path))
            {
                ProjectSetup.EnsureFolder(GenTexDir);
                const int w = 512, h = 256;
                var t = new Texture2D(w, h, TextureFormat.RGB24, false);
                var rnd = new System.Random(4);
                var heights = new int[w];
                int x0 = 0;
                while (x0 < w)
                {
                    int bw = rnd.Next(14, 48), bh = rnd.Next(40, 190);
                    for (int x = x0; x < Mathf.Min(w, x0 + bw); x++) heights[x] = bh;
                    x0 += bw + rnd.Next(0, 6);
                }
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float sky = (float)y / h;
                        var c = new Color(0.02f + 0.03f * sky, 0.03f + 0.04f * sky, 0.08f + 0.1f * sky);
                        if (y < heights[x])
                        {
                            c = new Color(0.015f, 0.018f, 0.03f);
                            bool window = (x % 6 < 3) && (y % 8 < 4);
                            if (window && rnd.NextDouble() < 0.28) c = new Color(1f, 0.8f, 0.45f) * (0.5f + (float)rnd.NextDouble() * 0.5f);
                        }
                        t.SetPixel(x, y, c);
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------- particles

        public enum Fx { Sparks, Dust, Impact, Embers, Gas, Confetti, Burst, Mist }

        public static ParticleSystem Particles(Transform parent, string name, Vector3 pos, Fx kind)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            var em = ps.emission;
            var shape = ps.shape;
            var col = ps.colorOverLifetime;
            var size = ps.sizeOverLifetime;
            var r = go.GetComponent<ParticleSystemRenderer>();
            bool additive = kind != Fx.Dust && kind != Fx.Gas && kind != Fx.Mist && kind != Fx.Confetti;
            r.sharedMaterial = ParticleMaterial(additive);
            r.shadowCastingMode = ShadowCastingMode.Off;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            col.enabled = true;
            switch (kind)
            {
                case Fx.Sparks:
                    main.duration = 0.4f; main.loop = false;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
                    main.startColor = new Color(1f, 0.75f, 0.3f);
                    main.gravityModifier = 1.2f;
                    em.rateOverTime = 0f;
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 40f; shape.radius = 0.02f;
                    col.color = Grad(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.3f, 0.05f));
                    var coll = ps.collision; coll.enabled = true; coll.type = ParticleSystemCollisionType.World; coll.bounce = 0.3f; coll.dampen = 0.4f;
                    r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 1f;
                    break;
                case Fx.Dust:
                    main.loop = true; main.playOnAwake = true;
                    main.startLifetime = 9f;
                    main.startSpeed = 0.03f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
                    main.startColor = new Color(0.9f, 0.9f, 0.85f, 0.45f);
                    main.maxParticles = 300;
                    em.rateOverTime = 25f;
                    shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(5f, 1.5f, 2f);
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.05f; noise.frequency = 0.3f;
                    col.color = Grad(new Color(1, 1, 1, 0), new Color(1, 1, 1, 0), 0.5f);
                    break;
                case Fx.Impact:
                    main.loop = true;
                    main.startLifetime = 0.25f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
                    main.startColor = new Color(1f, 0.25f, 0.2f);
                    em.rateOverTime = 40f;
                    shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.01f;
                    col.color = Grad(new Color(1f, 0.6f, 0.5f), new Color(1f, 0.1f, 0.05f));
                    break;
                case Fx.Embers:
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
                    main.startColor = new Color(1f, 0.55f, 0.15f);
                    main.gravityModifier = -0.15f;
                    em.rateOverTime = 120f;
                    shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(1.2f, 2.4f, 0.05f);
                    col.color = Grad(new Color(1f, 0.8f, 0.4f), new Color(1f, 0.2f, 0f));
                    break;
                case Fx.Gas:
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
                    main.startColor = new Color(0.85f, 0.95f, 0.9f, 0.18f);
                    main.gravityModifier = 0.02f;
                    main.maxParticles = 400;
                    em.rateOverTime = 0f;
                    shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(10f, 0.2f, 8f);
                    shape.rotation = new Vector3(90f, 0f, 0f);
                    size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
                    col.color = Grad(new Color(1, 1, 1, 0), new Color(1, 1, 1, 0), 0.6f);
                    break;
                case Fx.Confetti:
                    main.duration = 4f; main.loop = false;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 1f, 0.6f), new Color(0.3f, 0.8f, 1f));
                    main.gravityModifier = 0.3f;
                    em.rateOverTime = 0f;
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, 200), new ParticleSystem.Burst(0.8f, 120) });
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.radius = 0.3f;
                    shape.rotation = new Vector3(-90f, 0f, 0f);
                    col.color = Grad(UnityEngine.Color.white, new Color(1, 1, 1, 0), 0.8f);
                    r.sharedMaterial = ParticleMaterial(true);
                    break;
                case Fx.Burst:
                    main.duration = 0.5f; main.loop = false;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
                    main.startColor = new Color(0.4f, 1f, 0.8f);
                    em.rateOverTime = 0f;
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
                    shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.03f;
                    col.color = Grad(UnityEngine.Color.white, new Color(0.2f, 1f, 0.6f, 0f));
                    break;
                case Fx.Mist:
                    main.loop = true; main.playOnAwake = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                    main.startColor = new Color(0.6f, 0.8f, 1f, 0.08f);
                    main.maxParticles = 80;
                    em.rateOverTime = 10f;
                    shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(0.6f, 0.1f, 2f);
                    col.color = Grad(new Color(1, 1, 1, 0), new Color(1, 1, 1, 0), 0.5f);
                    break;
            }
            return ps;
        }

        static Gradient Grad(Color a, Color b, float midAlpha = -1f)
        {
            var g = new Gradient();
            if (midAlpha >= 0f)
                g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(midAlpha, 0.3f), new GradientAlphaKey(midAlpha, 0.7f), new GradientAlphaKey(0f, 1f) });
            else
                g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                    new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(b.a, 1f) });
            return g;
        }

        public static Material ParticleMaterial(bool additive)
        {
            string name = additive ? "fx_additive" : "fx_soft";
            if (cache.TryGetValue(name, out var c)) return c;
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", SoftDot());
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)(additive ? BlendMode.SrcAlpha : BlendMode.SrcAlpha));
            m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            Save(m, name);
            return cache[name];
        }
    }
}
