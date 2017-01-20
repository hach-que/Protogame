#if PLATFORM_WINDOWS

using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Protogame
{
    /// <summary>
    /// The effect asset compiler.
    /// </summary>
    public class EffectAssetCompiler : IAssetCompiler<EffectAsset>
    {
        public void Compile(EffectAsset asset, TargetPlatform platform)
        {
            if (string.IsNullOrEmpty(asset.Code))
            {
                return;
            }

            var output = new EffectContent();
            output.EffectCode = this.GetEffectPrefixCode() + asset.Code;

            var pathStorage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Protogame", "EffectCompilation", "Standard", asset.Name, platform.ToString());
            Directory.CreateDirectory(pathStorage);

            var tempPath = Path.Combine(pathStorage, "Effect.fx");
            using (var writer = new StreamWriter(tempPath))
            {
                writer.Write(output.EffectCode);
            }

            output.Identity = new ContentIdentity(tempPath);

            var tempDebugOutputPath = Path.Combine(pathStorage, "EffectDebug.bin");
            var tempReleaseOutputPath = Path.Combine(pathStorage, "EffectRelease.bin");

            var debugContent = EffectCompilerHelper.Compile(
                output,
                tempDebugOutputPath,
                platform,
                true,
                string.Empty);
            var releaseContent = EffectCompilerHelper.Compile(
                output,
                tempReleaseOutputPath,
                platform,
                false,
                string.Empty);

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // Magic flag that indicates new compiled effect format.
                    writer.Write((uint)0x12345678);

                    // Version 1 of new effect format.
                    writer.Write((uint)1);

                    var debugCode = debugContent.GetEffectCode();
                    var releaseCode = releaseContent.GetEffectCode();

                    writer.Write(debugCode.Length);
                    writer.Write(debugCode);
                    writer.Write(releaseCode.Length);
                    writer.Write(releaseCode);

                    var p = stream.Position;
                    var b = new byte[p];
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Read(b, 0, b.Length);

                    asset.PlatformData = new PlatformData { Platform = platform, Data = b };
                }
            }

            //File.Delete(tempPath);
            //File.Delete(tempOutputPath);

            try
            {
                asset.ReloadEffect();
            }
            catch (NoAssetContentManagerException)
            {
            }
        }

        /// <summary>
        /// Gets the prefix code for effects compiled under Protogame.
        /// </summary>
        /// <returns>The effect prefix code.</returns>
        private string GetEffectPrefixCode()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("Protogame.Assets.Effect.Macros.fx");

            if (stream == null)
            {
                throw new InvalidOperationException("Prefix code for effects could not be found");
            }

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

#endif