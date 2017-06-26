using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Protoinject;

namespace Protogame
{
    public class Render3DModelComponent : IRenderableComponent, IEnabledComponent, IHasTransform
    {
        private readonly INode _node;

        private readonly IFinalTransform _finalTransform;

        private readonly I3DRenderUtilities _renderUtilities;

        private readonly ITextureFromHintPath _textureFromHintPath;

        private readonly IRenderBatcher _renderBatcher;

        private readonly IAssetManager _assetManager;

        private IModelMesh[] _lastCachedMesh;

        private IAssetReference<TextureAsset>[] _lastCachedDiffuseTexture;

        private IAssetReference<TextureAsset>[] _lastCachedNormalMapTexture;

        private IAssetReference<TextureAsset>[] _lastCachedSpecularColorMapTexture;

        private Color?[] _lastCachedSpecularColor;

        private float?[] _lastCachedSpecularPower;

        private string[] _mode;

        private IEffectParameterSet[] _cachedEffectParameterSet;

        private IEffect[] _effectUsedForParameterSetCache;

        private Texture2D[] _lastSetDiffuseTexture;

        private Texture2D[] _lastSetNormalMap;

        private Texture2D[] _lastSetSpecularColorMap;

        private Color?[] _lastSetSpecularColor;

        private Color?[] _lastSetDiffuseColor;

        private float?[] _lastSetSpecularPower;

        private bool[] _lastDidSetDiffuseTexture;

        private bool[] _lastDidSetNormalMap;

        private bool[] _lastDidSetSpecularColorMap;

        private bool[] _lastDidSetSpecularColor;

        private bool[] _lastDidSetDiffuseColor;

        private bool[] _lastDidSetSpecularPower;

        private IEffect[] _cachedEffect;

        private IMaterial[] _lastMaterial;

        private IRenderRequest[] _renderRequests;

        private bool _useDefaultEffects;

        private IAssetReference<UberEffectAsset> _uberEffectAsset;

        private Matrix _lastWorldMatrix;

        private IAnimation _lastAnimation;

        private Stopwatch _animationTracker;

        private IAssetReference<ModelAsset> _modelAsset;

        private IModel _model;

        public Render3DModelComponent(
            INode node,
            I3DRenderUtilities renderUtilities,
            IAssetManager assetManager,
            ITextureFromHintPath textureFromHintPath,
            IRenderBatcher renderBatcher)
        {
            _node = node;
            _finalTransform = new DefaultFinalTransform(this, _node);
            _renderUtilities = renderUtilities;
            _textureFromHintPath = textureFromHintPath;
            _renderBatcher = renderBatcher;
            _assetManager = assetManager;
            _animationTracker = new Stopwatch();

            Enabled = true;
            Transform = new DefaultTransform();
        }

        public IAssetReference<ModelAsset> Model
        {
            get { return _modelAsset; }
            set
            {
                if (_modelAsset != value)
                {
                    _modelAsset = value;

                    if (_model != null)
                    {
                        _model.Dispose();
                        _model = null;
                    }
                }
            }
        }

        public IModel ModelInstance => _model;

        public IAssetReference<EffectAsset> Effect { get; set; }

        public bool Enabled { get; set; }

        public IMaterial OverrideMaterial { get; set; }

        public Func<IMaterial, IMaterial> OverrideMaterialFactory { get; set; }

        public ITransform Transform { get; }

        public IFinalTransform FinalTransform => _finalTransform;

        public IAnimation Animation { get; set; }

        private T[] InitArray<T>(T[] existing)
        {
            if (existing == null || existing.Length != _model.Meshes.Length)
            {
                return new T[_model.Meshes.Length];
            }

            return existing;
        }

