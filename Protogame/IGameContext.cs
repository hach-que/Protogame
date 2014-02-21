namespace Protogame
{
    using System;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// The GameContext interface.
    /// </summary>
    public interface IGameContext
    {
        /// <summary>
        /// Gets or sets the fps.
        /// </summary>
        /// <value>
        /// The fps.
        /// </value>
        int FPS { get; set; }

        /// <summary>
        /// Gets or sets the frame count.
        /// </summary>
        /// <value>
        /// The frame count.
        /// </value>
        int FrameCount { get; set; }

        /// <summary>
        /// Gets the game.
        /// </summary>
        /// <value>
        /// The game.
        /// </value>
        Game Game { get; }

        /// <summary>
        /// Gets or sets the game time.
        /// </summary>
        /// <value>
        /// The game time.
        /// </value>
        GameTime GameTime { get; set; }

        /// <summary>
        /// Gets the graphics.
        /// </summary>
        /// <value>
        /// The graphics.
        /// </value>
        GraphicsDeviceManager Graphics { get; }

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>
        /// The window.
        /// </value>
        IGameWindow Window { get; }

        /// <summary>
        /// Gets the world.
        /// </summary>
        /// <value>
        /// The world.
        /// </value>
        IWorld World { get; }

        /// <summary>
        /// Gets the world manager.
        /// </summary>
        /// <value>
        /// The world manager.
        /// </value>
        IWorldManager WorldManager { get; }

        /// <summary>
        /// Gets or sets the ray representing the mouse cursor in 3D space.  This is
        /// updated automatically by DefaultRenderContext based on the World, View and Projection
        /// properties of the current render context.
        /// </summary>
        /// <value>The ray representing the mouse cursor in 3D space.</value>
        Ray MouseRay { get; set; }

        /// <summary>
        /// The create world.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// The <see cref="IWorld"/>.
        /// </returns>
        IWorld CreateWorld<T>() where T : IWorld;

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
        IWorld CreateWorld<TFactory>(Func<TFactory, IWorld> creator);

        /// <summary>
        /// The resize window.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        void ResizeWindow(int width, int height);

        /// <summary>
        /// The switch world.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        void SwitchWorld<T>() where T : IWorld;

        /// <summary>
        /// The switch world.
        /// </summary>
        /// <param name="creator">
        /// The creator.
        /// </param>
        /// <typeparam name="TFactory">
        /// </typeparam>
        void SwitchWorld<TFactory>(Func<TFactory, IWorld> creator);

        /// <summary>
        /// The switch world.
        /// </summary>
        /// <param name="world">
        /// The world.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        void SwitchWorld<T>(T world) where T : IWorld;
    }
}