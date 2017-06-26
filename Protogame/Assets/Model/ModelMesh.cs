using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;

namespace Protogame
{
    /// <summary>
    /// Represents a mesh or submesh of a runtime model.
    /// </summary>
    public class ModelMesh : IModelMesh
    {
        /// <summary>
        /// The render batcher, which is used to create render requests.
        /// </summary>
        private readonly IRenderBatcher _renderBatcher;

        /// <summary>
        /// The model render configurations, which inform the model how it's vertices
        /// should be mapped to effects.
        /// </summary>
        private readonly IModelRenderConfiguration[] _modelRenderConfigurations;

        /// <summary>
        /// The index buffer.
        /// </summary>
        private IndexBuffer _indexBuffer;

        /// <summary>
        /// The cached vertex buffers.
        /// </summary>
        private Dictionary<object, VertexBuffer> _cachedVertexBuffers;

        /// <summary>
        /// The cached model vertex mapping.
        /// </summary>
        private ModelVertexMapping _cachedModelVertexMapping;

        /// <summary>
        /// The localised bounding region which is used for frustrum culling.
        /// </summary>
        private LocalisedBoundingRegion _localisedBoundingRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMesh"/> class.
        /// </summary>
        /// <param name="modelRenderConfigurations">
        /// The model render configurations which describe how to map model vertices
        /// to GPU vertices for rendering.
        /// </param>
        /// <param name="renderBatcher">
        /// The render batcher service which is used to queue rendering requests.
        /// </param>
        /// <param name="material">
        /// The material associated with this mesh.
        /// </param>
        /// <param name="vertexes">
        /// The vertexes associated with this mesh.
        /// </param>
        /// <param name="indices">
        /// The indices associated with the mesh.
        /// </param>
        public ModelMesh(
            IModelRenderConfiguration[] modelRenderConfigurations,
            IRenderBatcher renderBatcher,
            IMaterial material,
            ModelVertex[] vertexes,
            int[] indices)
        {
            _modelRenderConfigurations = modelRenderConfigurations;
            _renderBatcher = renderBatcher;

            Vertexes = vertexes;
            Indices = indices;
            Material = material;

            _cachedVertexBuffers = new Dictionary<object, VertexBuffer>();
            _modelRenderConfigurations = modelRenderConfigurations;
            _renderBatcher = renderBatcher;
        }

        /// <summary>
        /// Gets the material information associated with this model, if
        /// one exists.
        /// </summary>
        /// <remarks>
        /// This value is null if there is no material attached to this model.
        /// </remarks>
        /// <value>
        /// The material associated with this model.
        /// </value>
        public IMaterial Material { get; private set; }

        /// <summary>
        /// Gets the index buffer.
        /// </summary>
        /// <value>
        /// The index buffer.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the vertex or index buffers have not been loaded with <see cref="LoadBuffers"/>.
        /// </exception>
        public IndexBuffer IndexBuffer
        {
            get
            {
                if (_indexBuffer == null)
                {
                    throw new InvalidOperationException("Call LoadBuffers before accessing the index buffer");
                }

                return _indexBuffer;
            }
        }

        /// <summary>
        /// Gets the indices of the model.
        /// </summary>
        /// <value>
        /// The indices of the model.
        /// </value>
        public int[] Indices { get; private set; }

        /// <summary>
        /// Frees any vertex buffers that are cached inside this model.
        /// </summary>
        public void FreeCachedVertexBuffers()
        {
            foreach (var buffer in _cachedVertexBuffers)
            {
                buffer.Value.Dispose();
            }

            _cachedVertexBuffers.Clear();
        }


        /// <summary>
        /// Gets the vertexes of the model.
        /// </summary>
        /// <value>
        /// The vertexes of the model.
        /// </value>
        public ModelVertex[] Vertexes { get; private set; }

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
        public void Render(Model model, IModelBone[] flattenedBones, IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform)
        {
            var request = CreateRenderRequest(model, flattenedBones, renderContext, effect, effectParameterSet, transform);
            _renderBatcher.RenderRequestImmediate(renderContext, request);
        }

