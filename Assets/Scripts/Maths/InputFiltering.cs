using Assets.Framework.Maths;
using Framework.Maths;
using UnityEngine;

namespace Assets.Scripts.Maths
{
    public static class InputFiltering
    {
        const float circularPower = 2f;

        /// <summary>
        /// Filters input to create a smooth rampup in magnitude post-deadzone.
        /// Clamps magnitude to unit circle.
        /// Remember to remove Unity's deadzones.
        /// </summary>
        public static Vector2 ApplyDeadzone(Vector2 raw, float inner, float outer)
        {
            if (raw.magnitude < inner)
            {
                return Vector2.zero;
            }
            else if (raw.magnitude >= outer)
            {
                return raw.normalized;
            }

            // After cutting out the deadzones, we scale the passed-through input to smoothly 
            // ramp from 0-1, rather than abruptly jumping from 0 to the first deadzone value.
            // Using a circular power increases the amount of smaller values the player can access.
            var scaledRadialMag = Utility.Map(raw.magnitude, inner, outer, 0f, 1f);
            var input = raw.normalized * Mathf.Pow(scaledRadialMag, circularPower);

            // Clamp to unit circle.
            return input.normalized * Mathf.Clamp01(input.magnitude);
        }

        /// <summary>
        /// Input clamped more 'intelligently', to preserve expected magnitudes at diagonal angles.
        /// This is not necessarily better (it probably is not), but may feel more natural.
        /// For more explanation see: http://ludopathic.co.uk/2012/03/06/clamping-excessive-magnitudes/
        /// Remember to remove Unity's deadzones.
        /// </summary>
        public static Vector2 ApplyDeadzoneCircularized(Vector2 raw, float inner, float outer)
        {
            var smoothed = ApplyDeadzone(raw, inner, outer);

            var circularizedInput = new Vector2(
                smoothed.x * Mathf.Sqrt(1.0f - (smoothed.y * smoothed.y / 2.0f)),
                smoothed.y * Mathf.Sqrt(1.0f - (smoothed.x * smoothed.x / 2.0f)));

            return circularizedInput;
        }
    }
}
