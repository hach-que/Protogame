using System.Reflection;
using Assimp.Unmanaged;

namespace Protogame
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Assimp;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Quaternion = Microsoft.Xna.Framework.Quaternion;

    /// <summary>
    /// Reads an FBX file using AssImp and converts it to an <see cref="IModel"/>, which can be used
    /// at runtime for rendering, or serialized for storage as an asset.
    /// </summary>
    public class FbxReader
    {
        /// <summary>
        /// Load an FBX file from an array of bytes.
        /// </summary>
        /// <param name="data">
        /// The byte array containing the FBX data.
        /// </param>
        /// <param name="extension">
        /// The file extension for raw model data.
        /// </param>
        /// <param name="rawAdditionalAnimations">
        /// A dictionary mapping of animation names to byte arrays for additional FBX files to load.
        /// </param>
        /// <returns>
        /// The loaded <see cref="IModel"/>.
        /// </returns>
        public IModel Load(byte[] data, string extension, Dictionary<string, byte[]> rawAdditionalAnimations)
        {
            var file = Path.GetTempFileName() + "." + extension;
            using (var stream = new FileStream(file, FileMode.Create))
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }

            var additionalAnimationFiles = new Dictionary<string, string>();
            if (rawAdditionalAnimations != null)
            {
                foreach (var kv in rawAdditionalAnimations)
                {
                    var tempFile = Path.GetTempFileName() + "." + extension;
                    using (var stream = new FileStream(tempFile, FileMode.Create))
                    {
                        stream.Write(kv.Value, 0, kv.Value.Length);
                        stream.Close();
                    }

                    additionalAnimationFiles.Add(kv.Key, tempFile);
                }
            }

            var model = this.Load(file, additionalAnimationFiles);

            File.Delete(file);

            foreach (var kv in additionalAnimationFiles)
            {
                File.Delete(kv.Value);
            }

            return model;
        }

        /// <summary>
        /// Load an FBX file from a file on disk.
        /// </summary>
        /// <param name="filename">
        /// The filename of the FBX file to load.
        /// </param>
        /// <param name="additionalAnimationFiles">
        /// A dictionary mapping of animation names to filenames for additional FBX files to load.
        /// </param>
        /// <returns>
        /// The loaded <see cref="IModel"/>.
        /// </returns>
        public IModel Load(string filename, Dictionary<string, string> additionalAnimationFiles)
        {
            this.LoadAssimpLibrary();

            // Import the scene via AssImp.
            var importer = new AssimpContext();
            const PostProcessSteps ProcessFlags =
                PostProcessSteps.FlipUVs | PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.Triangulate
                | PostProcessSteps.SortByPrimitiveType | PostProcessSteps.FindInvalidData
                | PostProcessSteps.FlipWindingOrder;
            var scene = importer.ImportFile(filename, ProcessFlags);

            // Create the list of animations, including the null animation.
            var animations = new List<IAnimation> { this.CreateNullAnimation(scene) };

            // Import the basic animation.
            if (scene.AnimationCount > 0)
            {
                animations.AddRange(
                    scene.Animations.Select(
                        assimpAnimation => this.ImportAnimation(scene, assimpAnimation.Name, assimpAnimation)));
            }

            // For each additional animation file, import that and add the animation to the existing scene.
            foreach (var kv in additionalAnimationFiles)
            {
                var animationImporter = new AssimpContext();
                var additionalScene = animationImporter.ImportFile(kv.Value, ProcessFlags);

                if (additionalScene.AnimationCount != 1)
                {
                    // We can only import additional files that have a single animation.
                    continue;
                }

                animations.Add(this.ImportAnimation(scene, kv.Key, additionalScene.Animations[0]));
            }

            // Return the resulting model.
            return new Model(new AnimationCollection(animations));
        }

        /// <summary>
        /// Re-calculates the matrix transformations for each node in a node hierarchy for
        /// a given point in time in an animation.
        /// <para>
        /// When the FBX model is first imported, the node hierarchy represents the transformations
        /// for the bound (non-animated) model.  By applying the bone offset matrix followed by the
        /// cumulative node hierarchy transforms, we can obtain the original bound (non-animated) model.
        /// That is to say, the bone offset matrix is the direct inverse of the cumulative node
        /// hierarchy transforms, such that the combination of both forms an identity matrix for
        /// the mesh.
        /// </para>
        /// <para>
        /// This method clones the node hierarchy, but for nodes associated with bones in the FBX file,
        /// it recalculates the transformations for that bone based on the keys specified in the
        /// animation at a given point in time.  For nodes that are not associated with bones, we use
        /// the original transformation matrix.
        /// </para>
        /// <para>
        /// The method populates the tree of the <see cref="nodeAtTime"/> parameter, and calls itself
        /// recursively on child nodes of the original <see cref="assimpNode"/>.
        /// </para>
        /// </summary>
        /// <param name="nodeAtTime">
        /// The <see cref="NodeAtTime"/> structure that should be filled based on the other input information.
        /// </param>
        /// <param name="assimpNode">
        /// The AssImp node in the hierarchy that we are calculating for.  As this is a recursive function, we
        /// pass in different <see cref="NodeAtTime"/> and <see cref="Node"/> pairs to convert from the AssImp
        /// structure to the <see cref="NodeAtTime"/> structure.
        /// </param>
        /// <param name="mesh">
        /// The mesh that contains the bones manipulated by the animation.
        /// </param>
        /// <param name="assimpAnimation">
        /// The AssImp animation we are calculating for.
        /// </param>
        /// <param name="tick">
        /// The tick in the animation.
        /// </param>
        private void CalculateHierarchyAtTime(
            NodeAtTime nodeAtTime, 
            Node assimpNode, 
            Mesh mesh, 
            Assimp.Animation assimpAnimation, 
            int tick)
        {
            nodeAtTime.Name = assimpNode.Name;
            var transform = this.MatrixFromAssImpMatrix(assimpNode.Transform);

            Vector3 translation, scale;
            Quaternion rotation;
            transform.Decompose(out scale, out rotation, out translation);

            var channel = assimpAnimation.NodeAnimationChannels.FirstOrDefault(x => x.NodeName == nodeAtTime.Name);

            if (channel != null)
            {
                var bone = mesh.Bones.FirstOrDefault(x => assimpNode.Name.StartsWith(x.Name + "_$"));
                if (bone != null)
                {
                    Vector3 boneTranslation, boneScale;
                    Quaternion boneRotation;
                    this.MatrixFromAssImpMatrix(bone.OffsetMatrix)
                        .Decompose(out boneScale, out boneRotation, out boneTranslation);
                }

                nodeAtTime.Transform = Matrix.Identity;
                nodeAtTime.UseTransform = false;

                nodeAtTime.Transform *= Matrix.CreateTranslation(this.CalculateInterpolatedPosition(channel.PositionKeys, tick));
                nodeAtTime.Transform *= Matrix.CreateFromQuaternion(this.CalculateInterpolatedRotation(channel.RotationKeys, tick));
                nodeAtTime.Transform *= Matrix.CreateScale(this.CalculateInterpolatedScaling(channel.ScalingKeys, tick));

                nodeAtTime.Position = this.CalculateInterpolatedPosition(channel.PositionKeys, tick);
                nodeAtTime.Rotation = this.CalculateInterpolatedRotation(channel.RotationKeys, tick);
                nodeAtTime.Scaling = this.CalculateInterpolatedScaling(channel.ScalingKeys, tick);
            }
            else
            {
                nodeAtTime.Transform = transform;
                nodeAtTime.UseTransform = true;
            }

            if (assimpNode.ChildCount > 0)
            {
                foreach (var child in assimpNode.Children)
                {
                    var childNodeAtTime = new NodeAtTime { Parent = nodeAtTime };
                    nodeAtTime.Children.Add(childNodeAtTime);
                    this.CalculateHierarchyAtTime(childNodeAtTime, child, mesh, assimpAnimation, tick);
                }
            }
        }

        /// <summary>
        /// Calculates the interpolated position given the specified tick and array of position keys.
        /// </summary>
        /// <remarks>
        /// This function does not yet perform any interpolation.  When it does, the <see cref="tick"/>
        /// parameter will be updated to accept a float or double instead of an integer.
        /// </remarks>
        /// <param name="positionKeys">
        /// The position keys to interpolate between.
        /// </param>
        /// <param name="tick">
        /// The tick (also the index of the current key in the position keys array).
        /// </param>
        /// <returns>
        /// The interpolated position <see cref="Matrix"/>.
        /// </returns>
        private Vector3 CalculateInterpolatedPosition(List<VectorKey> positionKeys, int tick)
        {
            if (positionKeys.Count == 1)
            {
                return
                    new Vector3(positionKeys[0].Value.X, positionKeys[0].Value.Y, positionKeys[0].Value.Z);
            }

            var currentIndex = tick - 1;

            ////var nextIndex = currentIndex + 1 >= positionKeys.Length ? 0 : currentIndex + 1;
            if (currentIndex >= positionKeys.Count)
            {
                currentIndex = positionKeys.Count - 1;
            }

            // TODO: Actual interpolation.
            return
                new Vector3(
                    positionKeys[currentIndex].Value.X, 
                    positionKeys[currentIndex].Value.Y, 
                    positionKeys[currentIndex].Value.Z);
        }

        /// <summary>
        /// Calculates the interpolated rotation given the specified tick and array of rotation keys.
        /// </summary>
        /// <remarks>
        /// This function does not yet perform any interpolation.  When it does, the <see cref="tick"/>
        /// parameter will be updated to accept a float or double instead of an integer.
        /// </remarks>
        /// <param name="rotationKeys">
        /// The rotation keys to interpolate between.
        /// </param>
        /// <param name="tick">
        /// The tick (also the index of the current key in the rotation keys array).
        /// </param>
        /// <returns>
        /// The interpolated rotation <see cref="Matrix"/>.
        /// </returns>
        private Quaternion CalculateInterpolatedRotation(List<QuaternionKey> rotationKeys, int tick)
        {
            if (rotationKeys.Count == 1)
            {
                return
                    new Quaternion(
                        rotationKeys[0].Value.X, 
                        rotationKeys[0].Value.Y, 
                        rotationKeys[0].Value.Z, 
                        rotationKeys[0].Value.W);
            }

            var currentIndex = tick - 1;

            ////var nextIndex = currentIndex + 1 >= rotationKeys.Length ? 0 : currentIndex + 1;
            if (currentIndex >= rotationKeys.Count)
            {
                currentIndex = rotationKeys.Count - 1;
            }

            // TODO: Actual interpolation.
            return
                new Quaternion(
                    rotationKeys[currentIndex].Value.X, 
                    rotationKeys[currentIndex].Value.Y, 
                    rotationKeys[currentIndex].Value.Z, 
                    rotationKeys[currentIndex].Value.W);
        }

        /// <summary>
        /// Calculates the interpolated scale given the specified tick and array of scale keys.
        /// </summary>
        /// <remarks>
        /// This function does not yet perform any interpolation.  When it does, the <see cref="tick"/>
        /// parameter will be updated to accept a float or double instead of an integer.
        /// </remarks>
        /// <param name="scalingKeys">
        /// The scale keys to interpolate between.
        /// </param>
        /// <param name="tick">
        /// The tick (also the index of the current key in the scale keys array).
        /// </param>
        /// <returns>
        /// The interpolated scale <see cref="Matrix"/>.
        /// </returns>
        private Vector3 CalculateInterpolatedScaling(List<VectorKey> scalingKeys, int tick)
        {
            if (scalingKeys.Count == 1)
            {
                return
                    new Vector3(scalingKeys[0].Value.X, scalingKeys[0].Value.Y, scalingKeys[0].Value.Z);
            }

            var currentIndex = tick - 1;

            ////var nextIndex = currentIndex + 1 >= scalingKeys.Length ? 0 : currentIndex + 1;
            if (currentIndex >= scalingKeys.Count)
            {
                currentIndex = scalingKeys.Count - 1;
            }

            // TODO: Actual interpolation.
            return
                new Vector3(
                    scalingKeys[currentIndex].Value.X, 
                    scalingKeys[currentIndex].Value.Y, 
                    scalingKeys[currentIndex].Value.Z);
        }

        /// <summary>
        /// Copies a list of indices to a new list.
        /// </summary>
        /// <param name="baseIndices">
        /// The base indices list to copy from.
        /// </param>
        /// <returns>
        /// The new list of indices.
        /// </returns>
        private List<int> CopyIndices(IEnumerable<int> baseIndices)
        {
            return baseIndices.Select(index => index).ToList();
        }

        /// <summary>
        /// Copies a list of vertexes to a new list of vertexes.
        /// </summary>
        /// <param name="baseVertexes">
        /// The base vertexes list to copy from.
        /// </param>
        /// <returns>
        /// The new list of vertexes.
        /// </returns>
        private List<VertexPositionNormalTexture> CopyVertexes(IEnumerable<VertexPositionNormalTexture> baseVertexes)
        {
            return
                baseVertexes.Select(
                    vertex => new VertexPositionNormalTexture(vertex.Position, vertex.Normal, vertex.TextureCoordinate))
                    .ToList();
        }

        /// <summary>
        /// Creates the null animation for the current scene, that is the animation that actually
        /// contains no animation at all.  This function creates an animation with a single frame
        /// representing the original mesh.
        /// </summary>
        /// <param name="scene">
        /// The scene to generate the null animation from.
        /// </param>
        /// <returns>
        /// The <see cref="IAnimation"/> representing a single frame of the scene.
        /// </returns>
        private IAnimation CreateNullAnimation(Scene scene)
        {
            var vertexes = new List<VertexPositionNormalTexture>();
            var indices = new List<int>();

            // TODO: Assume one mesh for now.
            var mesh = scene.Meshes[0];

            // Import vertexes.
            // TODO: What to do with multiple texture coords?
            var uvs = mesh.TextureCoordinateChannels[0];
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                var pos = mesh.Vertices[i];
                var normal = mesh.Normals[i];
                var uv = i < uvs.Count ? uvs[i] : new Vector3D(0, 0, 0);

                vertexes.Add(
                    new VertexPositionNormalTexture(
                        new Vector3(pos.X, pos.Y, pos.Z), 
                        new Vector3(normal.X, normal.Y, normal.Z), 
                        new Vector2(uv.X, uv.Y)));
            }

            // Import indices.
            indices.AddRange(mesh.GetIndices());

            // Create frame 0.
            var frame = new Frame(vertexes.ToArray(), indices.ToArray());

            return new Animation(Animation.AnimationNullName, new IFrame[] { frame }, 0, 0);
        }

        /// <summary>
        /// Imports a specific animation from the specified scene, applying bone transformations
        /// and converting the result to an <see cref="IAnimation"/>.
        /// </summary>
        /// <param name="scene">
        /// The scene to generate the animation from.
        /// </param>
        /// <param name="name">
        /// The name of the animation.
        /// </param>
        /// <param name="assimpAnimation">
        /// The AssImp animation to apply to the scene.
        /// </param>
        /// <returns>
        /// The <see cref="IAnimation"/> representing the imported animation.
        /// </returns>
        private IAnimation ImportAnimation(Scene scene, string name, Assimp.Animation assimpAnimation)
        {
            var baseVertexes = new List<VertexPositionNormalTexture>();
            var baseIndices = new List<int>();

            // TODO: Assume one mesh for now.
            var mesh = scene.Meshes[0];

            // Import vertexes.
            // TODO: What to do with multiple texture coords?
            var uvs = mesh.TextureCoordinateChannels[0];
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                var pos = mesh.Vertices[i];
                var normal = mesh.Normals[i];
                var uv = uvs[i];

                baseVertexes.Add(
                    new VertexPositionNormalTexture(
                        new Vector3(pos.X, pos.Y, pos.Z), 
                        new Vector3(normal.X, normal.Y, normal.Z), 
                        new Vector2(uv.X, uv.Y)));
            }

            // Import indices.
            baseIndices.AddRange(mesh.GetIndices());

            // For each of the frames in the animation, calculate what they look like and
            // add them.
            var frames = new List<IFrame>();
            for (var i = 0; i < assimpAnimation.DurationInTicks; i++)
            {
                frames.Add(this.ImportFrame(baseVertexes, baseIndices, scene, assimpAnimation, i + 1));
            }

            return new Animation(
                name, 
                frames.ToArray(), 
                assimpAnimation.TicksPerSecond, 
                assimpAnimation.DurationInTicks);
        }

        /// <summary>
        /// Imports a single frame from an animation to an <see cref="IFrame"/> class.
        /// </summary>
        /// <param name="baseVertexes">
        /// The base vertexes of the mesh.  These will be copied, and the copy will have the animation
        /// transformations applied for the given frame.
        /// </param>
        /// <param name="baseIndices">
        /// The base indices of the mesh.  These will be copied, and the copy will have the animation
        /// transformations applied for the given frame.
        /// </param>
        /// <param name="scene">
        /// The scene that contains the mesh and animation that is being applied.
        /// </param>
        /// <param name="assimpAnimation">
        /// The AssImp animation that contains the specified frame / tick.
        /// </param>
        /// <param name="tick">
        /// The tick (or frame) of the animation to import.
        /// </param>
        /// <returns>
        /// The <see cref="IFrame"/> representing the imported frame.
        /// </returns>
        private IFrame ImportFrame(
            IEnumerable<VertexPositionNormalTexture> baseVertexes, 
            IEnumerable<int> baseIndices, 
            Scene scene, 
            Assimp.Animation assimpAnimation, 
            int tick)
        {
            var vertexes = this.CopyVertexes(baseVertexes);
            var indices = this.CopyIndices(baseIndices);

            var mesh = scene.Meshes[0];

            var channels = assimpAnimation.NodeAnimationChannels.Select(x => x.NodeName).ToArray();

            // Perform a sanity check on the scene we are animating.  All node animation
            // channels should also have a bone present that matches what they are animating,
            // or the FBX files aren't lined up.
            foreach (var nodeChannel in assimpAnimation.NodeAnimationChannels)
            {
                var matching = this.GetNodesAsLinear(scene.RootNode).FirstOrDefault(x => x.Name == nodeChannel.NodeName);
                if (matching == null)
                {
                    System.Console.WriteLine("WARNING: There is an animation applying to " + nodeChannel.NodeName + " but it is not present in the scene.");
                }
            }

            // Calculate the hierarchy as it would appear at a given point in time.  All transformations
            // in the tree are applicable to that particular node, but do not incorporate the transformations
            // of it's parent.
            var hierarchyAtTime = new NodeAtTime();
            this.CalculateHierarchyAtTime(hierarchyAtTime, scene.RootNode, mesh, assimpAnimation, tick);

            // Now traverse the nodes in the hierarchy and calculate all of their translations to the applicable
            // vertexes and bones.
            var transformsPerVertex = new List<Matrix>[vertexes.Count];
            this.TraverseHierarchyAndCalculateTransforms(transformsPerVertex, vertexes, hierarchyAtTime, mesh);

            // Apply the transforms to the vertexes.
            this.ApplyTransforms(transformsPerVertex, vertexes);

            return new Frame(vertexes.ToArray(), indices.ToArray());
        }

        private IEnumerable<Node> GetNodesAsLinear(Node node)
        {
            yield return node;
            foreach (var child in node.Children)
            {
                foreach (var linear in this.GetNodesAsLinear(child))
                {
                    yield return linear;
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Matrix"/> from an AssImp <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="matrix">
        /// The AssImp matrix to convert.
        /// </param>
        /// <returns>
        /// The <see cref="Matrix"/> representing the same transformation.
        /// </returns>
        private Matrix MatrixFromAssImpMatrix(Matrix4x4 matrix)
        {
            return new Matrix(
                matrix.A1, 
                matrix.B1, 
                matrix.C1, 
                matrix.D1, 
                matrix.A2, 
                matrix.B2, 
                matrix.C2, 
                matrix.D2, 
                matrix.A3, 
                matrix.B3, 
                matrix.C3, 
                matrix.D3, 
                matrix.A4, 
                matrix.B4, 
                matrix.C4, 
                matrix.D4);
        }

        /// <summary>
        /// Traverse the converted <see cref="NodeAtTime"/> hierarchy (with transforms updated by
        /// <see cref="CalculateHierarchyAtTime"/>) and calculate the cumulative transformations to
        /// the specified vertexes.
        /// </summary>
        /// <param name="vertexes">
        /// The vertexes that the transformations should be calculated for.
        /// </param>
        /// <param name="node">
        /// The root node to traverse.  This function is called recursively and as such, this
        /// will change to each child node as it's traversed.
        /// </param>
        /// <param name="mesh">
        /// The mesh containing the bones being applied.
        /// </param>
        private void TraverseHierarchyAndCalculateTransforms(
            List<Matrix>[] transformsPerVertex,
            List<VertexPositionNormalTexture> vertexes, 
            NodeAtTime node, 
            Mesh mesh)
        {
            // Find the bone associated with this node.  Each transformation is split up
            // in the FBX format, but the bone maps to the deepest node.  Thus when we
            // match exactly, we'll calculate all of the parent transformations for the bone
            // that are relevant.
            var bone = mesh.Bones.FirstOrDefault(x => x.Name == node.Name);

            if (bone != null)
            {
                foreach (var vertexWeight in bone.VertexWeights)
                {
                    var finalMatrix = this.MatrixFromAssImpMatrix(bone.OffsetMatrix);

                    var applicationNode = node;
                    while (applicationNode != null)
                    {
                        var transform = applicationNode.GetTransform();

                        finalMatrix *= transform;

                        applicationNode = applicationNode.Parent;
                    }

                    finalMatrix = finalMatrix * vertexWeight.Weight;

                    if (transformsPerVertex[vertexWeight.VertexID] == null)
                    {
                        transformsPerVertex[vertexWeight.VertexID] = new List<Matrix>();
                    }

                    transformsPerVertex[vertexWeight.VertexID].Add(finalMatrix);
                }
            }

            // Descend into the hierarchy further.
            foreach (var child in node.Children)
            {
                this.TraverseHierarchyAndCalculateTransforms(transformsPerVertex, vertexes, child, mesh);
            }
        }

        private void ApplyTransforms(
            List<Matrix>[] transformsPerVertex,
            List<VertexPositionNormalTexture> vertexes)
        {
            for (var i = 0; i < vertexes.Count; i++)
            {
                var vertex = vertexes[i];

                if (transformsPerVertex[i] == null)
                {
                    // Nothing to apply here.
                    continue;
                }

                var finalMatrix = transformsPerVertex[i][0];
                for (var a = 1; a < transformsPerVertex[i].Count; a++)
                {
                    finalMatrix += transformsPerVertex[i][a];
                }

                // TODO: What about normals!?!?!?
                vertexes[i] =
                    new VertexPositionNormalTexture(
                        Vector3.Transform(vertex.Position, finalMatrix), 
                        vertex.Normal, 
                        vertex.TextureCoordinate);
            }
        }

#if PLATFORM_LINUX
        private void LoadAssimpLibrary()
        {
            var targetDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;

            try
            {
                AssimpLibrary.Instance.LoadLibrary(
                    Path.Combine(targetDir, "libassimp_32.so.3.0.1"), 
                    Path.Combine(targetDir, "libassimp_64.so.3.0.1"));
            }
            catch (AssimpException ex)
            {
                if (ex.Message == "Assimp library already loaded.")
                    return;
                throw;
            }
        }
#elif PLATFORM_WINDOWS

        /// <summary>
        /// The load assimp library.
        /// </summary>
        private void LoadAssimpLibrary()
        {
            // Assimp.NET already has the correct values for Windows.
        }

#else
        private void LoadAssimpLibrary()
        {
            // Assimp.NET will not work on this platform anyway.
        }
#endif

        /// <summary>
        /// Represents a node in the node hierarchy at a specific point in time; that is
        /// it represents a node with the transforms calculated based on an animation.
        /// </summary>
        private class NodeAtTime
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NodeAtTime"/> class.
            /// </summary>
            public NodeAtTime()
            {
                this.Children = new List<NodeAtTime>();
            }

            /// <summary>
            /// Gets the children nodes.
            /// </summary>
            /// <value>
            /// The children nodes.
            /// </value>
            public List<NodeAtTime> Children { get; private set; }

            /// <summary>
            /// Gets or sets the name of the node.
            /// </summary>
            /// <value>
            /// The name of the node.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the parent of the node.
            /// </summary>
            /// <value>
            /// The parent of the node.
            /// </value>
            public NodeAtTime Parent { get; set; }

            public Matrix Transform { get; set; }

            public Vector3 Position { get; set; }

            public Quaternion Rotation { get; set; }

            public Vector3 Scaling { get; set; }

            public bool UseTransform { get; set; }

            public Matrix GetTransform()
            {
                if (!this.UseTransform)
                {
                    return
                        Matrix.Identity *
                        Matrix.CreateTranslation(this.Position) * 
                        Matrix.CreateFromQuaternion(this.Rotation) *
                        Matrix.CreateScale(this.Scaling);
                }
                else
                {
                    return this.Transform;
                }
            }
        }
    }
}