        /// <summary>
        /// Load the vertex and index buffer for this model.
        /// </summary>
        /// <param name="graphicsDevice">
        /// The graphics device.
        /// </param>
        public void LoadBuffers(GraphicsDevice graphicsDevice)
        {
            if (_indexBuffer == null)
            {
                _indexBuffer = new IndexBuffer(
                    graphicsDevice,
                    IndexElementSize.ThirtyTwoBits,
                    Indices.Length,
                    BufferUsage.WriteOnly);
                _indexBuffer.SetData(Indices);
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
        public IRenderRequest CreateRenderRequest(Model model, IModelBone[] flattenedBones, IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform)
        {
            if (Vertexes.Length == 0 && Indices.Length == 0)
            {
                throw new InvalidOperationException(
                    "This model does not have any vertexes or indices.  It's most " +
                    "likely been imported from an FBX file that only contains hierarchy, " +
                    "in which case there isn't anything to render.");
            }

            LoadBuffers(renderContext.GraphicsDevice);

            VertexBuffer vertexBuffer;
            if (_cachedVertexBuffers.ContainsKey(effect))
            {
                vertexBuffer = _cachedVertexBuffers[effect];
            }
            else
            {
                // Find the vertex mapping configuration for this model.
                if (_cachedModelVertexMapping == null)
                {
                    _cachedModelVertexMapping =
                        _modelRenderConfigurations.Select(x => x.GetVertexMappingToGPU(model, effect))
                            .FirstOrDefault(x => x != null);
                    if (_cachedModelVertexMapping == null)
                    {
                        throw new InvalidOperationException(
                            "No implementation of IModelRenderConfiguration could provide a vertex " +
                            "mapping for this model.  You must implement IModelRenderConfiguration " +
                            "and bind it in the dependency injection system, so that the engine is " +
                            "aware of how to map vertices in models to parameters in effects.");
                    }
                }

                var mappedVerticies = Array.CreateInstance(_cachedModelVertexMapping.VertexType, Vertexes.Length);
                for (var i = 0; i < Vertexes.Length; i++)
                {
                    var vertex = _cachedModelVertexMapping.MappingFunction(Vertexes[i]);
                    mappedVerticies.SetValue(vertex, i);
                }

                float maxX = 0f, maxY = 0f, maxZ = 0f;
                foreach (var vert in this.Vertexes)
                {
                    if (vert.Position.HasValue)
                    {
                        if (Math.Abs(vert.Position.Value.X) > maxX)
                        {
                            maxX = Math.Abs(vert.Position.Value.X);
                        }
                        if (Math.Abs(vert.Position.Value.Y) > maxY)
                        {
                            maxY = Math.Abs(vert.Position.Value.Y);
                        }
                        if (Math.Abs(vert.Position.Value.Z) > maxZ)
                        {
                            maxZ = Math.Abs(vert.Position.Value.Z);
                        }
                    }
                }

                var radius = new Vector3(maxX, maxY, maxZ).Length() * 2;

                _localisedBoundingRegion = new LocalisedBoundingRegion(radius);

                vertexBuffer = new VertexBuffer(
                    renderContext.GraphicsDevice,
                    _cachedModelVertexMapping.VertexDeclaration,
                    Vertexes.Length,
                    BufferUsage.WriteOnly);
                vertexBuffer.GetType().GetMethods().First(x => x.Name == "SetData" && x.GetParameters().Length == 1).MakeGenericMethod(_cachedModelVertexMapping.VertexType).Invoke(
                    vertexBuffer,
                    new[] { mappedVerticies });
                _cachedVertexBuffers[effect] = vertexBuffer;
            }

            if (effectParameterSet.HasSemantic<IBonesEffectSemantic>())
            {
                var bonesEffectSemantic = effectParameterSet.GetSemantic<IBonesEffectSemantic>();

                foreach (var bone in _flattenedBones)
                {
                    if (bone.ID == -1)
                    {
                        continue;
                    }

                    bonesEffectSemantic.Bones[bone.ID] = bone.GetFinalMatrix();
                }
            }

            // Create the render request.
            return _renderBatcher.CreateSingleRequestFromState(
                renderContext,
                effect,
                effectParameterSet,
                vertexBuffer,
                IndexBuffer,
                PrimitiveType.TriangleList,
                renderContext.World * transform, (m, vb, ib) =>
                {
                    var mappedVerticies = Array.CreateInstance(_cachedModelVertexMapping.VertexType, Vertexes.Length * m.Count);
                    var mappedIndicies = new int[Indices.Length * m.Count];

                    for (var im = 0; im < m.Count; im++)
                    {
                        for (var i = 0; i < Vertexes.Length; i++)
                        {
                            var vertex = _cachedModelVertexMapping.MappingFunction(Vertexes[i].Transform(m[im]));
                            mappedVerticies.SetValue(vertex, im * Vertexes.Length + i);
                        }

                        for (var i = 0; i < Indices.Length; i++)
                        {
                            mappedIndicies[im * Vertexes.Length + i] = Indices[i] + Vertexes.Length * im;
                        }
                    }

                    vb.GetType().GetMethods().First(x => x.Name == "SetData" && x.GetParameters().Length == 1).MakeGenericMethod(_cachedModelVertexMapping.VertexType).Invoke(
                        vb,
                        new[] { mappedVerticies });
                    ib.SetData(mappedIndicies);
                },
                _localisedBoundingRegion);
        }


        public void Dispose()
        {
            if (_indexBuffer != null)
            {
                _indexBuffer.Dispose();
                _indexBuffer = null;
            }
        }
    }
}
