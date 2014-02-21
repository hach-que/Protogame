namespace Protogame
{
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// A 3D world manager implementation that uses deferred lighting.
    /// </summary>
    public class DeferredLighting3DWorldManager : IWorldManager
    {
        /// <summary>
        /// The Protogame console.
        /// </summary>
        private readonly IConsole m_Console;

        /// <summary>
        /// The deferred lighting renderer used by this world manager.
        /// </summary>
        private readonly IDeferredRenderer m_DeferredRenderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredLighting3DWorldManager"/> class.
        /// </summary>
        /// <param name="console">
        /// The console.
        /// </param>
        /// <param name="deferredRendererFactory">
        /// The deferred renderer factory.
        /// </param>
        public DeferredLighting3DWorldManager(
            IConsole console,
            IDeferredRendererFactory deferredRendererFactory)
        {
            this.m_Console = console;
            this.m_DeferredRenderer = deferredRendererFactory.CreateDeferredRenderer();
        }

        /// <summary>
        /// The render.
        /// </summary>
        /// <param name="game">
        /// The game.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        public void Render<T>(T game) where T : Game, ICoreGame
        {
            game.RenderContext.Render(game.GameContext);

#if PLATFORM_WINDOWS
            if (game.RenderContext.GraphicsDevice != null)
            {
                game.RenderContext.GraphicsDevice.Clear(Color.Black);

                game.RenderContext.GraphicsDevice.Viewport = new Viewport(
                    0,
                    0,
                    game.Window.ClientBounds.Width,
                    game.Window.ClientBounds.Height);
            }
#endif

            game.RenderContext.Is3DContext = true;

            this.m_DeferredRenderer.BeginDeferredRendering(game.RenderContext);

            game.GameContext.World.RenderBelow(game.GameContext, game.RenderContext);

            foreach (var entity in game.GameContext.World.Entities)
            {
                entity.Render(game.GameContext, game.RenderContext);
            }

            game.GameContext.World.RenderAbove(game.GameContext, game.RenderContext);

            this.m_DeferredRenderer.EndDeferredRendering(game.RenderContext, false);

            game.RenderContext.Is3DContext = false;

            game.RenderContext.SpriteBatch.Begin();

            foreach (var pass in game.RenderContext.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                game.GameContext.World.RenderBelow(game.GameContext, game.RenderContext);

                foreach (var entity in game.GameContext.World.Entities.OrderBy(x => x.Z))
                {
                    entity.Render(game.GameContext, game.RenderContext);
                }

                game.GameContext.World.RenderAbove(game.GameContext, game.RenderContext);
            }

            this.m_Console.Render(game.GameContext, game.RenderContext);

            game.RenderContext.SpriteBatch.End();

            game.GraphicsDevice.BlendState = BlendState.Opaque;
            game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            game.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            game.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public IDeferredRenderer DeferredRenderer
        {
            get
            {
                return this.m_DeferredRenderer;
            }
        }

        /// <summary>
        /// The update.
        /// </summary>
        /// <param name="game">
        /// The game.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        public void Update<T>(T game) where T : Game, ICoreGame
        {
            game.UpdateContext.Update(game.GameContext);

            foreach (var entity in game.GameContext.World.Entities.ToList())
            {
                entity.Update(game.GameContext, game.UpdateContext);
            }

            game.GameContext.World.Update(game.GameContext, game.UpdateContext);

            this.m_Console.Update(game.GameContext, game.UpdateContext);
        }
    }
}