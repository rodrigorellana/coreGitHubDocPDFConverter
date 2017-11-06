using System.IO;
using System.Net;
using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public class WebReq
    {
        public string GetBodyHTMLFrom(string url)
        {
            WebRequest reqq = WebRequest.Create(url);
            reqq.Method = "POST";
            Stream req = reqq.GetRequestStreamAsync().Result;
            StreamReader asd = new StreamReader(req);
            string body = asd.ReadToEndAsync().Result;
            return body;
        }

        public async Task<string> DoSomething()
        {
            string url = " https://raw.githubusercontent.com/mapsplugin/cordova-plugin-googlemaps/master/README.md";
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);

            var ws = await request.GetResponseAsync();

            return ws.ResponseUri.ToString(); ;
        }
    }
}