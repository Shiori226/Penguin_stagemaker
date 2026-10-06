using UnityEngine;

/// <summary>
/// Runtime-only feedback for a newly reached ice checkpoint.
/// </summary>
public class CheckpointEffect : MonoBehaviour
{
    private ParticleSystem particles;

    private void Awake()
    {
        particles = gameObject.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop = false;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.65f, 0.95f, 1f, 1f),
            Color.white);
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst(0f, 14));

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Standard Unlit");
        }

        if (particleShader != null)
        {
            renderer.material = new Material(particleShader);
        }

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Play()
    {
        if (particles == null)
        {
            return;
        }

        particles.Clear(true);
        particles.Play(true);
    }
}
