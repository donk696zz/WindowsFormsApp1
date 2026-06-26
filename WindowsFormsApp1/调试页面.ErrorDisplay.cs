using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 调试页面
    {
        private Bitmap currentAnnotatedBitmap;
        private Bitmap currentErrorBitmap;
        private bool showingErrorBitmap;

        private void StoreInspectionImages(ModuleInspectionResult result)
        {
            DisposeCurrentInspectionImages();

            if (result.AnnotatedImage != null && !result.AnnotatedImage.Empty())
                currentAnnotatedBitmap = OpenCvImageHelper.ConvertMatToBitmap(result.AnnotatedImage);

            if (result.ErrorImage != null && !result.ErrorImage.Empty())
                currentErrorBitmap = OpenCvImageHelper.ConvertMatToBitmap(result.ErrorImage);

            showingErrorBitmap = false;
            showErrorButton.Enabled = result.ErrorRegions.Count > 0;
            showErrorButton.Text = "错误区域";
            ShowInspectionBitmap(false);
        }

        private void ShowInspectionBitmap(bool showError)
        {
            Bitmap bitmap = showError ? currentErrorBitmap : currentAnnotatedBitmap;
            if (bitmap == null)
                return;

            halcon1.SetImage(new Bitmap(bitmap));
        }

        private void showErrorButton_Click(object sender, EventArgs e)
        {
            if (currentErrorBitmap == null)
                return;

            showingErrorBitmap = !showingErrorBitmap;
            ShowInspectionBitmap(showingErrorBitmap);
            showErrorButton.Text = showingErrorBitmap ? "普通标注" : "错误区域";
        }

        private void DisposeCurrentInspectionImages()
        {
            currentAnnotatedBitmap?.Dispose();
            currentAnnotatedBitmap = null;
            currentErrorBitmap?.Dispose();
            currentErrorBitmap = null;
            showingErrorBitmap = false;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            DisposeCurrentInspectionImages();
            base.OnFormClosed(e);
        }
    }
}
