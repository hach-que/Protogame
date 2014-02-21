using System;
using Ninject.Modules;

namespace Protogame
{
    public class ProtogameDeferredLightingIoCModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IDeferredRenderer>().To<DefaultDeferredRenderer>();
            this.Bind<IQuadRenderer>().To<DefaultQuadRenderer>();
            this.Bind<IDeferredRendererFactory>().To<DefaultDeferredRendererFactory>();
        }
    }
}

