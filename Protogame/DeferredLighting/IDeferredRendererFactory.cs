using System;

namespace Protogame
{
    public interface IDeferredRendererFactory
    {
        IDeferredRenderer CreateDeferredRenderer();
    }
}

