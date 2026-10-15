using System.Collections.Generic;
using UnityEngine;
using Palinodia.Simulation;
using Palinodia.Presentation;

namespace Palinodia.Gameplay
{
    /// <summary>
    /// Scene stays small in source control. The fully playable test room is deterministic and
    /// generated in Play Mode from basic Unity primitives; no external asset downloads.
    /// </summary>
    public sealed class DemoBootstrap : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials = new List<Material>();
        private StageEnvironmentState stage;
        private PlayerActor actor;
        private FollowCamera rig;
        private AbilityController abilities;
        private LabObjective objective;

        private readonly Color wall = new Color(.19f, .24f, .3f);
        private readonly Color floor = new Color(.115f, .145f, .18f);
        private readonly Color accent = new Color(.14f, .55f, .67f);
        private readonly Color hot = new Color(1f, .35f, .09f);

        private void Start() => Build();

        private void Build()
        {
            Physics.gravity = new Vector3(0, -9.81f, 0);
            GameObject environment = new GameObject("LAB / ambient stage state");
            stage = environment.AddComponent<StageEnvironmentState>();

            RenderSettings.ambientLight = new Color(.46f, .53f, .61f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.10f, .15f, .20f);
            RenderSettings.fogDensity = .006f;
            RenderSettings.fogMode = FogMode.Exponential;
            MakeLight("Overhead primary light", new Vector3(-2, 8, -3), LightType.Directional, 1.05f);
            MakeLight("Thermal core bounce", new Vector3(-7, 5, -11), LightType.Point, 5f, hot, 13);
            MakeLight("Exit cool light", new Vector3(0, 5, 18), LightType.Point, 3.5f,
                new Color(.1f, .85f, 1f), 10);

            CreateArchitecture();
            CreatePlayer();

            GameObject heater = Shape(PrimitiveType.Cylinder, "HEAT CORE / source A",
                new Vector3(-7.5f, 1.9f, -11), new Vector3(2.1f, .9f, 2.1f), hot);
            PhysicalBodyState source = heater.AddComponent<PhysicalBodyState>();
            source.Configure(stage, BodyCategory.Heater, "HEAT CORE A / thermal reservoir",
                245, 3300, 1.8, 22);
            heater.AddComponent<EnergyCoreVisual>().Initialize(source);
            GameObject pedestal = Shape(PrimitiveType.Cylinder, "Core pedestal",
                new Vector3(-7.5f, .48f, -11), new Vector3(2.8f, .48f, 2.8f), wall);
            Decoration(PrimitiveType.Cylinder, "Core guard ring / nonblocking visual",
                new Vector3(-7.5f, 2.85f, -11), new Vector3(2.7f, .08f, 2.7f), accent);
            // No decorative collider is allowed to conceal the interactive source.
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f;
                Decoration(PrimitiveType.Cylinder, "Core containment light " + i,
                    new Vector3(-7.5f + Mathf.Cos(a) * 1.53f, 1.95f, -11 + Mathf.Sin(a) * 1.53f),
                    new Vector3(.13f, .92f, .13f), new Color(1f, .46f, .1f), true);
            }

            MakeRigidbodyBox("Movable steel crate", new Vector3(5, .8f, -6), new Vector3(1.4f, 1.4f, 1.4f),
                new Color(.54f, .63f, .73f), BodyCategory.Metal, 75, 3000);
            MakeRigidbodyBox("Movable steel crate", new Vector3(7, .8f, -5), new Vector3(1.2f, 1.2f, 1.2f),
                new Color(.37f, .52f, .62f), BodyCategory.Metal, 55, 2400);
            CreateMetalBall("Kinetic experiment sphere 01", new Vector3(-4, 1.4f, .5f), 1.05f);
            CreateMetalBall("Kinetic experiment sphere 02", new Vector3(3.5f, 1.3f, 4f), .88f);

            PhysicalBodyState first = CreateDrone("SENTRY / 01", new Vector3(-4, 1.55f, 8.0f), .1f);
            PhysicalBodyState second = CreateDrone("SENTRY / 02", new Vector3(7, 1.55f, 12), 1.8f);

            // Entrance, corridor and locked gate physically block progress until both sentries fail.
            Shape(PrimitiveType.Cube, "Gate wall left", new Vector3(-9.55f, 3.2f, 17),
                new Vector3(14.9f, 6.4f, 1.0f), wall);
            Shape(PrimitiveType.Cube, "Gate wall right", new Vector3(9.55f, 3.2f, 17),
                new Vector3(14.9f, 6.4f, 1.0f), wall);
            GameObject door = Shape(PrimitiveType.Cube, "LOCKED research gate", new Vector3(0, 2.3f, 17),
                new Vector3(4.25f, 4.6f, 1f), new Color(.61f, .28f, .24f));
            Shape(PrimitiveType.Cylinder, "Exit beacon plinth", new Vector3(0, .13f, 19.4f),
                new Vector3(4.1f, .12f, 4.1f), accent);
            GameObject beacon = Shape(PrimitiveType.Cylinder, "Exit beacon marker", new Vector3(0, 3.15f, 19.4f),
                new Vector3(1.65f, .12f, 1.65f), new Color(.68f, .27f, .22f));
            Collider beaconCollider = beacon.GetComponent<Collider>();
            if (beaconCollider != null) Destroy(beaconCollider);

