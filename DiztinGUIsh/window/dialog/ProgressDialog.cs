﻿using System.Windows.Forms;

namespace DiztinGUIsh.window.dialog
{
    public partial class ProgressDialog : Form
    {
        private readonly string overrideText;

        public ProgressDialog(bool marquee = false, string textOverride = null)
        {
            InitializeComponent();
            progressBar1.Value = 0;
            progressBar1.Maximum = 100;

            overrideText = textOverride;
            UpdateProgressText();

            if (marquee)
                progressBar1.Style = ProgressBarStyle.Marquee;
        }

        private void UpdateProgressText()
        {
            if (overrideText != null)
            {
                label2.Text = overrideText;
                return;
            }

            var percentDone = (int)(100 * ((float)progressBar1.Value / (float)progressBar1.Maximum));
            label2.Text = $@"{percentDone}%";
        }

        public void UpdateProgress(int i)
        {
            this.InvokeIfRequired(() =>
            {
                progressBar1.Value = i;
                UpdateProgressText();
            });
        }
    }
}

