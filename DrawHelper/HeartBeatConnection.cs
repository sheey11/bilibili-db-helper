using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using System.Text.RegularExpressions;

namespace DrawHelper {
    public class HeartBeatConnection {
        private static string HEARTBEAT_URL = @"ws://broadcastlv.chat.bilibili.com:2244/sub";
        private static string WEB_SOCKET_JSON = @"{""uid"":0,""roomid"":5446}";
        private static byte[] HEARTBEAT_DATA = { 0, 0, 0, 16, 0, 16, 0, 1, 0, 0, 0, 2, 0, 0, 0, 1 };

        public delegate void DrawUpdateEventHandler(object sender, DrawUpdateEventArgs e);
        public DrawUpdateEventHandler OnDrawUpdate;

        WebSocket ws = new WebSocket(HEARTBEAT_URL);

        public HeartBeatConnection() {
            ws.Connect();
            var firstData = Encoding.UTF8.GetBytes(WEB_SOCKET_JSON);

            var buffer = new byte[16];
            buffer[3] = (byte)(16 + firstData.Length);
            buffer[5] = 16;
            buffer[7] = 1;
            buffer[11] = 7;
            buffer[15] = 1;

            ws.Send(MergeData(buffer, firstData));
            ws.OnMessage += Ws_OnMessage;
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e) {
            var data = e.RawData;

            var a = data[3];
            var s = data[5];
            var code = data[11];

            switch (code) {
                case 8:
                    ws.Send(HEARTBEAT_DATA);
                    break;
                case 5:
                    string json = "";
                    for (int g = 0; g < data.Length; g += a) {
                        a = data[g + 3];
                        s = data[g + 5];
                        json = Encoding.UTF8.GetString(data.SubArray(g + s, a - s));
                        if (json.Contains(@"""cmd"":""DRAW_UPDATE""")) {
                            int x, y;
                            string color;
                            x = int.Parse(Regex.Match(json, @"""x_min"":(\d+)").Groups[1].Value);
                            y = int.Parse(Regex.Match(json, @"""y_min"":(\d+)").Groups[1].Value);
                            color = Regex.Match(json, @"""color"":""(.)").Groups[1].Value;

                            OnDrawUpdate?.Invoke(this, new DrawUpdateEventArgs(x, y, color));
                        }
                    }
                    break;
            }
        }
        
        private byte[] MergeData(byte[] a, byte[] b) {
            var buffer = new byte[a.Length + b.Length];
            a.CopyTo(buffer, 0);
            b.CopyTo(buffer, a.Length);
            return buffer;
        }
    }
    public class DrawUpdateEventArgs : EventArgs {
        public DrawUpdateEventArgs(int x ,int y, string color) {
            this.X = x;
            this.Y = y;
            this.Color = color;
        }
        public int X { get; }
        public int Y { get; }
        public string Color { get; }
    }
}
