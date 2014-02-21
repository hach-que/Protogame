
#if PLATFORM_WINDOWS || PLATFORM_LINUX

namespace Protogame
{
    /// <summary>
    /// The model asset compiler.
    /// </summary>
    public class ModelAssetCompiler : IAssetCompiler<ModelAsset>
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
        public void Compile(ModelAsset asset, TargetPlatform platform)
        {
            if (asset.RawData == null)
            {
                return;
            }

            var reader = new FbxReader();
            var model = reader.Load(asset.RawData, asset.Extension, asset.RawAdditionalAnimations);
            var serializer = new ModelSerializer();
            var data = serializer.Serialize(model);

            asset.PlatformData = new PlatformData { Data = data, Platform = platform };

            try
            {
                asset.ReloadModel();
            }
            catch (NoAssetContentManagerException)
            {
            }
        }
    }
}

#endif