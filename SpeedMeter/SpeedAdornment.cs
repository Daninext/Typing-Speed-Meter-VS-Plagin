using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpeedMeter
{
    internal sealed class SpeedAdornment
    {
        private const double AdornmentWidth = 160;
        private const double AdornmentHeight = 150;

        private readonly IWpfTextView view;
        private readonly SpeedForm form;
        private readonly IAdornmentLayer adornmentLayer;
        private DateTime start;

        private char? lastChar = null;
        private int totalWords = 0;
        private long totalChars = 0;

        static internal SpeedAdornment instance;

        public SpeedAdornment(IWpfTextView view)
        {
            instance = this;

            this.view = view ?? throw new ArgumentNullException("view");

            start = DateTime.UtcNow;
            form = new SpeedForm();

            this.adornmentLayer = view.GetAdornmentLayer("SpeedAdornment");

            view.ViewportHeightChanged += delegate { OnSizeChanged(); };
            view.ViewportWidthChanged += delegate { OnSizeChanged(); };
        }

        public string CharSpeed { get { if (totalChars == 0) return null; return ((int)(totalChars / DateTime.UtcNow.Subtract(start).TotalMinutes)).ToString(); } }
        public string WordSpeed { get { if (totalWords == 0) return null; return ((int)(totalWords / DateTime.UtcNow.Subtract(start).TotalMinutes)).ToString(); } }
        public string TotalWords { get { return totalWords.ToString(); } }

        public void UpdateValues(char typedChar)
        {
            if (lastChar == null)
                start = DateTime.UtcNow;

            ++totalChars;

            if (lastChar != null)
                if (char.IsWhiteSpace(typedChar) && char.IsLetter((char)lastChar))
                    ++totalWords;

            var interval = DateTime.UtcNow.Subtract(start).TotalMinutes;
            if (interval > 0.1f)
            {
                form.CSpeed.Content = "Char/min: " + CharSpeed;
                form.WSpeed.Content = "Word/min: " + WordSpeed;
            }

            lastChar = typedChar;
            form.WTotal.Content = "Total words: " + TotalWords;
        }

        private void OnSizeChanged()
        {
            this.adornmentLayer.RemoveAllAdornments();

            Canvas.SetLeft(this.form, this.view.ViewportRight - AdornmentWidth);
            Canvas.SetTop(this.form, this.view.ViewportBottom - AdornmentHeight);

            this.adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this.form, null);
        }
    }
}
