using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Lockdown.EditorTools
{
    // builds the player rig: tracked head, two hands, locomotion and personal ui
    public static class RigBuilder
    {
        public static PlayerRig Build(Vector3 feet, float yaw, bool inGame)
        {
            var root = new GameObject("PlayerRig");
            root.transform.position = feet;
            root.transform.eulerAngles = new Vector3(0f, yaw, 0f);
            var rig = root.AddComponent<PlayerRig>();

            var space = new GameObject("TrackingSpace").transform;
            space.SetParent(root.transform, false);
            rig.trackingSpace = space;

            // head
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(space, false);
            headGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            headGo.tag = "MainCamera";
            var cam = headGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            headGo.AddComponent<AudioListener>();
            var tpd = headGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            tpd.positionInput = new InputActionProperty(new InputAction("HeadPosition", InputActionType.PassThrough, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction("HeadRotation", InputActionType.PassThrough, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            tpd.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            rig.head = cam;

            // fade quad and vignette right in front of the eyes
            var fadeMat = BuildKit.Custom("fade", "Lockdown/ViewOverlay", m => m.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f)));
            var fade = BuildKit.Prim(headGo.transform, PrimitiveType.Quad, "Fade", new Vector3(0f, 0f, 0.05f), new Vector3(0.5f, 0.5f, 1f), fadeMat);
            fade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var vigMat = BuildKit.Custom("vignette", "Lockdown/ViewOverlay", m =>
            {
                m.SetTexture("_BaseMap", VignetteTexture());
                m.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
                m.renderQueue = 3990;
            });
            // far enough that both eyes see the same mask
            var vig = BuildKit.Prim(headGo.transform, PrimitiveType.Quad, "Vignette", new Vector3(0f, 0f, 0.8f), new Vector3(3.2f, 3.2f, 1f), vigMat);
            var sf = headGo.AddComponent<ScreenFade>();
            sf.fadeQuad = fade.GetComponent<Renderer>();
            sf.vignette = vig.GetComponent<Renderer>();

            var lineMat = BuildKit.Custom("pointer_line", "Universal Render Pipeline/Particles/Unlit", m =>
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3100;
                m.SetOverrideTag("RenderType", "Transparent");
            });
            var gloveMat = BuildKit.Paint("glove", new Color(0.16f, 0.18f, 0.22f), 0.35f);
            var reticleMat = BuildKit.Flat("reticle", new Color(0.4f, 0.95f, 1f, 1f), false);

            rig.left = MakeHand(space, Hand.Left, gloveMat, lineMat, reticleMat);
            rig.right = MakeHand(space, Hand.Right, gloveMat, lineMat, reticleMat);

            // teleport arc and target ring
            var loco = root.AddComponent<Locomotion>();
            loco.rig = rig;
            var arcGo = new GameObject("TeleportArc");
            arcGo.transform.SetParent(root.transform, false);
            var arc = arcGo.AddComponent<LineRenderer>();
            arc.sharedMaterial = lineMat;
            arc.widthMultiplier = 0.012f;
            arc.useWorldSpace = true;
            arc.enabled = false;
            arc.shadowCastingMode = ShadowCastingMode.Off;
            loco.arc = arc;
            var marker = BuildKit.Prim(root.transform, PrimitiveType.Cylinder, "TeleportMarker", Vector3.zero, new Vector3(0.5f, 0.005f, 0.5f),
                BuildKit.Flat("teleport_marker", new Color(0.3f, 1f, 0.75f, 0.45f), true));
            marker.SetActive(false);
            loco.marker = marker.transform;

            // personal ui
            var hud = new GameObject("Hud");
            hud.AddComponent<Hud>();
            var narr = new GameObject("Narrator");
            narr.AddComponent<Narrator>();
            if (inGame)
            {
                var wrist = new GameObject("WristPanel").AddComponent<WristPanel>();
                wrist.hand = rig.left.transform;
                new GameObject("PauseMenu").AddComponent<PauseMenu>();
                new GameObject("EndScreen").AddComponent<EndScreen>();
            }
            return rig;
        }

        static HandController MakeHand(Transform space, Hand side, Material glove, Material line, Material reticleMat)
        {
            var go = new GameObject(side + "Hand");
            go.transform.SetParent(space, false);
            go.transform.localPosition = new Vector3(side == Lockdown.Hand.Left ? -0.2f : 0.2f, 1.1f, 0.3f);
            var hc = go.AddComponent<HandController>();
            hc.hand = side;

            var vis = new GameObject("Glove");
            vis.transform.SetParent(go.transform, false);
            var hv = vis.AddComponent<HandVisual>();
            hv.material = glove;
            hv.isLeft = side == Lockdown.Hand.Left;
            hc.visual = hv;

            var aim = new GameObject("Aim").transform;
            aim.SetParent(go.transform, false);
            hc.aim = aim;

            var rayGo = new GameObject("Ray");
            rayGo.transform.SetParent(go.transform, false);
            var lr = rayGo.AddComponent<LineRenderer>();
            lr.sharedMaterial = line;
            lr.widthMultiplier = 0.004f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.enabled = false;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            hc.ray = lr;

            var ret = BuildKit.Prim(go.transform, PrimitiveType.Sphere, "Reticle", Vector3.zero, Vector3.one * 0.015f, reticleMat);
            ret.SetActive(false);
            hc.reticle = ret.transform;
            return hc;
        }

        static Texture2D VignetteTexture()
        {
            string path = BuildKit.GenTexDir + "/vignette.png";
            if (!System.IO.File.Exists(path))
            {
                ProjectSetup.EnsureFolder(BuildKit.GenTexDir);
                const int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.6f, r));
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
