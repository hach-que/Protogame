using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Protogame
{
    using System.Collections.Generic;
    using Assimp;
    using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;

    public class DefaultDeferredRenderer : IDeferredRenderer
    {
        private readonly IQuadRenderer m_QuadRenderer;

        private readonly EffectAsset m_ClearGBufferEffect;

        private readonly EffectAsset m_DirectionalLightEffect;

        private readonly EffectAsset m_CombineFinalEffect;

        private readonly EffectAsset m_PointLightEffect;

        private readonly EffectAsset m_RenderGBufferEffect;

        private readonly ModelAsset m_SphereModel;

        private RenderTarget2D m_ColorRenderTarget;

        private RenderTarget2D m_NormalRenderTarget;

        private RenderTarget2D m_DepthRenderTarget;

        private RenderTarget2D m_LightRenderTarget;

        private SpriteBatch m_SpriteBatch;

        private List<Action<IRenderContext>> m_PendingLights; 

        public DefaultDeferredRenderer(
            IAssetManagerProvider assetManagerProvider,
            IQuadRenderer quadRenderer)
        {
            var assetManager = assetManagerProvider.GetAssetManager();

            this.m_ClearGBufferEffect = assetManager.Get<EffectAsset>("effect.Protogame.DeferredLighting.ClearGBuffer");
            this.m_DirectionalLightEffect = assetManager.Get<EffectAsset>("effect.Protogame.DeferredLighting.DirectionalLight");
            this.m_CombineFinalEffect = assetManager.Get<EffectAsset>("effect.Protogame.DeferredLighting.CombineFinal");
            this.m_PointLightEffect = assetManager.Get<EffectAsset>("effect.Protogame.DeferredLighting.PointLight");
            this.m_RenderGBufferEffect = assetManager.Get<EffectAsset>("effect.Protogame.DeferredLighting.RenderGBuffer");
            this.m_SphereModel = assetManager.Get<ModelAsset>("effect.Protogame.DeferredLighting.Sphere");

            this.m_QuadRenderer = quadRenderer;

            this.m_PendingLights = new List<Action<IRenderContext>>();
        }

        /// <summary>
        /// Determines whether the render target size matches the size of the current back buffer.
        /// </summary>
        /// <returns><c>true</c>, if the render target size matches the size of the back buffer, <c>false</c> otherwise.</returns>
        /// <param name="renderTarget">The render target to check.</param>
        /// <param name="graphicsDevice">The current graphics device.</param>
        private bool RenderTargetMatchesBackBuffer(RenderTarget2D renderTarget, GraphicsDevice graphicsDevice)
        {
            return renderTarget.Width == graphicsDevice.PresentationParameters.BackBufferWidth &&
                renderTarget.Height == graphicsDevice.PresentationParameters.BackBufferHeight;
        }

        /// <summary>
        /// Ensures the render targets for the G-buffer are initialized correctly and match the
        /// current back buffer width and height.
        /// </summary>
        /// <param name="renderContext">The current render context.</param>
        private void InitGBuffer(IRenderContext renderContext)
        {
            if (this.m_ColorRenderTarget == null || !this.RenderTargetMatchesBackBuffer(this.m_ColorRenderTarget, renderContext.GraphicsDevice))
            {
                if (this.m_ColorRenderTarget != null)
                {
                    this.m_ColorRenderTarget.Dispose();
                }

                this.m_ColorRenderTarget = new RenderTarget2D(
                    renderContext.GraphicsDevice, 
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.Depth24);
            }

            if (this.m_NormalRenderTarget == null || !this.RenderTargetMatchesBackBuffer(this.m_NormalRenderTarget, renderContext.GraphicsDevice))
            {
                if (this.m_NormalRenderTarget != null)
                {
                    this.m_NormalRenderTarget.Dispose();
                }

                this.m_NormalRenderTarget = new RenderTarget2D(
                    renderContext.GraphicsDevice, 
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None);
            }

            if (this.m_DepthRenderTarget == null || !this.RenderTargetMatchesBackBuffer(this.m_DepthRenderTarget, renderContext.GraphicsDevice))
            {
                if (this.m_DepthRenderTarget != null)
                {
                    this.m_DepthRenderTarget.Dispose();
                }

                this.m_DepthRenderTarget = new RenderTarget2D(
                    renderContext.GraphicsDevice, 
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight,
                    false,
                    SurfaceFormat.Single,
                    DepthFormat.None);
            }

            if (this.m_LightRenderTarget == null || !this.RenderTargetMatchesBackBuffer(this.m_LightRenderTarget, renderContext.GraphicsDevice))
            {
                if (this.m_LightRenderTarget != null)
                {
                    this.m_LightRenderTarget.Dispose();
                }

                this.m_LightRenderTarget = new RenderTarget2D(
                    renderContext.GraphicsDevice, 
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                    renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None);
            }

            if (this.m_SpriteBatch == null)
            {
                this.m_SpriteBatch = new SpriteBatch(renderContext.GraphicsDevice);
            }
        }

        /// <summary>
        /// Sets the current render targets to point to the G-buffer.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        private void SetGBuffer(IRenderContext renderContext)
        {
            renderContext.PushRenderTarget(
                this.m_ColorRenderTarget,
                this.m_NormalRenderTarget,
                this.m_DepthRenderTarget);
        }

        /// <summary>
        /// Resolves the G-buffer by resetting the render targets to the back buffer.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        private void ResolveGBuffer(IRenderContext renderContext)
        {
            renderContext.PopRenderTarget();
        }

        /// <summary>
        /// Clears the G-buffer by setting the default values into each render target.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        private void ClearGBuffer(IRenderContext renderContext)
        {
            foreach (var pass in this.m_ClearGBufferEffect.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                this.m_QuadRenderer.Render(renderContext, Vector2.One * -1, Vector2.One);
            }
        }

        private void DrawDirectionalLight(IRenderContext renderContext, Vector3 lightDirection, Color color)
        {
            var halfPixel = new Vector2
            {
                X = 0.5f / renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Y = 0.5f / renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight
            };

            var effect = this.m_DirectionalLightEffect.Effect;

            effect.Parameters["ColorMap"].SetValue(this.m_ColorRenderTarget);
            effect.Parameters["NormalMap"].SetValue(this.m_NormalRenderTarget);
            effect.Parameters["DepthMap"].SetValue(this.m_DepthRenderTarget);

            effect.Parameters["LightDirection"].SetValue(lightDirection);
            effect.Parameters["Color"].SetValue(color.ToVector3());

            effect.Parameters["CameraPosition"].SetValue(renderContext.CameraPosition);
            effect.Parameters["InvertViewProjection"].SetValue(
                Matrix.Invert(
                renderContext.View * renderContext.Projection));

            effect.Parameters["HalfPixel"].SetValue(halfPixel);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                this.m_QuadRenderer.Render(renderContext, Vector2.One * -1, Vector2.One);
            }
        }

        private void DrawPointLight(
            IRenderContext renderContext,
            Vector3 lightPosition,
            Color color,
            float lightRadius,
            float lightIntensity)
        {
            var halfPixel = new Vector2
            {
                X = 0.5f / renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Y = 0.5f / renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight
            };

            var effect = this.m_PointLightEffect.Effect;

            effect.Parameters["ColorMap"].SetValue(this.m_ColorRenderTarget);
            effect.Parameters["NormalMap"].SetValue(this.m_NormalRenderTarget);
            effect.Parameters["DepthMap"].SetValue(this.m_DepthRenderTarget);

            var sphereWorldMatrix = Matrix.CreateScale(lightRadius) * Matrix.CreateTranslation(lightPosition);
            effect.Parameters["World"].SetValue(sphereWorldMatrix);
            effect.Parameters["View"].SetValue(renderContext.View);
            effect.Parameters["Projection"].SetValue(renderContext.Projection);

            effect.Parameters["LightPosition"].SetValue(lightPosition);

            effect.Parameters["Color"].SetValue(color.ToVector3());
            effect.Parameters["LightRadius"].SetValue(lightRadius);
            effect.Parameters["LightIntensity"].SetValue(lightIntensity);

            effect.Parameters["CameraPosition"].SetValue(renderContext.CameraPosition);
            effect.Parameters["InvertViewProjection"].SetValue(
                Matrix.Invert(
                renderContext.View * renderContext.Projection));

            effect.Parameters["HalfPixel"].SetValue(halfPixel);

            var cameraToCentre = Vector3.Distance(renderContext.CameraPosition, lightPosition);
            renderContext.GraphicsDevice.RasterizerState = 
                cameraToCentre < lightRadius ? 
                RasterizerState.CullClockwise : 
                RasterizerState.CullCounterClockwise;
            renderContext.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                var animation = this.m_SphereModel.AvailableAnimations[Animation.AnimationNullName];
                var frame = animation.Frames[0];

                frame.LoadBuffers(renderContext.GraphicsDevice);

                renderContext.GraphicsDevice.Indices = frame.IndexBuffer;
                renderContext.GraphicsDevice.SetVertexBuffer(frame.VertexBuffer);

                renderContext.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    frame.VertexBuffer.VertexCount,
                    0,
                    frame.IndexBuffer.IndexCount / 3);
            }

            renderContext.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            renderContext.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        private void RenderLights(IRenderContext renderContext)
        {
            renderContext.PushRenderTarget(this.m_LightRenderTarget);
            renderContext.GraphicsDevice.Clear(Color.Transparent);
            renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            renderContext.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            foreach (var action in this.m_PendingLights)
            {
                action(renderContext);
            }

            this.m_PendingLights.Clear();

            renderContext.GraphicsDevice.BlendState = BlendState.Opaque;
            renderContext.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            renderContext.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            renderContext.PopRenderTarget();
        }

        private void RenderDebug(IRenderContext renderContext)
        {
            int halfWidth = renderContext.GraphicsDevice.Viewport.Width / 2;
            int halfHeight = renderContext.GraphicsDevice.Viewport.Height / 2;

            this.m_SpriteBatch.Begin();
            this.m_SpriteBatch.Draw(
                this.m_ColorRenderTarget,
                new Rectangle(0, 0, halfWidth, halfHeight),
                Color.White);
            this.m_SpriteBatch.Draw(
                this.m_NormalRenderTarget,
                new Rectangle(0, halfHeight, halfWidth, halfHeight),
                Color.White);
            this.m_SpriteBatch.Draw(
                this.m_DepthRenderTarget,
                new Rectangle(halfWidth, 0, halfWidth, halfHeight),
                Color.White);
            this.m_SpriteBatch.Draw(
                this.m_LightRenderTarget,
                new Rectangle(halfWidth, halfHeight, halfWidth, halfHeight),
                Color.White);
            this.m_SpriteBatch.End();
        }

        public void AddDirectionalLight(Vector3 lightDirection, Color color)
        {
            this.m_PendingLights.Add(x => this.DrawDirectionalLight(x, lightDirection, color));
        }

        public void AddPointLight(Vector3 lightPosition,
            Color color,
            float lightRadius,
            float lightIntensity)
        {
            this.m_PendingLights.Add(x => this.DrawPointLight(x, lightPosition, color, lightRadius, lightIntensity));
        }

        public void BeginDeferredRendering(IRenderContext renderContext)
        {
            this.InitGBuffer(renderContext);
            this.SetGBuffer(renderContext);
            this.ClearGBuffer(renderContext);

            // This render target change shouldn't be necessary, but MonoGame
            // won't render to the targets correctly without it.
            this.ResolveGBuffer(renderContext);
            this.SetGBuffer(renderContext);

            renderContext.PushEffect(this.m_RenderGBufferEffect.Effect);
        }

        public void EndDeferredRendering(IRenderContext renderContext, bool debug)
        {
            renderContext.PopEffect();

            this.ResolveGBuffer(renderContext);
            this.RenderLights(renderContext);

            if (debug)
            {
                this.RenderDebug(renderContext);
            }
            else
            {
                var halfPixel = new Vector2
                {
                    X = 0.5f / renderContext.GraphicsDevice.PresentationParameters.BackBufferWidth,
                    Y = 0.5f / renderContext.GraphicsDevice.PresentationParameters.BackBufferHeight
                };

                var effect = this.m_CombineFinalEffect.Effect;

                effect.Parameters["ColorMap"].SetValue(this.m_ColorRenderTarget);
                effect.Parameters["LightMap"].SetValue(this.m_LightRenderTarget);
                effect.Parameters["HalfPixel"].SetValue(halfPixel);

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    this.m_QuadRenderer.Render(renderContext, Vector2.One * -1, Vector2.One);
                }
            }
        }
    }
}

