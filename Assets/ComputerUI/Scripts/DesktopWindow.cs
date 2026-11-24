using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DesktopWindow : MonoBehaviour
    {
        public Image titleBar;
        public Color activeColor;
        public bool IsOpen { get; private set; }
        CanvasGroup group;
        float progress;

        public void Open()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            Apply();
            Focus();
        }

        public void Focus()
        {
            if (!IsOpen) return;
            transform.SetAsLastSibling();
            RefreshTitles();
        }

        public void Close()
        {
            IsOpen = false;
            Apply();
            RefreshTitles();
        }

        void RefreshTitles()
        {
            bool found = false;
            for (int i = transform.parent.childCount - 1; i >= 0; i--)
            {
                var w = transform.parent.GetChild(i).GetComponent<DesktopWindow>();
                if (!w) continue;
                w.titleBar.color = !found && w.IsOpen ? w.activeColor : new Color32(85, 105, 123, 255);
                found |= w.IsOpen;
            }
        }

        void Update()
        {
            float target = IsOpen ? 1 : 0;
            if (progress == target)
            {
                if (!IsOpen) gameObject.SetActive(false);
                return;
            }
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime * 7);
            Apply();
            if (!IsOpen && progress == 0) gameObject.SetActive(false);
        }

        void Apply()
        {
            if (!group) group = GetComponent<CanvasGroup>();
            group.alpha = progress;
            group.blocksRaycasts = IsOpen;
            group.interactable = IsOpen;
            transform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, progress);
        }
    }
}
