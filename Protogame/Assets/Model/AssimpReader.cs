#if PLATFORM_LINUX || PLATFORM_MACOS || PLATFORM_WINDOWS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if PLATFORM_LINUX
using System.Reflection;
#endif
using Assimp;
using Assimp.Configs;
#if PLATFORM_LINUX
using Assimp.Unmanaged;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Protogame
{
    /// <summary>
    /// Reads a model file using AssImp and converts it to an <see cref="IModel"/>, which can be used
    /// at runtime for rendering, or serialized for storage as an asset.
    /// </summary>
    public class AssimpReader
    {
        private readonly IModelRenderConfiguration[] _modelRenderConfigurations;
        private readonly IRenderBatcher _renderBatcher;

        public AssimpReader(IModelRenderConfiguration[] modelRenderConfigurations, IRenderBatcher renderBatcher)
        {
            _modelRenderConfigurations = modelRenderConfigurations;
            _renderBatcher = renderBatcher;
        }

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
        /// <param name="options">Additional options for the import.</param>
        /// <returns>
        /// The loaded <see cref="IModel"/>.
        /// </returns>
        public IModel Load(byte[] data, string name, string extension, Dictionary<string, byte[]> rawAdditionalAnimations, string[] options)
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

            var model = this.Load(file, name, additionalAnimationFiles, options);

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
        /// <param name="options">Additional options for the import.</param>
        /// <returns>
        /// The loaded <see cref="IModel"/>.
        /// </returns>
        public IModel Load(string filename, string name, Dictionary<string, string> additionalAnimationFiles, string[] options)
        {
            this.LoadAssimpLibrary();

            // Import the scene via AssImp.
            var importer = new AssimpContext();
            PostProcessSteps ProcessFlags = 0;
            if (options == null)
            {
                ProcessFlags |= PostProcessSteps.FlipUVs | PostProcessSteps.Triangulate |
                                PostProcessSteps.FlipWindingOrder;
            }
            else
            {
                foreach (var v in options)
                {
                    PostProcessSteps flag;
                    if (Enum.TryParse(v, true, out flag))
                    {
                        ProcessFlags |= flag;
                        Console.Write("(on: " + flag + ") ");
                    }
                }
            }
            
            var scene = importer.ImportFile(filename, ProcessFlags);

            // Import the meshes.
            var importedMeshes = new IModelMesh[scene.MeshCount];
            for (var i = 0; i < scene.MeshCount; i++)
            {
                Material material = null;
                IModelBone boneHierarchy;

                var boneWeightingMap = this.BuildBoneWeightingMap(scene, i);
                var staticTransformMap = this.BuildStaticTransformMap(scene, i);

                var vertexes = this.ImportVertexes(scene, i, boneWeightingMap, staticTransformMap);
                var indices = this.ImportIndices(scene, i);

                if (options?.Contains("!NoBoneHierarchy") ?? false)
                {
                    boneHierarchy = null;
                }
                else
                {
                    boneHierarchy = this.ImportBoneHierarchy(scene.RootNode, scene.Meshes[i]);
                }

                // If the mesh has a material associated with it, read in
                // the material information.
                if (scene.MaterialCount > scene.Meshes[i].MaterialIndex)
                {
                    var assimpMaterial = scene.Materials[scene.Meshes[i].MaterialIndex];

                    material = ConvertMaterial(assimpMaterial);
                }

                importedMeshes[i] = new ModelMesh(
                    _modelRenderConfigurations,
                    _renderBatcher,
                    material,
                    boneHierarchy,
                    vertexes,
                    indices);
            }
            
            // Create the list of animations, including the null animation.
            var animations = new List<IAnimation>();

            // Import the basic animation.
            if (scene.AnimationCount > 0)
            {
                animations.AddRange(
                    scene.Animations.Select(
                        assimpAnimation => this.ImportAnimation(assimpAnimation.Name, assimpAnimation)));
            }

            // For each additional animation file, import that and add the animation to the existing scene.
            animations.AddRange(
                from kv in additionalAnimationFiles
                let animationImporter = new AssimpContext()
                let additionalScene = animationImporter.ImportFile(kv.Value, ProcessFlags)
                where additionalScene.AnimationCount == 1
                select this.ImportAnimation(kv.Key, additionalScene.Animations[0]));

            // Return the resulting model.
            return new Model(
                name,
                new AnimationCollection(animations),
                importedMeshes);
        }

        private Material ConvertMaterial(Assimp.Material m)
        {
            return new Material
            {
                ColorAmbient = m.HasColorAmbient ? (Color?)ConvertMaterialColor(m.ColorAmbient) : null,
                ColorDiffuse = m.HasColorDiffuse ? (Color?)ConvertMaterialColor(m.ColorDiffuse) : null,
                ColorSpecular = m.HasColorSpecular ? (Color?)ConvertMaterialColor(m.ColorSpecular) : null,
                ColorEmissive = m.HasColorEmissive ? (Color?)ConvertMaterialColor(m.ColorEmissive) : null,
                ColorTransparent = m.HasColorTransparent ? (Color?)ConvertMaterialColor(m.ColorTransparent) : null,
                ColorReflective = m.HasColorReflective ? (Color?)ConvertMaterialColor(m.ColorReflective) : null,
                TextureEmissive = m.HasTextureDiffuse ? ConvertMaterialTexture(m.TextureEmissive) : null,
                TextureAmbient = m.HasTextureAmbient ? ConvertMaterialTexture(m.TextureAmbient) : null,
                TextureDisplacement = m.HasTextureDisplacement ? ConvertMaterialTexture(m.TextureDisplacement) : null,
                TextureSpecular = m.HasTextureSpecular ? ConvertMaterialTexture(m.TextureSpecular) : null,
                TextureDiffuse = m.HasTextureDiffuse ? ConvertMaterialTexture(m.TextureDiffuse) : null,
                TextureHeight = m.HasTextureHeight ? ConvertMaterialTexture(m.TextureHeight) : null,
                TextureLightMap = m.HasTextureLightMap ? ConvertMaterialTexture(m.TextureLightMap) : null,
                TextureNormal = m.HasTextureNormal ? ConvertMaterialTexture(m.TextureNormal) : null,
                TextureOpacity = m.HasTextureOpacity ? ConvertMaterialTexture(m.TextureOpacity) : null,
                TextureReflection = m.HasTextureReflection ? ConvertMaterialTexture(m.TextureReflection) : null,
                PowerSpecular = m.HasShininess ? (float?)(m.Shininess / 255f) : null,
            };
        }

        private IMaterialTexture ConvertMaterialTexture(TextureSlot t)
        {
            return new MaterialTexture
            {
                HintPath = t.FilePath
            };
        }

        private Color ConvertMaterialColor(Color4D c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }

        /// <summary>
        /// Imports the FBX bone hierarchy of the specified mesh, converting each node in the hierarchy
        /// to a model bone.
        /// </summary>
        /// <remarks>
        /// This function recursively traverses the hierarchy, calling itself each time with a different <see cref="node"/>
        /// argument.
        /// </remarks>
        /// <param name="node">
        /// The current node to import.
        /// </param>
        /// <param name="mesh">
        /// The mesh that is being imported.
        /// </param>
        /// <returns>
        /// The imported bone hierarchy.
        /// </returns>
        private IModelBone ImportBoneHierarchy(Node node, Mesh mesh)
        {
            var childBones =
                node.Children.Select(child => this.ImportBoneHierarchy(child, mesh)).ToDictionary(k => k.Name, v => v);

            var boneIndex = -1;
            var offsetMatrix = Matrix.Identity;
            if (mesh != null)
            {
                if (mesh.Bones.Count > 48)
                {
                    throw new InvalidOperationException(
                        "This model contains more bones than the supported maximum (48).");
                }

                for (var i = 0; i < mesh.Bones.Count; i++)
                {
                    if (mesh.Bones[i].Name == node.Name)
                    {
                        boneIndex = i;
                        offsetMatrix = this.MatrixFromAssImpMatrix(mesh.Bones[i].OffsetMatrix);
                        break;
                    }
                }
            }

            var transform = this.MatrixFromAssImpMatrix(node.Transform);
            Vector3 translation, scale;
            Quaternion rotation;
            transform.Decompose(out scale, out rotation, out translation);

            return new ModelBone(boneIndex, node.Name, childBones, offsetMatrix, translation, rotation, scale);
        }

        /// <summary>
        /// Builds a bone weighting map.  A bone weighting map is a map of each vertex to the
        /// bone indexes and weightings that apply to it.
        /// </summary>
        /// <remarks>
        /// This is used to build up the map which is assigned against the vertexes in the model.  These
        /// bone indices and weightings are then pushed through to the hardware in the BLENDWEIGHTS
        /// and BLENDINDICES semantic fields.
        /// </remarks>
        /// <param name="scene">
        /// The scene to build the map from.
        /// </param>
        /// <returns>
        /// The bone weighting map.
        /// </returns>
        private Dictionary<Vector3, KeyValuePair<Byte4, Vector4>> BuildBoneWeightingMap(Scene scene, int meshIndex)
        {
            var map = new Dictionary<Vector3, List<KeyValuePair<int, float>>>();
            
            for (var i = 0; i < scene.Meshes[meshIndex].BoneCount; i++)
            {
                var bone = scene.Meshes[meshIndex].Bones[i];

                for (var w = 0; w < bone.VertexWeightCount; w++)
                {
                    var assimpVertex = scene.Meshes[meshIndex].Vertices[bone.VertexWeights[w].VertexID];
                    var vertex = new Vector3(assimpVertex.X, assimpVertex.Y, assimpVertex.Z);

                    if (!map.ContainsKey(vertex))
                    {
                        map[vertex] = new List<KeyValuePair<int, float>>();
                    }

                    var list = map[vertex];

                    if (list.All(x => x.Key != i))
                    {
                        list.Add(new KeyValuePair<int, float>(i, bone.VertexWeights[w].Weight));
                    }
                }
            }

            var boneWeightMap = new Dictionary<Vector3, KeyValuePair<Byte4, Vector4>>();

            foreach (var kv in map)
            {
                var indices = new Vector4();
                var weights = new Vector4();

                if (kv.Value.Count >= 1)
                {
                    indices.X = kv.Value[0].Key;
                    weights.X = kv.Value[0].Value;
                }

                if (kv.Value.Count >= 2)
                {
                    indices.Y = kv.Value[1].Key;
                    weights.Y = kv.Value[1].Value;
                }

                if (kv.Value.Count >= 3)
                {
                    indices.Z = kv.Value[2].Key;
                    weights.Z = kv.Value[2].Value;
                }

                if (kv.Value.Count >= 4)
                {
                    indices.W = kv.Value[3].Key;
                    weights.W = kv.Value[3].Value;
                }

                if (kv.Value.Count >= 5)
                {
                    throw new InvalidOperationException("A vertex has more than 4 bones associated with it.");
                }

                boneWeightMap.Add(kv.Key, new KeyValuePair<Byte4, Vector4>(new Byte4(indices), weights));
            }

            return boneWeightMap;
        }

        /// <summary>
        /// Builds the static transform map.  This is used by vertexes which do not have bones to
        /// weight them.  When there are no bone weights available, we transform the vertexes based
        /// on the scene hierarchy directly on import.
        /// </summary>
        /// <param name="scene">The scene to process.</param>
        /// <returns>
        /// A map of meshes to transform matrices.  Because this importer can only 
        /// handle a single mesh per scene, this will only contain one entry.
        /// </returns>
        private Dictionary<Mesh, Matrix> BuildStaticTransformMap(Scene scene, int meshIndex)
        {
            var matrix = this.GetStaticTransformMatrixForFirstMesh(scene.RootNode, meshIndex);

            if (matrix != null)
            {
                return new Dictionary<Mesh, Matrix>
                {
                    {scene.Meshes[meshIndex], matrix.Value}
                };
            }

            return new Dictionary<Mesh, Matrix>();
        }

        /// <summary>
        /// Returns the cumulative transformation matrix if the specified node or one of it's
        /// descendants specifies the transformation matrix for the first mesh in the scene
        /// (the only one that this importer processes).
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <returns>A matrix, or null.</returns>
        private Matrix? GetStaticTransformMatrixForFirstMesh(Node node, int meshIndex)
        {
            if (node.MeshCount > 0 && node.MeshIndices[0] == meshIndex)
            {
                return this.MatrixFromAssImpMatrix(node.Transform);
            }

            if (node.ChildCount > 0)
            {
                foreach (var child in node.Children)
                {
                    var result = GetStaticTransformMatrixForFirstMesh(child, meshIndex);
                    if (result != null)
                    {
                        // We have to return a matrix which is this node's transformation
                        // matrix, multiplied by the child matrix.
                        var ourMatrix = this.MatrixFromAssImpMatrix(node.Transform);
                        return ourMatrix*result.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Imports the mesh indices from the scene.
        /// </summary>
        /// <param name="scene">
        /// The scene that contains the mesh.
        /// </param>
        /// <returns>
        /// The imported indices.
        /// </returns>
        private int[] ImportIndices(Scene scene, int meshIndex)
        {
            var mesh = scene.Meshes[0];

            return mesh.GetIndices();
        }

        /// <summary>
        /// Imports the mesh vertexes from the scene and adds the appropriate bone weighting data to each vertex.
        /// </summary>
        /// <param name="scene">
        ///     The scene that contains the mesh.
        /// </param>
        /// <param name="boneWeightingMap">
        ///     The bone weighting map.
        /// </param>
        /// <param name="staticTransformMap"></param>
        /// <returns>
        /// The imported vertexes.
        /// </returns>
        private ModelVertex[] ImportVertexes(Scene scene, int meshIndex, Dictionary<Vector3, KeyValuePair<Byte4, Vector4>> boneWeightingMap, Dictionary<Mesh, Matrix> staticTransformMap)
        {
            var mesh = scene.Meshes[0];

            var vertexes = new List<ModelVertex>();

            // Calculate channel associations.
            var uvCount = 0;
            var uvwCount = 0;
            for (var c = 0; c < mesh.TextureCoordinateChannelCount; c++)
            {
                if (mesh.UVComponentCount[c] == 2)
                {
                    uvCount++;
                }
                else if (mesh.UVComponentCount[c] == 3)
                {
                    uvwCount++;
                }
            }

            // Check indices count.
            Vector3[] generatedTangents = null;
            Vector3[] generatedBitangents = null;
            var indices = mesh.GetIndices();
            if (indices.Length%3 != 0)
            {
                Console.WriteLine(
                    "WARNING: Model does not have triplets of indices, which means the loaded model has likely not " +
                    "triangulated.  Automatic calculation of tangents and bitangents is not possible, and the model " +
                    "will most likely not render correctly in-game.");
            }
            else
            {
                // Check if we need to generate bitangents/binormals and tangents.
                if (!mesh.HasTangentBasis && mesh.HasNormals && mesh.HasTextureCoords(0))
                {
                    var positions = mesh.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
                    var normals = mesh.Normals.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
                    var texCoords = mesh.TextureCoordinateChannels[0].Select(x => new Vector2(x.X, x.Y)).ToArray();
                    
                    MeshHelper.CalculateTangentFrames(
                        positions, indices, normals, texCoords, out generatedTangents, out generatedBitangents);
                }
            }

            // If we have no possibility of obtaining tangent and bitangent information,
            // warn the user.
            if (!mesh.HasTangentBasis && generatedTangents == null && generatedBitangents == null)
            {
                Console.WriteLine(
                    "WARNING: Model does not include binormal and tangent information, and none " +
                    "could be generated.  Normal mapping will not be possible.");
            }

            // Import vertexes.
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                Vector3? position, normal, tangent, bitangent;
                Color[] colors = new Color[mesh.VertexColorChannelCount];
                Vector2[] texCoordsUV = new Vector2[uvCount];
                Vector3[] texCoordsUVW = new Vector3[uvwCount];
                Byte4? boneIndices;
                Vector4? boneWeights;

                if (mesh.HasVertices)
                {
                    var v = mesh.Vertices[i];
                    position = new Vector3(v.X, v.Y, v.Z);
                }
                else
                {
                    position = null;
                }

                if (mesh.HasNormals)
                {
                    var v = mesh.Normals[i];
                    normal = new Vector3(v.X, v.Y, v.Z);
                }
                else
                {
                    normal = null;
                }

                if (mesh.HasTangentBasis)
                {
                    var v1 = mesh.Tangents[i];
                    var v2 = mesh.BiTangents[i];
                    tangent = new Vector3(v1.X, v1.Y, v1.Z);
                    bitangent = new Vector3(v2.X, v2.Y, v2.Z);
                }
                else if (generatedTangents != null && generatedBitangents != null)
                {
                    tangent = generatedTangents[i];
                    bitangent = generatedBitangents[i];
                }
                else
                {
                    tangent = null;
                    bitangent = null;
                }

                var uvCountTemp = 0;
                var uvwCountTemp = 0;
                for (var c = 0; c < mesh.TextureCoordinateChannelCount; c++)
                {
                    if (mesh.UVComponentCount[c] == 2)
                    {
                        var v = mesh.TextureCoordinateChannels[c][i];
                        texCoordsUV[uvCountTemp] = new Vector2(v.X, v.Y);
                        uvCountTemp++;
                    }
                    else if (mesh.UVComponentCount[c] == 3)
                    {
                        var v = mesh.TextureCoordinateChannels[c][i];
                        texCoordsUVW[uvwCountTemp] = new Vector3(v.X, v.Y, v.Z);
                        uvwCountTemp++;
                    }
                }

                boneIndices = null;
                boneWeights = null;
                if (position != null)
                {
                    if (boneWeightingMap.ContainsKey(position.Value))
                    {
                        boneIndices = boneWeightingMap[position.Value].Key;
                        boneWeights = boneWeightingMap[position.Value].Value;
                    }
                    else if (staticTransformMap.ContainsKey(mesh))
                    {
                        // Transform the position by the world matrix on import.
                        position = Vector3.Transform(position.Value, staticTransformMap[mesh]);

                        // Also transform the normal by the world matrix, if it's present.
                        if (normal.HasValue)
                        {
                            normal = Vector3.Transform(normal.Value, staticTransformMap[mesh]);
                        }
                    }
                }

                vertexes.Add(
                    new ModelVertex(
                        position,
                        normal,
                        tangent,
                        bitangent,
                        colors,
                        texCoordsUV,
                        texCoordsUVW,
                        boneIndices,
                        boneWeights));
            }

            return vertexes.ToArray();
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
        /// Imports an FBX animation, storing the transformations that apply to each bone at each time.
        /// </summary>
        /// <param name="name">
        /// The name of the animation.
        /// </param>
        /// <param name="assimpAnimation">
        /// The AssImp animation to apply to the scene.
        /// </param>
        /// <returns>
        /// The <see cref="IAnimation"/> representing the imported animation.
        /// </returns>
        private IAnimation ImportAnimation(string name, Assimp.Animation assimpAnimation)
        {
            var translation = new Dictionary<string, IDictionary<double, Vector3>>();
            var rotation = new Dictionary<string, IDictionary<double, Quaternion>>();
            var scale = new Dictionary<string, IDictionary<double, Vector3>>();

            foreach (var animChannel in assimpAnimation.NodeAnimationChannels)
            {
                if (animChannel.HasPositionKeys)
                {
                    if (!translation.ContainsKey(animChannel.NodeName))
                    {
                        translation[animChannel.NodeName] = new Dictionary<double, Vector3>();
                    }

                    foreach (var positionKey in animChannel.PositionKeys)
                    {
                        var vector = new Vector3(positionKey.Value.X, positionKey.Value.Y, positionKey.Value.Z);

                        translation[animChannel.NodeName].Add(positionKey.Time, vector);
                    }
                }

                if (animChannel.HasRotationKeys)
                {
                    if (!rotation.ContainsKey(animChannel.NodeName))
                    {
                        rotation[animChannel.NodeName] = new Dictionary<double, Quaternion>();
                    }

                    foreach (var rotationKey in animChannel.RotationKeys)
                    {
                        var quaternion = new Quaternion(
                            rotationKey.Value.X, 
                            rotationKey.Value.Y, 
                            rotationKey.Value.Z, 
                            rotationKey.Value.W);

                        rotation[animChannel.NodeName].Add(rotationKey.Time, quaternion);
                    }
                }

                if (animChannel.HasScalingKeys)
                {
                    if (!scale.ContainsKey(animChannel.NodeName))
                    {
                        scale[animChannel.NodeName] = new Dictionary<double, Vector3>();
                    }

                    foreach (var scaleKey in animChannel.ScalingKeys)
                    {
                        var vector = new Vector3(scaleKey.Value.X, scaleKey.Value.Y, scaleKey.Value.Z);

                        scale[animChannel.NodeName].Add(scaleKey.Time, vector);
                    }
                }
            }

            return new Animation(
                name, 
                assimpAnimation.TicksPerSecond, 
                assimpAnimation.DurationInTicks, 
                translation, 
                rotation, 
                scale);
        }

#if PLATFORM_LINUX
        /// <summary>
        /// Loads the AssImp library.
        /// </summary>
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
        /// Loads the AssImp library.
        /// </summary>
        private void LoadAssimpLibrary()
        {
            // Assimp.NET already has the correct values for Windows.
        }

#else
        /// <summary>
        /// Loads the AssImp library.
        /// </summary>
        private void LoadAssimpLibrary()
        {
            // Assimp.NET will not work on this platform anyway.
        }
#endif
    }
}

#endif