using System;
using UnityEngine;

namespace CtrlAltDefuse.Desktop
{
    [CreateAssetMenu(menuName = "Ctrl Alt Defuse/Manual Rule Table")]
    public sealed class ManualRuleTable : ScriptableObject
    {
        public Section[] sections = Array.Empty<Section>();

        [Serializable]
        public class Section
        {
            public string tab;
            public Page[] pages = Array.Empty<Page>();
        }

        [Serializable]
        public class Page
        {
            public string title;
            [TextArea] public string introduction;
            public Rule[] rules = Array.Empty<Rule>();
        }

        [Serializable]
        public class Rule
        {
            public string condition;
            [TextArea] public string response;
        }
    }
}