        public void Render(ComponentizedEntity entity, IGameContext gameContext, IRenderContext renderContext)
        {
            if (!Enabled)
            {
                return;
            }

            if (_modelAsset != null && _model == null && _modelAsset.IsReady)
            {
                _model = _modelAsset.Asset.InstantiateModel();
            }

            if (_model == null)
            {
                return;
            }

            if (renderContext.IsCurrentRenderPass<I3DRenderPass>())
            {
                if (Effect == null)
                {
                    _useDefaultEffects = true;
                }
                else
                {
                    _useDefaultEffects = false;
                }

                if (_useDefaultEffects && _uberEffectAsset == null)
                {
                    _uberEffectAsset = _assetManager.Get<UberEffectAsset>("effect.BuiltinSurface");
                }

                if (_model != null)
                {
                    var matrix = FinalTransform.AbsoluteMatrix;
                    
                    bool changedRenderRequest = _lastWorldMatrix != matrix;
                    string changedRenderRequestBy = changedRenderRequest ? "matrix" : "";
                    
                    var animation = GetModelAnimation(ref changedRenderRequest, ref changedRenderRequestBy);

                    if (animation != null)
                    {
                        animation.Apply(_model, _animationTracker.ElapsedMilliseconds / 1000f, 0.5f);
                    }

                    _lastCachedMesh = InitArray(_lastCachedMesh);
                    _lastCachedDiffuseTexture = InitArray(_lastCachedDiffuseTexture);
                    _lastCachedNormalMapTexture = InitArray(_lastCachedNormalMapTexture);
                    _lastCachedSpecularColorMapTexture = InitArray(_lastCachedSpecularColorMapTexture);
                    _lastCachedSpecularColor = InitArray(_lastCachedSpecularColor);
                    _lastCachedSpecularPower = InitArray(_lastCachedSpecularPower);
                    _mode = InitArray(_mode);
                    _cachedEffectParameterSet = InitArray(_cachedEffectParameterSet);
                    _effectUsedForParameterSetCache = InitArray(_effectUsedForParameterSetCache);
                    _lastSetDiffuseTexture = InitArray(_lastSetDiffuseTexture);
                    _lastSetNormalMap = InitArray(_lastSetNormalMap);
                    _lastSetSpecularColorMap = InitArray(_lastSetSpecularColorMap);
                    _lastSetSpecularColor = InitArray(_lastSetSpecularColor);
                    _lastSetDiffuseColor = InitArray(_lastSetDiffuseColor);
                    _lastSetSpecularPower = InitArray(_lastSetSpecularPower);
                    _lastDidSetDiffuseTexture = InitArray(_lastDidSetDiffuseTexture);
                    _lastDidSetNormalMap = InitArray(_lastDidSetNormalMap);
                    _lastDidSetSpecularColorMap = InitArray(_lastDidSetSpecularColorMap);
                    _lastDidSetSpecularColor = InitArray(_lastDidSetSpecularColor);
                    _lastDidSetDiffuseColor = InitArray(_lastDidSetDiffuseColor);
                    _lastDidSetSpecularPower = InitArray(_lastDidSetSpecularPower);
                    _cachedEffect = InitArray(_cachedEffect);
                    _lastMaterial = InitArray(_lastMaterial);
                    _renderRequests = InitArray(_renderRequests);
                    
                    for (var i = 0; i < _model.Meshes.Length; i++)
                    {
                        var mesh = _model.Meshes[i];

                        if (OverrideMaterial == null && OverrideMaterialFactory != null)
                        {
                            OverrideMaterial = OverrideMaterialFactory(mesh.Material);
                        }

                        var material = OverrideMaterial ?? mesh.Material;

                        UpdateCachedMesh(i, material, ref changedRenderRequest, ref changedRenderRequestBy);

                        var effect = GetEffect(i, ref changedRenderRequest, ref changedRenderRequestBy);

                        var parameterSet = GetEffectParameterSet(i, material, ref changedRenderRequest, ref changedRenderRequestBy);

                        _renderRequests[i] = mesh.CreateRenderRequest(_model, renderContext, effect, parameterSet, matrix);
                    }

                    _lastWorldMatrix = matrix;

                    for (var i = 0; i < _renderRequests.Length; i++)
                    {
                        _renderBatcher.QueueRequest(
                            renderContext,
                            _renderRequests[i]);
                    }
                }
                else
                {
                    _lastCachedMesh = null;
                    _lastCachedDiffuseTexture = null;
                }
            }
        }

