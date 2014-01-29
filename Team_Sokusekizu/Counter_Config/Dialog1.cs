using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    class Dialog1
    {
        // 目標：画像ファイル保存先をダイアログで指定する．またその設定情報をconfigファイルに記録する
        // ファイル保存→保存先のファイル名(パス)を返す→configファイルに保存

        private FolderBrowserDialog ofd;
        private string path;

        public Dialog1()
        {            
            //FolderBrowserDialogクラスのインスタンスを作成
            this.ofd = new FolderBrowserDialog();

            this.path = "";
        }

        public string SavePictureDialog()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = "保存先選択";

            //ダイアログの表示
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                path = fbd.SelectedPath;
            }
            return path;

            
        }
    }
}