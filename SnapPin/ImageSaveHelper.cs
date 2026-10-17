using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapPin
{
    internal static class ImageSaveHelper
    {
        public static bool SaveWithDialog(Bitmap image, IWin32Window owner)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG 图片 (*.png)|*.png|JPEG 图片 (*.jpg)|*.jpg|位图 (*.bmp)|*.bmp";
                dialog.FileName = "SnapPin_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    try
                    {
                        image.Save(dialog.FileName);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "保存失败：" + ex.Message,
                            "SnapPin",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            return false;
        }
    }
}
