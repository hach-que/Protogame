namespace Protogame
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    /// <summary>
    /// The default render context implementation.
    /// </summary>
    internal class DefaultRenderContext : IRenderContext
    {
        /// <summary>
        /// The current stack of effect.
        /// </summary>
        private readonly Stack<Effect> m_Effects = new Stack<Effect>();

        /// <summary>
        /// The current stack of render targets.
        /// </summary>
        private readonly Stack<RenderTargetBinding[]> m_RenderTargets = new Stack<RenderTargetBinding[]>();

        /// <summary>
        /// The current bounding frustum.
        /// </summary>
        private BoundingFrustum m_BoundingFrustum;

        /// <summary>
        /// Gets the bounding frustum for the current view and projection matrixes.
        /// </summary>
        /// <value>
        /// The bounding frustum for the current view and projection matrixes.
        /// </value>
        public BoundingFrustum BoundingFrustum
        {
            get
            {
                return this.m_BoundingFrustum;
            }
        }

        /// <summary>
        /// Gets the current active effect for rendering.
        /// </summary>
        /// <value>
        /// The effect.
        /// </value>
        public Effect Effect
        {
            get
            {
                return this.m_Effects.Peek();
            }
        }

        /// <summary>
        /// Gets the associated graphics device.
        /// </summary>
        /// <value>
        /// The graphics device.
        /// </value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the world manager is currently
        /// rendering a 3D context.  For games using a 2D world manager, this will
        /// always be false.  For 3D games, world managers will do an initial 3D pass
        /// followed by a 2D pass that is rendered on top (for UI, etc.) In a 3D
        /// context, only I3DRenderUtilities can be used; in a 2D context, only
        /// I2DRenderUtilities can be used.
        /// </summary>
        /// <value>
        /// Whether the rendering context is currently a 3D context.
        /// </value>
        public bool Is3DContext { get; set; }

        /// <summary>
        /// Gets or sets the projection matrix for 3D rendering.
        /// </summary>
        /// <value>
        /// The projection matrix for 3D rendering.
        /// </value>
        public Matrix Projection
        {
            get
            {
                return this.GetEffectMatrix(x => x.Projection);
            }

            set
            {
                this.SetEffectMatrix(x => x.Projection = value);
                this.RecalculateBoundingFrustum();
            }
        }

        /// <summary>
        /// Gets a texture representing a single white pixel.
        /// </summary>
        /// <value>
        /// The single white pixel.
        /// </value>
        public Texture2D SingleWhitePixel { get; private set; }

        /// <summary>
        /// Gets a sprite batch associated with the current device, upon which 2D rendering is performed.
        /// </summary>
        /// <value>
        /// The sprite batch.
        /// </value>
        public SpriteBatch SpriteBatch { get; private set; }

        /// <summary>
        /// Gets or sets the current camera position.  This is used for deferred rendering for lighting
        /// calculations.  You can ignore this property if you're not using deferred rendering.
        /// </summary>
        /// <value>
        /// The current camera position.  This is used for deferred rendering for lighting
        /// calculations.  You can ignore this property if you're not using deferred rendering.
        /// </value>
        public Vector3 CameraPosition { get; set; }

        /// <summary>
        /// Gets or sets the view matrix for 3D rendering.
        /// </summary>
        /// <value>
        /// The view matrix for 3D rendering.
        /// </value>
        public Matrix View
        {
            get
            {
                return this.GetEffectMatrix(x => x.View);
            }

            set
            {
                this.SetEffectMatrix(x => x.View = value);
                this.RecalculateBoundingFrustum();
            }
        }

        /// <summary>
        /// Gets or sets the world matrix for 3D rendering.
        /// </summary>
        /// <value>
        /// The world matrix for 3D rendering.
        /// </value>
        public Matrix World
        {
            get
            {
                return this.GetEffectMatrix(x => x.World);
            }

            set
            {
                this.SetEffectMatrix(x => x.World = value);
            }
        }

        /// <summary>
        /// The enable textures.
        /// </summary>
        public void EnableTextures()
        {
            var basicEffect = this.m_Effects.Peek() as BasicEffect;

            if (basicEffect != null)
            {
                basicEffect.TextureEnabled = true;
                basicEffect.VertexColorEnabled = false;
            }
        }

        /// <summary>
        /// The enable vertex colors.
        /// </summary>
        public void EnableVertexColors()
        {
            var basicEffect = this.m_Effects.Peek() as BasicEffect;

            if (basicEffect != null)
            {
                basicEffect.VertexColorEnabled = true;
                basicEffect.TextureEnabled = false;
            }
        }

        /// <summary>
        /// Pop an effect from the current rendering context.
        /// </summary>
        public void PopEffect()
        {
            // Prevent the original effect from ever being pushed off the stack.
            if (this.m_Effects.Count > 1)
            {
                var outgoing = this.m_Effects.Pop();

                // Automatically copy the matrices from the outgoing effect to the current
                // effect if applicable.  This reduces confusion when popping an effect
                // from the stack and losing the matrices.
                if (this.m_Effects.Peek() is IEffectMatrices && outgoing is IEffectMatrices)
                {
                    var outgoingMatrices = (IEffectMatrices)outgoing;
                    var newMatrices = (IEffectMatrices)this.m_Effects.Peek();
                    newMatrices.Projection = outgoingMatrices.Projection;
                    newMatrices.View = outgoingMatrices.View;
                    newMatrices.World = outgoingMatrices.World;
                }
            }
        }

        /// <summary>
        /// Pops the current render target from the current rendering context.  If there are no more render targets
        /// in the stack after this call, then the rendering will default back to rendering to the back buffer.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the current render target does not match the top of the stack.  This indicates that there
        /// is other code calling SetRenderTarget, and changing the render target with this method may corrupt
        /// the rendering state.
        /// </exception>
        public void PopRenderTarget()
        {
            if (this.m_RenderTargets.Count == 0)
            {
                return;
            }

            var expectedTargets = this.m_RenderTargets.Peek();
            var currentTargets = this.GraphicsDevice.GetRenderTargets();
            if (currentTargets.Length != expectedTargets.Length)
            {
                throw new InvalidOperationException(
                    "Current render targets do not match last render targets "
                    + "pushed onto the stack with PushRenderTarget.  Ensure there "
                    + "is no code manually calling SetRenderTarget and not restoring "
                    + "it at the end of it's execution.");
            }
            
            for (var i = 0; i < currentTargets.Length; i++)
            {
                var expected = expectedTargets[i];
                var current = currentTargets[i];

                if (current.RenderTarget != expected.RenderTarget)
                {
                    throw new InvalidOperationException(
                        "Current render target does not match last render target "
                        + "pushed onto the stack with PushRenderTarget.  Ensure there "
                        + "is no code manually calling SetRenderTarget and not restoring "
                        + "it at the end of it's execution.");
                }
            }

            this.m_RenderTargets.Pop();

            if (this.m_RenderTargets.Count == 0)
            {
                this.GraphicsDevice.SetRenderTargets(null);
            }
            else
            {
                this.GraphicsDevice.SetRenderTargets(this.m_RenderTargets.Peek());
            }
        }

        /// <summary>
        /// Push an effect onto the current rendering context, making it the active effect used for rendering.
        /// </summary>
        /// <typeparam name="T">
        /// The effect type.
        /// </typeparam>
        /// <param name="effect">
        /// The effect instance.
        /// </param>
        public void PushEffect<T>(T effect) where T : Effect
        {
            // Automatically copy the matrices from the current effect to the new
            // effect if applicable.  This reduces confusion when pushing a new effect
            // onto the stack and not having things render correctly.
            if (this.m_Effects.Peek() is IEffectMatrices && effect is IEffectMatrices)
            {
                var existingMatrices = (IEffectMatrices)this.m_Effects.Peek();
                var newMatrices = (IEffectMatrices)effect;
                newMatrices.Projection = existingMatrices.Projection;
                newMatrices.View = existingMatrices.View;
                newMatrices.World = existingMatrices.World;
            }

            this.m_Effects.Push(effect);
        }

        /// <summary>
        /// Push a render target onto the current rendering context, making it
        /// the active target for rendering.  By using the PushRenderTarget / PopRenderTarget
        /// methods, this allows you to safely chain render target switches, without risk
        /// of losing the previous render target.  An example of where this can be used is
        /// if you want to capture the next frame, you can simply start with a PushRenderTarget
        /// and as long as all other render target switching uses these methods or respects the
        /// previous render target, then everything will be captured as intended.
        /// </summary>
        /// <param name="renderTarget">
        /// The render target instance to make active.
        /// </param>
        public void PushRenderTarget(RenderTargetBinding renderTarget)
        {
            this.m_RenderTargets.Push(new[] { renderTarget });
            this.GraphicsDevice.SetRenderTargets(renderTarget);
        }

        /// <summary>
        /// Push an array of render targets onto the current rendering context, making them
        /// the active target for rendering.  By using the PushRenderTarget / PopRenderTarget
        /// methods, this allows you to safely chain render target switches, without risk
        /// of losing the previous render target.
        /// </summary>
        /// <param name="renderTargets">
        /// The render targets to make active.
        /// </param>
        public void PushRenderTarget(params RenderTargetBinding[] renderTargets)
        {
            this.m_RenderTargets.Push(renderTargets);
            this.GraphicsDevice.SetRenderTargets(renderTargets);
        }

        /// <summary>
        /// The render.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        public void Render(IGameContext context)
        {
            this.GraphicsDevice = context.Graphics.GraphicsDevice;
            if (this.m_Effects.Count == 0)
            {
                this.m_Effects.Push(new BasicEffect(context.Graphics.GraphicsDevice));
            }

            if (this.SpriteBatch == null)
            {
                this.SpriteBatch = new SpriteBatch(context.Graphics.GraphicsDevice);
            }

            if (this.SingleWhitePixel == null)
            {
                this.SingleWhitePixel = new Texture2D(context.Graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                this.SingleWhitePixel.SetData(new[] { Color.White });
            }

            // Update the MouseRay property of the game context.
            var mouseState = Mouse.GetState();
            var mouse = new Vector2(mouseState.X, mouseState.Y);
            var nearSource = new Vector3(mouse, 0f);
            var farSource = new Vector3(mouse, 1f);

            var nearPoint = this.GraphicsDevice.Viewport.Unproject(
                nearSource, 
                this.Projection, 
                this.View, 
                this.World);

            var farPoint = this.GraphicsDevice.Viewport.Unproject(
                farSource, 
                this.Projection, 
                this.View, 
                this.World);

            var direction = farPoint - nearPoint;
            direction.Normalize();

            if (float.IsNaN(direction.X) || float.IsNaN(direction.Y) || float.IsNaN(direction.Z))
            {
                direction = Vector3.Zero;
            }

            context.MouseRay = new Ray(nearPoint, direction);
        }

        /// <summary>
        /// The set active texture.
        /// </summary>
        /// <param name="texture">
        /// The texture.
        /// </param>
        public void SetActiveTexture(Texture2D texture)
        {
            this.GraphicsDevice.Textures[0] = texture;

            var basicEffect = this.m_Effects.Peek() as BasicEffect;
            var textureEffect = this.m_Effects.Peek() as IEffectTexture;

            if (basicEffect != null)
            {
                basicEffect.Texture = texture;
            }

            if (textureEffect != null)
            {
                textureEffect.Texture = texture;
            }
        }

        /// <summary>
        /// Applies a get property operation against the current effect, returning
        /// the identity matrix if the current effect does not implement <see cref="IEffectMatrices"/>.
        /// </summary>
        /// <param name="prop">
        /// The lambda representing the property getter operation to perform.
        /// </param>
        /// <returns>
        /// The <see cref="Matrix"/> if the effect implements <see cref="IEffectMatrices"/>, or <see cref="Matrix.Identity"/>.
        /// </returns>
        private Matrix GetEffectMatrix(Func<IEffectMatrices, Matrix> prop)
        {
            var effectMatrices = this.m_Effects.Peek() as IEffectMatrices;

            if (effectMatrices != null)
            {
                return prop(effectMatrices);
            }

            return Matrix.Identity;
        }

        /// <summary>
        /// The recalculate bounding frustum.
        /// </summary>
        private void RecalculateBoundingFrustum()
        {
            this.m_BoundingFrustum = new BoundingFrustum(this.View * this.Projection);
        }

        /// <summary>
        /// Applies a set property operation against the current effect, performing a
        /// no-op if the current effect does not implement <see cref="IEffectMatrices"/>.
        /// </summary>
        /// <param name="assign">
        /// The lambda representing the property setter operation to perform.
        /// </param>
        private void SetEffectMatrix(Action<IEffectMatrices> assign)
        {
            var effectMatrices = this.m_Effects.Peek() as IEffectMatrices;

            if (effectMatrices != null)
            {
                assign(effectMatrices);
            }
        }
    }
}