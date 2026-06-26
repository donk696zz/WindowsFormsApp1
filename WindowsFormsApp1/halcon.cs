using OpenCvSharp;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 历史名称保留为halcon，内部已经完全改为PictureBox/OpenCV显示。
    /// 保留类名可避免其他窗体重新排版。
    /// </summary>
    public partial class halcon : UserControl
    {
        public halcon()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        public void SetImage(Mat image)
        {
            if (image == null || image.Empty())
                return;
            SetImage(OpenCvImageHelper.ConvertMatToBitmap(image));
        }

        public void SetImage(Bitmap bitmap)
        {
            if (bitmap == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<Bitmap>(SetImage), bitmap);
                return;
            }

            Image oldImage = pictureBox1.Image;
            pictureBox1.Image = bitmap;
            oldImage?.Dispose();
        }

        public void ClearDisplay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ClearDisplay));
                return;
            }

            Image oldImage = pictureBox1.Image;
            pictureBox1.Image = null;
            oldImage?.Dispose();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            ClearDisplay();
            base.OnHandleDestroyed(e);
        }
    }
}
