using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Lockdown.EditorTools
{
    public static partial class LevelBuilder
    {
        // ---------------------------------------------------------------- furniture and dressing

        static void Furniture()
        {
            // open office desks
            BuildKit.Model(props, "metal_office_desk", new Vector3(2.6f, 0f, 1.5f), 0f);
            BuildKit.Model(props, "metal_office_desk", new Vector3(9.3f, 0f, 0.75f), 0f);
            BuildKit.Model(props, "metal_office_desk", new Vector3(9.3f, 0f, 3.7f), 180f);
            BuildKit.Model(props, "classic_laptop", new Vector3(9.0f, 0.79f, 0.7f), 180f);
            BuildKit.Model(props, "classic_laptop", new Vector3(9.6f, 0.79f, 3.75f), 0f);
            BuildKit.Model(props, "television_02", new Vector3(2.2f, 0.79f, 1.3f), 180f);
            BuildKit.Model(props, "stationery_supplies", new Vector3(3.2f, 0.79f, 1.4f), 20f, 1f, false);
            BuildKit.Model(props, "desk_lamp_arm_01", new Vector3(10.1f, 0.79f, 0.45f), 200f, 0.8f, false);
            // tall drawer cabinet on the west wall, a fuse sits on top
            BuildKit.Model(props, "drawer_cabinet", new Vector3(0.3f, 0f, 1.5f), 90f);
            BuildKit.Model(props, "modern_wooden_cabinet", new Vector3(2.6f, 0f, 0.35f), 0f);
            BuildKit.Model(props, "potted_plant_02", new Vector3(0.55f, 0f, 0.5f), 30f);
            BuildKit.Model(props, "WetFloorSign_01", new Vector3(7.3f, 0f, 0.9f), 35f);
            BuildKit.Model(props, "WetFloorSign_01", new Vector3(11.3f, 0f, 0.6f), 120f);
            // storage corner by ladder a
            BuildKit.Model(props, "steel_frame_shelves_01", new Vector3(4.85f, 0f, 9.7f), 180f);
            BuildKit.Model(props, "cardboard_box_01", new Vector3(7.2f, 0f, 6.7f), 15f);
            BuildKit.Model(props, "cardboard_box_01", new Vector3(7.6f, 0f, 6.3f), -20f);
            BuildKit.Model(props, "cardboard_box_01", new Vector3(7.35f, 0.34f, 6.55f), 70f);
            BuildKit.Model(props, "ladder_sectioned_01", new Vector3(4.22f, 0f, 8.1f), 90f);
            // office interior
            BuildKit.Model(props, "metal_office_desk", new Vector3(10.1f, 0.01f, 6.6f), 0f);
            BuildKit.Model(props, "classic_laptop", new Vector3(10.6f, 0.8f, 6.55f), 180f);
            BuildKit.Model(props, "desk_lamp_arm_01", new Vector3(9.4f, 0.8f, 6.4f), 160f, 0.8f, false);
            BuildKit.Model(props, "modern_wooden_cabinet", new Vector3(10.1f, 0.01f, 9.72f), 180f, 0.7f);
            // walls: power box, alarm, cameras
            BuildKit.Model(props, "power_box_01", new Vector3(0.12f, 2.2f, 5.0f), 90f, 1f, false);
            BuildKit.Model(props, "fire_alarm", new Vector3(0.08f, 1.5f, 5.3f), 90f, 1.4f, false);
            BuildKit.Model(props, "fire_alarm", new Vector3(7.88f, 1.5f, 6.6f), -90f, 1.4f, false);
            Camera3(new Vector3(0.3f, 4.2f, 0.3f), 45f);
            Camera3(new Vector3(11.7f, 4.2f, 0.3f), -45f);
            Camera3(new Vector3(7.7f, 4.2f, 9.7f), 200f);

            // warden screens with the custom monitor shader
            var screenMat = BuildKit.Custom("warden_screen", "Lockdown/MonitorNoise", m =>
            {
                m.SetColor("_Tint", new Color(1f, 0.22f, 0.15f));
                m.SetFloat("_Static", 0.22f);
            });
            WallScreen(new Vector3(1.9f, 3.5f, 6.88f), 0f, new Vector2(1.6f, 0.9f), screenMat);
            WallScreen(new Vector3(6f, 3.3f, 0.12f), 180f, new Vector2(1.8f, 1f), screenMat);
            WallScreen(new Vector3(0.12f, 3.2f, 7.5f), -90f, new Vector2(1.2f, 0.7f), screenMat);

            // poster with the fuse code and the mirror tip
            var poster = BuildKit.Note(props, "SafetyPoster", new Vector3(0.12f, 1.55f, 3.1f), new Vector3(0f, -90f, 0f), new Vector2(0.42f, 0.56f),
                "<b>FLOOR 4 ELECTRICAL</b>\n\nFuse ratings must match the slot label above each holder.\n\nA wrong rating will arc.\n\nAfter all three are seated, pull the MAIN lever down firmly.\n\n<size=80%>facilities, ext 4410</size>", 0.016f);
            BuildKit.Note(props, "LaserMemo", new Vector3(11.88f, 1.65f, 2.75f), new Vector3(0f, 90f, 0f), new Vector2(0.32f, 0.3f),
                "<b>VAULT OPTICAL LOCK</b>\nemitter runs on the 12V pack in K's safe.\nmirrors turn in 15 degree steps.\nsensor is right of the vault door.", 0.013f);
        }

        static void Camera3(Vector3 pos, float yaw)
        {
            var root = BuildKit.Node(props, "SecurityCamera", pos, yaw);
            var head = BuildKit.Node(root, "Head", Vector3.zero);
            BuildKit.Model(head, "security_camera_01", new Vector3(0f, -0.12f, 0f), 180f, 1f, false);
            var led = BuildKit.Prim(head, PrimitiveType.Sphere, "Led", new Vector3(0.05f, 0f, 0.25f), Vector3.one * 0.02f, BuildKit.Glow("cam_led", Color.red, new Color(3f, 0.1f, 0.1f)));
            var wc = root.gameObject.AddComponent<WatchingCamera>();
            wc.yaw = head;
            wc.led = led.GetComponent<Renderer>();
        }

        static void WallScreen(Vector3 pos, float yaw, Vector2 size, Material screen)
        {
            var root = BuildKit.Node(props, "WardenScreen", pos, yaw);
            BuildKit.Box(root, "Bezel", new Vector3(0f, 0f, 0.02f), new Vector3(size.x + 0.08f, size.y + 0.08f, 0.05f), BuildKit.Paint("bezel", new Color(0.05f, 0.05f, 0.06f), 0.6f), false);
            var q = BuildKit.Prim(root, PrimitiveType.Quad, "Screen", new Vector3(0f, 0f, -0.006f), new Vector3(size.x, size.y, 1f), screen);
            BuildKit.Label(root, "WARDEN", new Vector3(0f, -size.y * 0.38f, -0.01f), Vector3.zero, new Vector2(size.x, 0.1f), size.y * 0.09f, new Color(1f, 0.4f, 0.3f));
        }

        // ---------------------------------------------------------------- puzzles

        static void BuildPuzzles()
        {
            var fuseBox = BuildFuseBox();
            BuildFuses();
            BuildClock();
            BuildSafe();
            BuildLaser();
            BuildConsole();
        }

        static FuseBox BuildFuseBox()
        {
            // forward points into the west wall, front face is local -z
            var root = BuildKit.Node(puzzles, "FuseBox", new Vector3(0.12f, 1.35f, 4.0f), -90f);
            var grey = BuildKit.Surface("PaintedMetal006", Vector2.one, 0.45f, 0.5f, new Color(0.55f, 0.6f, 0.55f));
            var dark = BuildKit.Paint("holder", new Color(0.08f, 0.08f, 0.09f), 0.3f);
            BuildKit.Box(root, "Back", new Vector3(0f, 0f, 0.0f), new Vector3(0.75f, 0.85f, 0.1f), grey);
            BuildKit.Box(root, "Hood", new Vector3(0f, 0.45f, -0.08f), new Vector3(0.75f, 0.04f, 0.18f), grey);
            var fb = root.gameObject.AddComponent<FuseBox>();
            fb.status = BuildKit.Label(root, "NO FUSES", new Vector3(0f, 0.33f, -0.06f), Vector3.zero, new Vector2(0.6f, 0.08f), 0.045f, UiKit.Warn);
            BuildKit.Label(root, "MAIN DISTRIBUTION  F4", new Vector3(0f, -0.37f, -0.06f), Vector3.zero, new Vector2(0.7f, 0.06f), 0.03f, new Color(0.95f, 0.75f, 0.1f));
            var lampMat = BuildKit.Glow("slot_lamp", new Color(0.3f, 0.05f, 0.05f), new Color(1.2f, 0.1f, 0.08f));
            for (int i = 0; i < 3; i++)
            {
                float x = -0.24f + i * 0.17f;
                BuildKit.Box(root, "Holder" + i, new Vector3(x, 0.02f, -0.07f), new Vector3(0.09f, 0.18f, 0.06f), dark);
                var sockGo = BuildKit.Node(root, "Slot" + i, new Vector3(x, 0.02f, -0.12f));
                var sock = sockGo.gameObject.AddComponent<ItemSocket>();
                sock.captureRadius = 0.12f;
                sock.ghost = Ghost(sockGo, new Vector3(0.04f, 0.1f, 0.04f));
                fb.slots[i] = sock;
                fb.slotLabels[i] = BuildKit.Label(root, "?A", new Vector3(x, 0.17f, -0.06f), Vector3.zero, new Vector2(0.14f, 0.06f), 0.05f, Color.white);
                var lamp = BuildKit.Prim(root, PrimitiveType.Sphere, "Lamp" + i, new Vector3(x, -0.13f, -0.06f), Vector3.one * 0.03f, lampMat);
                fb.slotLamps[i] = lamp.GetComponent<Renderer>();
            }
            // main lever
            var hinge = BuildKit.Node(root, "LeverHinge", new Vector3(0.3f, -0.05f, -0.1f));
            var red = BuildKit.Paint("lever_red", new Color(0.75f, 0.08f, 0.05f), 0.5f);
            BuildKit.Box(hinge, "Arm", new Vector3(0f, 0.17f, 0f), new Vector3(0.03f, 0.34f, 0.03f), grey, false);
            BuildKit.Prim(hinge, PrimitiveType.Sphere, "Grip", new Vector3(0f, 0.35f, 0f), Vector3.one * 0.07f, red, true);
            BuildKit.Prim(root, PrimitiveType.Cylinder, "Pivot", new Vector3(0.3f, -0.05f, -0.09f), new Vector3(0.06f, 0.03f, 0.06f), dark, false, new Vector3(0f, 0f, 90f));
            var lever = hinge.gameObject.AddComponent<LeverControl>();
            lever.hinge = hinge;
            lever.localAxis = Vector3.right;
            lever.upAngle = 10f;
            lever.downAngle = -120f;
            fb.lever = lever;
            BuildKit.Label(root, "MAIN", new Vector3(0.3f, -0.3f, -0.06f), Vector3.zero, new Vector2(0.12f, 0.05f), 0.03f, Color.white);
            fb.sparks = BuildKit.Particles(root, "Sparks", new Vector3(0f, 0f, -0.15f), BuildKit.Fx.Sparks);
            return fb;
        }

        static GameObject Ghost(Transform parent, Vector3 size)
        {
            var mat = BuildKit.Custom("socket_ghost", "Lockdown/Hologram", m =>
            {
                m.SetColor("_Color", new Color(0.3f, 0.9f, 1f, 1f));
                m.SetFloat("_Alpha", 0.6f);
                m.SetFloat("_Glitch", 0f);
            });
            var g = BuildKit.Prim(parent, PrimitiveType.Cube, "Ghost", Vector3.zero, size, mat);
            g.SetActive(false);
            return g;
        }

        static Grabbable Item(Transform parent, string name, string id, Vector3 pos, Vector3 euler, float mass, float weight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var g = go.AddComponent<Grabbable>();
            g.itemId = id;
            g.displayName = name;
            g.weight = weight;
            return g;
        }

        static void BuildFuses()
        {
            int[] ratings = { 10, 20, 30 };
            Color[] colors = { new Color(0.85f, 0.1f, 0.1f), new Color(0.95f, 0.8f, 0.1f), new Color(0.15f, 0.75f, 0.25f) };
            // hiding spots: top of the tall cabinet, between boxes, and a desk drawer
            Vector3[] spots = { new Vector3(0.35f, 1.94f, 1.5f), new Vector3(7.72f, 0.025f, 6.8f), Vector3.zero };
            var drawer = BuildDeskDrawer();
            var white = BuildKit.Paint("fuse_white", new Color(0.92f, 0.92f, 0.9f), 0.6f);
            for (int i = 0; i < 3; i++)
            {
                Transform parent = i == 2 ? drawer : puzzles;
                Vector3 pos = i == 2 ? new Vector3(0f, 0.05f, 0f) : spots[i];
                var g = Item(parent, "Fuse" + ratings[i] + "A", "fuse" + ratings[i], pos, i == 1 ? new Vector3(90f, 30f, 0f) : Vector3.zero, 0.15f, 0f);
                // stays put until someone picks it up
                g.startKinematic = true;
                var body = BuildKit.Prim(g.transform, PrimitiveType.Cylinder, "Body", Vector3.zero, new Vector3(0.035f, 0.045f, 0.035f), white, false);
                BuildKit.Prim(g.transform, PrimitiveType.Cylinder, "Band", Vector3.zero, new Vector3(0.037f, 0.015f, 0.037f), BuildKit.Paint("fuse_band" + ratings[i], colors[i], 0.4f), false);
                BuildKit.Prim(g.transform, PrimitiveType.Cylinder, "CapTop", new Vector3(0f, 0.045f, 0f), new Vector3(0.03f, 0.006f, 0.03f), BuildKit.Paint("fuse_cap", new Color(0.7f, 0.7f, 0.72f), 0.8f, 1f), false);
                BuildKit.Prim(g.transform, PrimitiveType.Cylinder, "CapBottom", new Vector3(0f, -0.045f, 0f), new Vector3(0.03f, 0.006f, 0.03f), BuildKit.Get("fuse_cap"), false);
                var col = g.gameObject.AddComponent<CapsuleCollider>();
                col.radius = 0.02f; col.height = 0.1f; col.direction = 1;
                BuildKit.Label(g.transform, ratings[i] + "A", new Vector3(0f, 0.025f, -0.0185f), Vector3.zero, new Vector2(0.05f, 0.02f), 0.014f, Color.black);
                BuildKit.Label(g.transform, ratings[i] + "A", new Vector3(0f, 0.025f, 0.0185f), new Vector3(0f, 180f, 0f), new Vector2(0.05f, 0.02f), 0.014f, Color.black);
            }
        }

        static Transform BuildDeskDrawer()
        {
            // small pedestal beside the west desk, top drawer holds a fuse
            var root = BuildKit.Node(props, "DrawerPedestal", new Vector3(3.95f, 0f, 1.6f), 0f);
            var metal = BuildKit.Surface("PaintedMetal006", Vector2.one, 0.4f, 0.5f, new Color(0.35f, 0.38f, 0.42f));
            BuildKit.Box(root, "Left", new Vector3(-0.21f, 0.33f, 0f), new Vector3(0.02f, 0.66f, 0.5f), metal);
            BuildKit.Box(root, "Right", new Vector3(0.21f, 0.33f, 0f), new Vector3(0.02f, 0.66f, 0.5f), metal);
            BuildKit.Box(root, "Back", new Vector3(0f, 0.33f, -0.24f), new Vector3(0.44f, 0.66f, 0.02f), metal);
            BuildKit.Box(root, "Bottom", new Vector3(0f, 0.01f, 0f), new Vector3(0.44f, 0.02f, 0.5f), metal);
            BuildKit.Box(root, "Top", new Vector3(0f, 0.67f, 0f), new Vector3(0.44f, 0.02f, 0.5f), metal);
            BuildKit.Box(root, "LowerFront", new Vector3(0f, 0.22f, 0.24f), new Vector3(0.4f, 0.42f, 0.02f), metal);
            var drawer = BuildKit.Node(root, "Drawer", new Vector3(0f, 0.5f, 0.0f));
            BuildKit.Box(drawer, "Front", new Vector3(0f, 0f, 0.24f), new Vector3(0.4f, 0.2f, 0.02f), metal);
            BuildKit.Box(drawer, "Floor", new Vector3(0f, -0.09f, 0f), new Vector3(0.38f, 0.01f, 0.46f), metal);
            BuildKit.Box(drawer, "SideL", new Vector3(-0.185f, -0.03f, 0f), new Vector3(0.01f, 0.12f, 0.46f), metal, false);
            BuildKit.Box(drawer, "SideR", new Vector3(0.185f, -0.03f, 0f), new Vector3(0.01f, 0.12f, 0.46f), metal, false);
            var handle = BuildKit.Box(drawer, "Handle", new Vector3(0f, 0.03f, 0.27f), new Vector3(0.16f, 0.02f, 0.03f), BuildKit.Paint("chrome", new Color(0.8f, 0.8f, 0.82f), 0.9f, 1f));
            var sd = drawer.gameObject.AddComponent<SlideDrawer>();
            sd.body = drawer;
            sd.maxTravel = 0.32f;
            var inside = BuildKit.Node(drawer, "Inside", new Vector3(0.05f, -0.08f, -0.05f));
            return inside;
        }

        static void BuildClock()
        {
            // forward points into the north office wall, viewer stands south of it
            var root = BuildKit.Node(puzzles, "WallClock", new Vector3(9.0f, 1.65f, 9.86f), 0f);
            var rim = BuildKit.Paint("clock_rim", new Color(0.12f, 0.12f, 0.13f), 0.7f, 0.6f);
            var faceMat = BuildKit.Paint("clock_face", new Color(0.95f, 0.94f, 0.9f), 0.3f);
            BuildKit.Box(root, "Recess", new Vector3(0f, 0f, 0.02f), new Vector3(0.36f, 0.36f, 0.03f), BuildKit.Paint("recess", new Color(0.05f, 0.05f, 0.05f), 0.1f), false);
            BuildKit.Prim(root, PrimitiveType.Cylinder, "Rim", new Vector3(0f, 0f, 0.025f), new Vector3(0.5f, 0.02f, 0.5f), rim, false, new Vector3(90f, 0f, 0f));
            var hinge = BuildKit.Node(root, "FaceHinge", new Vector3(-0.22f, 0f, -0.01f));
            var face = BuildKit.Node(hinge, "Face", new Vector3(0.22f, 0f, 0f));
            BuildKit.Prim(face, PrimitiveType.Cylinder, "Disc", Vector3.zero, new Vector3(0.44f, 0.01f, 0.44f), faceMat, false, new Vector3(90f, 0f, 0f));
            for (int h = 1; h <= 12; h++)
            {
                Vector3 p = Quaternion.AngleAxis(-h * 30f, Vector3.forward) * new Vector3(0f, 0.17f, 0f);
                BuildKit.Label(face, h.ToString(), new Vector3(p.x, p.y, -0.012f), Vector3.zero, new Vector2(0.06f, 0.04f), 0.032f, new Color(0.1f, 0.1f, 0.12f));
            }
            BuildKit.Label(face, "KESTREL", new Vector3(0f, 0.07f, -0.012f), Vector3.zero, new Vector2(0.15f, 0.03f), 0.014f, new Color(0.5f, 0.1f, 0.1f));
            var cp = root.gameObject.AddComponent<ClockPuzzle>();
            cp.face = hinge;
            cp.faceOpenEuler = new Vector3(0f, 100f, 0f);
            cp.hourHand = ClockHand(face, "HourHand", 0.1f, 0.022f, -0.016f, 0f);
            cp.minuteHand = ClockHand(face, "MinuteHand", 0.16f, 0.014f, -0.022f, -90f);
            BuildKit.Prim(face, PrimitiveType.Cylinder, "Center", new Vector3(0f, 0f, -0.026f), new Vector3(0.025f, 0.004f, 0.025f), rim, false, new Vector3(90f, 0f, 0f));
            // keycard waits in the recess
            var card = Item(root, "OverrideCard", "keycard", new Vector3(0f, 0f, 0.012f), Vector3.zero, 0.05f, 0f);
            card.startKinematic = true;
            BuildKit.Box(card.transform, "Body", Vector3.zero, new Vector3(0.085f, 0.054f, 0.004f), BuildKit.Paint("card_body", new Color(0.12f, 0.35f, 0.75f), 0.6f), false);
            BuildKit.Box(card.transform, "Stripe", new Vector3(0f, -0.015f, -0.0022f), new Vector3(0.085f, 0.01f, 0.001f), BuildKit.Paint("card_stripe", new Color(0.05f, 0.05f, 0.05f), 0.4f), false);
            BuildKit.Label(card.transform, "OVERRIDE\n<size=60%>WARDEN L5</size>", new Vector3(0f, 0.008f, -0.0025f), Vector3.zero, new Vector2(0.08f, 0.035f), 0.011f, Color.white);
            var bc = card.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.085f, 0.054f, 0.01f);
            cp.reward = card;
            // backup log on the office desk
            cp.clueNote = BuildKit.Note(props, "BackupLog", new Vector3(10.5f, 0.805f, 6.45f), new Vector3(90f, 180f, 0f), new Vector2(0.21f, 0.27f), "", 0.012f);
        }

        static RotaryControl ClockHand(Transform face, string name, float length, float width, float z, float startDeg)
        {
            var pivot = BuildKit.Node(face, name, new Vector3(0f, 0f, z));
            var mat = BuildKit.Paint("clock_hand", new Color(0.08f, 0.08f, 0.1f), 0.5f, 0.3f);
            var blade = BuildKit.Box(pivot, "Blade", new Vector3(0f, length / 2f, 0f), new Vector3(width, length, 0.004f), mat, false);
            var grabber = BuildKit.Box(pivot, "Grab", new Vector3(0f, length * 0.7f, -0.01f), new Vector3(0.05f, length * 0.6f, 0.03f), mat);
            grabber.GetComponent<Renderer>().enabled = false;
            grabber.GetComponent<Collider>().isTrigger = true;
            var rc = pivot.gameObject.AddComponent<RotaryControl>();
            rc.pivot = pivot;
            rc.localAxis = Vector3.forward;
            rc.mode = RotaryControl.Mode.Drag;
            rc.stepDegrees = 30f;
            rc.stepSound = "clock_tick";
            rc.initialDegrees = startDeg;
            return rc;
        }

        static void BuildSafe()
        {
            // forward points into the east office wall
            var root = BuildKit.Node(puzzles, "Safe", new Vector3(11.6f, 0f, 7.4f), 90f);
            var steel = BuildKit.Surface("Metal009", Vector2.one, 0.55f, 0.8f, new Color(0.5f, 0.55f, 0.52f));
            BuildKit.Box(root, "Body", new Vector3(0f, 0.42f, 0.03f), new Vector3(0.62f, 0.84f, 0.5f), steel);
            BuildKit.Box(root, "Inside", new Vector3(0f, 0.42f, 0.0f), new Vector3(0.5f, 0.68f, 0.4f), BuildKit.Paint("safe_inside", new Color(0.1f, 0.1f, 0.1f), 0.2f), false);
            var hinge = BuildKit.Node(root, "DoorHinge", new Vector3(-0.3f, 0.42f, -0.24f));
            var door = BuildKit.Node(hinge, "Door", new Vector3(0.3f, 0f, 0f));
            BuildKit.Box(door, "Slab", new Vector3(0f, 0f, -0.02f), new Vector3(0.58f, 0.78f, 0.05f), steel);
            BuildKit.Box(door, "Handle", new Vector3(0.18f, -0.12f, -0.06f), new Vector3(0.03f, 0.14f, 0.03f), BuildKit.Get("chrome"), false);
            var sp = root.gameObject.AddComponent<SafePuzzle>();
            sp.door = hinge;
            // dial
            var dialRoot = BuildKit.Node(door, "Dial", new Vector3(0f, 0.08f, -0.06f));
            BuildKit.Prim(dialRoot, PrimitiveType.Cylinder, "Knob", Vector3.zero, new Vector3(0.19f, 0.018f, 0.19f), BuildKit.Paint("dial_black", new Color(0.06f, 0.06f, 0.07f), 0.7f, 0.5f), true, new Vector3(90f, 0f, 0f));
            BuildKit.Prim(dialRoot, PrimitiveType.Cylinder, "Grip", new Vector3(0f, 0f, -0.025f), new Vector3(0.07f, 0.02f, 0.07f), BuildKit.Get("chrome"), true, new Vector3(90f, 0f, 0f));
            for (int n = 0; n < RunSecrets.SafeNumbers; n++)
            {
                Vector3 p = Quaternion.AngleAxis(-n * 9f, Vector3.forward) * new Vector3(0f, 0.076f, 0f);
                if (n % 5 == 0)
                    BuildKit.Label(dialRoot, n.ToString(), new Vector3(p.x, p.y, -0.02f), new Vector3(0f, 0f, -n * 9f), new Vector2(0.03f, 0.016f), 0.014f, Color.white);
                else
                    BuildKit.Box(dialRoot, "Tick", new Vector3(p.x, p.y, -0.019f), new Vector3(0.002f, 0.008f, 0.001f), BuildKit.Paint("tick_white", Color.white, 0.2f), false).transform.localEulerAngles = new Vector3(0f, 0f, -n * 9f);
            }
            BuildKit.Box(door, "Marker", new Vector3(0f, 0.185f, -0.05f), new Vector3(0.008f, 0.02f, 0.01f), BuildKit.Paint("marker_red", Color.red, 0.3f), false);
            var rc = dialRoot.gameObject.AddComponent<RotaryControl>();
            rc.pivot = dialRoot;
            rc.localAxis = Vector3.forward;
            rc.mode = RotaryControl.Mode.Twist;
            rc.twistGain = 1.2f;
            sp.dial = rc;
            var lampMat = BuildKit.Glow("safe_lamp", new Color(0.1f, 0.1f, 0.1f), new Color(0.15f, 0.03f, 0.02f));
            for (int i = 0; i < 3; i++)
            {
                var l = BuildKit.Prim(door, PrimitiveType.Sphere, "StageLamp" + i, new Vector3(-0.06f + i * 0.06f, 0.29f, -0.05f), Vector3.one * 0.025f, lampMat);
                sp.stageLamps[i] = l.GetComponent<Renderer>();
            }
            BuildKit.Label(door, "KESTREL  SECURE", new Vector3(0f, -0.3f, -0.046f), Vector3.zero, new Vector2(0.4f, 0.04f), 0.025f, new Color(0.8f, 0.7f, 0.3f));
            sp.note = BuildKit.Note(root, "SafeNote", new Vector3(0.05f, 0.845f, -0.1f), new Vector3(90f, 0f, 0f), new Vector2(0.09f, 0.09f), "", 0.011f);
            // heavy battery inside
            var bat = Item(root, "Battery", "battery", new Vector3(0f, 0.17f, 0.0f), Vector3.zero, 6f, 0.85f);
            bat.startKinematic = true;
            bat.allowDistanceGrab = false;
            BuildKit.Box(bat.transform, "Case", Vector3.zero, new Vector3(0.22f, 0.15f, 0.13f), BuildKit.Paint("battery_case", new Color(0.12f, 0.12f, 0.13f), 0.3f));
            BuildKit.Box(bat.transform, "Top", new Vector3(0f, 0.08f, 0f), new Vector3(0.22f, 0.015f, 0.13f), BuildKit.Paint("battery_top", new Color(0.95f, 0.75f, 0.1f), 0.4f), false);
            BuildKit.Label(bat.transform, "12V  HEAVY", new Vector3(0f, 0f, -0.066f), Vector3.zero, new Vector2(0.2f, 0.05f), 0.03f, new Color(0.95f, 0.75f, 0.1f));
            sp.reward = bat;
        }

        static void BuildLaser()
        {
            // emitter on the east wall, forward into the wall, muzzle shoots west
            var root = BuildKit.Node(puzzles, "LaserEmitter", new Vector3(11.85f, 1.25f, 2.0f), 90f);
            var body = BuildKit.Surface("PaintedMetal006", Vector2.one, 0.5f, 0.7f, new Color(0.25f, 0.27f, 0.3f));
            BuildKit.Box(root, "Bracket", new Vector3(0f, -0.12f, 0.05f), new Vector3(0.2f, 0.03f, 0.25f), body);
            BuildKit.Box(root, "Housing", new Vector3(0f, 0f, -0.05f), new Vector3(0.24f, 0.2f, 0.32f), body);
            BuildKit.Prim(root, PrimitiveType.Cylinder, "Barrel", new Vector3(0f, 0f, -0.25f), new Vector3(0.07f, 0.06f, 0.07f), BuildKit.Get("chrome"), false, new Vector3(90f, 0f, 0f));
            var em = root.gameObject.AddComponent<LaserEmitter>();
            var muzzle = BuildKit.Node(root, "Muzzle", new Vector3(0f, 0f, -0.32f), 180f);
            em.muzzle = muzzle;
            var cradle = BuildKit.Node(root, "Cradle", new Vector3(0f, 0.18f, -0.05f));
            BuildKit.Box(cradle, "Tray", new Vector3(0f, -0.08f, 0f), new Vector3(0.26f, 0.015f, 0.17f), body, false);
            var sock = cradle.gameObject.AddComponent<ItemSocket>();
            sock.acceptIds = new[] { "battery" };
            sock.rejectIds = new[] { "fuse10", "fuse20", "fuse30", "keycard" };
            sock.captureRadius = 0.25f;
            sock.insertSound = "lever_clunk";
            sock.ghost = Ghost(cradle, new Vector3(0.22f, 0.15f, 0.13f));
            em.batterySocket = sock;
            em.powerLamp = BuildKit.Prim(root, PrimitiveType.Sphere, "PowerLamp", new Vector3(0.08f, 0.06f, -0.215f), Vector3.one * 0.025f, BuildKit.Glow("emitter_lamp", Color.red, Color.red)).GetComponent<Renderer>();
            BuildKit.Label(root, "OPTICAL KEY\n<size=60%>12V pack</size>", new Vector3(0f, 0.0f, -0.215f), Vector3.zero, new Vector2(0.2f, 0.08f), 0.022f, new Color(0.95f, 0.75f, 0.1f));
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(root, false);
            var lr = beamGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.018f;
            lr.numCapVertices = 2;
            lr.sharedMaterial = BuildKit.Custom("laser_beam", "Lockdown/LaserBeam", m => { m.SetColor("_Color", new Color(1f, 0.08f, 0.06f)); m.SetFloat("_Intensity", 3f); });
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.enabled = false;
            em.beam = lr;
            em.impact = BuildKit.Particles(puzzles, "LaserImpact", Vector3.zero, BuildKit.Fx.Impact);

            // three mirror pedestals; solution yaws are 45, -135, 45
            Mirror("MirrorA", new Vector3(6f, 0f, 2f), 45f + 60f);
            Mirror("MirrorB", new Vector3(6f, 0f, 5f), -135f - 75f);
            Mirror("MirrorC", new Vector3(3.2f, 0f, 5f), 45f + 105f);

            // sensor right of the vault shutter
            var sRoot = BuildKit.Node(puzzles, "LaserSensor", new Vector3(3.2f, 1.25f, 6.86f), 0f);
            BuildKit.Box(sRoot, "Plate", new Vector3(0f, 0f, 0.01f), new Vector3(0.22f, 0.22f, 0.03f), body);
            var lens = BuildKit.Prim(sRoot, PrimitiveType.Cylinder, "Lens", new Vector3(0f, 0f, -0.01f), new Vector3(0.12f, 0.01f, 0.12f), BuildKit.Glow("sensor_lens", new Color(0.2f, 0.02f, 0.02f), new Color(0.6f, 0.05f, 0.05f)), true, new Vector3(90f, 0f, 0f));
            var sensor = sRoot.gameObject.AddComponent<LaserSensor>();
            sensor.lens = lens.GetComponent<Renderer>();
            BuildKit.Label(sRoot, "OPTICAL\nSENSOR", new Vector3(0f, 0.2f, -0.01f), Vector3.zero, new Vector2(0.25f, 0.08f), 0.025f, new Color(0.95f, 0.75f, 0.1f));
            em.sensor = sensor;
            em.shutter = shutter;
        }

        static void Mirror(string name, Vector3 pos, float startYaw)
        {
            var root = BuildKit.Node(puzzles, name, pos);
            var steel = BuildKit.Paint("pedestal", new Color(0.2f, 0.21f, 0.23f), 0.5f, 0.8f);
            BuildKit.Prim(root, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), steel, true);
            BuildKit.Prim(root, PrimitiveType.Cylinder, "Column", new Vector3(0f, 0.52f, 0f), new Vector3(0.1f, 0.5f, 0.1f), steel, true);
            var table = BuildKit.Node(root, "Turntable", new Vector3(0f, 1.04f, 0f));
            BuildKit.Prim(table, PrimitiveType.Cylinder, "Plate", Vector3.zero, new Vector3(0.3f, 0.015f, 0.3f), BuildKit.Paint("turntable", new Color(0.6f, 0.6f, 0.62f), 0.7f, 0.9f), false);
            var face = BuildKit.Box(table, "MirrorFace", new Vector3(0f, 0.21f, 0f), new Vector3(0.34f, 0.3f, 0.015f), BuildKit.Paint("mirror", new Color(0.85f, 0.9f, 0.95f), 0.98f, 1f));
            BuildKit.Box(table, "Frame", new Vector3(0f, 0.21f, 0.012f), new Vector3(0.37f, 0.33f, 0.01f), steel, false);
            BuildKit.Box(table, "Arrow", new Vector3(0f, 0.385f, -0.01f), new Vector3(0.04f, 0.03f, 0.01f), BuildKit.Glow("mirror_arrow", new Color(0.2f, 0.8f, 1f), new Color(0.2f, 0.8f, 1.5f)), false);
            var handle = BuildKit.Prim(table, PrimitiveType.Sphere, "Handle", new Vector3(0f, 0.05f, 0.24f), Vector3.one * 0.07f, BuildKit.Paint("handle_blue", new Color(0.15f, 0.45f, 0.9f), 0.5f), true);
            BuildKit.Box(table, "HandleArm", new Vector3(0f, 0.05f, 0.12f), new Vector3(0.025f, 0.025f, 0.22f), steel, false);
            var rc = table.gameObject.AddComponent<RotaryControl>();
            rc.pivot = table;
            rc.localAxis = Vector3.up;
            rc.mode = RotaryControl.Mode.Drag;
            rc.stepDegrees = 15f;
            rc.initialDegrees = startYaw;
            var lm = table.gameObject.AddComponent<LaserMirror>();
            lm.face = face.transform;
            lm.turntable = rc;
        }

        static void BuildConsole()
        {
            // forward points north, viewer arrives from the shutter
            var root = BuildKit.Node(puzzles, "WardenConsole", new Vector3(2.3f, 0f, 8.35f), 0f);
            var body = BuildKit.Surface("PaintedMetal006", Vector2.one, 0.5f, 0.6f, new Color(0.18f, 0.19f, 0.22f));
            BuildKit.Box(root, "Desk", new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 0.5f), body);
            var top = BuildKit.Node(root, "Top", new Vector3(0f, 0.93f, -0.02f));
            top.localEulerAngles = new Vector3(-15f, 0f, 0f);
            BuildKit.Box(top, "Panel", Vector3.zero, new Vector3(1.1f, 0.04f, 0.5f), body);
            var rcn = root.gameObject.AddComponent<RebootConsole>();
            // card reader
            var reader = BuildKit.Node(top, "Reader", new Vector3(-0.32f, 0.08f, 0f));
            BuildKit.Box(reader, "Housing", new Vector3(0f, -0.03f, 0f), new Vector3(0.12f, 0.08f, 0.05f), BuildKit.Paint("reader", new Color(0.05f, 0.05f, 0.06f), 0.6f));
            var slot = BuildKit.Node(reader, "Slot", new Vector3(0f, 0.035f, 0f));
            var sock = slot.gameObject.AddComponent<ItemSocket>();
            sock.acceptIds = new[] { "keycard" };
            sock.rejectIds = new[] { "fuse10", "fuse20", "fuse30" };
            sock.captureRadius = 0.15f;
            sock.insertSound = "card_insert";
            sock.ghost = Ghost(slot, new Vector3(0.085f, 0.054f, 0.004f));
            rcn.cardReader = sock;
            BuildKit.Label(top, "OVERRIDE CARD", new Vector3(-0.32f, 0.025f, -0.17f), new Vector3(90f, 0f, 0f), new Vector2(0.25f, 0.05f), 0.022f, new Color(0.95f, 0.75f, 0.1f));
            // big reboot button
            var btn = BuildKit.Node(top, "Reboot", new Vector3(0.25f, 0.02f, 0f));
            BuildKit.Prim(btn, PrimitiveType.Cylinder, "Collar", Vector3.zero, new Vector3(0.16f, 0.02f, 0.16f), BuildKit.Paint("collar_yellow", new Color(0.95f, 0.75f, 0.1f), 0.4f), false);
            var capGo = BuildKit.Prim(btn, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 0.035f, 0f), new Vector3(0.12f, 0.025f, 0.12f), BuildKit.Glow("reboot_red", new Color(0.8f, 0.05f, 0.05f), new Color(0.8f, 0.05f, 0.03f)), true);
            var press = btn.gameObject.AddComponent<Pressable>();
            press.cap = capGo.transform;
            press.capTravel = 0f;
            rcn.rebootButton = press;
            BuildKit.Label(top, "HOLD TO REBOOT", new Vector3(0.25f, 0.025f, -0.17f), new Vector3(90f, 0f, 0f), new Vector2(0.3f, 0.05f), 0.022f, new Color(0.95f, 0.75f, 0.1f));
            // monitor with progress bar
            var mon = BuildKit.Node(root, "Monitor", new Vector3(0f, 1.35f, 0.15f));
            BuildKit.Box(mon, "Bezel", new Vector3(0f, 0f, 0.02f), new Vector3(0.8f, 0.45f, 0.04f), BuildKit.Get("bezel"));
            BuildKit.Prim(mon, PrimitiveType.Quad, "Screen", new Vector3(0f, 0f, -0.001f), new Vector3(0.74f, 0.4f, 1f), BuildKit.Paint("screen_dark", new Color(0.02f, 0.03f, 0.04f), 0.8f));
            BuildKit.Box(mon, "Stand", new Vector3(0f, -0.35f, 0.05f), new Vector3(0.06f, 0.3f, 0.04f), body, false);
            rcn.screen = BuildKit.Label(mon, "", new Vector3(0f, 0.06f, -0.004f), Vector3.zero, new Vector2(0.7f, 0.22f), 0.05f, Color.red);
            BuildKit.Box(mon, "BarBack", new Vector3(0f, -0.13f, -0.004f), new Vector3(0.6f, 0.04f, 0.002f), BuildKit.Paint("bar_back", new Color(0.1f, 0.1f, 0.1f), 0.2f), false);
            var fillPivot = BuildKit.Node(mon, "FillPivot", new Vector3(-0.3f, -0.13f, -0.006f));
            BuildKit.Box(fillPivot, "Fill", new Vector3(0.3f, 0f, 0f), new Vector3(0.6f, 0.036f, 0.002f), BuildKit.Glow("bar_fill", Color.green, new Color(0.2f, 2f, 0.4f)), false);
            rcn.progressFill = fillPivot;
            // warden core hologram
            var holo = BuildKit.Prim(root, PrimitiveType.Sphere, "WardenCore", new Vector3(0f, 2.1f, 0.3f), Vector3.one * 0.45f,
                BuildKit.Custom("warden_core", "Lockdown/Hologram", m => { m.SetColor("_Color", new Color(1f, 0.2f, 0.15f, 1f)); m.SetFloat("_Alpha", 0.8f); }));
            var spin = holo.AddComponent<Spinner>();
            spin.degreesPerSecond = new Vector3(0f, 25f, 0f);
            spin.bobHeight = 0.04f;
            rcn.hologram = holo.GetComponent<Renderer>();
            rcn.exitDoor = exitDoor;
        }

        // ---------------------------------------------------------------- chips and zones

        static void Chips()
        {
            Vector3[] spots =
            {
                new Vector3(4.85f, 1.32f, 9.6f),
                new Vector3(2.6f, 0.12f, 1.6f),
                new Vector3(7.75f, 2.85f, 9.75f),
                new Vector3(10.85f, 0.86f, 6.35f),
                new Vector3(0.95f, 0.15f, 7.6f),
            };
            var mat = BuildKit.Custom("chip_holo", "Lockdown/Hologram", m => { m.SetColor("_Color", new Color(0.3f, 1f, 0.6f, 1f)); m.SetFloat("_Alpha", 0.9f); m.SetFloat("_Glitch", 0.1f); });
            var core = BuildKit.Glow("chip_core", new Color(0.1f, 0.4f, 0.2f), new Color(0.2f, 1.5f, 0.6f));
            for (int i = 0; i < spots.Length; i++)
            {
                var root = BuildKit.Node(puzzles, "OverrideChip" + i, spots[i]);
                var spinner = BuildKit.Node(root, "Spin", Vector3.zero);
                BuildKit.Box(spinner, "Chip", Vector3.zero, new Vector3(0.05f, 0.05f, 0.008f), core, false);
                BuildKit.Prim(spinner, PrimitiveType.Sphere, "Glow", Vector3.zero, Vector3.one * 0.09f, mat);
                var c = root.gameObject.AddComponent<Collectible>();
                c.spinner = spinner;
                c.burst = BuildKit.Particles(root, "Burst", Vector3.zero, BuildKit.Fx.Burst);
            }
        }

        static void Zones()
        {
            Zone("ladder", new Vector3(6.1f, 1.2f, 8.2f), new Vector3(1.6f, 2.4f, 1.2f));
            Zone("catwalk", new Vector3(9f, 3.9f, 8.5f), new Vector3(6.5f, 1.6f, 3.2f));
            Zone("office", new Vector3(10f, 1.2f, 8f), new Vector3(3.8f, 2.2f, 3.8f));
            Zone("clock", new Vector3(9f, 1.4f, 9.1f), new Vector3(1.8f, 2.2f, 1.6f));
            Zone("safe", new Vector3(10.9f, 1.2f, 7.4f), new Vector3(1.6f, 2.2f, 1.8f));
        }

        static void Zone(string id, Vector3 pos, Vector3 size)
        {
            var z = BuildKit.Node(puzzles, "Zone_" + id, pos).gameObject.AddComponent<ZoneTrigger>();
            z.zoneId = id;
            z.size = size;
        }
    }
}
