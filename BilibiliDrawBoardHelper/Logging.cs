using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BilibiliDrawBoardHelper
{
    public static class Logging
    {
        private static StreamWriter logFile = new StreamWriter("draw_log.log");
        public static void Log(string log) {
            logFile.WriteLine(log);
            logFile.Flush();
        }
    }
}
