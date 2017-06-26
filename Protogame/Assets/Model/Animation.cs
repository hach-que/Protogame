namespace Protogame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// A default implementation of <see cref="IAnimation"/>.
    /// </summary>
    public class Animation : IAnimation
    {
        /// <summary>
        /// The rotations to be applied to bones at specified times.
        /// </summary>
        private readonly IDictionary<string, IDictionary<double, Quaternion>> m_RotationForBones;

        /// <summary>
        /// The rotation keys that each bone has defined.
        /// </summary>
        private readonly Dictionary<string, List<double>> m_RotationKeys;

        /// <summary>
        /// The scales to be applied to bones at specified times.
        /// </summary>
        private readonly IDictionary<string, IDictionary<double, Vector3>> m_ScaleForBones;

        /// <summary>
        /// The scaling keys that each bone has defined.
        /// </summary>
        private readonly Dictionary<string, List<double>> m_ScaleKeys;

        /// <summary>
        /// The translations to be applied to bones at specified times.
        /// </summary>
        private readonly IDictionary<string, IDictionary<double, Vector3>> m_TranslationForBones;

        /// <summary>
        /// The translation keys that each bone has defined.
        /// </summary>
        private readonly Dictionary<string, List<double>> m_TranslationKeys;

        /// <summary>
        /// Initializes a new instance of the <see cref="Animation"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the animation.
        /// </param>
        /// <param name="ticksPerSecond">
        /// The ticks per second, or rate at which this animation plays.
        /// </param>
        /// <param name="durationInTicks">
        /// The duration in ticks, or total number of frames that this animation has.
        /// </param>
        /// <param name="translationForBones">
        /// The translation keys applicable to the animation.
        /// </param>
        /// <param name="rotationForBones">
        /// The rotation keys applicable to the animation.
        /// </param>
        /// <param name="scaleForBones">
        /// The scale keys applicable to the application.
        /// </param>
        public Animation(
            string name, 
            double ticksPerSecond, 
            double durationInTicks, 
            IDictionary<string, IDictionary<double, Vector3>> translationForBones, 
            IDictionary<string, IDictionary<double, Quaternion>> rotationForBones, 
            IDictionary<string, IDictionary<double, Vector3>> scaleForBones)
        {
            this.DurationInTicks = durationInTicks;
            this.TicksPerSecond = ticksPerSecond;
            this.Name = name;

            this.m_TranslationForBones = translationForBones;
            this.m_RotationForBones = rotationForBones;
            this.m_ScaleForBones = scaleForBones;

            this.m_TranslationKeys =
                this.m_TranslationForBones.Select(
                    x => new KeyValuePair<string, List<double>>(x.Key, x.Value.Keys.OrderBy(y => y).ToList()))
                    .ToDictionary(k => k.Key, v => v.Value);

            this.m_RotationKeys =
                this.m_RotationForBones.Select(
                    x => new KeyValuePair<string, List<double>>(x.Key, x.Value.Keys.OrderBy(y => y).ToList()))
                    .ToDictionary(k => k.Key, v => v.Value);

            this.m_ScaleKeys =
                this.m_ScaleForBones.Select(
                    x => new KeyValuePair<string, List<double>>(x.Key, x.Value.Keys.OrderBy(y => y).ToList()))
                    .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Gets the duration of the animation in ticks.
        /// </summary>
        /// <remarks>
        /// This value multiplied by <see cref="TicksPerSecond"/> will give you the
        /// total number of seconds that the animation runs for.
        /// </remarks>
        /// <value>
        /// The duration of the animation in ticks.
        /// </value>
        public double DurationInTicks { get; private set; }

        /// <summary>
        /// Gets the name of the animation.
        /// </summary>
        /// <value>
        /// The name of the animation.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a read-only representation of the rotation keys used in this animation.
        /// </summary>
        /// <remarks>
        /// Modifying this dictionary will not have the intended effect.
        /// </remarks>
        /// <value>
        /// A read-only representation of the rotation keys used in this animation.
        /// </value>
        public IDictionary<string, IDictionary<double, Quaternion>> RotationKeys
        {
            get
            {
                return this.m_RotationForBones;
            }
        }

        /// <summary>
        /// Gets a read-only representation of the scale keys used in this animation.
        /// </summary>
        /// <remarks>
        /// Modifying this dictionary will not have the intended effect.
        /// </remarks>
        /// <value>
        /// A read-only representation of the scale keys used in this animation.
        /// </value>
        public IDictionary<string, IDictionary<double, Vector3>> ScaleKeys
        {
            get
            {
                return this.m_ScaleForBones;
            }
        }

        /// <summary>
        /// Gets the number of ticks that occur per second for this animation.
        /// </summary>
        /// <value>
        /// The number of ticks that occur per second for this animation.
        /// </value>
        public double TicksPerSecond { get; private set; }

        /// <summary>
        /// Gets a read-only representation of the translation keys used in this animation.
        /// </summary>
        /// <remarks>
        /// Modifying this dictionary will not have the intended effect.
        /// </remarks>
        /// <value>
        /// A read-only representation of the translation keys used in this animation.
        /// </value>
        public IDictionary<string, IDictionary<double, Vector3>> TranslationKeys
        {
            get
            {
                return this.m_TranslationForBones;
            }
        }

        /// <summary>
        /// Modifies the specified model to align to this animation at the specified frame and then renders it.
        /// </summary>
        /// <param name="renderContext">
        ///     The current render context.
        /// </param>
        /// <param name="transform">
        ///     The world transformation to apply.
        /// </param>
        /// <param name="effectParameterSet"></param>
        /// <param name="model">
        ///     The model to update.
        /// </param>
        /// <param name="secondFraction">
        ///     The time elapsed.
        /// </param>
        /// <param name="multiply">
        ///     The multiplication factor to apply to the animation speed.
        /// </param>
        /// <param name="effect"></param>
        public void Render(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform, IModel model, TimeSpan secondFraction, float multiply)
        {
            this.Render(renderContext, effect, effectParameterSet, transform, model, (float)(secondFraction.TotalSeconds % (this.DurationInTicks / this.TicksPerSecond)), multiply);
        }

        /// <summary>
        /// Modifies the specified model to align to this animation at the specified frame and then renders it.
        /// </summary>
        /// <param name="renderContext">
        ///     The current render context.
        /// </param>
        /// <param name="transform">
        ///     The world transformation to apply.
        /// </param>
        /// <param name="effectParameterSet"></param>
        /// <param name="model">
        ///     The model to update.
        /// </param>
        /// <param name="totalSeconds">
        ///     The time elapsed.
        /// </param>
        /// <param name="multiply">
        ///     The multiplication factor to apply to the animation speed.
        /// </param>
        /// <param name="effect"></param>
        public void Render(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform, IModel model, float totalSeconds, float multiply)
        {
            this.Apply(model, totalSeconds, multiply);

            // Render the model.
            model.Render(renderContext, effect, effectParameterSet, transform);
        }

        /// <summary>
        /// Applies the animation at a specified time to the model.
        /// </summary>
        /// <param name="model">
        /// The model to apply the animation to.
        /// </param>
        /// <param name="totalSeconds">
        /// The time elapsed.
        /// </param>
        /// <param name="multiply">
        /// The multiplication factor to apply to the animation speed.
        /// </param>
        public void Apply(IModel model, float totalSeconds, float multiply)
        {
            totalSeconds = (float)(totalSeconds * this.TicksPerSecond * multiply);

            foreach (var mesh in model.Meshes)
            {
                foreach (var bone in mesh.Bones.Keys)
                {
                    var boneObj = mesh.Bones[bone];

                    if (this.m_TranslationKeys.ContainsKey(bone))
                    {
                        double translationKeyPrevious, translationKeyNext;

                        this.FindSurroundingTickValues(
                            this.m_TranslationKeys[bone],
                            totalSeconds,
                            out translationKeyPrevious,
                            out translationKeyNext);

                        if (Math.Abs(translationKeyPrevious - (-1)) < 0.0001f || Math.Abs(translationKeyNext - (-1)) < 0.0001f)
                        {
                            if (Math.Abs(translationKeyPrevious - (-1)) >= 0.0001f)
                            {
                                boneObj.CurrentTranslation = this.m_TranslationForBones[bone][translationKeyPrevious];
                            }
                            else if (Math.Abs(translationKeyNext - (-1)) >= 0.0001f)
                            {
                                boneObj.CurrentTranslation = this.m_TranslationForBones[bone][translationKeyNext];
                            }
                        }
                        else
                        {
                            var previousTranslation = this.m_TranslationForBones[bone][translationKeyPrevious];
                            var nextTranslation = this.m_TranslationForBones[bone][translationKeyNext];

                            var actualTranslation = previousTranslation;

                            if (Math.Abs(translationKeyPrevious - translationKeyNext) > 0.0001f)
                            {
                                var amount =
                                    (float)
                                    -((translationKeyPrevious - totalSeconds) / (translationKeyNext - translationKeyPrevious));

                                actualTranslation = Vector3.Lerp(previousTranslation, nextTranslation, amount);
                            }

                            boneObj.CurrentTranslation = actualTranslation;
                        }
                    }

                    if (this.m_RotationKeys.ContainsKey(bone))
                    {
                        double rotationKeyPrevious, rotationKeyNext;

                        this.FindSurroundingTickValues(
                            this.m_RotationKeys[bone],
                            totalSeconds,
                            out rotationKeyPrevious,
                            out rotationKeyNext);

                        if (Math.Abs(rotationKeyPrevious - (-1)) < 0.0001f || Math.Abs(rotationKeyNext - (-1)) < 0.0001f)
                        {
                            if (Math.Abs(rotationKeyPrevious - (-1)) >= 0.0001f)
                            {
                                boneObj.CurrentRotation = this.m_RotationForBones[bone][rotationKeyPrevious];
                            }
                            else if (Math.Abs(rotationKeyNext - (-1)) >= 0.0001f)
                            {
                                boneObj.CurrentRotation = this.m_RotationForBones[bone][rotationKeyNext];
                            }
                        }
                        else
                        {
                            var previousRotation = this.m_RotationForBones[bone][rotationKeyPrevious];
                            var nextRotation = this.m_RotationForBones[bone][rotationKeyNext];

                            var actualRotation = previousRotation;

                            if (Math.Abs(rotationKeyPrevious - rotationKeyNext) > 0.0001f)
                            {
                                actualRotation = Quaternion.Lerp(
                                    previousRotation,
                                    nextRotation,
                                    (float)-((rotationKeyPrevious - totalSeconds) / (rotationKeyNext - rotationKeyPrevious)));
                            }

                            boneObj.CurrentRotation = actualRotation;
                        }
                    }

                    if (this.m_ScaleKeys.ContainsKey(bone))
                    {
                        double scaleKeyPrevious, scaleKeyNext;

                        this.FindSurroundingTickValues(this.m_ScaleKeys[bone], totalSeconds, out scaleKeyPrevious, out scaleKeyNext);

                        if (Math.Abs(scaleKeyPrevious - (-1)) < 0.0001f || Math.Abs(scaleKeyNext - (-1)) < 0.0001f)
                        {
                            if (Math.Abs(scaleKeyPrevious - (-1)) >= 0.0001f)
                            {
                                boneObj.CurrentScale = this.m_ScaleForBones[bone][scaleKeyPrevious];
                            }
                            else if (Math.Abs(scaleKeyNext - (-1)) >= 0.0001f)
                            {
                                boneObj.CurrentScale = this.m_ScaleForBones[bone][scaleKeyNext];
                            }
                        }
                        else
                        {
                            var previousScale = this.m_ScaleForBones[bone][scaleKeyPrevious];
                            var nextScale = this.m_ScaleForBones[bone][scaleKeyNext];

                            var actualScale = previousScale;

                            if (Math.Abs(scaleKeyPrevious - scaleKeyNext) > 0.0001f)
                            {
                                actualScale = Vector3.Lerp(
                                    previousScale,
                                    nextScale,
                                    (float)-((scaleKeyPrevious - totalSeconds) / (scaleKeyNext - scaleKeyPrevious)));
                            }

                            boneObj.CurrentScale = actualScale;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies the animation at a specified time to the model.
        /// </summary>
        /// <param name="model">
        /// The model to apply the animation to.
        /// </param>
        /// <param name="frame">
        /// The frame to draw at.
        /// </param>
        public void Apply(IModel model, double frame)
        {
            var calculatedSeconds = (float)(frame / this.TicksPerSecond);

            this.Apply(model, calculatedSeconds, 1);
        }

        /// <summary>
        /// Modifies the specified model to align to this animation at the specified frame and then renders it.
        /// </summary>
        /// <param name="renderContext">
        ///     The current render context.
        /// </param>
        /// <param name="transform">
        ///     The world transformation to apply.
        /// </param>
        /// <param name="effectParameterSet"></param>
        /// <param name="model">
        ///     The model to update.
        /// </param>
        /// <param name="frame">
        ///     The frame to draw at.
        /// </param>
        /// <param name="effect"></param>
        public void Render(IRenderContext renderContext, IEffect effect, IEffectParameterSet effectParameterSet, Matrix transform, IModel model, double frame)
        {
            var calculatedSeconds = (float)(frame / this.TicksPerSecond);

            this.Render(renderContext, effect, effectParameterSet, transform, model, calculatedSeconds, 1);
        }

        /// <summary>
        /// Finds the nearest previous and next floating point number to the specified floating point number
        /// using a binary search algorithm.
        /// </summary>
        /// <param name="keys">
        /// The list of floating point numbers to search through.
        /// </param>
        /// <param name="current">
        /// The current, or target value to find.
        /// </param>
        /// <param name="previous">
        /// The previous value to this value.
        /// </param>
        /// <param name="next">
        /// The next value to this value.
        /// </param>
        private void FindSurroundingTickValues(IList<double> keys, double current, out double previous, out double next)
        {
            int lowest = 0;
            int highest = keys.Count - 1;

            if (keys.Count == 0)
            {
                previous = -1;
                next = -1;
                return;
            }

            while (true)
            {
                if (lowest >= 0 && Math.Abs(keys[lowest] - current) < 0.0001f)
                {
                    previous = keys[lowest];
                    next = keys[lowest];
                    return;
                }

                if (highest < keys.Count && Math.Abs(keys[highest] - current) < 0.0001f)
                {
                    previous = keys[highest];
                    next = keys[highest];
                    return;
                }

                if (lowest == keys.Count - 1 && keys[lowest] <= current)
                {
                    previous = keys[lowest];
                    next = -1;
                    return;
                }

                if (highest == 0 && keys[highest] > current)
                {
                    previous = -1;
                    next = keys[highest];
                    return;
                }

                if (lowest + 1 < keys.Count && Math.Abs(keys[lowest + 1] - current) < 0.0001f)
                {
                    previous = keys[lowest + 1];
                    next = keys[lowest + 1];
                    return;
                }

                if (highest - 1 >= 0 && Math.Abs(keys[highest - 1] - current) < 0.0001f)
                {
                    previous = keys[highest - 1];
                    next = keys[highest - 1];
                    return;
                }

                if (lowest < keys.Count - 1 && keys[lowest] <= current && current <= keys[lowest + 1])
                {
                    previous = keys[lowest];
                    next = keys[lowest + 1];
                    return;
                }

                if (highest >= 1 && keys[highest - 1] >= current && current >= keys[highest])
                {
                    previous = keys[highest - 1];
                    next = keys[highest];
                    return;
                }

                if (lowest == highest && Math.Abs(keys[lowest] - current) > 0.0001f)
                {
                    throw new InvalidOperationException();
                }

                var mid = (int)Math.Ceiling(((highest - (double)lowest) / 2f) + lowest);

                if (mid > 0 && keys[mid] <= current)
                {
                    lowest = mid;
                }
                else if (mid < keys.Count && keys[mid] >= current)
                {
                    highest = Math.Max(lowest, mid - 1);
                }
            }
        }
    }
}