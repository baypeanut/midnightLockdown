using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lockdown.EditorTools
{
    // builds the floor four level from code so the layout lives in version control
    public static partial class LevelBuilder
    {
        public const string ScenePath = "Assets/Lockdown/Scenes/Lockdown.unity";
        const float W = 12f, D = 10f, H = 5f, T = 0.2f;

        static Transform env, props, puzzles, fxRoot, lightsRoot;
        static readonly List<Light> mainLights = new List<Light>();
        static readonly List<Renderer> mainPanels = new List<Renderer>();
        static readonly List<Light> emergency = new List<Light>();
        static readonly List<Transform> beacons = new List<Transform>();
        static readonly List<Renderer> beaconLenses = new List<Renderer>();
        static SlidingDoor exitDoor;
        static DissolveShutter shutter;

        [MenuItem("Lockdown/Build/Level")]
        public static void Build()
        {
            BuildKit.Reset();
            mainLights.Clear(); mainPanels.Clear(); emergency.Clear(); beacons.Clear(); beaconLenses.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            env = new GameObject("Environment").transform;
            props = new GameObject("Props").transform;
            puzzles = new GameObject("Puzzles").transform;
            fxRoot = new GameObject("Effects").transform;
            lightsRoot = new GameObject("Lighting").transform;

            SceneLighting();
            Shell();
            Office();
            Catwalk();
            Vault();
            Furniture();
            BuildPuzzles();
            Chips();
            Zones();
            Systems();

            // held items glow on hover, which needs the emission keyword on their materials
            foreach (var g in Object.FindObjectsByType<Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.sharedMaterials)
                    {
                        if (!m || !m.HasProperty("_EmissionColor") || m.IsKeywordEnabled("_EMISSION")) continue;
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", Color.black);
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                        EditorUtility.SetDirty(m);
                    }

            var rig = RigBuilder.Build(new Vector3(8.6f, 0f, 4.2f), -90f, true);
            ProjectSetup.EnsureFolder("Assets/Lockdown/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[build] level saved " + ScenePath);
        }

        // ---------------------------------------------------------------- lighting

        static void SceneLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.03f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fogStartDistance = 7f;
            RenderSettings.fogEndDistance = 28f;
            RenderSettings.skybox = null;
            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.transform.SetParent(lightsRoot, false);
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.55f, 0.85f);
            moon.intensity = 0.18f;
            moon.shadows = LightShadows.None;
            moon.transform.eulerAngles = new Vector3(38f, -70f, 0f);
        }

        // ---------------------------------------------------------------- shell

        static void Shell()
        {
            const string wall = "PaintedPlaster017";
            // floor and ceiling
            BuildKit.Slab(env, "Floor", new Vector3(W / 2, -0.05f, D / 2), new Vector3(W, 0.1f, D), "Concrete034", 2f, 0.55f, 0f, true, new Color(0.72f, 0.73f, 0.76f));
            BuildKit.Slab(env, "Ceiling", new Vector3(W / 2, H + 0.05f, D / 2), new Vector3(W, 0.1f, D), "OfficeCeiling001", 1.2f, 0.1f);
            // west wall
            BuildKit.Slab(env, "WallWest", new Vector3(-T / 2, H / 2, D / 2), new Vector3(T, H, D), wall, 2f);
            // north wall
            BuildKit.Slab(env, "WallNorth", new Vector3(W / 2, H / 2, D + T / 2), new Vector3(W, H, T), wall, 2f);
            // south wall with elevator opening x 5.2 to 6.8, height 2.4
            BuildKit.Slab(env, "WallSouthA", new Vector3(2.6f, H / 2, -T / 2), new Vector3(5.2f, H, T), wall, 2f);
            BuildKit.Slab(env, "WallSouthB", new Vector3(9.4f, H / 2, -T / 2), new Vector3(5.2f, H, T), wall, 2f);
            BuildKit.Slab(env, "WallSouthTop", new Vector3(6f, (H + 2.4f) / 2, -T / 2), new Vector3(1.6f, H - 2.4f, T), wall, 2f);
            // east wall with window band z 2.9 to 5.7, y 1.0 to 2.8
            float ex = W + T / 2;
            BuildKit.Slab(env, "WallEastA", new Vector3(ex, H / 2, 1.45f), new Vector3(T, H, 2.9f), wall, 2f);
            BuildKit.Slab(env, "WallEastB", new Vector3(ex, H / 2, 7.85f), new Vector3(T, H, 4.3f), wall, 2f);
            BuildKit.Slab(env, "WallEastLow", new Vector3(ex, 0.5f, 4.3f), new Vector3(T, 1f, 2.8f), wall, 2f);
            BuildKit.Slab(env, "WallEastHigh", new Vector3(ex, (H + 2.8f) / 2, 4.3f), new Vector3(T, H - 2.8f, 2.8f), wall, 2f);
            var glass = BuildKit.Box(env, "Window", new Vector3(W + 0.02f, 1.9f, 4.3f), new Vector3(0.02f, 1.8f, 2.8f), BuildKit.Glass("window_glass", new Color(0.55f, 0.65f, 0.8f, 0.18f)));
            var skyMat = BuildKit.Custom("skyline", "Universal Render Pipeline/Unlit", m => m.SetTexture("_BaseMap", BuildKit.Skyline()));
            var sky = BuildKit.Prim(env, PrimitiveType.Quad, "Skyline", new Vector3(W + 3f, 2.2f, 4.3f), new Vector3(9f, 4.5f, 1f), skyMat, false, new Vector3(0f, 90f, 0f));
            // window frame bars
            var frame = BuildKit.Paint("window_frame", new Color(0.12f, 0.13f, 0.15f), 0.5f, 0.6f);
            for (int i = 0; i <= 4; i++)
                BuildKit.Box(env, "Mullion", new Vector3(W - 0.01f, 1.9f, 2.9f + i * 0.7f), new Vector3(0.05f, 1.8f, 0.05f), frame, false);
            BuildKit.Box(env, "Sill", new Vector3(W - 0.08f, 0.98f, 4.3f), new Vector3(0.2f, 0.05f, 2.9f), frame);

            // baseboards for detail
            var trim = BuildKit.Paint("baseboard", new Color(0.18f, 0.18f, 0.2f), 0.3f);
            BuildKit.Box(env, "TrimW", new Vector3(0.01f, 0.06f, D / 2), new Vector3(0.02f, 0.12f, D), trim, false);
            BuildKit.Box(env, "TrimN", new Vector3(W / 2, 0.06f, D - 0.01f), new Vector3(W, 0.12f, 0.02f), trim, false);
            BuildKit.Box(env, "TrimS", new Vector3(W / 2, 0.06f, 0.01f), new Vector3(W, 0.12f, 0.02f), trim, false);

            // elevator: cabin behind the exit door
            var cabin = BuildKit.Paint("elevator", new Color(0.55f, 0.57f, 0.6f), 0.8f, 0.8f);
            BuildKit.Box(env, "ElevatorFloor", new Vector3(6f, -0.05f, -1f), new Vector3(1.8f, 0.1f, 1.6f), BuildKit.Surface("Tiles101", new Vector2(2f, 2f), 0.5f), true, true);
            BuildKit.Box(env, "ElevatorBack", new Vector3(6f, 1.3f, -1.85f), new Vector3(1.8f, 2.6f, 0.1f), cabin);
            BuildKit.Box(env, "ElevatorL", new Vector3(5.05f, 1.3f, -1f), new Vector3(0.1f, 2.6f, 1.8f), cabin);
            BuildKit.Box(env, "ElevatorR", new Vector3(6.95f, 1.3f, -1f), new Vector3(0.1f, 2.6f, 1.8f), cabin);
            BuildKit.Box(env, "ElevatorTop", new Vector3(6f, 2.55f, -1f), new Vector3(1.8f, 0.1f, 1.8f), BuildKit.Glow("elevator_light", Color.white, new Color(1.6f, 1.6f, 1.5f)), false);
            var doorRoot = BuildKit.Node(env, "ExitDoor", new Vector3(6f, 0f, -0.02f));
            var doorPanel = BuildKit.Box(doorRoot, "Panel", new Vector3(0f, 1.2f, 0f), new Vector3(1.6f, 2.4f, 0.08f), BuildKit.Paint("door_steel", new Color(0.42f, 0.44f, 0.47f), 0.7f, 0.85f));
            exitDoor = doorRoot.gameObject.AddComponent<SlidingDoor>();
            exitDoor.panel = doorPanel.transform;
            exitDoor.openOffset = new Vector3(1.65f, 0f, 0f);
            var exitLamp = BuildKit.Prim(doorRoot, PrimitiveType.Cube, "ExitLamp", new Vector3(0f, 2.6f, 0.1f), new Vector3(0.5f, 0.12f, 0.04f), BuildKit.Glow("exit_lamp", Color.red, new Color(2f, 0.2f, 0.15f)));
            exitDoor.statusLight = exitLamp.GetComponent<Renderer>();
            BuildKit.Label(doorRoot, "EXIT", new Vector3(0f, 2.6f, 0.075f), new Vector3(0f, 180f, 0f), new Vector2(0.5f, 0.12f), 0.08f, Color.white);
        }

        // ---------------------------------------------------------------- office

        static void Office()
        {
            const string wall = "PaintedPlaster017";
            float h = 2.6f;
            // west wall at x 8 with door gap z 7.2 to 8.4
            BuildKit.Slab(env, "OfficeWallW_A", new Vector3(8f, h / 2, 6.6f), new Vector3(T, h, 1.2f), wall, 2f, 0.3f, 0f, false, new Color(0.85f, 0.9f, 0.95f));
            BuildKit.Slab(env, "OfficeWallW_B", new Vector3(8f, h / 2, 9.2f), new Vector3(T, h, 1.6f), wall, 2f, 0.3f, 0f, false, new Color(0.85f, 0.9f, 0.95f));
            BuildKit.Slab(env, "OfficeWallW_Top", new Vector3(8f, (h + 2.3f) / 2, 7.8f), new Vector3(T, h - 2.3f, 1.2f), wall, 2f, 0.3f, 0f, false, new Color(0.85f, 0.9f, 0.95f));
            BuildKit.Slab(env, "OfficeWallS", new Vector3(10f, h / 2, 6f), new Vector3(4f + T, h, T), wall, 2f, 0.3f, 0f, false, new Color(0.85f, 0.9f, 0.95f));
            BuildKit.Box(env, "OfficeCarpet", new Vector3(10.05f, 0.005f, 8.05f), new Vector3(3.9f, 0.01f, 3.9f), BuildKit.Surface("Carpet012", new Vector2(3f, 3f), 0.05f), true, true);
            BuildKit.Label(env, "J. KOWALSKI\n<size=60%>OPERATIONS MANAGER</size>", new Vector3(7.88f, 2.0f, 8.75f), new Vector3(0f, 90f, 0f), new Vector2(0.6f, 0.2f), 0.05f, new Color(0.9f, 0.9f, 0.85f));

            // office door, opens from inside only
            var doorRoot = BuildKit.Node(env, "OfficeDoor", new Vector3(8f, 0f, 7.8f));
            var panel = BuildKit.Box(doorRoot, "Panel", new Vector3(0f, 1.15f, 0f), new Vector3(0.08f, 2.3f, 1.2f), BuildKit.Surface("PaintedMetal006", Vector2.one, 0.5f, 0.6f));
            var door = doorRoot.gameObject.AddComponent<SlidingDoor>();
            door.panel = panel.transform;
            door.openOffset = new Vector3(0f, 0f, 1.25f);
            var lamp = BuildKit.Prim(doorRoot, PrimitiveType.Cube, "DoorLamp", new Vector3(0.12f, 2.42f, 0f), new Vector3(0.03f, 0.05f, 0.2f), BuildKit.Glow("door_lamp", Color.red, Color.red));
            door.statusLight = lamp.GetComponent<Renderer>();
            // release button on the inside face
            var btnRoot = BuildKit.Node(env, "DoorRelease", new Vector3(8.13f, 1.2f, 8.75f), -90f);
            BuildKit.Box(btnRoot, "Plate", new Vector3(0f, 0f, 0.01f), new Vector3(0.14f, 0.2f, 0.02f), BuildKit.Paint("plate", new Color(0.8f, 0.8f, 0.78f), 0.4f), false);
            var cap = BuildKit.Prim(btnRoot, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 0f, -0.01f), new Vector3(0.07f, 0.015f, 0.07f), BuildKit.Glow("release_green", new Color(0.1f, 0.6f, 0.2f), new Color(0.1f, 1.2f, 0.3f)), true, new Vector3(90f, 0f, 0f));
            var press = btnRoot.gameObject.AddComponent<Pressable>();
            press.cap = cap.transform;
            press.onPress.AddListener(door.Open);
            BuildKit.Label(btnRoot, "DOOR\nRELEASE", new Vector3(0f, 0.15f, -0.002f), Vector3.zero, new Vector2(0.2f, 0.08f), 0.022f, Color.white);
        }

        // ---------------------------------------------------------------- catwalk, roof and ladders

        static void Catwalk()
        {
            const string plate = "DiamondPlate001";
            float top = 2.7f;
            // office roof doubles as the catwalk deck, with a hatch at x 10.6 to 11.4, z 8.6 to 9.4
            BuildKit.Slab(env, "RoofW", new Vector3(9.3f, top - 0.05f, 8f), new Vector3(2.6f, 0.1f, 4f), plate, 1f, 0.5f, 0.8f, true);
            BuildKit.Slab(env, "RoofE", new Vector3(11.7f, top - 0.05f, 8f), new Vector3(0.6f, 0.1f, 4f), plate, 1f, 0.5f, 0.8f, true);
            BuildKit.Slab(env, "RoofS", new Vector3(11f, top - 0.05f, 7.3f), new Vector3(0.8f, 0.1f, 2.6f), plate, 1f, 0.5f, 0.8f, true);
            BuildKit.Slab(env, "RoofN", new Vector3(11f, top - 0.05f, 9.7f), new Vector3(0.8f, 0.1f, 0.6f), plate, 1f, 0.5f, 0.8f, true);
            // office ceiling seen from inside
            BuildKit.Box(env, "OfficeCeiling", new Vector3(10f, 2.58f, 8f), new Vector3(4f, 0.02f, 4f), BuildKit.Surface("OfficeCeiling001", new Vector2(4f, 4f), 0.1f), false);
            // walkway from the ladder to the roof along the north wall
            BuildKit.Slab(env, "Walkway", new Vector3(6.8f, top - 0.05f, 9.45f), new Vector3(2.4f, 0.1f, 1.1f), plate, 1f, 0.5f, 0.8f, true);

            var rail = BuildKit.Paint("rail_yellow", new Color(0.95f, 0.75f, 0.1f), 0.5f, 0.3f);
            // walkway south rail with a gap for ladder A at x 5.7 to 6.5
            Rail(new Vector3(6.5f, top, 8.92f), new Vector3(8f, top, 8.92f), rail);
            Rail(new Vector3(5.62f, top, 8.92f), new Vector3(5.62f, top, 10f), rail);
            // roof edges
            Rail(new Vector3(8f, top, 6.02f), new Vector3(12f, top, 6.02f), rail);
            Rail(new Vector3(8.02f, top, 6.02f), new Vector3(8.02f, top, 8.9f), rail);
            // hatch guard on the south side only, the west side is the way to the ladder
            Rail(new Vector3(10.55f, top, 8.55f), new Vector3(11.45f, top, 8.55f), rail);
            BuildKit.Label(env, "HATCH\n<size=60%>office below</size>", new Vector3(11f, top + 0.012f, 8.35f), new Vector3(90f, 0f, 0f), new Vector2(0.8f, 0.3f), 0.07f, new Color(0.95f, 0.75f, 0.1f));

            // ladder a: hall floor to walkway, rails extend a meter above the deck
            Ladder("LadderA", new Vector3(6.1f, 0f, 8.78f), 0f, top + 1.0f);
            // ladder b: roof hatch down into the office
            Ladder("LadderB", new Vector3(11f, 0f, 9.34f), 180f, top + 1.0f);
            BuildKit.Label(env, "MAINTENANCE\nACCESS", new Vector3(6.1f, 1.6f, 9.88f), new Vector3(0f, 0f, 0f), new Vector2(0.6f, 0.2f), 0.06f, new Color(0.95f, 0.75f, 0.1f));
        }

        static void Rail(Vector3 a, Vector3 b, Material m)
        {
            Vector3 mid = (a + b) / 2f;
            float len = Vector3.Distance(a, b);
            var root = BuildKit.Node(env, "Rail", mid);
            root.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            BuildKit.Box(root, "Top", new Vector3(0f, 1.0f, 0f), new Vector3(0.05f, 0.05f, len), m);
            BuildKit.Box(root, "Mid", new Vector3(0f, 0.5f, 0f), new Vector3(0.03f, 0.03f, len), m, false);
            int posts = Mathf.Max(2, Mathf.CeilToInt(len / 1.2f) + 1);
            for (int i = 0; i < posts; i++)
                BuildKit.Box(root, "Post", new Vector3(0f, 0.5f, -len / 2 + len * i / (posts - 1)), new Vector3(0.05f, 1f, 0.05f), m);
            // invisible barrier so the player cannot walk through
            var wallGo = BuildKit.Box(root, "Guard", new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 1.1f, len), m);
            wallGo.GetComponent<Renderer>().enabled = false;
        }

        static void Ladder(string name, Vector3 basePos, float yaw, float height)
        {
            var root = BuildKit.Node(env, name, basePos, yaw);
            var steel = BuildKit.Paint("ladder_steel", new Color(0.55f, 0.57f, 0.6f), 0.6f, 0.9f);
            BuildKit.Box(root, "RailL", new Vector3(-0.23f, height / 2, 0f), new Vector3(0.05f, height, 0.06f), steel);
            BuildKit.Box(root, "RailR", new Vector3(0.23f, height / 2, 0f), new Vector3(0.05f, height, 0.06f), steel);
            int rungs = Mathf.FloorToInt((height - 0.25f) / 0.3f);
            for (int i = 0; i <= rungs; i++)
            {
                var rung = BuildKit.Prim(root, PrimitiveType.Cylinder, "Rung", new Vector3(0f, 0.25f + i * 0.3f, 0f), new Vector3(0.035f, 0.23f, 0.035f), steel, true, new Vector3(0f, 0f, 90f));
                rung.AddComponent<Climbable>();
            }
            // rails are grippable too
            foreach (Transform c in root)
                if (c.name.StartsWith("Rail")) c.gameObject.AddComponent<Climbable>();
        }

        // ---------------------------------------------------------------- vault

        static void Vault()
        {
            const string conc = "Concrete034";
            // east wall x 4, z 7 to 10
            BuildKit.Slab(env, "VaultWallE", new Vector3(4f, H / 2, 8.5f), new Vector3(T, H, 3f + T), conc, 2f);
            // south wall z 7 with entrance x 1.2 to 2.6
            BuildKit.Slab(env, "VaultWallS_A", new Vector3(0.6f, H / 2, 7f), new Vector3(1.2f, H, T), conc, 2f);
            BuildKit.Slab(env, "VaultWallS_B", new Vector3(3.3f, H / 2, 7f), new Vector3(1.4f, H, T), conc, 2f);
            BuildKit.Slab(env, "VaultWallS_Top", new Vector3(1.9f, (H + 2.4f) / 2, 7f), new Vector3(1.4f, H - 2.4f, T), conc, 2f);
            BuildKit.Box(env, "VaultFloor", new Vector3(2f, 0.005f, 8.5f), new Vector3(3.9f, 0.01f, 2.9f), BuildKit.Surface("Rubber004", new Vector2(3f, 2f), 0.2f), true, true);
            BuildKit.Label(env, "SERVER VAULT\n<size=55%>authorised staff only</size>", new Vector3(1.9f, 2.75f, 6.88f), new Vector3(0f, 0f, 0f), new Vector2(1.4f, 0.3f), 0.09f, new Color(0.95f, 0.75f, 0.1f));

            // dissolving security shutter
            var shRoot = BuildKit.Node(env, "VaultShutter", new Vector3(1.9f, 1.2f, 7f));
            var shMat = BuildKit.Custom("shutter_dissolve", "Lockdown/Dissolve", m =>
            {
                m.SetTexture("_BaseMap", BuildKit.Acg("Metal009", "diff"));
                m.SetColor("_BaseColor", new Color(0.55f, 0.57f, 0.6f));
                m.SetColor("_EdgeColor", new Color(1f, 0.4f, 0.1f));
                m.SetFloat("_Cutoff", 0f);
            });
            var sh = BuildKit.Box(shRoot, "Shutter", Vector3.zero, new Vector3(1.4f, 2.4f, 0.06f), shMat);
            shutter = shRoot.gameObject.AddComponent<DissolveShutter>();
            shutter.target = sh.GetComponent<Renderer>();
            shutter.blocker = sh.GetComponent<Collider>();
            shutter.embers = BuildKit.Particles(shRoot, "Embers", new Vector3(0f, 0f, -0.05f), BuildKit.Fx.Embers);

            // server racks with blinking leds
            var rackMat = BuildKit.Surface("PaintedMetal006", Vector2.one, 0.5f, 0.6f, new Color(0.2f, 0.21f, 0.24f));
            var ledG = BuildKit.Glow("led_green", Color.green, new Color(0.2f, 2f, 0.4f));
            var ledB = BuildKit.Glow("led_blue", Color.cyan, new Color(0.2f, 0.9f, 2f));
            Vector3[] racks = { new Vector3(0.5f, 1f, 7.8f), new Vector3(0.5f, 1f, 8.5f), new Vector3(0.5f, 1f, 9.2f), new Vector3(1.6f, 1f, 9.55f), new Vector3(3.3f, 1f, 9.55f) };
            for (int i = 0; i < racks.Length; i++)
            {
                bool west = i < 3;
                var r = BuildKit.Box(env, "Rack", racks[i], west ? new Vector3(0.8f, 2f, 0.6f) : new Vector3(0.6f, 2f, 0.7f), rackMat);
                for (int k = 0; k < 8; k++)
                {
                    Vector3 face = west ? new Vector3(0.41f, 0f, 0f) : new Vector3(0f, 0f, -0.36f);
                    Vector3 ledPos = racks[i] + face + new Vector3(0f, -0.8f + k * 0.22f, 0f);
                    var led = BuildKit.Box(env, "Led", ledPos, west ? new Vector3(0.01f, 0.02f, 0.4f) : new Vector3(0.4f, 0.02f, 0.01f), k % 3 == 0 ? ledB : ledG, false);
                    var blink = led.AddComponent<Blinker>();
                    blink.period = 0.4f + (i * 8 + k) % 7 * 0.17f;
                }
            }
            BuildKit.Particles(env, "ColdMist", new Vector3(2f, 0.05f, 8.4f), BuildKit.Fx.Mist);
            var vaultLight = BuildKit.PointLight(lightsRoot, "VaultLight", new Vector3(2f, 3.6f, 8.5f), new Color(0.45f, 0.7f, 1f), 1.8f, 7f);
        }

        // ---------------------------------------------------------------- systems

        static void Systems()
        {
            // ceiling fixtures with tube glow
            Vector3[] fixtures = { new Vector3(3f, 4.1f, 2.5f), new Vector3(9f, 4.1f, 2.5f), new Vector3(6f, 4.1f, 5f), new Vector3(3f, 4.1f, 5.5f), new Vector3(10f, 2.45f, 8f) };
            var tubeMat = BuildKit.Glow("tube", new Color(0.9f, 0.9f, 0.88f), Color.black);
            for (int i = 0; i < fixtures.Length; i++)
            {
                bool office = i == 4;
                var f = BuildKit.Model(props, "mounted_fluorescent_lights", fixtures[i] + Vector3.up * (office ? 0.1f : 0f), 0f, 1f, false);
                var tube = BuildKit.Box(props, "Tube", fixtures[i] + Vector3.down * 0.03f, new Vector3(0.8f, 0.02f, 0.5f), tubeMat, false);
                mainPanels.Add(tube.GetComponent<Renderer>());
                if (!office)
                {
                    var chainMat = BuildKit.Paint("chain", new Color(0.3f, 0.3f, 0.32f), 0.4f, 0.8f);
                    BuildKit.Box(props, "Hanger", fixtures[i] + new Vector3(-0.35f, 0.45f, 0f), new Vector3(0.01f, 0.9f, 0.01f), chainMat, false);
                    BuildKit.Box(props, "Hanger", fixtures[i] + new Vector3(0.35f, 0.45f, 0f), new Vector3(0.01f, 0.9f, 0.01f), chainMat, false);
                }
                var l = BuildKit.PointLight(lightsRoot, "MainLight" + i, fixtures[i] + Vector3.down * 0.3f, new Color(1f, 0.95f, 0.85f), office ? 1.4f : 2.6f, office ? 5f : 9f, i == 2);
                mainLights.Add(l);
            }

            // emergency beacons
            Vector3[] bpos = { new Vector3(0.15f, 3.4f, 2.5f), new Vector3(11.85f, 3.4f, 1.2f), new Vector3(7f, 3.4f, 9.85f) };
            var lens = BuildKit.Glow("beacon_lens", new Color(1f, 0.1f, 0.05f), new Color(2.5f, 0.15f, 0.1f));
            var baseMat = BuildKit.Paint("beacon_base", new Color(0.15f, 0.15f, 0.15f), 0.3f);
            foreach (var p in bpos)
            {
                var b = BuildKit.Node(props, "Beacon", p);
                BuildKit.Prim(b, PrimitiveType.Cylinder, "Base", Vector3.zero, new Vector3(0.14f, 0.03f, 0.14f), baseMat);
                var spin = BuildKit.Node(b, "Spin", new Vector3(0f, 0.08f, 0f));
                var dome = BuildKit.Prim(spin, PrimitiveType.Sphere, "Lens", Vector3.zero, new Vector3(0.12f, 0.14f, 0.12f), lens);
                BuildKit.Prim(spin, PrimitiveType.Cube, "Mirror", new Vector3(0f, 0f, 0.02f), new Vector3(0.08f, 0.1f, 0.01f), baseMat);
                beacons.Add(spin);
                beaconLenses.Add(dome.GetComponent<Renderer>());
                emergency.Add(BuildKit.PointLight(lightsRoot, "Emergency", p + Vector3.down * 0.3f, new Color(1f, 0.12f, 0.08f), 1.6f, 8f));
            }

            var gridGo = new GameObject("PowerGrid");
            var grid = gridGo.AddComponent<PowerGrid>();
            grid.mainLights = mainLights.ToArray();
            grid.mainLightPanels = mainPanels.ToArray();
            grid.emergencyLights = emergency.ToArray();
            grid.beacons = beacons.ToArray();
            grid.beaconLenses = beaconLenses.ToArray();
            var alarmGo = BuildKit.Node(fxRoot, "Alarm", new Vector3(6f, 4f, 5f));
            grid.alarm = Sfx.Loop(alarmGo.gameObject, "alarm_whoop", 0.22f, 20f);
            var humGo = BuildKit.Node(fxRoot, "Hum", new Vector3(6f, 3.5f, 4f));
            grid.hum = Sfx.Loop(humGo.gameObject, "room_hum_loop", 0.25f, 14f);
            grid.hum.playOnAwake = false;
            var ventGo = BuildKit.Node(fxRoot, "VentWind", new Vector3(8f, 3.6f, 9.4f));
            Sfx.Loop(ventGo.gameObject, "vent_wind_loop", 0.5f, 5f);

            // game director, effects
            var dir = new GameObject("GameDirector").AddComponent<GameDirector>();
            var fx = new GameObject("SuppressionFx").AddComponent<SuppressionFx>();
            fx.gas = BuildKit.Particles(fxRoot, "HalonGas", new Vector3(6f, 4.6f, 5f), BuildKit.Fx.Gas);
            fx.celebration = BuildKit.Particles(fxRoot, "Celebration", new Vector3(6f, 0.3f, 4f), BuildKit.Fx.Confetti);
            var hb = BuildKit.Node(fxRoot, "Heartbeat", new Vector3(6f, 1.5f, 5f));
            fx.heartbeat = Sfx.Loop(hb.gameObject, "heartbeat_loop", 0.5f, 30f);
            fx.heartbeat.playOnAwake = false;
            fx.heartbeat.spatialBlend = 0f;
            BuildKit.Particles(fxRoot, "CatwalkDust", new Vector3(8.5f, 3.6f, 8.5f), BuildKit.Fx.Dust);
            BuildKit.Particles(fxRoot, "HallDust", new Vector3(6f, 2f, 3.5f), BuildKit.Fx.Dust);
        }
    }
}
