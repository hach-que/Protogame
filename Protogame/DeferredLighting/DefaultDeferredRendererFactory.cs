using System;
using Ninject.Syntax;
using Ninject;

namespace Protogame
{
    public class DefaultDeferredRendererFactory : IDeferredRendererFactory
    {
        private readonly IResolutionRoot m_ResolutionRoot;

        public DefaultDeferredRendererFactory(IResolutionRoot resolutionRoot)
        {
            this.m_ResolutionRoot = resolutionRoot;
        }

        public IDeferredRenderer CreateDeferredRenderer()
        {
            return this.m_ResolutionRoot.Get<IDeferredRenderer>();
        }
    }
}

