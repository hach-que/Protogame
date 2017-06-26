using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Protogame
{
    public class DefaultStandardPointLightRenderer : ILightRenderer<IStandardPointLight>
    {
        private readonly IAssetReference<EffectAsset> _pointLightEffect;
        private readonly IAssetReference<ModelAsset> _pointLightSphereModel;

        private IEffectParameterSet _parameterSet;
        private IModel _pointLightSphereModelInstance;

        public DefaultStandardPointLightRenderer(
            IAssetManager assetManager)
        {
            _pointLightEffect = assetManager.Get<EffectAsset>("effect.PointLight");
            _pointLightSphereModel = assetManager.Get<ModelAsset>("model.LightSphere");
        }

        public void Render(IGameContext gameContext, IRenderContext renderContext, ILightContext lightContext, IEnumerable<ILight> lights)
        {
            if (!_pointLightEffect.IsReady || !_pointLightSphereModel.IsReady)
            {
                return;
            }

            if (_parameterSet == null)
            {
                _parameterSet = _pointLightEffect.Asset.Effect.CreateParameterSet();
            }
            if (_pointLightSphereModelInstance == null)
            {
                _pointLightSphereModelInstance = _pointLightSphereModel.Asset.InstantiateModel();
            }

            var oldRasterizerState = renderContext.GraphicsDevice.RasterizerState;
            var oldWorld = renderContext.World;

            if (lightContext.DiffuseLightRenderTarget != null && lightContext.SpecularLightRenderTarget != null)
            {
                renderContext.PushRenderTarget(lightContext.DiffuseLightRenderTarget, lightContext.SpecularLightRenderTarget);
            }

            foreach (var ulight in lights)
            {
                var light = (IStandardPointLight)ulight;

                _parameterSet.Unlock();
                _parameterSet["Color"]?.SetValue(lightContext.DeferredColorMap);
                _parameterSet["Normal"]?.SetValue(lightContext.DeferredNormalMap);
                _parameterSet["Depth"]?.SetValue(lightContext.DeferredDepthMap);
                _parameterSet["Specular"]?.SetValue(lightContext.DeferredSpecularMap);
                _parameterSet["CameraPosition"]?.SetValue(renderContext.CameraPosition);
                _parameterSet["LightPosition"]?.SetValue(light.LightPosition);
                _parameterSet["LightColor"]?.SetValue(light.LightColor.ToVector3());
                _parameterSet["LightRadius"]?.SetValue(light.LightRadius);
                _parameterSet["LightIntensity"]?.SetValue(light.LightIntensity);
                _parameterSet["LightInvertViewProjection"]?.SetValue(Matrix.Invert(renderContext.View * renderContext.Projection));
                _parameterSet["World"]?.SetValue(renderContext.World);
                _parameterSet["View"]?.SetValue(renderContext.View);
                _parameterSet["Projection"]?.SetValue(renderContext.Projection);

                // If we are inside the lighting sphere, we need to invert the culling mode so
                // that we see the inside faces of the model.
                float cameraToCenter = Vector3.Distance(renderContext.CameraPosition, light.LightPosition);
                if (cameraToCenter < light.LightRadius)
                {
                    renderContext.GraphicsDevice.RasterizerState = lightContext.RasterizerStateCullClockwiseFace;
                }
                else
                {
                    renderContext.GraphicsDevice.RasterizerState = lightContext.RasterizerStateCullCounterClockwiseFace;
                }

                renderContext.World = Matrix.CreateScale(light.LightRadius) * Matrix.CreateTranslation(light.LightPosition);

                _pointLightSphereModelInstance.Meshes[0].Render(_pointLightSphereModelInstance, renderContext, _pointLightEffect.Asset.Effect, _parameterSet, Matrix.Identity);
            }

            renderContext.World = oldWorld;
            renderContext.GraphicsDevice.RasterizerState = oldRasterizerState;

            if (lightContext.DiffuseLightRenderTarget != null && lightContext.SpecularLightRenderTarget != null)
            {
                renderContext.PopRenderTarget();
            }
        }
    }
}
