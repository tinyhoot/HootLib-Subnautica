using System;

namespace HootLib.Objects
{
    /// <summary>
    /// A simple timer meant to facilitate running components at set intervals rather than every single frame.
    /// </summary>
    public class Hootimer
    {
        public float Interval;
        public Func<float> TimeProvider;
        private float _timePassed;
        
        /// <summary>
        /// Create a hooty timer.
        /// </summary>
        /// <param name="timerFunction">The function which provides time intervals in seconds, such as
        /// <see cref="PDA.GetDeltaTime()"/></param>
        /// <param name="secondsInterval">The interval in seconds between successful ticks.</param>
        public Hootimer(Func<float> timerFunction, float secondsInterval)
        {
            Interval = secondsInterval;
            TimeProvider = timerFunction;
        }

        /// <summary>
        /// Poll the <see cref="TimeProvider"/> and see whether the time <see cref="Interval"/> has passed. Resets
        /// automatically.
        /// </summary>
        /// <returns>True if the interval has been exceeded this tick, false if it has not.</returns>
        public bool Tick()
        {
            _timePassed += TimeProvider.Invoke();
            if (_timePassed < Interval)
                return false;
            // Reset and go again.
            _timePassed = 0f;
            return true;
        }
    }
}