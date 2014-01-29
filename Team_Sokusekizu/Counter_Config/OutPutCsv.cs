using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    class OutPutCsv
    {
        public static void CSVSave(bool[] flag, int Nsum, int Asum,int no)
        {
            // 必要な変数を宣言する
            DateTime dt = DateTime.Now;

            // CSVファイルに保存
            // ファイルには分かりやすいように日付をつける
            using (StreamWriter sw = new StreamWriter("PcounterData" + no + "-" + dt.Year + "_" + dt.Month + "_" + dt.Day + ".csv", true))
            {
                // 日付出力
                sw.Write(dt.Year + "/" + dt.Month + "/" + dt.Day + ",");

                // 時刻出力
                sw.Write(dt.Hour + ":" + dt.Minute + ":" + dt.Second + ",");

                // 何番のskeletonが認識されたかをTrue＆Falseで出力
                for (int i = 0; i < flag.Length; i++)
                {
                    sw.Write(flag[i].ToString() + ",");
                }

                // 現在の認識人数と総認識人数を出力
                sw.Write(Nsum + "," + Asum + ",");
                sw.Write("\n");
                sw.Close();
            }
        }

        public static void config(string path, string filename)
        {
            // 必要な変数を宣言する
            DateTime dt = DateTime.Now;

            // configファイルの保存
            using (StreamWriter sw = new StreamWriter(@"config.csv"))
            {
                //sw.Write(dt.Year + "/" + dt.Month + "/" + dt.Day + ",");
                sw.WriteLine(path,filename);
                sw.Close();
            }
        }
    }
}