        private IAnimation GetModelAnimation(ref bool changedRenderRequest, ref string changedRenderRequestBy)
        {
            if (Animation != _lastAnimation)
            {
                changedRenderRequest = true;
                changedRenderRequestBy += ":animation";

                _lastAnimation = Animation;
                _animationTracker.Restart();
            }

            return _lastAnimation;
        }

        private IEffectParameterSet GetEffectParameterSet(int meshIndex, IMaterial material, ref bool changedRenderRequest, ref string changedRenderRequestBy)
        {
            Texture2D lastCachedDiffuseTexture = null;
            Texture2D lastCachedNormalMapTexture = null;
            Texture2D lastCachedSpecularColorMapTexture = null;

            if (_lastCachedDiffuseTexture[meshIndex] == null || _lastCachedDiffuseTexture[meshIndex].IsReady)
            {
                lastCachedDiffuseTexture = _lastCachedDiffuseTexture[meshIndex]?.Asset?.Texture;
            }
            if (_lastCachedNormalMapTexture[meshIndex] == null || _lastCachedNormalMapTexture[meshIndex].IsReady)
            {
                lastCachedNormalMapTexture = _lastCachedNormalMapTexture[meshIndex]?.Asset?.Texture;
            }
            if (_lastCachedSpecularColorMapTexture[meshIndex] == null || _lastCachedSpecularColorMapTexture[meshIndex].IsReady)
            {
                lastCachedSpecularColorMapTexture = _lastCachedSpecularColorMapTexture[meshIndex]?.Asset?.Texture;
            }

            if (_effectUsedForParameterSetCache == _cachedEffect &&
                changedRenderRequest == false &&
                (/*!_lastDidSetDiffuseTexture || */
                    _lastSetDiffuseTexture[meshIndex] == lastCachedDiffuseTexture) &&
                (/*!_lastDidSetNormalMap || */_lastSetNormalMap[meshIndex] == lastCachedNormalMapTexture) &&
                (!_lastDidSetSpecularPower[meshIndex] || _lastSetSpecularPower[meshIndex] == _lastCachedSpecularPower[meshIndex]) &&
                (/*!_lastDidSetSpecularColorMap || */_lastSetSpecularColorMap[meshIndex] == lastCachedSpecularColorMapTexture) &&
                (!_lastDidSetSpecularColor[meshIndex] || _lastSetSpecularColor[meshIndex] == _lastCachedSpecularColor[meshIndex]) &&
                (!_lastDidSetDiffuseColor[meshIndex] || _lastSetDiffuseColor[meshIndex] == (material.ColorDiffuse ?? Color.Black)))
            {
                // Reuse the existing parameter set.
                return _cachedEffectParameterSet[meshIndex];
            }

            changedRenderRequest = true;
            changedRenderRequestBy += ":parameterset";

            // Create a new parameter set and cache it.
            _cachedEffectParameterSet[meshIndex] = _cachedEffect[meshIndex].CreateParameterSet();
            _effectUsedForParameterSetCache[meshIndex] = _cachedEffect[meshIndex];

            _lastSetDiffuseTexture[meshIndex] = null;
            _lastSetNormalMap[meshIndex] = null;
            _lastSetSpecularPower[meshIndex] = null;
            _lastSetSpecularColorMap[meshIndex] = null;
            _lastSetSpecularColor[meshIndex] = null;
            _lastSetDiffuseColor[meshIndex] = null;
            _lastDidSetDiffuseTexture[meshIndex] = false;
            _lastDidSetNormalMap[meshIndex] = false;
            _lastDidSetSpecularPower[meshIndex] = false;
            _lastDidSetSpecularColorMap[meshIndex] = false;
            _lastDidSetSpecularColor[meshIndex] = false;
            _lastDidSetDiffuseColor[meshIndex] = false;

            if (_cachedEffectParameterSet[meshIndex].HasSemantic<ITextureEffectSemantic>())
            {
                if (_lastCachedDiffuseTexture[meshIndex] != null &&
                    _lastCachedDiffuseTexture[meshIndex].IsReady)
                {
                    _cachedEffectParameterSet[meshIndex].GetSemantic<ITextureEffectSemantic>().Texture =
                        _lastCachedDiffuseTexture[meshIndex].Asset.Texture;
                    _lastSetDiffuseTexture[meshIndex] = _lastCachedDiffuseTexture[meshIndex].Asset.Texture;
                    _lastDidSetDiffuseTexture[meshIndex] = true;
                }
            }

            if (_cachedEffectParameterSet[meshIndex].HasSemantic<INormalMapEffectSemantic>())
            {
                if (_lastCachedNormalMapTexture[meshIndex] != null &&
                    _lastCachedNormalMapTexture[meshIndex].IsReady)
                {
                    _cachedEffectParameterSet[meshIndex].GetSemantic<INormalMapEffectSemantic>().NormalMap =
                        _lastCachedNormalMapTexture[meshIndex].Asset.Texture;
                    _lastSetNormalMap[meshIndex] = _lastCachedNormalMapTexture[meshIndex].Asset.Texture;
                    _lastDidSetNormalMap[meshIndex] = true;
                }
            }

            if (_cachedEffectParameterSet[meshIndex].HasSemantic<ISpecularEffectSemantic>())
            {
                if (_lastCachedSpecularPower[meshIndex] != null)
                {
                    var semantic = _cachedEffectParameterSet[meshIndex].GetSemantic<ISpecularEffectSemantic>();
                    semantic.SpecularPower = _lastCachedSpecularPower[meshIndex].Value;
                    _lastSetSpecularPower[meshIndex] = _lastCachedSpecularPower[meshIndex].Value;
                    _lastDidSetSpecularPower[meshIndex] = true;

                    if (_lastCachedSpecularColorMapTexture[meshIndex] != null && _lastCachedSpecularColorMapTexture[meshIndex].IsReady)
                    {
                        semantic.SpecularColorMap = _lastCachedSpecularColorMapTexture[meshIndex].Asset.Texture;
                        _lastSetSpecularColorMap[meshIndex] = _lastCachedSpecularColorMapTexture[meshIndex].Asset.Texture;
                        _lastDidSetSpecularColorMap[meshIndex] = true;
                    }
                    else if (_lastCachedSpecularColor[meshIndex] != null)
                    {
                        semantic.SpecularColor = _lastCachedSpecularColor[meshIndex].Value;
                        _lastSetSpecularColor[meshIndex] = _lastCachedSpecularColor[meshIndex].Value;
                        _lastDidSetSpecularColor[meshIndex] = true;
                    }
                }
            }

            if (_cachedEffectParameterSet[meshIndex].HasSemantic<IColorDiffuseEffectSemantic>())
            {
                var v = material.ColorDiffuse ?? Color.Black;
                _cachedEffectParameterSet[meshIndex].GetSemantic<IColorDiffuseEffectSemantic>().Diffuse = v;
                _lastSetDiffuseColor[meshIndex] = v;
                _lastDidSetDiffuseColor[meshIndex] = true;
            }

            return _cachedEffectParameterSet[meshIndex];
        }

