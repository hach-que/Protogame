using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Protogame
{
    public class DefaultQuadRenderer : IQuadRenderer
    {
        private VertexPositionTexture[] m_VertexCache;

        private short[] m_IndexCache;

        public void Render(IRenderContext renderContext, Vector2 v1, Vector2 v2)
        {
            if (this.m_VertexCache == null || this.m_IndexCache == null)
            {
                this.CalculateCache();
            }

            this.m_VertexCache[0].Position.X = v2.X;
            this.m_VertexCache[0].Position.Y = v1.Y;

            this.m_VertexCache[1].Position.X = v1.X;
            this.m_VertexCache[1].Position.Y = v1.Y;

            this.m_VertexCache[2].Position.X = v1.X;
            this.m_VertexCache[2].Position.Y = v2.Y;

            this.m_VertexCache[3].Position.X = v2.X;
            this.m_VertexCache[3].Position.Y = v2.Y;

            renderContext.GraphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                this.m_VertexCache,
                0,
                4, 
                this.m_IndexCache,
                0,
                2);
        }

        private void CalculateCache()
        {
            this.m_VertexCache = new VertexPositionTexture[]
            {
                new VertexPositionTexture(
                    new Vector3(0,0,0),
                    new Vector2(1,1)),
                new VertexPositionTexture(
                    new Vector3(0,0,0),
                    new Vector2(0,1)),
                new VertexPositionTexture(
                    new Vector3(0,0,0),
                    new Vector2(0,0)),
                new VertexPositionTexture(
                    new Vector3(0,0,0),
                    new Vector2(1,0))
            };

            this.m_IndexCache = new short[] { 0, 1, 2, 2, 3, 0 };
        }
    }
}

