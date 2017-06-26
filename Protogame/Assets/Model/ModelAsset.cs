using System;
using System.Linq;

namespace Protogame
{
    public class ModelAsset : IAsset
    {
        private readonly IModelSerializer _modelSerializer;
        private readonly byte[] _data;
        private IModel _cachedModel;
        
        public ModelAsset(IModelSerializer modelSerializer, string name, byte[] data)
        {
            _modelSerializer = modelSerializer;
            Name = name;
            _data = data;
        }

        public string Name { get; private set; }

        /// <summary>
        /// If the model asset has bones for animation, instantiates a new copy of the model asset.  If the
        /// model asset has no bones (and can not be animated with them), returns a shared copy of the model asset.
        /// </summary>
        /// <returns>The model object.</returns>
        public IModel InstantiateModel()
        {
            if (_data != null)
            {
                if (_cachedModel == null)
                {
                    _cachedModel = _modelSerializer.Deserialize(Name, _data);
                }

                if (_cachedModel.Meshes.Any(x => x.Root != null))
                {
                    // Model has bones for animation; we need to pass a unique instance to the caller so they can
                    // modify and apply bone animations which require a unique vertex buffer.
                    return _modelSerializer.Deserialize(Name, _data);
                }

                // Since this model can not be animated, we can use a shared instance and hence share vertex
                // and index buffers.
                return _cachedModel;
            }

            throw new InvalidOperationException("Attempted to instantiate a model, but no platform data was loaded.");
        }
    }
}