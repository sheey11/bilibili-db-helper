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
using DrawHelper;

namespace BilibiliDrawBoardHelper {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private Bitmap SourceBitmap;
        private string ImageFilePath = "";
        private HeartBeatConnection heartbeat = new HeartBeatConnection();
        private ColorPalettle palettle = new ColorPalettle();


        private void RefreshImage() {
            SourceBitmap = BiliBoard.GetBoardImage();
            var img = BiliBoard.BitmapToBitmapImage(SourceBitmap);
            this.Dispatcher.Invoke(() => previewImg.Source = img);
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

        private void drawBtn_Click(object sender, RoutedEventArgs e) {
            // 停止按钮
            try {
                if (DrawHelper.DrawHelper.IsDrawing) {
                    if (DrawHelper.DrawHelper.StopDrawing()) {
                        drawBtn.Content = "开始画吧";
                        return;
                    }
                    else {
                        System.Windows.Forms.MessageBox.Show("好像出现了什么错误");
                        return;
                    }
                }

                // 判断输入内容
                var cookie = GetCookieStr();
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

                if (settings.StartX > 1280 || settings.StartX < 0 || settings.StartY > 720 || settings.StartY < 0) {
                    System.Windows.Forms.MessageBox.Show("坐标越界了.");
                    return;
                }

                // 添加Callback
                settings.Finished = new Action<int, int>((x, y) => this.Dispatcher.Invoke(() => {
                    Log(string.Format("绘制到点({0}, {1}), 已完成绘制.\n", new object[] { x, y }));
                    //stateBar.Text = "已完成.";
                    drawBtn.Content = "开始画吧";
                }));
                settings.Started = new Action(() => this.Dispatcher.Invoke(() => {
                    Log("开始绘画.\n");
                    //stateBar.Text = "开始绘画.";
                }));
                settings.DrawPixelCallback = new Action<bool, bool, string, int>((isSuccess, isStop, message, i) => this.Dispatcher.Invoke(() => {
                    if (isSuccess) {
                        Log(string.Format("成功绘制第{0}个像素, ", i));
                        //stateBar.Text = string.Format("成功绘制第{0}个像素, ", i);
                    }
                    else {
                        Log(string.Format("绘制第{0}个像素失败, ", i));
                        //stateBar.Text = string.Format("绘制第{0}个像素失败, ", i);
                    }

                    if (!isStop)
                        Log(message);
                    else {
                        Log(string.Format("由于错误, 已停止绘制, 错误信息: {0}\n", message));
                        //stateBar.Text = "由于错误, 已停止绘制";
                        System.Windows.Forms.MessageBox.Show(string.Format("由于错误, 已停止绘制, 错误信息: {0}\n", message));
                    }
                }));
                if (DrawHelper.DrawHelper.DrawWithNewThread(settings))
                    drawBtn.Content = "停止";
                else
                    System.Windows.Forms.MessageBox.Show("好像出现了什么错误");

            }
            catch (Exception ex) {
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

        private void previewBtn_Click(object sender, RoutedEventArgs e) {
            // 判断输入内容
            int imgX, imgY, sX, sY;
            if (ImageFilePath == "" || !File.Exists(ImageFilePath)) {
                System.Windows.Forms.MessageBox.Show("Image please");
                return;
            }
            try {
                imgX = Convert.ToInt32(imgStartXTBox.Text);
                imgY = Convert.ToInt32(imgStartYTBox.Text);
                sX = Convert.ToInt32(startXTBox.Text);
                sY = Convert.ToInt32(startYTBox.Text);
            }
            catch {
                System.Windows.Forms.MessageBox.Show("检查坐标是否为整数.");
                return;
            }
            // ====================
            var img = Bitmap.FromFile(ImageFilePath);

            var bitmap = BiliBoard.GetBoardImage();

            int clipX = sX - 10, clipY = sY - 10;
            int clipWidth = img.Width + 20, clipHeight = img.Height + 20;
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
            writer.Dispose();
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
            RefreshImage();

            heartbeat.OnDrawUpdate += OnDrawUpdate;
        }

        private void OnDrawUpdate(object sender, DrawUpdateEventArgs e) {
            var color = palettle.ConvertToColor(e.Color);
            SourceBitmap.SetPixel(e.X, e.Y, color);
            var img = BiliBoard.BitmapToBitmapImage(SourceBitmap);
            this.Dispatcher.Invoke(() => previewImg.Source = img);
        }

        private void PreviewDraw(Bitmap img, Bitmap drawImg, int startX, int startY, int width, int height) {
            var bmp = new Bitmap(width, height);
            int maxX = startX + width, maxY = startY + height;

            var palettle = new ColorPalettle();

            for (var x = startX; x <= maxX; x++) {
                for (var y = startY; y < maxY; y++) {
                    // 原画 or 图片
                    var isInPictureRange = x - startX - width > 0 && y - startY - height > 0;
                    var flag = isInPictureRange ? palettle.ConvertToFlag(drawImg.GetPixel(x - startX - width, y - startY - height)) : "";
                    var isDrawable = flag == "" ? false : true;

                    if (isDrawable) {
                        
                    }
                }
            }
        }
    }
}
