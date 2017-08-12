using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DrawHelper {
    public class DrawSettings {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int ImageStartX { get; set; }
        public int ImageStartY { get; set; }
        public string ImagePath { get; set; }
        public string Cookie { get; set; }
        public Action Started { get; set; }
        /// <summary>
        /// 每次画一个像素时都会触发, 参数1为是否成功, 参数2为是否已停止, 参数3为消息, 参数4为已绘制的像素个数
        /// </summary>
        public Action<bool, bool, string, int> DrawPixelCallback { get; set; }
        /// <summary>
        /// x, y
        /// </summary>
        public Action<int, int> Finished { get; set; }

    }
}
