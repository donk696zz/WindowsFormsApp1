using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    internal static class ResponsiveLayoutService
    {
        public static void ConfigureMainWindow(Form form, SplitContainer mainContainer)
        {
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimumSize = new Size(900, 560);
            form.WindowState = FormWindowState.Maximized;
            mainContainer.FixedPanel = FixedPanel.Panel2;
            mainContainer.Panel2MinSize = 220;
            mainContainer.SplitterWidth = 5;

            EventHandler resize = delegate
            {
                int rightWidth = Math.Max(220, Math.Min(300, form.ClientSize.Width / 5));
                SetRightPanelWidth(mainContainer, rightWidth);
            };
            form.Resize += resize;
            form.Shown += resize;
            resize(form, EventArgs.Empty);
        }

        private static void SetRightPanelWidth(SplitContainer split, int rightWidth)
        {
            int target = split.ClientSize.Width - rightWidth - split.SplitterWidth;
            SetSplitterDistance(split, target);
        }

        private static void SetSplitterDistance(SplitContainer split, int target)
        {
            int maximum = split.ClientSize.Width - split.SplitterWidth - split.Panel2MinSize;
            int minimum = split.Panel1MinSize;
            if (maximum < minimum)
                return;

            target = Math.Max(minimum, Math.Min(maximum, target));
            if (split.SplitterDistance != target)
                split.SplitterDistance = target;
        }
    }
}
