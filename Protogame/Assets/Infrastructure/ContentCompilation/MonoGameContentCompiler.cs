#if PLATFORM_WINDOWS || PLATFORM_MACOS || PLATFORM_LINUX

using System;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using MonoGamePlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;

namespace Protogame
{
    /// <summary>
    /// An abstract class that provides common functions for asset compilers that
    /// compile via the MonoGame content pipeline.
    /// </summary>
    /// <remarks>
    /// The base abstract class for asset compilers that pass their content through
    /// the MonoGame content pipeline.  This provides a general purpose mechanism
    /// to call upon the MonoGame content pipeline to compile the input object
    /// and return the resulting XNB file.
    /// </remarks>
    public abstract class MonoGameContentCompiler
    {
        /// <summary>
        /// Compile the specified input object and return the XNB file as a byte array.
        /// </summary>
        /// <param name="content">
        /// The content to compile.
        /// </param>
        /// <returns>
        /// The compiled XNB file as a byte array.
        /// </returns>
        protected byte[] CompileAndGetBytes(object content)
        {
            var pathStorage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Protogame", "ContentCompilation", new Guid().ToString());
            Directory.CreateDirectory(pathStorage);

            var temp = Path.Combine(pathStorage, "Input.txt");
            var compiler = new ContentCompiler();
            try
            {
                using (var stream = new FileStream(temp, FileMode.Open, FileAccess.Write))
                {
                    compiler.Compile(
                        stream, 
                        content, 
                        MonoGamePlatform.Windows, 
                        GraphicsProfile.Reach, 
                        false,
                        pathStorage,
                        pathStorage);
                }

                byte[] result;
                using (var stream = new FileStream(temp, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = 0;
                    using (var reader = new BinaryReader(stream))
                    {
                        result = reader.ReadBytes((int)stream.Length);
                    }
                }

                //File.Delete(temp);
                return result;
            }
            finally
            {
                //File.Delete(temp);
            }
        }
    }
}

#endif