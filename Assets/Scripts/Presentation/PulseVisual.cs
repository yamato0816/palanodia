using UnityEngine;

namespace Palinodia.Presentation
{
    public sealed class PulseVisual : MonoBehaviour
    {
        private float lifetime;
        private float radius;
        private Renderer visual;
        private Material runtimeMaterial;

        public static void Spawn(Vector3 position, bool thermal, float size)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = thermal ? "Thermal shockfront (VFX)" : "Impulse shockfront (VFX)";
            orb.transform.position = position;
            Collider c = orb.GetComponent<Collider>();
            if (c != null) Destroy(c);
            PulseVisual pulse = orb.AddComponent<PulseVisual>();
            pulse.radius = size;
            pulse.visual = orb.GetComponent<Renderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (shader == null) shader = Shader.Find("Standard");
            pulse.runtimeMaterial = new Material(shader);
            pulse.runtimeMaterial.color = thermal ? new Color(1f, .38f, .08f, .42f) :
                new Color(.12f, .8f, 1f, .4f);
            pulse.visual.material = pulse.runtimeMaterial;
            orb.transform.localScale = Vector3.one * .15f;
            Destroy(orb, .33f);
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            float progress = Mathf.Clamp01(lifetime / .32f);
            transform.localScale = Vector3.one * Mathf.Lerp(.15f, radius * 2, progress);
            if (runtimeMaterial != null)
            {
                Color color = runtimeMaterial.color;
                color.a = .32f * (1f - progress);
                runtimeMaterial.color = color;
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