        private void UpdateCachedMesh(int meshIndex, IMaterial material, ref bool changedRenderRequest, ref string changedRenderRequestBy)
        {
            if (_lastCachedMesh[meshIndex] != _model.Meshes[meshIndex] || _lastMaterial[meshIndex] != material)
            {
                if (_lastMaterial[meshIndex] != material)
                {
                    changedRenderRequest = true;
                    changedRenderRequestBy += ":material";
                }

                _lastMaterial[meshIndex] = material;

                if (_lastCachedMesh[meshIndex] != _model.Meshes[meshIndex])
                {
                    changedRenderRequest = true;
                    changedRenderRequestBy += ":model";
                }

                if (material.TextureDiffuse != null)
                {
                    if (material.TextureDiffuse.TextureAsset != null)
                    {
                        _lastCachedDiffuseTexture[meshIndex] = material.TextureDiffuse.TextureAsset;
                    }
                    else
                    {
                        _lastCachedDiffuseTexture[meshIndex] =
                            _textureFromHintPath.GetTextureFromHintPath(material.TextureDiffuse);
                    }

                    if (material.TextureNormal != null)
                    {
                        if (material.TextureNormal.TextureAsset != null)
                        {
                            _lastCachedNormalMapTexture[meshIndex] = material.TextureNormal.TextureAsset;
                        }
                        else
                        {
                            _lastCachedNormalMapTexture[meshIndex] =
                                _textureFromHintPath.GetTextureFromHintPath(material.TextureNormal);
                        }
                    }
                    else
                    {
                        _lastCachedNormalMapTexture[meshIndex] = null;
                    }

                    if (material.PowerSpecular != null)
                    {
                        _lastCachedSpecularPower[meshIndex] = material.PowerSpecular.Value;

                        if (material.TextureSpecular != null)
                        {
                            if (material.TextureSpecular.TextureAsset != null)
                            {
                                _lastCachedSpecularColorMapTexture[meshIndex] = material.TextureSpecular.TextureAsset;
                            }
                            else
                            {
                                _lastCachedSpecularColorMapTexture[meshIndex] =
                                    _textureFromHintPath.GetTextureFromHintPath(material.TextureSpecular);
                            }
                        }
                        else if (material.ColorSpecular != null)
                        {
                            _lastCachedSpecularColor[meshIndex] = material.ColorSpecular.Value;
                        }
                        else
                        {
                            _lastCachedSpecularColor[meshIndex] = null;
                        }
                    }
                    else
                    {
                        _lastCachedSpecularPower[meshIndex] = null;
                    }

                    _mode[meshIndex] = "texture";
                }
                else if (material.ColorDiffuse != null)
                {
                    _mode[meshIndex] = "diffuse";
                }
                else
                {
                    _mode[meshIndex] = "color";
                }
                _lastCachedMesh[meshIndex] = _model.Meshes[meshIndex];
            }
        }

