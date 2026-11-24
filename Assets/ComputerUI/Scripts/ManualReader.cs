using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    public sealed class ManualReader : MonoBehaviour
    {
        public ManualRuleTable table;
        public RectTransform tabList;
        public Button tabTemplate, previous, next;
        public Text pageTitle, pageNumber, introduction;
        public RectTransform content, rowTemplate;
        public ScrollRect scroll;
        int section, page;
        Button[] tabs;

        void Awake()
        {
            previous.onClick.AddListener(() => Show(section, page - 1));
            next.onClick.AddListener(() => Show(section, page + 1));
            int count = table && table.sections != null ? table.sections.Length : 0;
            tabs = new Button[count];
            tabList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, count * 40);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button tab = Instantiate(tabTemplate, tabList);
                tab.gameObject.SetActive(true);
                ((RectTransform)tab.transform).anchoredPosition = new Vector2(0, -40 * i);
                tab.GetComponentInChildren<Text>().text = table.sections[i].tab;
                tab.onClick.AddListener(() => Show(index, 0));
                tabs[i] = tab;
            }
            Show(0, 0);
        }

        public void Show(int selected, int requestedPage)
        {
            foreach (RectTransform child in content)
            {
                if (child == introduction.rectTransform || child == rowTemplate) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            section = Mathf.Clamp(selected, 0, Mathf.Max(0, tabs.Length - 1));
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].image.color = i == section ? new Color32(36, 72, 132, 255) : new Color32(219, 226, 227, 255);
                tabs[i].GetComponentInChildren<Text>().color = i == section ? Color.white : new Color32(30, 48, 63, 255);
            }

            var pages = tabs.Length > 0 ? table.sections[section].pages : null;
            int count = pages?.Length ?? 0;
            page = Mathf.Clamp(requestedPage, 0, Mathf.Max(0, count - 1));
            var entry = count > 0 ? pages[page] : null;
            pageTitle.text = entry?.title ?? "No entries";
            pageNumber.text = count > 0 ? $"Page {page + 1} of {count}" : "0 pages";
            previous.interactable = page > 0;
            next.interactable = page + 1 < count;
            introduction.text = entry?.introduction ?? "";
            float y = Mathf.Ceil(introduction.preferredHeight) + 12;
            introduction.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
            if (entry?.rules != null)
            {
                foreach (var rule in entry.rules)
                {
                    var row = Instantiate(rowTemplate, content);
                    row.gameObject.SetActive(true);
                    row.anchoredPosition = new Vector2(0, -y);
                    var labels = row.GetComponentsInChildren<Text>();
                    labels[0].text = rule.condition;
                    labels[1].text = rule.response;
                    float height = Mathf.Max(42, Mathf.Ceil(Mathf.Max(labels[0].preferredHeight, labels[1].preferredHeight)) + 14);
                    foreach (var label in labels)
                        label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 10);
                    row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    y += height + 2;
                }
            }
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
            content.anchoredPosition = Vector2.zero;
        }
    }
}
