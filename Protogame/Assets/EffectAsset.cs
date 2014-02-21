namespace Protogame
{
    using System;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// The effect asset.
    /// </summary>
    public class EffectAsset : MarshalByRefObject, IAsset
    {
        /// <summary>
        /// The m_ asset content manager.
        /// </summary>
        private readonly IAssetContentManager m_AssetContentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectAsset"/> class.
        /// </summary>
        /// <param name="assetContentManager">
        /// The asset content manager.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="code">
        /// The code.
        /// </param>
        /// <param name="platformData">
        /// The platform data.
        /// </param>
        /// <param name="sourcedFromRaw">
        /// The sourced from raw.
        /// </param>
        public EffectAsset(
            IAssetContentManager assetContentManager, 
            string name, 
            string code, 
            PlatformData platformData, 
            bool sourcedFromRaw)
        {
            this.Name = name;
            this.Code = code;
            this.PlatformData = platformData;
            this.m_AssetContentManager = assetContentManager;
            this.SourcedFromRaw = sourcedFromRaw;

            if (this.PlatformData != null)
            {
                try
                {
                    this.ReloadEffect();
                }
                catch (NoAssetContentManagerException)
                {
                }
            }
        }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public string Code { get; set; }

        /// <summary>
        /// Gets a value indicating whether compiled only.
        /// </summary>
        /// <value>
        /// The compiled only.
        /// </value>
        public bool CompiledOnly
        {
            get
            {
                return this.Code == null;
            }
        }

        /// <summary>
        /// Gets the effect.
        /// </summary>
        /// <value>
        /// The effect.
        /// </value>
        public Effect Effect { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the platform data.
        /// </summary>
        /// <value>
        /// The platform data.
        /// </value>
        public PlatformData PlatformData { get; set; }

        /// <summary>
        /// Gets a value indicating whether source only.
        /// </summary>
        /// <value>
        /// The source only.
        /// </value>
        public bool SourceOnly
        {
            get
            {
                return this.PlatformData == null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not this asset was sourced from a raw file (such as a PNG image).
        /// </summary>
        /// <value>
        /// The sourced from raw.
        /// </value>
        public bool SourcedFromRaw { get; private set; }

        /// <summary>
        /// The reload effect.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        /// <exception cref="NoAssetContentManagerException">
        /// </exception>
        public void ReloadEffect()
        {
            if (this.m_AssetContentManager == null)
            {
                throw new NoAssetContentManagerException();
            }

            if (this.m_AssetContentManager != null && this.PlatformData != null)
            {
                // We don't load XNB-based effects because MonoGame doesn't have a reader
                // that supports them, and the XNB format mangles the shader bytecode if we
                // try and parse it out manually.  We also can't just define our own effect reader
                // as that doesn't seem to get picked up by the MonoGame content manager.

                // FIXME: We shouldn't be casting IAssetContentManager like this!
                var assetContentManager = this.m_AssetContentManager as AssetContentManager;
                if (assetContentManager == null)
                {
                    throw new NoAssetContentManagerException();
                }

                var serviceProvider = assetContentManager.ServiceProvider;
                var graphicsDeviceProvider =
                    (IGraphicsDeviceService)serviceProvider.GetService(typeof(IGraphicsDeviceService));
                if (graphicsDeviceProvider != null && graphicsDeviceProvider.GraphicsDevice != null)
                {
                    var graphicsDevice = graphicsDeviceProvider.GraphicsDevice;

                    // Load the effect for the first time.
                    var effect = new Effect(graphicsDevice, this.PlatformData.Data);

                    // Determine what kind of effect class we should use.
                    var hasSeperatedMatrixes = effect.Parameters["World"] != null && effect.Parameters["View"] != null && effect.Parameters["Projection"] != null;
                    var hasMatrix = effect.Parameters["WorldViewProj"] != null || hasSeperatedMatrixes;
                    var hasTexture = effect.Parameters["Texture"] != null;
                    if (hasMatrix && hasTexture)
                    {
                        this.Effect = new EffectWithMatricesAndTexture(graphicsDevice, this.PlatformData.Data, hasSeperatedMatrixes);
                    }
                    else if (hasMatrix)
                    {
                        this.Effect = new EffectWithMatrices(graphicsDevice, this.PlatformData.Data, hasSeperatedMatrixes);
                    }
                    else if (hasTexture)
                    {
                        this.Effect = new EffectWithTexture(graphicsDevice, this.PlatformData.Data);
                    }
                    else
                    {
                        this.Effect = effect;
                    }
                }
            }
        }

        /// <summary>
        /// The resolve.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// The <see cref="T"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public T Resolve<T>() where T : class, IAsset
        {
            if (typeof(T).IsAssignableFrom(typeof(EffectAsset)))
            {
                return this as T;
            }

            throw new InvalidOperationException("Asset already resolved to EffectAsset.");
        }
    }
}