using System;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;

namespace DrawHelper {
    public static class DrawHelper {
        private static DrawSettings settings { get; set; }
        private static ColorPalettle palettle = new ColorPalettle();
        private static Thread drawThread;
        public static bool IsDrawing {
            get {
                return drawThread != null && drawThread.IsAlive;
            }
        }

        const string DRAWBOARD_ACTION_DRAW_URL = "http://api.live.bilibili.com/activity/v1/SummerDraw/draw";

        public static bool StopDrawing() {
            if (drawThread == null || !drawThread.IsAlive) return true;
            try {
                drawThread.Abort();
                return true;
            }
            catch {
                return false;
            }
        }


        public static bool DrawWithNewThread(DrawSettings setting) {
            try {
                if (drawThread != null && drawThread.IsAlive) drawThread.Abort();
                drawThread = new Thread(() => Draw(setting));
                drawThread.Start();
                return true;
            }
            catch {
                return false;
            }
            //await Task.Run(() => Draw(setting));
        }
        public static void Draw(DrawSettings setting) {
            settings = setting;
            if (!File.Exists(settings.ImagePath)) {
                System.Windows.Forms.MessageBox.Show("找不到图片啦~");
                settings.Finished(0, 0);
                return;
            }
            // 读取 cookie
            //Console.WriteLine("开始初始化Cookie...");
            var cookies = new CookieContainer();
            var eachCookie = settings.Cookie.Split(';');
            foreach (string cookie in eachCookie) {
                var tmpCookie = cookie.Replace(" ", "");
                var cookieName = tmpCookie.Split('=')[0];
                var cookieValue = tmpCookie.Split('=')[1].Replace(",", "%2C");
                cookies.Add(new Cookie(cookieName, cookieValue, "/", @".bilibili.com"));
                //Console.WriteLine("正在添加Cookie: {0}", cookieName);
            }

            // 载入图片
            var bitmap = new Bitmap(settings.ImagePath);
            int x = settings.ImageStartX, y = settings.ImageStartY, i = 0;
            //Console.WriteLine("开始创作~");
            settings.Started();
            for (; y < bitmap.Height; y++) {
                for (; x < bitmap.Width; x++) {
                    var color = bitmap.GetPixel(x, y);
                    var colorFlag = palettle.ConvertToFlag(color);
                    if (colorFlag == "") continue;

                    i++;

                    var respone = SendDataByPost(DRAWBOARD_ACTION_DRAW_URL, GetPostData(x, y, colorFlag), ref cookies);
                    var waitTime = Regex.Match(respone, @"(?<=\{""time"":)\d+(?=\})").Value;
                    if (respone.Contains(@"""code"":-400"))
                        settings.DrawPixelCallback(false, false, "需要等待" + waitTime + "秒\n", i);
                    //Console.WriteLine("第{0}次绘画失败, 需要等待{1}秒~ x = {2},y = {3}", i, waitTime, x, y);
                    else if (respone.Contains(@"""code"":0"))
                        settings.DrawPixelCallback(true, false, string.Format("位置({0}, {1}), 开始等待180秒.\n", new object[] { x, y }), i);
                    //Console.WriteLine("第{0}次绘画成功, 开始等待{1}秒~ x = {2},y = {3}", i, waitTime, x, y);
                    else if (respone.Contains(@"""code"":-101")) {
                        settings.DrawPixelCallback(false, true, "未登录或未绑定手机.\n", i);
                        return;
                    }
                    else
                        settings.DrawPixelCallback(false, true, respone, i);

                    Thread.Sleep(Int32.Parse(waitTime) * 1000);
                }
                x = 0;
            }
            settings.Finished(x, y);
        }

        public static string GetPostData(int img_x, int img_y, string colorFlag) {
            return "x_min=" + (settings.StartX + img_x).ToString() +
                "&y_min=" + (settings.StartY + img_y).ToString() +
                "&x_max=" + (settings.StartX + img_x).ToString() +
                "&y_max=" + (settings.StartY + img_y).ToString() +
                "&color=" + colorFlag;
        }

        // copy from csdn blog
        /// <summary>
        /// 通过POST方式发送数据
        /// </summary>
        /// <param name="Url">url</param>
        /// <param name="postDataStr">Post数据</param>
        /// <param name="cookie">Cookie容器</param>
        /// <returns></returns>
        private static string SendDataByPost(string Url, string postDataStr, ref CookieContainer cookie) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            if (cookie.Count == 0) {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else {
                request.CookieContainer = cookie;
            }

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postDataStr.Length;
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }
    }
}
