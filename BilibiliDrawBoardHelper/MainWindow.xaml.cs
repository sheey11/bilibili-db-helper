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

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            RefreshImageAsync();
            logBox.Text += "\n";
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

        private void drawBtn_Click(object sender, RoutedEventArgs e) {
            if(cookieTBox.Text == "") {
                System.Windows.Forms.MessageBox.Show("Cookie please");
                return;
            }
            if (ImageFilePath == "" || !File.Exists(ImageFilePath)) {
                System.Windows.Forms.MessageBox.Show("Image please");
                return;
            }

            var settings = new DrawHelper.DrawSettings();
            settings.Cookie = cookieTBox.Text;
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
            settings.Finished = new Action<int, int>((x, y) => this.Dispatcher.Invoke(() => logBox.Text += string.Format("绘制到点({x}, {y}), 已完成绘制.\n")));
            settings.Started = new Action(() => this.Dispatcher.Invoke(() => logBox.Text += "开始绘画.\n"));
            settings.DrawPixelCallback = new Action<bool, bool, string, int>((isSuccess, isStop, message, i) => this.Dispatcher.Invoke(() => {
                if (isSuccess) 
                    logBox.Text += string.Format("成功绘制第{i}个像素, ");
                else
                    logBox.Text += string.Format("绘制第{i}个像素失败, ");
                if (isStop)
                    logBox.Text += message;
                else
                    logBox.Text += string.Format("由于错误, 已停止绘制, 错误信息: {message}\n");
            }));
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
    }
}
