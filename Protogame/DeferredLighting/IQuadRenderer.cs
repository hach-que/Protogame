using System;
using Microsoft.Xna.Framework;

namespace Protogame
{
    public interface IQuadRenderer
    {
        void Render(IRenderContext renderContext, Vector2 v1, Vector2 v2);
    }
}

