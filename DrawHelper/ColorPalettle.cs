using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;

namespace DrawHelper {
    public class ColorPalettle {
        private string colorData = "rgb(255, 193, 7): R;rgb(85, 85, 85): 3;rgb(0, 70, 112): E;rgb(255, 255, 255): 1;rgb(170, 170, 170): 2;rgb(33, 150, 243): G;rgb(226, 102, 158): A;rgb(0, 0, 0): 0;rgb(63, 81, 181): D;rgb(137, 230, 66): M;rgb(184, 63, 39): U;rgb(233, 30, 99): 9;rgb(255, 87, 34): T;rgb(244, 67, 54): 8;rgb(59, 229, 219): I;rgb(22, 115, 0): K;rgb(254, 211, 199): 4;rgb(250, 172, 142): 6;rgb(255, 152, 0): S;rgb(255, 139, 131): 7;rgb(55, 169, 60): L;rgb(156, 39, 176): B;rgb(248, 203, 140): P;rgb(5, 113, 151): F;rgb(255, 196, 206): 5;rgb(255, 246, 209): O;rgb(121, 85, 72): V;rgb(103, 58, 183): C;rgb(0, 188, 212): H;rgb(255, 235, 59): Q;rgb(215, 255, 7): N;rgb(151, 253, 220): J";
        private Dictionary<string, Color> palettle = new Dictionary<string, Color>();

        public ColorPalettle() {
            // 把 ColorData 转换成 Color 和它的 Flag 存入 Dictionary
            var eachColor = colorData.Split(';');
            foreach (string colorStr in eachColor) {
                var result = Regex.Match(colorStr, @"rgb\((\d+), (\d+), (\d+)\): (.)");
                var color = Color.FromArgb(255,
                    Convert.ToByte(result.Groups[1].Value),// 1, R
                    Convert.ToByte(result.Groups[2].Value),// 2, G
                    Convert.ToByte(result.Groups[3].Value));// 3, B
                var flag = result.Groups[4].Value;
                palettle.Add(flag, color);
            }
        }

        public string ConvertToFlag(Color color) {
            // 反向查找
            var r = from item in palettle where item.Value.R == color.R && item.Value.B == color.B && color.G == color.G select item;// 忽略Alpha通道
            if (r.Count() == 0) return "";
            return r.ToList()[0].Key;
        }

        public Color ConvertToColor(string flag) {
            return palettle[flag];
        }
    }
}
