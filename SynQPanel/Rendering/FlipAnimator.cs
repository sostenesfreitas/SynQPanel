using System;

namespace SynQPanel.Rendering
{
    public sealed class FlipAnimator
    {
        private float _progress;
        private bool _animating;
        private DateTime _startTime;

        // REMOVED: private const float Duration = 0.6f;

        public void Trigger()
        {
            _animating = true;
            _progress = 0f;
            _startTime = DateTime.UtcNow;
        }

        // UPDATED: Accept duration from the item settings
        public float Update(float durationSeconds)
        {
            if (!_animating)
                return 1f; // fully settled

            // Safety check to prevent divide by zero
            if (durationSeconds <= 0.05f) durationSeconds = 0.05f;

            float elapsed = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

            _progress = elapsed / durationSeconds;

            if (_progress >= 1f)
            {
                _progress = 1f;
                _animating = false;
            }

            return _progress;
        }

        public bool IsAnimating => _animating;
    }
}
