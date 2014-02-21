namespace Protogame
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// The raw model load strategy.
    /// </summary>
    public class RawModelLoadStrategy : ILoadStrategy
    {
        /// <summary>
        /// Gets the asset extensions.
        /// </summary>
        /// <value>
        /// The asset extensions.
        /// </value>
        public string[] AssetExtensions
        {
            get
            {
                return new[] { "fbx", "x" };
            }
        }

        /// <summary>
        /// Gets a value indicating whether scan source path.
        /// </summary>
        /// <value>
        /// The scan source path.
        /// </value>
        public bool ScanSourcePath
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The attempt load.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object AttemptLoad(string path, string name, ref DateTime? lastModified)
        {
            var file = new FileInfo(Path.Combine(path, name.Replace('.', Path.DirectorySeparatorChar) + ".fbx"));

            if (file.Exists)
            {
                lastModified = file.LastWriteTime;

                // If there are name-anim.fbx files, read those in as additional animations.
                var directory = file.Directory;
                var otherAnimations = new Dictionary<string, byte[]>();
                if (directory != null)
                {
                    var lastComponent = name.Split('.').Last();
                    var otherAnimationFiles = directory.GetFiles(lastComponent + "-*.fbx");
                    otherAnimations =
                        otherAnimationFiles.ToDictionary(
                            key => key.Name.Split('-').Last().Split('.').First(), 
                            value => this.ReadModelData(value.FullName));
                }

                return
                    new
                    {
                        Loader = typeof(ModelAssetLoader).FullName, 
                        PlatformData = (PlatformData)null, 
                        RawData = this.ReadModelData(file.FullName), 
                        RawAdditionalAnimations = otherAnimations, 
                        SourcedFromRaw = true,
                        Extension = "fbx"
                    };
            }

            file = new FileInfo(Path.Combine(path, name.Replace('.', Path.DirectorySeparatorChar) + ".x"));

            if (file.Exists)
            {
                lastModified = file.LastWriteTime;

                return
                    new
                    {
                        Loader = typeof(ModelAssetLoader).FullName,
                        PlatformData = (PlatformData)null,
                        RawData = this.ReadModelData(file.FullName),
                        RawAdditionalAnimations = new Dictionary<string, byte[]>(),
                        SourcedFromRaw = true,
                        Extension = "x"
                    };
            }

            return null;
        }

        /// <summary>
        /// The read model data.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The <see cref="byte[]"/>.
        /// </returns>
        private byte[] ReadModelData(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var binary = new BinaryReader(fileStream))
                {
                    return binary.ReadBytes((int)fileStream.Length);
                }
            }
        }
    }
}