        private IEffect GetEffect(int meshIndex, ref bool changedRenderRequest, ref string changedRenderRequestBy)
        {
            if (_cachedEffect[meshIndex] != null)
            {
                return _cachedEffect[meshIndex];
            }

            changedRenderRequest = true;
            changedRenderRequestBy += ":effect";

            IEffect effect;
            if (!_useDefaultEffects)
            {
                effect = Effect.Asset.Effect;
            }
            else
            {
                var skinnedSuffix = _lastCachedMesh[meshIndex].Bones == null ? null : "Skinned";

                switch (_mode[meshIndex])
                {
                    case "texture":
                        if (_lastCachedNormalMapTexture[meshIndex] != null && _lastCachedSpecularPower[meshIndex] != null)
                        {
                            if (_lastCachedSpecularColorMapTexture[meshIndex] != null)
                            {
                                effect =
                                    _uberEffectAsset.Asset.Effects["TextureNormalSpecColMap" + skinnedSuffix];
                            }
                            else if (_lastCachedSpecularColor[meshIndex] != null)
                            {
                                effect =
                                    _uberEffectAsset.Asset.Effects["TextureNormalSpecColCon" + skinnedSuffix];
                            }
                            else
                            {
                                effect =
                                    _uberEffectAsset.Asset.Effects["TextureNormalSpecColDef" + skinnedSuffix];
                            }
                        }
                        else if (_lastCachedNormalMapTexture[meshIndex] != null)
                        {
                            effect = _uberEffectAsset.Asset.Effects["TextureNormal" + skinnedSuffix];
                        }
                        else
                        {
                            effect = _uberEffectAsset.Asset.Effects["Texture" + skinnedSuffix];
                        }
                        break;
                    case "color":
                        effect = _uberEffectAsset.Asset.Effects["Color" + skinnedSuffix];
                        break;
                    case "diffuse":
                        effect = _uberEffectAsset.Asset.Effects["Diffuse" + skinnedSuffix];
                        break;
                    default:
                        throw new InvalidOperationException("Unknown default effect type.");
                }
            }

            _cachedEffect[meshIndex] = effect;
            return _cachedEffect[meshIndex];
        }
    }
}
