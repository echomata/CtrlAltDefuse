using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    internal sealed class WrongLanguagePopup
    {
        // The manual reader localizes rule content and navigation. These labels belong to the desktop prefab.
        static readonly Dictionary<string, string> Spanish = new()
        {
            { "Bomb Defusal Manual", "Manual de desactivación" },
            { "Field Manual", "Manual de campo" },
            { "CONTENTS", "ÍNDICE" },
            { "Scroll to read", "Desplázate para leer" },
            { "Module", "Módulo" },
            { "Condition", "Condición" },
            { "Response", "Respuesta" },
            { "Guard", "Control" },
            { "Pointer Guard", "Control del puntero" },
            { "Recalibrate", "Recalibrar" },
            { "Esc  ·  Leave desk", "Esc  ·  Salir" },
            { "Power", "Apagar" },
            { "Monitor in standby", "Monitor en reposo" },
            { "Wake monitor", "Activar monitor" }
        };

        readonly RelayDesktop desktop;
        readonly DesktopWindow window;
        readonly ManualReader reader;
        readonly Dictionary<Text, string> originals = new();
        public bool IsOpen => window.IsOpen;

        public WrongLanguagePopup(RelayDesktop desktop, Texture2D icon, System.Action reloaded)
        {
            this.desktop = desktop;
            reader = desktop.manual.GetComponentInChildren<ManualReader>(true);
            window = Object.Instantiate(desktop.guard, desktop.guard.transform.parent);
            window.name = "Wrong Language";
            var frame = (RectTransform)window.transform;
            window.titleBar.transform.Find("Pointer Guard").GetComponent<Text>().text = "RELAY | Idioma del sitio";
            window.titleBar.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            frame.Find(desktop.guardTitle.name).GetComponent<Text>().text = "Idioma cambiado";
            frame.Find(desktop.guardMessage.name).GetComponent<Text>().text =
                "Recarga el sitio para volver al idioma original.";

            var reload = frame.Find("Recalibrate").GetComponent<Button>();
            var label = reload.GetComponentInChildren<Text>(true);
            label.text = "Recargar";
            label.rectTransform.anchoredPosition += new Vector2(11, 0);
            label.rectTransform.sizeDelta -= new Vector2(20, 0);
            var symbol = new GameObject("Reload icon", typeof(RectTransform), typeof(RawImage));
            symbol.layer = reload.gameObject.layer;
            var image = symbol.GetComponent<RawImage>();
            image.texture = icon;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.SetParent(reload.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(17, 17);
            rect.anchoredPosition = new Vector2(5, 0);
            reload.onClick = new Button.ButtonClickedEvent();
            reload.onClick.AddListener(() =>
            {
                if (!IsOpen || desktop.Sleeping || !desktop.input.Engaged) return;
                window.Close();
                foreach (var pair in originals)
                    if (pair.Key) pair.Key.text = pair.Value;
                originals.Clear();
                desktop.manual.Open();
                reader.SetSpanish(false);
                reloaded();
            });
        }

        public void Show()
        {
            desktop.manual.Open();
            reader.SetSpanish(true);
            foreach (var label in desktop.GetComponentsInChildren<Text>(true))
            {
                var owner = label.GetComponentInParent<DesktopWindow>(true);
                if (owner && owner != desktop.manual && owner != desktop.guard) continue;
                if (!Spanish.TryGetValue(label.text, out string translation)) continue;
                originals.Add(label, label.text);
                label.text = translation;
            }
            window.Open();
        }

        public void KeepInFront()
        {
            var frame = window.transform;
            if (frame.GetSiblingIndex() != frame.parent.childCount - 1) window.Focus();
        }
    }
}
