using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Protogame
{
    /// <summary>
    /// Represents a mesh or submesh of a runtime model.  Each mesh or submesh can have a different
    /// material applied to it.
    /// </summary>
    public interface IModelMesh : IDisposable
    {
        /// <summary>
        /// Gets the material information associated with this mesh, if
        /// one exists.
        /// </summary>
        /// <remarks>
        /// This value is null if there is no material attached to this mesh.
        /// </remarks>
        /// <value>
        /// The material associated with this mesh.
        /// </value>
        IMaterial Material { get; }

        /// <summary>
        /// Gets the index buffer.
        /// </summary>
        /// <value>
        /// The index buffer.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the vertex or index buffers have not been loaded with <see cref="LoadBuffers"/>.
        /// </exception>
        IndexBuffer IndexBuffer { get; }

        /// <summary>
        /// Gets the indices of the mesh.
        /// </summary>
        /// <value>
        /// The indices of the mesh.
        /// </value>
        int[] Indices { get; }

        /// <summary>
        /// Gets the vertexes of the mesh.
        /// </summary>
        /// <value>
        /// The vertexes of the mesh.
        /// </value>
        ModelVertex[] Vertexes { get; }

        /// <summary>
        /// Renders the mesh using the specified transform.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="effectParameterSet"></param>
        /// <param name="effect"></param>
        void Render(Model model, IModelBone[] flattenedBones, IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform);

        /// <summary>
        /// Creates a render request for the mesh using the specified transform.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <param name="effect"></param>
        /// <param name="effectParameterSet"></param>
        /// <param name="transform">The transform.</param>
        IRenderRequest CreateRenderRequest(Model model, IModelBone[] flattenedBones, IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform);
        
        /// <summary>
        /// Frees any vertex buffers that are cached inside this mesh.
        /// </summary>
        void FreeCachedVertexBuffers();

        /// <summary>
        /// Loads vertex and index buffers for all of animations in this mesh.
        /// </summary>
        /// <param name="graphicsDevice">
        /// The graphics device.
        /// </param>
        void LoadBuffers(GraphicsDevice graphicsDevice);
    }
}
