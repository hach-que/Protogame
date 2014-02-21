using System;
using System.Reflection;
#if PLATFORM_WINDOWS
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Protogame
{
    /// <summary>
    /// The effect asset compiler.
    /// </summary>
    public class EffectAssetCompiler : IAssetCompiler<EffectAsset>
    {
        /// <summary>
        /// The compile.
        /// </summary>
        /// <param name="asset">
        /// The asset.
        /// </param>
        /// <param name="platform">
        /// The platform.
        /// </param>
        public void Compile(EffectAsset asset, TargetPlatform platform)
        {
            if (string.IsNullOrEmpty(asset.Code))
            {
                return;
            }

            var output = new EffectContent();
            output.EffectCode = this.GetEffectPrefixCode() + asset.Code;

            var tempPath = Path.GetTempFileName();
            using (var writer = new StreamWriter(tempPath))
            {
                writer.Write(output.EffectCode);
            }

            output.Identity = new ContentIdentity(tempPath);

            var tempOutputPath = Path.GetTempFileName();

            var processor = new EffectProcessor();
            var context = new DummyContentProcessorContext(TargetPlatformCast.ToMonoGamePlatform(platform));
            context.ActualOutputFilename = tempOutputPath;
            var content = processor.Process(output, context);

            asset.PlatformData = new PlatformData { Platform = platform, Data = content.GetEffectCode() };

            File.Delete(tempPath);
            File.Delete(tempOutputPath);

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
            var stream = assembly.GetManifestResourceStream("Protogame.Assets.Effects.Macros.fx");

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