namespace Protogame
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// This represents a runtime model, with full support for animation and bone manipulation.
    /// </summary>
    public class Model : IModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class.
        /// </summary>
        /// <param name="name">
        /// The model name.
        /// </param>
        /// <param name="availableAnimations">
        /// The available animations.
        /// </param>
        /// <param name="meshes">
        /// The meshes and submeshes inside the model.
        /// </param>
        public Model(
            string name,
            IAnimationCollection availableAnimations,
            IModelMesh[] meshes)
        {
            Name = name;
            AvailableAnimations = availableAnimations;
            Meshes = meshes;
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
        /// Gets the model's meshes and submeshes.
        /// </summary>
        /// <remarks>
        /// All models have at least one mesh.
        /// </remarks>
        public IModelMesh[] Meshes { get; private set; }

        /// <summary>
        /// Renders the model using the specified transform.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="effectParameterSet"></param>
        /// <param name="effect"></param>
        public void Render(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform)
        {
            for (var i = 0; i < Meshes.Length; i++)
            {
                Meshes[i].Render(this, renderContext, effect, effectParameterSet, transform);
            }
        }

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

        public void Dispose()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }
        }
    }
}