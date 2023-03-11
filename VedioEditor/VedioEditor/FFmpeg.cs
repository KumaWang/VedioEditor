using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VedioEditor
{
    static class FFmpeg
    {
        public static void Run(string cmd) 
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false; //必要参数
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg\\ffmpeg.exe";
            process.StartInfo.Arguments = cmd;
            process.Start();
            process.WaitForExit();//等待程序执行完退出进程    
            process.Close();
        }
    }
}
