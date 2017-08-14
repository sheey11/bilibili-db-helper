using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Net;
using System.IO;
using static BilibiliDrawBoardHelper.Logging;
using System.Text.RegularExpressions;
using System.Xml;

namespace BilibiliDrawBoardHelper {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private string ImageFilePath = "";
        private const string GET_IMAGE_URL = @"http://api.live.bilibili.com/activity/v1/SummerDraw/bitmap";

        private void drawBtn_Click(object sender, RoutedEventArgs e) {
            // 停止按钮
            try {
                if (DrawHelper.DrawHelper.IsDrawing) {
                    if (DrawHelper.DrawHelper.StopDrawing())
                        drawBtn.Content = "开始画吧";
                    else {
                        System.Windows.Forms.MessageBox.Show("好像出现了什么错误");
                        return;
                    }
                }

                // 判断输入内容
                var cookie = GetCookieStr();
                if(cookie == "") {
                    System.Windows.Forms.MessageBox.Show("检查cookie");
                    return;
                }
                
                if (cookie == "") {
                    System.Windows.Forms.MessageBox.Show("Cookie please");
                    return;
                }
                if (ImageFilePath == "" || !File.Exists(ImageFilePath)) {
                    System.Windows.Forms.MessageBox.Show("Image please");
                    return;
                }

                var settings = new DrawHelper.DrawSettings();
                settings.Cookie = cookie;
                settings.ImagePath = ImageFilePath;
                try {
                    settings.ImageStartX = Convert.ToInt32(imgStartXTBox.Text);
                    settings.ImageStartY = Convert.ToInt32(imgStartYTBox.Text);
                    settings.StartX = Convert.ToInt32(startXTBox.Text);
                    settings.StartY = Convert.ToInt32(startYTBox.Text);
                }
                catch {
                    System.Windows.Forms.MessageBox.Show("检查坐标是否为整数.");
                    return;
                }

                if(settings.StartX > 1280 || settings.StartX<0 || settings.StartY>720 || settings.StartY < 0) {
                    System.Windows.Forms.MessageBox.Show("坐标越界了.");
                    return;
                }

                // 检查Cookie
                //var r = Regex.Match(cookieTBox.Text, @"(.+?=.+?;\s{0,2}){3,}(.+?=.+)").Value == cookieTBox.Text;
                //if (!r) {
                //    System.Windows.Forms.MessageBox.Show("检查cookie");
                //    return;
                //}

                // 添加Callback
                settings.Finished = new Action<int, int>((x, y) => this.Dispatcher.Invoke(() => {
                    Log(string.Format("绘制到点({0}, {1}), 已完成绘制.\n", new object[] { x, y }));
                    stateBar.Text = "已完成.";
                    drawBtn.Content = "开始画吧";
                }));
                settings.Started = new Action(() => this.Dispatcher.Invoke(() => {
                    Log("开始绘画.\n");
                    stateBar.Text = "开始绘画.";
                }));
                settings.DrawPixelCallback = new Action<bool, bool, string, int>((isSuccess, isStop, message, i) => this.Dispatcher.Invoke(() => {
                    if (isSuccess) {
                        Log(string.Format("成功绘制第{0}个像素, ", i));
                        stateBar.Text = string.Format("成功绘制第{0}个像素, ", i);
                    }
                    else {
                        Log(string.Format("绘制第{0}个像素失败, ", i));
                        stateBar.Text = string.Format("绘制第{0}个像素失败, ", i);
                    }

                    if (!isStop)
                        Log(message);
                    else {
                        Log(string.Format("由于错误, 已停止绘制, 错误信息: {0}\n", message));
                        stateBar.Text = "由于错误, 已停止绘制";
                        System.Windows.Forms.MessageBox.Show(string.Format("由于错误, 已停止绘制, 错误信息: {0}\n", message));
                    }
                }));
                if (DrawHelper.DrawHelper.DrawWithNewThread(settings))
                    drawBtn.Content = "停止";
                else
                    System.Windows.Forms.MessageBox.Show("好像出现了什么错误");
                
            }
            catch(Exception ex) {
                Log(ex.ToString());
                System.Windows.Forms.MessageBox.Show("好像出现了什么奇怪的错误, 详情请检查日志.");
            }
        }