            objective = gameObject.AddComponent<LabObjective>();
            objective.Initialize(actor, door, beacon.GetComponent<Renderer>(), first, second);
            abilities = actor.gameObject.AddComponent<AbilityController>();
            abilities.Initialize(actor, rig, stage);
            LabHUD hud = gameObject.AddComponent<LabHUD>();
            hud.Initialize(abilities, objective, actor, stage);
        }

        private void CreateArchitecture()
        {
            Shape(PrimitiveType.Cube, "Laboratory floor", new Vector3(0, -.52f, 0),
                new Vector3(35f, 1f, 46f), floor);
            Shape(PrimitiveType.Cube, "West wall", new Vector3(-17.5f, 4, 0),
                new Vector3(1, 8, 45), wall);
            Shape(PrimitiveType.Cube, "East wall", new Vector3(17.5f, 4, 0),
                new Vector3(1, 8, 45), wall);
            Shape(PrimitiveType.Cube, "North wall", new Vector3(0, 4, 22.5f),
                new Vector3(36, 8, 1), wall);
            Shape(PrimitiveType.Cube, "South wall", new Vector3(0, 4, -22.5f),
                new Vector3(36, 8, 1), wall);
            Shape(PrimitiveType.Cube, "Roof rim west", new Vector3(-16.9f, 7.8f, 0),
                new Vector3(1.5f, .35f, 46), accent);
            Shape(PrimitiveType.Cube, "Roof rim east", new Vector3(16.9f, 7.8f, 0),
                new Vector3(1.5f, .35f, 46), accent);
            for (int i = 0; i < 11; i++)
            {
                float z = -20f + i * 3.9f;
                Decoration(PrimitiveType.Cube, "Runway neon left " + i,
                    new Vector3(-3.3f, .012f, z), new Vector3(.075f, .025f, 2.1f),
                    new Color(.14f, .73f, 1f), true);
                Decoration(PrimitiveType.Cube, "Runway neon right " + i,
                    new Vector3(3.3f, .012f, z), new Vector3(.075f, .025f, 2.1f),
                    new Color(.14f, .73f, 1f), true);
            }
            for (int i = 0; i < 8; i++)
            {
                float z = -18f + i * 5f;
                Decoration(PrimitiveType.Cube, "Wall strip west " + i,
                    new Vector3(-16.91f, 2.5f, z), new Vector3(.08f, 2f, .12f),
                    new Color(.13f, .62f, .92f), true);
                Decoration(PrimitiveType.Cube, "Wall strip east " + i,
                    new Vector3(16.91f, 2.5f, z), new Vector3(.08f, 2f, .12f),
                    new Color(.13f, .62f, .92f), true);
            }
            for (int z = -19; z <= 14; z += 6)
            {
                Shape(PrimitiveType.Cube, "Warning rail west", new Vector3(-16.85f, .14f, z),
                    new Vector3(.22f, .28f, 3.4f), accent);
                Shape(PrimitiveType.Cube, "Warning rail east", new Vector3(16.85f, .14f, z),
                    new Vector3(.22f, .28f, 3.4f), accent);
            }
            Shape(PrimitiveType.Cube, "Central cover left", new Vector3(-9, 1, 2),
                new Vector3(2.2f, 2, 6.2f), new Color(.26f, .32f, .38f));
            Shape(PrimitiveType.Cube, "Central cover right", new Vector3(9, 1, 3),
                new Vector3(2.2f, 2, 6.2f), new Color(.26f, .32f, .38f));
            for (int i = 0; i < 6; i++)
            {
                Decoration(PrimitiveType.Cube, "Ceiling strip " + i, new Vector3(0, 7.5f, -18 + i * 7),
                    new Vector3(26, .12f, .2f), accent, true);
            }
            Shape(PrimitiveType.Cylinder, "Heater warning pedestal", new Vector3(-7.5f, .075f, -11),
                new Vector3(4.5f, .08f, 4.5f), new Color(.55f, .3f, .1f));
        }

        private void CreatePlayer()
        {
            GameObject root = new GameObject("PLAYER / energy conversion test subject");
            root.transform.position = new Vector3(0, .12f, -15.7f);
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.radius = .45f;
            cc.height = 1.85f;
            cc.center = new Vector3(0, .95f, 0);
            cc.stepOffset = .26f;
            GameObject proxy = Shape(PrimitiveType.Capsule, "Player visual placeholder",
                root.transform.position + Vector3.up * .94f, new Vector3(.78f, .93f, .78f),
                new Color(.26f, .74f, .93f));
            proxy.transform.SetParent(root.transform, true);
            Collider primitive = proxy.GetComponent<Collider>();
            if (primitive != null) Destroy(primitive);
            GameObject visor = Shape(PrimitiveType.Cube, "Energy sensory visor", root.transform.position +
                new Vector3(0, 1.45f, .36f), new Vector3(.55f, .17f, .14f), accent);
            visor.transform.SetParent(root.transform, true);
            Collider vc = visor.GetComponent<Collider>();
            if (vc != null) Destroy(vc);
            PhysicalBodyState body = root.AddComponent<PhysicalBodyState>();
            body.Configure(stage, BodyCategory.Living, "SUBJECT / protagonist", 36.8, 4500, 1.5, 22, 45);

            GameObject camera = new GameObject("Main Camera / orbit rig");
            camera.tag = "MainCamera";
            Camera cam = camera.AddComponent<Camera>();
            cam.fieldOfView = 69;
            cam.nearClipPlane = .1f;
            camera.AddComponent<AudioListener>();
            rig = camera.AddComponent<FollowCamera>();
            rig.Initialize(root.transform);
            actor = root.AddComponent<PlayerActor>();
            actor.Initialize(rig, body);
        }

        private PhysicalBodyState CreateDrone(string name, Vector3 position, float phase)
        {
            GameObject root = Shape(PrimitiveType.Sphere, name, position, Vector3.one * 1.38f,
                new Color(.82f, .25f, .2f));
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 24;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            GameObject eye = Shape(PrimitiveType.Cube, "Optical array", position + Vector3.forward * .67f,
                new Vector3(.62f, .18f, .17f), hot);
            eye.transform.SetParent(root.transform, true);
            Collider eyeCollider = eye.GetComponent<Collider>();
            if (eyeCollider != null) Destroy(eyeCollider);
            PhysicalBodyState body = root.AddComponent<PhysicalBodyState>();
            body.Configure(stage, BodyCategory.Machine, name + " / hardware telemetry",
                38, 540, .4, 22, 104, 6700);
            DroneAgent drone = root.AddComponent<DroneAgent>();
            drone.Initialize(actor, stage, phase);
            root.AddComponent<DroneVisual>().Initialize(body);
            return body;
        }

        private void CreateMetalBall(string label, Vector3 position, float scale)
        {
            GameObject sphere = Shape(PrimitiveType.Sphere, label, position,
                Vector3.one * scale * 2, new Color(.73f, .79f, .85f));
            Rigidbody rb = sphere.AddComponent<Rigidbody>();
            rb.mass = scale * 23;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            PhysicalBodyState body = sphere.AddComponent<PhysicalBodyState>();
            body.Configure(stage, BodyCategory.Metal, label, 22, 1750, 1.7);
        }

        private void MakeRigidbodyBox(string label, Vector3 position, Vector3 size, Color tint,
            BodyCategory category, float mass, double capacity)
        {
            GameObject box = Shape(PrimitiveType.Cube, label, position, size, tint);
            Rigidbody rb = box.AddComponent<Rigidbody>();
            rb.mass = mass;
            PhysicalBodyState body = box.AddComponent<PhysicalBodyState>();
            body.Configure(stage, category, label, 22, capacity, 1.0);
        }

        private GameObject Shape(PrimitiveType type, string label, Vector3 position, Vector3 size, Color tint)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            g.name = label;
            g.transform.position = position;
            g.transform.localScale = size;
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.color = tint;
            ownedMaterials.Add(mat);
            Renderer renderer = g.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
            return g;
        }

        private GameObject Decoration(PrimitiveType type, string label, Vector3 at, Vector3 scale,
            Color tint, bool emissive = false)
        {
            GameObject go = Shape(type, label, at, scale, tint);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            if (emissive)
            {
                Material mat = go.GetComponent<Renderer>().sharedMaterial;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", tint * 2.4f);
                }
            }
            return go;
        }

        private static void MakeLight(string label, Vector3 at, LightType type, float intensity,
            Color? color = null, float range = 25)
        {
            GameObject root = new GameObject(label);
            root.transform.position = at;
            if (type == LightType.Directional) root.transform.rotation = Quaternion.Euler(48, -28, 0);
            Light light = root.AddComponent<Light>();
            light.type = type;
            light.intensity = intensity;
            light.color = color ?? new Color(.64f, .79f, .94f);
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
                if (material != null) Destroy(material);
        }
    }
}
