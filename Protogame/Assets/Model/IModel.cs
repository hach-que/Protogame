namespace Protogame
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// An interface representing a runtime model.
    /// </summary>
    public interface IModel : IDisposable
    {
        /// <summary>
        /// The name of the model, which usually aligns to the model asset that
        /// the model was loaded from.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the available animations.
        /// </summary>
        /// <value>
        /// The available animations.
        /// </value>
        IAnimationCollection AvailableAnimations { get; }

        /// <summary>
        /// Gets the model's meshes and submeshes.
        /// </summary>
        /// <remarks>
        /// All models have at least one mesh.
        /// </remarks>
        IModelMesh[] Meshes { get; }

        /// <summary>
        /// Renders the model using the specified transform.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="effectParameterSet"></param>
        /// <param name="effect"></param>
        void Render(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform);

        /// <summary>
        /// Frees any vertex buffers that are cached inside this model.
        /// </summary>
        void FreeCachedVertexBuffers();

        /// <summary>
        /// Loads vertex and index buffers for all of animations in this model.
        /// </summary>
        /// <param name="graphicsDevice">
        /// The graphics device.
        /// </param>
        void LoadBuffers(GraphicsDevice graphicsDevice);
    }
}