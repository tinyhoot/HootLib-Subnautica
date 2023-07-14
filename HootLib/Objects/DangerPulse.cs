using UnityEngine;

namespace HootLib.Objects
{
    /// <summary>
    /// Handles the red pulse around hud elements, like when food gets dangerously low.
    /// </summary>
    public class DangerPulse
    {
        public Animation Animation;
        public AnimationState State;

        public float ActivationDelay { get; private set; }
        public float Speed { get; private set; }
        public bool Enabled => Animation.isPlaying || ActivationDelay >= 0f;

        public AnimationCurve ActivationCurve { get; private set; }  // Whether to activate (play) the animation at all.
        public AnimationCurve SpeedCurve { get; private set; }  // The speed at which the animation plays.

        private float _waitDuration;

        public DangerPulse(Animation animation)
        {
            Animation = animation;
            Animation.wrapMode = WrapMode.Once;
            State = Animation.GetState(0);
            State.blendMode = AnimationBlendMode.Blend;
            State.layer = 0;
            State.speed = 1f;
            State.weight = 1f;
            Animation.clip = State.clip;  // For some reason this is not assigned if the animation was copied, it's odd.
            Animation.Rewind();  // So we don't get stuck with a red bar on load.

            // Define basic curves which more or less mimic vanilla behaviour.
            ActivationCurve = new AnimationCurve();
            SpeedCurve = new AnimationCurve();
            ActivationCurve.AddKey(new Keyframe(0.21f, -1f));  // Any value above 0.21 turns the animation off.
            ActivationCurve.AddKey(new Keyframe(0.2f, 3f, 0.05f, 0f));
            SpeedCurve.AddKey(new Keyframe(0.21f, -1f));
            SpeedCurve.AddKey(new Keyframe(0.2f, 1f));
            SpeedCurve.AddKey(new Keyframe(0f, 2));  // Gradually get faster as we approach zero.
        }
        
        /// <summary>
        /// Set the animation curves for the pulse. You can think of the animation curves as "functions" which take a
        /// time value between 0 and 1 and produce an output based on it. <br/>
        /// As an example, you could use the percentage of how full a hud bar is as input, which you'll need to pass
        /// in the <see cref="Update"/> method.
        /// </summary>
        /// <param name="activationDelay">The activation curve decides whether the animation is playing. The animation
        /// will play if this curve returns a value >= 0. Values bigger than zero indicate the delay before the
        /// animation starts playing.</param>
        /// <param name="speed">The speed curve decides how quickly the animation plays. Values below 0.5 are clamped.
        /// </param>
        public void SetAnimationCurves(AnimationCurve activationDelay, AnimationCurve speed)
        {
            ActivationCurve = activationDelay;
            SpeedCurve = speed;
        }

        /// <summary>
        /// Update the animation and its current state.
        /// </summary>
        /// <param name="curveTime">The time (x coordinate) on the curve.</param>
        /// <param name="deltaTime">The time elapsed since the last update.</param>
        public void Update(float curveTime, float deltaTime)
        {
            if (Animation.isPlaying)
            {
                _waitDuration = 0f;
                return;
            }

            // Ensure we don't get odd values.
            curveTime = Mathf.Clamp01(curveTime);
            _waitDuration += deltaTime;
            ActivationDelay = ActivationCurve.Evaluate(curveTime);
            // Do not allow values that are too low.
            Speed = Mathf.Max(SpeedCurve.Evaluate(curveTime), 0.5f);
            State.speed = Speed;
            
            if (ActivationDelay >= 0 && _waitDuration >= ActivationDelay)
            {
                _waitDuration = 0f;
                // Telling it to play when it is already playing is entirely fine and has no side effects.
                Animation.Play();
            }
            else
            {
                Animation.Stop();
            }
        }
    }
}