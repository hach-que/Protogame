namespace Protogame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// This represents a runtime model, with full support for animation and bone manipulation.
    /// </summary>
    public class Model : IModel
    {
        /// <summary>
        /// The flattened version of the bone structures.
        /// </summary>
        private readonly IModelBone[] _flattenedBones;

        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class.
        /// </summary>
        /// <param name="name">
        /// The model name.
        /// </param>
        /// <param name="availableAnimations">
        /// The available animations.
        /// </param>
        /// <param name="rootBone">
        /// The root bone, or null if there's no skeletal information.
        /// </param>
        /// <param name="meshes">
        /// The meshes and submeshes inside the model.
        /// </param>
        public Model(
            string name,
            IAnimationCollection availableAnimations,
            IModelMesh[] meshes,
            IModelBone rootBone)
        {
            Name = name;
            AvailableAnimations = availableAnimations;
            Root = rootBone;
            Meshes = meshes;

            if (Root != null)
            {
                _flattenedBones = Root.Flatten();
                Bones = _flattenedBones.ToDictionary(k => k.Name, v => v);
            }
        }

        /// <summary>
        /// The name of the model, which usually aligns to the model asset that
        /// the model was loaded from.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the available animations.
        /// </summary>
        /// <value>
        /// The available animations.
        /// </value>
        public IAnimationCollection AvailableAnimations { get; private set; }

        /// <summary>
        /// Gets the root bone of the model's skeleton.
        /// </summary>
        /// <remarks>
        /// This value is null if there is no skeleton attached to the model.
        /// </remarks>
        /// <value>
        /// The root bone of the model's skeleton.
        /// </value>
        public IModelBone Root { get; private set; }

        /// <summary>
        /// Gets the model's bones by their names.
        /// </summary>
        /// <remarks>
        /// This value is null if there is no skeleton attached to the model.
        /// </remarks>
        /// <value>
        /// The model bones addressed by their names.
        /// </value>
        public IDictionary<string, IModelBone> Bones { get; private set; }

        /// <summary>
        /// Gets the model's meshes and submeshes.
        /// </summary>
        /// <remarks>
        /// All models have at least one mesh.
        /// </remarks>
        public IModelMesh[] Meshes { get; private set; }

        /// <summary>
        /// Frees any vertex buffers that are cached inside this model.
        /// </summary>
        public void FreeCachedVertexBuffers()
        {
            foreach (var mesh in Meshes)
            {
                mesh.FreeCachedVertexBuffers();
            }
        }

        /// <summary>
        /// Renders the model using the specified transform and GPU mapping.
        /// </summary>
        /// <param name="renderContext">
        ///     The render context.
        /// </param>
        /// <param name="transform">
        ///     The transform.
        /// </param>
        /// <param name="effectParameterSet"></param>
        /// <param name="effect"></param>
        public void Render(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform)
        {
            foreach (var mesh in Meshes)
            {
                mesh.Render(this, _flattenedBones, renderContext, effect, effectParameterSet, transform);
            }
        }
        
        /// <summary>
        /// Load the vertex and index buffer for this model.
        /// </summary>
        /// <param name="graphicsDevice">
        /// The graphics device.
        /// </param>
        public void LoadBuffers(GraphicsDevice graphicsDevice)
        {
            foreach (var mesh in Meshes)
            {
                mesh.LoadBuffers(graphicsDevice);
            }
        }

        /// <summary>
        /// Creates a render request for the model using the specified transform.
        /// </summary>
        /// <param name="renderContext">
        ///     The render context.
        /// </param>
        /// <param name="effect"></param>
        /// <param name="effectParameterSet"></param>
        /// <param name="transform">
        ///     The transform.
        /// </param>
        public IRenderRequest[] CreateRenderRequests(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform)
        {
            var requests = new IRenderRequest[Meshes.Length];
            for (var i = 0; i < Meshes.Length; i++)
            {
                requests[i] = Meshes[i].CreateRenderRequest(this, _flattenedBones, renderContext, effect, effectParameterSet, transform);
            }
            return requests;
        }

        public void Dispose()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }
        }
    }
}