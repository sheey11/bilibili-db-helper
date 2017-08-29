using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using System.IO;
using System.Windows.Media.Imaging;

namespace BilibiliDrawBoardHelper {
    static class BiliBoard {

        private const string GET_IMAGE_URL = @"http://api.live.bilibili.com/activity/v1/SummerDraw/bitmap";

        public static Bitmap GetBoardImage() {
            var palettle = new DrawHelper.ColorPalettle();
            var imageData = SendDataByGet(GET_IMAGE_URL);
            // Json是啥? NewtonSoft又是啥?
            imageData = imageData.Replace(@"{""code"":0,""msg"":""success"",""message"":""success"",""data"":{""bitmap"":""", "").Replace(@"""}}", "");
            var count = imageData.Count();
            Bitmap img = new Bitmap(1280, 720);
            for (int i = 0; i < count; i++) {
                var x = i % 1280;
                var y = (i - x) / 1280;
                img.SetPixel(x, y, palettle.ConvertToColor(imageData[i].ToString()));
            }
            return img;
        }
        
        public static BitmapImage GetBoardBitmapImage() {
            return BitmapToBitmapImage(GetBoardImage());
        }

        public static string SendDataByGet(string Url) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }
        
        public static BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bitmap) {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream()) {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }
    }
}
