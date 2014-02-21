namespace Protogame
{
    /// <summary>
    /// The model asset loader.
    /// </summary>
    public class ModelAssetLoader : IAssetLoader
    {
        /// <summary>
        /// The m_ asset content manager.
        /// </summary>
        private IAssetContentManager m_AssetContentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelAssetLoader"/> class.
        /// </summary>
        /// <param name="assetContentManager">
        /// The asset content manager.
        /// </param>
        public ModelAssetLoader(IAssetContentManager assetContentManager)
        {
            this.m_AssetContentManager = assetContentManager;
        }

        /// <summary>
        /// The can handle.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool CanHandle(dynamic data)
        {
            return data.Loader == typeof(ModelAssetLoader).FullName;
        }

        /// <summary>
        /// The can new.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool CanNew()
        {
            return true;
        }

        /// <summary>
        /// The get default.
        /// </summary>
        /// <param name="assetManager">
        /// The asset manager.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="IAsset"/>.
        /// </returns>
        public IAsset GetDefault(IAssetManager assetManager, string name)
        {
            return null;
        }

        /// <summary>
        /// The get new.
        /// </summary>
        /// <param name="assetManager">
        /// The asset manager.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="IAsset"/>.
        /// </returns>
        public IAsset GetNew(IAssetManager assetManager, string name)
        {
            return new ModelAsset(name, null, null, null, false, string.Empty);
        }

        /// <summary>
        /// The handle.
        /// </summary>
        /// <param name="assetManager">
        /// The asset manager.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The <see cref="IAsset"/>.
        /// </returns>
        public IAsset Handle(IAssetManager assetManager, string name, dynamic data)
        {
            if (data is CompiledAsset)
            {
                return new ModelAsset(name, null, null, data.PlatformData, false, string.Empty);
            }

            PlatformData platformData = null;
            if (data.PlatformData != null)
            {
                platformData = new PlatformData { Platform = data.PlatformData.Platform, Data = data.PlatformData.Data };
            }

            var model = new ModelAsset(
                name, 
                ByteReader.ReadAsByteArray(data.RawData), 
                data.RawAdditionalAnimations, 
                platformData, 
                data.SourcedFromRaw != null && (bool)data.SourcedFromRaw,
                data.Extension ?? string.Empty);

            return model;
        }
    }
}