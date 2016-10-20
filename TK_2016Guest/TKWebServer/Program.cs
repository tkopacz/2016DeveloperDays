using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TKWebServer {
    class Program {
        static void Main(string[] args) {
            string environmentVariable = Environment.GetEnvironmentVariable("ComputerName");
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://+:8088/");
            httpListener.Start();
            while (true) {
                HttpListenerContext context = httpListener.GetContext();
                string str = context.Request.Url.ToString();
                using (context.Response) {
                    byte[] bytes = Encoding.UTF8.GetBytes($"Hello, working - {str}");
                    context.Response.ContentLength64 = (long)((int)bytes.Length);
                    context.Response.OutputStream.Write(bytes, 0, (int)bytes.Length);
                }
            }

        }
    }
}
