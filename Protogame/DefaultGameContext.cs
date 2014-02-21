namespace Protogame
{
    using System;
    using Microsoft.Xna.Framework;
    using Ninject;

    /// <summary>
    /// The default game context.
    /// </summary>
    internal class DefaultGameContext : IGameContext
    {
        /// <summary>
        /// The m_ kernel.
        /// </summary>
        private readonly IKernel m_Kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultGameContext"/> class.
        /// </summary>
        /// <param name="kernel">
        /// The kernel.
        /// </param>
        /// <param name="game">
        /// The game.
        /// </param>
        /// <param name="graphics">
        /// The graphics.
        /// </param>
        /// <param name="window">
        /// The window.
        /// </param>
        /// <param name="world">
        /// The world.
        /// </param>
        /// <param name="worldManager">
        /// The world manager.
        /// </param>
        public DefaultGameContext(
            IKernel kernel, 
            Game game, 
            GraphicsDeviceManager graphics, 
            IGameWindow window, 
            IWorld world, 
            IWorldManager worldManager)
        {
            this.m_Kernel = kernel;
            this.Game = game;
            this.Graphics = graphics;
            this.World = world;
            this.WorldManager = worldManager;
            this.Window = window;
        }

        /// <summary>
        /// Gets or sets the fps.
        /// </summary>
        /// <value>
        /// The fps.
        /// </value>
        public int FPS { get; set; }

        /// <summary>
        /// Gets or sets the frame count.
        /// </summary>
        /// <value>
        /// The frame count.
        /// </value>
        public int FrameCount { get; set; }

        /// <summary>
        /// Gets the game.
        /// </summary>
        /// <value>
        /// The game.
        /// </value>
        public Game Game { get; internal set; }

        /// <summary>
        /// Gets or sets the game time.
        /// </summary>
        /// <value>
        /// The game time.
        /// </value>
        public GameTime GameTime { get; set; }

        /// <summary>
        /// Gets or sets the graphics.
        /// </summary>
        /// <value>
        /// The graphics.
        /// </value>
        public GraphicsDeviceManager Graphics { get; set; }

        /// <summary>
        /// Gets or sets the window.
        /// </summary>
        /// <value>
        /// The window.
        /// </value>
        public IGameWindow Window { get; set; }

        /// <summary>
        /// Gets or sets the world.
        /// </summary>
        /// <value>
        /// The world.
        /// </value>
        public IWorld World { get; set; }

        /// <summary>
        /// Gets the world manager.
        /// </summary>
        /// <value>
        /// The world manager.
        /// </value>
        public IWorldManager WorldManager { get; internal set; }

        /// <summary>
        /// Gets or sets the ray representing the mouse cursor in 3D space.  This is
        /// updated automatically by DefaultRenderContext based on the World, View and Projection
        /// properties of the current render context.
        /// </summary>
        /// <value>The ray representing the mouse cursor in 3D space.</value>
        public Ray MouseRay { get; set; }

        /// <summary>
        /// The create world.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// The <see cref="IWorld"/>.
        /// </returns>
        public IWorld CreateWorld<T>() where T : IWorld
        {
            return this.m_Kernel.Get<T>();
        }

        /// <summary>
        /// The create world.
        /// </summary>
        /// <param name="creator">
        /// The creator.
        /// </param>
        /// <typeparam name="TFactory">
        /// </typeparam>
        /// <returns>
        /// The <see cref="IWorld"/>.
        /// </returns>
        public IWorld CreateWorld<TFactory>(Func<TFactory, IWorld> creator)
        {
            return creator(this.m_Kernel.Get<TFactory>());
        }

        /// <summary>
        /// The resize window.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        public void ResizeWindow(int width, int height)
        {
            if (this.Window.ClientBounds.Width == width && this.Window.ClientBounds.Height == height)
            {
                return;
            }

            this.Graphics.PreferredBackBufferWidth = width;
            this.Graphics.PreferredBackBufferHeight = height;
            this.Graphics.ApplyChanges();
        }

        /// <summary>
        /// The switch world.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        public void SwitchWorld<T>() where T : IWorld
        {
            if (this.World != null)
            {
                this.World.Dispose();
            }

            this.World = this.CreateWorld<T>();
        }

        /// <summary>
        /// The switch world.
        /// </summary>
        /// <param name="creator">
        /// The creator.
        /// </param>
        /// <typeparam name="TFactory">
        /// </typeparam>
        public void SwitchWorld<TFactory>(Func<TFactory, IWorld> creator)
        {
            if (this.World != null)
            {
                this.World.Dispose();
            }

            this.World = this.CreateWorld(creator);
        }

        /// <summary>
        /// The switch world.
        /// </summary>
        /// <param name="world">
        /// The world.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        public void SwitchWorld<T>(T world) where T : IWorld
        {
            if (this.World != null)
            {
                this.World.Dispose();
            }

            this.World = world;
        }
    }
}