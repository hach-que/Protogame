namespace Protogame
{
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Ninject;
    using Ninject.Parameters;

    /// <summary>
    /// The core Protogame game implementation.  You should derive your Game instance from this class.
    /// </summary>
    /// <typeparam name="TInitialWorld">
    /// The initial world class to start the game with.
    /// </typeparam>
    /// <typeparam name="TWorldManager">
    /// The world manager class for this game.
    /// </typeparam>
    public abstract class CoreGame<TInitialWorld, TWorldManager> : Game, ICoreGame
        where TInitialWorld : IWorld where TWorldManager : IWorldManager
    {
        /// <summary>
        /// The dependency injection kernel.
        /// </summary>
        private readonly IKernel m_Kernel;

        /// <summary>
        /// The total number of frames that have elapsed since the last second interval.
        /// </summary>
        private int m_TotalFrames;

        /// <summary>
        /// The total amount of time that has elapsed since the last second interval.
        /// </summary>
        private float m_ElapsedTime;

        /// <summary>
        /// The graphics device manager instance.
        /// </summary>
        private readonly GraphicsDeviceManager m_GraphicsDeviceManager;

        /// <summary>
        /// The current profiler instance.
        /// </summary>
        private readonly IProfiler m_Profiler;

        /// <summary>
        /// An array of the engine hooks that are loaded.
        /// </summary>
        private IEngineHook[] m_EngineHooks;

        /// <summary>
        /// Gets the current game context.  You should not generally access this property; outside
        /// an explicit Update or Render loop, the state of the game context is not guaranteed.  Inside
        /// the context of an Update or Render loop, the game context is already provided.
        /// </summary>
        /// <value>
        /// The current game context.
        /// </value>
        public IGameContext GameContext { get; private set; }

        /// <summary>
        /// Gets the current update context.  You should not generally access this property; outside
        /// an explicit Update loop, the state of the update context is not guaranteed.  Inside
        /// the context of an Update loop, the update context is already provided.
        /// </summary>
        /// <value>
        /// The current update context.
        /// </value>
        public IUpdateContext UpdateContext { get; private set; }

        /// <summary>
        /// Gets the current render context.  You should not generally access this property; outside
        /// an explicit Render loop, the state of the render context is not guaranteed.  Inside
        /// the context of an Render loop, the render context is already provided.
        /// </summary>
        /// <value>
        /// The current update context.
        /// </value>
        public IRenderContext RenderContext { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreGame{TInitialWorld,TWorldManager}"/> class. 
        /// Initializes a new instance of the <see cref="Protogame.CoreGame&lt;&gt;"/> class.
        /// </summary>
        /// <param name="kernel">
        /// The dependency injection kernel.
        /// </param>
        public CoreGame(IKernel kernel)
        {
            this.m_Kernel = kernel;
            this.m_GraphicsDeviceManager = new GraphicsDeviceManager(this);
            this.m_GraphicsDeviceManager.PreparingDeviceSettings +=
                (sender, e) =>
                {
                    e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                        RenderTargetUsage.PreserveContents;
                };
            this.m_Profiler = kernel.TryGet<IProfiler>();
            if (this.m_Profiler == null)
            {
                kernel.Bind<IProfiler>().To<NullProfiler>();
                this.m_Profiler = kernel.Get<IProfiler>();
            }

            // TODO: Fix this because it means we can't have more than one game using the same IoC container.
            var assetContentManager = new AssetContentManager(this.Services);
            this.Content = assetContentManager;
            kernel.Bind<IAssetContentManager>().ToMethod(x => assetContentManager);
        }

        /// <summary>
        /// A platform independent representation of a game window.
        /// </summary>
        /// <value>
        /// The game window.
        /// </value>
        public new IGameWindow Window { get; private set; }

        /// <summary>
        /// The load content.
        /// </summary>
        protected override void LoadContent()
        {
            // The interception library can't properly intercept class types, which
            // means we can't simply do this.m_Kernel.Get<TInitialWorld>() because
            // none of the calls will be intercepted.  Instead, we need to bind the
            // IWorld and IWorldManager to their initial types and then unbind them
            // after they've been constructed.
            this.m_Kernel.Bind<IWorld>().To<TInitialWorld>();
            this.m_Kernel.Bind<IWorldManager>().To<TWorldManager>();
            var world = this.m_Kernel.Get<IWorld>();
            var worldManager = this.m_Kernel.Get<IWorldManager>();
            this.m_Kernel.Unbind<IWorld>();
            this.m_Kernel.Unbind<IWorldManager>();

            // Construct a platform-independent game window.
            this.Window = this.ConstructGameWindow();

#if PLATFORM_WINDOWS
            // Register for the window resize event so we can scale
            // the window correctly.
            var shouldHandleResize = true;
            base.Window.ClientSizeChanged += (sender, e) =>
            {
                if (!shouldHandleResize)
                {
                    return;
                }

                shouldHandleResize = false;
                var width = base.Window.ClientBounds.Width;
                var height = base.Window.ClientBounds.Height;
                this.GameContext.Graphics.PreferredBackBufferWidth = width;
                this.GameContext.Graphics.PreferredBackBufferHeight = height;
                this.GameContext.Graphics.ApplyChanges();
                shouldHandleResize = true;
            };
#endif

            // Create the game context.
            this.GameContext = this.m_Kernel.Get<IGameContext>(
                new ConstructorArgument("game", this), 
                new ConstructorArgument("graphics", this.m_GraphicsDeviceManager), 
                new ConstructorArgument("world", world), 
                new ConstructorArgument("worldManager", worldManager), 
                new ConstructorArgument("window", this.ConstructGameWindow()));

            // Create the update and render contexts.
            this.UpdateContext = this.m_Kernel.Get<IUpdateContext>();
            this.RenderContext = this.m_Kernel.Get<IRenderContext>();

            // Retrieve all engine hooks.  These can be set up by additional modules
            // to change runtime behaviour.
            this.m_EngineHooks = this.m_Kernel.GetAll<IEngineHook>().ToArray();

            // Set up defaults.
            this.Window.Title = "Protogame!";
        }

        /// <summary>
        /// The update.
        /// </summary>
        /// <param name="gameTime">
        /// The game time.
        /// </param>
        protected override void Update(GameTime gameTime)
        {
            using (this.m_Profiler.Measure("update", this.GameContext.FrameCount.ToString()))
            {
                // Measure FPS.
                using (this.m_Profiler.Measure("measure_fps"))
                {
                    this.GameContext.FrameCount += 1;
                    this.m_ElapsedTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (this.m_ElapsedTime >= 1000f)
                    {
                        this.GameContext.FPS = this.m_TotalFrames;
                        this.m_TotalFrames = 0;
                        this.m_ElapsedTime = 0;
                    }
                }

                // If this is before the 60th frame, skip so that MonoGame can initialize properly.
                if (this.GameContext.FrameCount < 60)
                {
                    return;
                }

                // Update the game.
                using (this.m_Profiler.Measure("main"))
                {
                    this.GameContext.GameTime = gameTime;
                    foreach (var hook in this.m_EngineHooks)
                    {
                        hook.Update(this.GameContext, this.UpdateContext);
                    }

                    this.GameContext.WorldManager.Update(this);
                }

                base.Update(gameTime);
            }
        }

        /// <summary>
        /// The draw.
        /// </summary>
        /// <param name="gameTime">
        /// The game time.
        /// </param>
        protected override void Draw(GameTime gameTime)
        {
            using (this.m_Profiler.Measure("render", (this.GameContext.FrameCount - 1).ToString()))
            {
                this.m_TotalFrames++;

                // If this is before the 60th frame, skip so that MonoGame can initialize properly.
                if (this.GameContext.FrameCount < 60)
                {
                    this.GraphicsDevice.Clear(Color.Black);
                    return;
                }

                // Render the game.
                using (this.m_Profiler.Measure("main"))
                {
                    this.GameContext.GameTime = gameTime;
                    foreach (var hook in this.m_EngineHooks)
                    {
                        hook.Render(this.GameContext, this.RenderContext);
                    }

                    this.GameContext.WorldManager.Render(this);
                }

                base.Draw(gameTime);
            }
        }

        /// <summary>
        /// Constructs an implementation of <see cref="IGameWindow"/> based on the current game.  This method
        /// abstracts the current platform.
        /// </summary>
        /// <returns>
        /// The game window instance.
        /// </returns>
        private IGameWindow ConstructGameWindow()
        {
#if PLATFORM_WINDOWS || PLATFORM_MACOS || PLATFORM_LINUX
            return new DefaultGameWindow(base.Window);
#elif PLATFORM_ANDROID || PLATFORM_OUYA
            return new AndroidGameWindow(base.Window);
#endif
        }

#if PLATFORM_ANDROID || PLATFORM_OUYA
        public Microsoft.Xna.Framework.AndroidGameWindow AndroidGameWindow
        {
            get
            {
                return base.Window;
            }
        }
#endif
    }
}