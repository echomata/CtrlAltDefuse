using UnityEngine;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class BlinkingDesktopTarget
    {
        const float DefaultHiddenSeconds = .3f;
        readonly RectTransform target;
        readonly float visibleSeconds, hiddenSeconds;
        float remaining, horizontalFraction;

        public BlinkingDesktopTarget(RectTransform target, float visibleSeconds = .5f, float hiddenSeconds = DefaultHiddenSeconds)
        {
            this.target = target;
            this.visibleSeconds = visibleSeconds;
            this.hiddenSeconds = hiddenSeconds;
        }

        public void Show()
        {
            horizontalFraction = Random.value;
            Reveal();
        }

        public void Tick(float deltaTime)
        {
            remaining -= deltaTime;
            if (remaining > 0) return;
            if (target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(false);
                remaining = hiddenSeconds;
                return;
            }

            // Jump by at least a quarter of the available horizontal travel.
            horizontalFraction = Mathf.Repeat(horizontalFraction + Random.Range(.25f, .75f), 1f);
            Reveal();
        }

        void Reveal()
        {
            Rect area = ((RectTransform)target.parent).rect;
            Rect bounds = target.rect;
            const float inset = 6f;
            target.localPosition = new Vector3(
                Mathf.Lerp(area.xMin - bounds.xMin + inset, area.xMax - bounds.xMax - inset, horizontalFraction),
                Random.Range(area.yMin - bounds.yMin + inset, area.yMax - bounds.yMax - inset));
            target.gameObject.SetActive(true);
            remaining = visibleSeconds;
        }
    }
}
