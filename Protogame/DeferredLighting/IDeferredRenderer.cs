namespace Protogame
{
    using Microsoft.Xna.Framework;

    public interface IDeferredRenderer
    {
        void AddDirectionalLight(Vector3 lightDirection, Color color);

        void AddPointLight(Vector3 lightPosition, Color color, float lightRadius, float lightIntensity);

        void BeginDeferredRendering(IRenderContext renderContext);

        void EndDeferredRendering(IRenderContext renderContext, bool debug);
    }
}