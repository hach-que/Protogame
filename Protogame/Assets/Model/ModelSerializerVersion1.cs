using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Protogame
{
    /// <summary>
    /// Serializes and deserializes <see cref="IModel"/> for storage in a binary format.
    /// </summary>
    public class ModelSerializerVersion1 : IModelSerializer
    {
        private readonly IModelRenderConfiguration[] _modelRenderConfigurations;
        private readonly IRenderBatcher _renderBatcher;

        public ModelSerializerVersion1(
            IModelRenderConfiguration[] modelRenderConfigurations,
            IRenderBatcher renderBatcher)
        {
            _modelRenderConfigurations = modelRenderConfigurations;
            _renderBatcher = renderBatcher;
        }

        /// <summary>
        /// Deserialize the specified byte array into a concrete <see cref="Model"/> implementation.
        /// </summary>
        /// <param name="data">
        /// The byte array to deserialize.
        /// </param>
        /// <returns>
        /// The deserialized <see cref="Model"/>.
        /// </returns>
        public Model Deserialize(string name, byte[] data)
        {
            using (var memory = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(memory))
                {
                    return this.DeserializeModel(name, reader);
                }
            }
        }

        /// <summary>
        /// Serializes the specified <see cref="IModel"/> into a byte array.
        /// </summary>
        /// <param name="model">
        /// The model to serialize.
        /// </param>
        /// <returns>
        /// The resulting byte array.
        /// </returns>
        public byte[] Serialize(IModel model)
        {
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory))
                {
                    this.SerializeModel(writer, model);

                    var length = memory.Position;
                    memory.Seek(0, SeekOrigin.Begin);
                    var bytes = new byte[length];
                    memory.Read(bytes, 0, (int)length);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Deserializes a collection of animations from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized collection of animations.
        /// </returns>
        private IEnumerable<IAnimation> DeserializeAnimations(BinaryReader reader)
        {
            var animationCount = reader.ReadInt32();

            for (var i = 0; i < animationCount; i++)
            {
                var name = reader.ReadString();
                var ticksPerSecond = reader.ReadDouble();
                var durationInTicks = reader.ReadDouble();
                var translationForBones = this.DeserializeStringFloatVector3DictionaryDictionary(reader);
                var rotationForBones = this.DeserializeStringFloatQuaternionDictionaryDictionary(reader);
                var scaleForBones = this.DeserializeStringFloatVector3DictionaryDictionary(reader);

                yield return
                    new Animation(
                        name, 
                        ticksPerSecond, 
                        durationInTicks, 
                        translationForBones, 
                        rotationForBones, 
                        scaleForBones);
            }
        }

        /// <summary>
        /// Deserializes the bone hierarchy of a model from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The root bone in the hierarchy.
        /// </returns>
        private IModelBone DeserializeBoneHierarchy(BinaryReader reader)
        {
            var id = reader.ReadInt32();
            var name = reader.ReadString();

            var count = reader.ReadUInt32();
            var children = new Dictionary<string, IModelBone>();

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = this.DeserializeBoneHierarchy(reader);

                children.Add(key, value);
            }

            var boneOffset = this.DeserializeMatrix(reader);
            var translation = this.DeserializeVector3(reader);
            var rotation = this.DeserializeQuaternion(reader);
            var scale = this.DeserializeVector3(reader);

            return new ModelBone(id, name, children, boneOffset, translation, rotation, scale);
        }

        /// <summary>
        /// Deserializes a <see cref="Byte4"/> from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized packed vector.
        /// </returns>
        private Byte4 DeserializeByte4(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();

            return new Byte4(x, y, z, w);
        }

        /// <summary>
        /// Deserializes a float-quaternion dictionary from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The float-quaternion dictionary.
        /// </returns>
        private IDictionary<double, Quaternion> DeserializeFloatQuaternionDictionary(BinaryReader reader)
        {
            var count = reader.ReadUInt32();
            var result = new Dictionary<double, Quaternion>();

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadDouble();
                var value = this.DeserializeQuaternion(reader);

                result.Add(key, value);
            }

            return result;
        }

        /// <summary>
        /// Deserializes a float-vector3 dictionary from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The float-vector3 dictionary.
        /// </returns>
        private IDictionary<double, Vector3> DeserializeFloatVector3Dictionary(BinaryReader reader)
        {
            var count = reader.ReadUInt32();
            var result = new Dictionary<double, Vector3>();

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadDouble();
                var value = this.DeserializeVector3(reader);

                result.Add(key, value);
            }

            return result;
        }

        /// <summary>
        /// Deserializes a list of model indices from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized model indices.
        /// </returns>
        private int[] DeserializeIndices(BinaryReader reader)
        {
            var indexCount = reader.ReadInt32();
            var indices = new List<int>();

            for (var i = 0; i < indexCount; i++)
            {
                indices.Add(reader.ReadInt32());
            }

            return indices.ToArray();
        }

        /// <summary>
        /// Deserializes a <see cref="Matrix"/> from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized matrix.
        /// </returns>
        private Matrix DeserializeMatrix(BinaryReader reader)
        {
            var m11 = reader.ReadSingle();
            var m12 = reader.ReadSingle();
            var m13 = reader.ReadSingle();
            var m14 = reader.ReadSingle();
            var m21 = reader.ReadSingle();
            var m22 = reader.ReadSingle();
            var m23 = reader.ReadSingle();
            var m24 = reader.ReadSingle();
            var m31 = reader.ReadSingle();
            var m32 = reader.ReadSingle();
            var m33 = reader.ReadSingle();
            var m34 = reader.ReadSingle();
            var m41 = reader.ReadSingle();
            var m42 = reader.ReadSingle();
            var m43 = reader.ReadSingle();
            var m44 = reader.ReadSingle();

            return new Matrix(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        }

        /// <summary>
        /// Deserializes a model from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized model.
        /// </returns>
        private Model DeserializeModel(string name, BinaryReader reader)
        {
            var animations = new AnimationCollection(this.DeserializeAnimations(reader));
            var boneHierarchy = this.DeserializeBoneHierarchy(reader);
            var vertexes = this.DeserializeVertexes(reader);
            var indices = this.DeserializeIndices(reader);

            var meshes = new IModelMesh[]
            {
                new ModelMesh(_modelRenderConfigurations, _renderBatcher, null, vertexes, indices)
            };

            return new Model(name, animations, meshes, boneHierarchy);
        }

        /// <summary>
        /// Deserializes a <see cref="Quaternion"/> from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized quaternion.
        /// </returns>
        private Quaternion DeserializeQuaternion(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Deserializes a dictionary of float-quaternion dictionaries from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The dictionary of float-quaternion dictionaries.
        /// </returns>
        private IDictionary<string, IDictionary<double, Quaternion>> DeserializeStringFloatQuaternionDictionaryDictionary(BinaryReader reader)
        {
            var count = reader.ReadUInt32();
            var result = new Dictionary<string, IDictionary<double, Quaternion>>();

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = this.DeserializeFloatQuaternionDictionary(reader);

                result.Add(key, value);
            }

            return result;
        }

        /// <summary>
        /// Deserializes a dictionary of float-vector3 dictionaries from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The dictionary of float-vector3 dictionaries.
        /// </returns>
        private IDictionary<string, IDictionary<double, Vector3>> DeserializeStringFloatVector3DictionaryDictionary(
            BinaryReader reader)
        {
            var count = reader.ReadUInt32();
            var result = new Dictionary<string, IDictionary<double, Vector3>>();

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = this.DeserializeFloatVector3Dictionary(reader);

                result.Add(key, value);
            }

            return result;
        }

        /// <summary>
        /// Deserializes a <see cref="Vector2"/> from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized vector.
        /// </returns>
        private Vector2 DeserializeVector2(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();

            return new Vector2(x, y);
        }

        /// <summary>
        /// Deserializes a <see cref="Vector3"/> from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized vector.
        /// </returns>
        private Vector3 DeserializeVector3(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Deserializes a <see cref="Vector4"/> from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized vector.
        /// </returns>
        private Vector4 DeserializeVector4(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();

            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Deserializes a list of model vertexes from binary data.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read from.
        /// </param>
        /// <returns>
        /// The deserialized vertexes.
        /// </returns>
        private ModelVertex[] DeserializeVertexes(BinaryReader reader)
        {
            var vertexCount = reader.ReadInt32();
            var vertexes = new List<ModelVertex>();

            for (var i = 0; i < vertexCount; i++)
            {
                var pos = this.DeserializeVector3(reader);
                var normal = this.DeserializeVector3(reader);
                var uv = this.DeserializeVector2(reader);
                var weight = this.DeserializeVector4(reader);
                var index = this.DeserializeByte4(reader);

                vertexes.Add(new ModelVertex(pos, normal, null, null, new Color[0], new Vector2[] { uv }, null, index, weight));
            }

            return vertexes.ToArray();
        }

        /// <summary>
        /// Serializes a collection of animations to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the animations will be serialized.
        /// </param>
        /// <param name="availableAnimations">
        /// The animations to serialize.
        /// </param>
        private void SerializeAnimations(BinaryWriter writer, IAnimationCollection availableAnimations)
        {
            writer.Write(availableAnimations.Count());

            foreach (var animation in availableAnimations)
            {
                writer.Write(animation.Name);
                writer.Write(animation.TicksPerSecond);
                writer.Write(animation.DurationInTicks);
                this.SerializeStringFloatVector3DictionaryDictionary(writer, animation.TranslationKeys);
                this.SerializeStringFloatQuaternionDictionaryDictionary(writer, animation.RotationKeys);
                this.SerializeStringFloatVector3DictionaryDictionary(writer, animation.ScaleKeys);
            }
        }

        /// <summary>
        /// Serializes the bone hierarchy of a model to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the bone hierarchy of a model will be serialized.
        /// </param>
        /// <param name="root">
        /// The bone of the hierarchy to serialize.
        /// </param>
        private void SerializeBoneHierarchy(BinaryWriter writer, IModelBone root)
        {
            writer.Write(root.ID);
            writer.Write(root.Name);

            writer.Write((uint)root.Children.Count);

            foreach (var kv in root.Children)
            {
                writer.Write(kv.Key);
                this.SerializeBoneHierarchy(writer, kv.Value);
            }

            this.SerializeMatrix(writer, root.BoneOffset);
            this.SerializeVector3(writer, root.DefaultTranslation);
            this.SerializeQuaternion(writer, root.DefaultRotation);
            this.SerializeVector3(writer, root.DefaultScale);
        }

        /// <summary>
        /// Serializes a float-quaternion dictionary to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the float-quaternion dictionary will be serialized.
        /// </param>
        /// <param name="dictionary">
        /// The float-quaternion dictionary to serialize.
        /// </param>
        private void SerializeFloatQuaternionDictionary(BinaryWriter writer, IDictionary<double, Quaternion> dictionary)
        {
            writer.Write((uint)dictionary.Count);

            foreach (var kv in dictionary)
            {
                writer.Write(kv.Key);
                this.SerializeQuaternion(writer, kv.Value);
            }
        }

        /// <summary>
        /// Serializes a float-vector3 dictionary to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the float-vector3 dictionary will be serialized.
        /// </param>
        /// <param name="dictionary">
        /// The float-quaternion vector3 to serialize.
        /// </param>
        private void SerializeFloatVector3Dictionary(BinaryWriter writer, IDictionary<double, Vector3> dictionary)
        {
            writer.Write((uint)dictionary.Count);

            foreach (var kv in dictionary)
            {
                writer.Write(kv.Key);
                this.SerializeVector3(writer, kv.Value);
            }
        }

        /// <summary>
        /// Serializes an array of model indices to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the model indices will be serialized.
        /// </param>
        /// <param name="indices">
        /// The model indices to serialize.
        /// </param>
        private void SerializeIndices(BinaryWriter writer, int[] indices)
        {
            writer.Write(indices.Length);

            foreach (var index in indices)
            {
                writer.Write(index);
            }
        }

        /// <summary>
        /// Serializes a <see cref="Matrix"/> to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the <see cref="Matrix"/> will be serialized.
        /// </param>
        /// <param name="matrix">
        /// The matrix to serialize.
        /// </param>
        private void SerializeMatrix(BinaryWriter writer, Matrix matrix)
        {
            writer.Write(matrix.M11);
            writer.Write(matrix.M12);
            writer.Write(matrix.M13);
            writer.Write(matrix.M14);
            writer.Write(matrix.M21);
            writer.Write(matrix.M22);
            writer.Write(matrix.M23);
            writer.Write(matrix.M24);
            writer.Write(matrix.M31);
            writer.Write(matrix.M32);
            writer.Write(matrix.M33);
            writer.Write(matrix.M34);
            writer.Write(matrix.M41);
            writer.Write(matrix.M42);
            writer.Write(matrix.M43);
            writer.Write(matrix.M44);
        }

        /// <summary>
        /// Serializes a model to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the model will be serialized.
        /// </param>
        /// <param name="model">
        /// The model to serialize.
        /// </param>
        private void SerializeModel(BinaryWriter writer, IModel model)
        {
            this.SerializeAnimations(writer, model.AvailableAnimations);
            this.SerializeBoneHierarchy(writer, model.Root);
            this.SerializeVertexes(writer, model.Meshes[0].Vertexes);
            this.SerializeIndices(writer, model.Meshes[0].Indices);
        }

        /// <summary>
        /// Serializes a <see cref="Quaternion"/> to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the <see cref="Quaternion"/> will be serialized.
        /// </param>
        /// <param name="quaternion">
        /// The quaternion to serialize.
        /// </param>
        private void SerializeQuaternion(BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.X);
            writer.Write(quaternion.Y);
            writer.Write(quaternion.Z);
            writer.Write(quaternion.W);
        }

        /// <summary>
        /// Serializes a dictionary of float-quaternion dictionaries to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the dictionary of float-quaternion dictionaries will be serialized.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary of float-quaternion dictionaries to serialize.
        /// </param>
        private void SerializeStringFloatQuaternionDictionaryDictionary(
            BinaryWriter writer, 
            IDictionary<string, IDictionary<double, Quaternion>> dictionary)
        {
            writer.Write((uint)dictionary.Count);

            foreach (var kv in dictionary)
            {
                writer.Write(kv.Key);
                this.SerializeFloatQuaternionDictionary(writer, kv.Value);
            }
        }

        /// <summary>
        /// Serializes a dictionary of float-vector3 dictionaries to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the dictionary of float-vector3 dictionaries will be serialized.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary of float-vector3 dictionaries to serialize.
        /// </param>
        private void SerializeStringFloatVector3DictionaryDictionary(
            BinaryWriter writer, 
            IDictionary<string, IDictionary<double, Vector3>> dictionary)
        {
            writer.Write((uint)dictionary.Count);

            foreach (var kv in dictionary)
            {
                writer.Write(kv.Key);
                this.SerializeFloatVector3Dictionary(writer, kv.Value);
            }
        }

        /// <summary>
        /// Serializes a <see cref="Vector2"/> to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the <see cref="Vector2"/> will be serialized.
        /// </param>
        /// <param name="vector">
        /// The vector to serialize.
        /// </param>
        private void SerializeVector2(BinaryWriter writer, Vector2 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
        }

        /// <summary>
        /// Serializes a <see cref="Vector3"/> to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the <see cref="Vector3"/> will be serialized.
        /// </param>
        /// <param name="vector">
        /// The vector to serialize.
        /// </param>
        private void SerializeVector3(BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }

        /// <summary>
        /// Serializes a <see cref="Vector4"/> to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the <see cref="Vector4"/> will be serialized.
        /// </param>
        /// <param name="vector">
        /// The vector to serialize.
        /// </param>
        private void SerializeVector4(BinaryWriter writer, Vector4 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
            writer.Write(vector.W);
        }

        /// <summary>
        /// Serializes an array of model vertexes to a binary stream.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to which the model vertexes will be serialized.
        /// </param>
        /// <param name="vertexes">
        /// The vertexes to serialize.
        /// </param>
        private void SerializeVertexes(BinaryWriter writer, ModelVertex[] vertexes)
        {
            writer.Write(vertexes.Length);

            for (var i = 0; i < vertexes.Length; i++)
            {
                this.SerializeVector3(writer, vertexes[i].Position ?? Vector3.Zero);
                this.SerializeVector3(writer, vertexes[i].Normal ?? Vector3.Zero);
                this.SerializeVector2(writer, vertexes[i].TexCoordsUV[0]);
                this.SerializeVector4(writer, vertexes[i].BoneWeights ?? Vector4.Zero);
                this.SerializeVector4(writer, (vertexes[i].BoneIndices ?? new Byte4()).ToVector4());
            }
        }
    }
}