using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Behavior interface for components that emit particles.
/// Implement this interface for particle effects like fire, smoke, explosions, etc.
/// </summary>
public interface IParticleEmitter
{
    /// <summary>
    /// Whether the particle emitter is currently active and emitting particles.
    /// </summary>
    bool IsEmitting { get; set; }

    /// <summary>
    /// The rate at which particles are emitted (particles per second).
    /// </summary>
    float EmissionRate { get; set; }

    /// <summary>
    /// The maximum number of particles that can be alive at once.
    /// </summary>
    int MaxParticles { get; set; }

    /// <summary>
    /// The lifetime of each particle in seconds.
    /// </summary>
    float ParticleLifetime { get; set; }

    /// <summary>
    /// The initial velocity range for new particles.
    /// </summary>
    Vector2D<float> InitialVelocity { get; set; }

    /// <summary>
    /// The random velocity variation applied to particles.
    /// </summary>
    Vector2D<float> VelocityVariation { get; set; }

    /// <summary>
    /// The gravity or acceleration applied to particles over time.
    /// </summary>
    Vector2D<float> Gravity { get; set; }

    /// <summary>
    /// The initial size of particles.
    /// </summary>
    float InitialSize { get; set; }

    /// <summary>
    /// The final size of particles (for size animation over lifetime).
    /// </summary>
    float FinalSize { get; set; }

    /// <summary>
    /// The initial color of particles.
    /// </summary>
    Vector4D<float> InitialColor { get; set; }

    /// <summary>
    /// The final color of particles (for color animation over lifetime).
    /// </summary>
    Vector4D<float> FinalColor { get; set; }

    /// <summary>
    /// The texture used for rendering particles.
    /// </summary>
    string? ParticleTexture { get; set; }

    /// <summary>
    /// Start emitting particles.
    /// </summary>
    void StartEmission();

    /// <summary>
    /// Stop emitting new particles (existing particles continue until they expire).
    /// </summary>
    void StopEmission();

    /// <summary>
    /// Immediately clear all active particles.
    /// </summary>
    void ClearParticles();

    /// <summary>
    /// Emit a burst of particles immediately.
    /// </summary>
    /// <param name="count">The number of particles to emit</param>
    void EmitBurst(int count);

    /// <summary>
    /// Event raised when the particle system finishes (all particles have expired).
    /// </summary>
    event Action? ParticleSystemFinished;
}