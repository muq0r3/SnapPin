using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SnapPin
{
    internal static class HotkeyParser
    {
        public static bool TryParse(string text, out uint modifiers, out Keys key)
        {
            modifiers = 0;
            key = Keys.None;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var resultKey = Keys.None;
            var parts = text.Split('+');
            foreach (string raw in parts)
            {
                string part = raw.Trim();
                if (part.Length == 0)
                {
                    return false;
                }

                switch (part.ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= NativeMethods.MOD_CONTROL;
                        continue;
                    case "alt":
                        modifiers |= NativeMethods.MOD_ALT;
                        continue;
                    case "shift":
                        modifiers |= NativeMethods.MOD_SHIFT;
                        continue;
                    case "win":
                    case "windows":
                        modifiers |= NativeMethods.MOD_WIN;
                        continue;
                }

                if (resultKey != Keys.None)
                {
                    return false;
                }

                if (!TryParseKey(part, out resultKey))
                {
                    return false;
                }
            }

            if (resultKey == Keys.None)
            {
                return false;
            }

            key = resultKey;
            return true;
        }

        private static bool TryParseKey(string text, out Keys key)
        {
            if (text.Length == 1 && text[0] >= '0' && text[0] <= '9')
            {
                key = (Keys)((int)Keys.D0 + (text[0] - '0'));
                return true;
            }

            if (text.Length == 1 && text[0] >= 'A' && text[0] <= 'Z')
            {
                key = (Keys)text[0];
                return true;
            }

            switch (text.ToLowerInvariant())
            {
                case "esc":
                case "escape":
                    key = Keys.Escape;
                    return true;
                case "backspace":
                    key = Keys.Back;
                    return true;
                case "enter":
                case "return":
                    key = Keys.Enter;
                    return true;
                case "space":
                case "spacebar":
                    key = Keys.Space;
                    return true;
                case "tab":
                    key = Keys.Tab;
                    return true;
            }

            return Enum.TryParse(text, true, out key);
        }

        public static string Format(uint modifiers, Keys key)
        {
            var parts = new List<string>();
            if ((modifiers & NativeMethods.MOD_CONTROL) != 0)
            {
                parts.Add("Ctrl");
            }
            if ((modifiers & NativeMethods.MOD_ALT) != 0)
            {
                parts.Add("Alt");
            }
            if ((modifiers & NativeMethods.MOD_SHIFT) != 0)
            {
                parts.Add("Shift");
            }
            if ((modifiers & NativeMethods.MOD_WIN) != 0)
            {
                parts.Add("Win");
            }
            parts.Add(KeyToString(key));
            return string.Join("+", parts);
        }

        public static string KeyToString(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((int)(key - Keys.D0)).ToString();
            }
            if (key >= Keys.A && key <= Keys.Z)
            {
                return ((char)key).ToString();
            }

            switch (key)
            {
                case Keys.Space:
                    return "Space";
                case Keys.Escape:
                    return "Esc";
                case Keys.Enter:
                    return "Enter";
                case Keys.Back:
                    return "Backspace";
                case Keys.Tab:
                    return "Tab";
                default:
                    return key.ToString();
            }
        }
    }
}
