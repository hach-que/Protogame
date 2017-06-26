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
        /// Gets the root bone of the model's skeleton.
        /// </summary>
        /// <remarks>
        /// This value is null if there is no skeleton attached to the model.
        /// </remarks>
        /// <value>
        /// The root bone of the model's skeleton.
        /// </value>
        IModelBone Root { get; }

        /// <summary>
        /// Gets the model's bones by their names.
        /// </summary>
        /// <remarks>
        /// This value is null if there is no skeleton attached to the model.
        /// </remarks>
        /// <value>
        /// The model bones addressed by their names.
        /// </value>
        IDictionary<string, IModelBone> Bones { get; }

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
        /// Creates a render request for the model using the specified transform.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="effect"></param>
        /// <param name="effectParameterSet"></param>
        /// <param name="transform">The transform.</param>
        IRenderRequest[] CreateRenderRequests(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform);

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