        private void openImageBtn_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.DefaultExt = ".png";
            ofd.Filter = "PNG File|*.png";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == true) {
                ImageFilePath = ofd.FileName;
                openImageTBlock.Text = ofd.FileName.Split('\\').Last();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            RefreshImageAsync();
        }

        public string HttpGet(string Url) {
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
        private BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bitmap) {
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
        private void RefreshImage() {
            var palettle = new DrawHelper.ColorPalettle();
            var imageData = HttpGet(GET_IMAGE_URL);
            // Json是啥? NewtonSoft又是啥?
            imageData = imageData.Replace(@"{""code"":0,""msg"":""success"",""message"":""success"",""data"":{""bitmap"":""", "").Replace(@"""}}", "");
            var count = imageData.Count();
            Bitmap img = new Bitmap(1280, 720);
            for (int i = 0; i < count; i++) {
                var x = i % 1280;
                var y = (i - x) / 1280;
                img.SetPixel(x, y, palettle.ConvertToColor(imageData[i].ToString()));
            }
            var sourceImg = BitmapToBitmapImage(img);
            this.Dispatcher.Invoke(() => previewImg.Source = sourceImg);
        }
        private async void RefreshImageAsync() {
            await Task.Run(() => RefreshImage());
        }

        private void saveImageBtn_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.DefaultExt = ".png";
            sfd.Filter = "PNG File|*.png";
            sfd.FileName = "233.png";
            sfd.AddExtension = true;
            if (sfd.ShowDialog() == true) {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapImage)previewImg.Source));
                using (var fs = new FileStream(sfd.FileName, System.IO.FileMode.Create)) {
                    encoder.Save(fs);
                }
            }
        }
        
        private string GetCookieStr() {
            if (DedeuserID.Text == "" || DedeuserID__ckMd5.Text == "" || SESSDATA.Text == "")
                return "";
            return string.Format("DedeUserID={0}; DedeUserID__ckMd5={1}; SESSDATA={2}", new object[] { DedeuserID.Text, DedeuserID__ckMd5.Text, SESSDATA.Text });
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            var writer = XmlWriter.Create("config.xml");
            writer.WriteStartElement("settings");
            writer.WriteElementString("imagePath", ImageFilePath);
            writer.WriteElementString("startX", startXTBox.Text);
            writer.WriteElementString("startY", startYTBox.Text);
            writer.WriteElementString("imgStartX", imgStartXTBox.Text);
            writer.WriteElementString("imgStartY", imgStartYTBox.Text);
            writer.WriteElementString("DedeUserID", DedeuserID.Text);
            writer.WriteElementString("DedeUserID__ckMd5", DedeuserID__ckMd5.Text);
            writer.WriteElementString("SESSDATA", SESSDATA.Text);
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (File.Exists("config.xml")) {
                var doc = new XmlDocument();
                doc.Load("config.xml");
                var rootNode = doc.SelectSingleNode("settings");
                foreach(XmlNode node in rootNode.ChildNodes) {
                    switch (node.Name) {
                        case "imagePath":
                            if (!File.Exists(node.InnerText)) continue;
                            ImageFilePath = node.InnerText;
                            openImageTBlock.Text = node.InnerText.Split('\\').Last();
                            break;
                        case "startX":
                            startXTBox.Text = node.InnerText;
                            break;
                        case "startY":
                            startYTBox.Text = node.InnerText;
                            break;
                        case "imgStartX":
                            imgStartXTBox.Text = node.InnerText;
                            break;
                        case "imgStartY":
                            imgStartYTBox.Text = node.InnerText;
                            break;
                        case "DedeUserID":
                            DedeuserID.Text = node.InnerText;
                            break;
                        case "DedeUserID__ckMd5":
                            DedeuserID__ckMd5.Text = node.InnerText;
                            break;
                        case "SESSDATA":
                            SESSDATA.Text = node.InnerText;
                            break;
                    }
                }
            }

            RefreshImageAsync();
        }
    }
}
