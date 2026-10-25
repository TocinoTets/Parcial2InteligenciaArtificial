using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Etiqueta de texto que cada agente dibuja sobre sí mismo en pantalla (IMGUI).
    /// Es únicamente FEEDBACK visual: no participa de la toma de decisiones ni expone
    /// información a otros agentes.
    /// </summary>
    public sealed class AgentLabel
    {
        private GUIStyle textStyle;
        private GUIStyle outlineStyle;
        private Color cachedColor;
        private int cachedFontSize = -1;

        public void Draw(Vector3 worldAnchor, string text, Color textColor, float width = 230f, float height = 140f, int fontSize = 11)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            Vector3 screen = camera.WorldToScreenPoint(worldAnchor);
            if (screen.z <= 0f) return;

            if (textStyle == null || cachedColor != textColor || cachedFontSize != fontSize)
            {
                textStyle = CreateStyle(fontSize, textColor);
                outlineStyle = CreateStyle(fontSize, new Color(0f, 0f, 0f, 0.85f));
                cachedColor = textColor;
                cachedFontSize = fontSize;
            }

            Rect rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y, width, height);

            // Contorno (mejora la lectura sobre el escenario).
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, outlineStyle);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, outlineStyle);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, outlineStyle);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, outlineStyle);

            GUI.Label(rect, text, textStyle);
        }

        private static GUIStyle CreateStyle(int fontSize, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false,
                richText = false,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            return style;
        }
    }
}
