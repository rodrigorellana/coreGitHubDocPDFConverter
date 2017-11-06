using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HttpWebRequestExtensions
    {
        private static bool RemoteFileExists(string url, out string realUrl)
        {
            realUrl = string.Empty;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";

                var a = (HttpWebResponse)request.GetResponseAsync().Result;
                var success = a.StatusCode == HttpStatusCode.OK;

                realUrl = a.ResponseUri.AbsoluteUri;
                return success;
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }


        public static Task<string> MakeAsyncRequest(
            string url,
            string contentType,
            out string realURL)
        {
            realURL = string.Empty;
            try
            {
                string realUrlForGooglUrls;
                bool error = false;

                if (!RemoteFileExists(url, out realUrlForGooglUrls))
                {
                    error = true;
                    if (url.StartsWith("https://github.com") && url.EndsWith(".md"))
                    {
                        url = url.Replace(".md", "");
                        error = !RemoteFileExists(url, out realUrlForGooglUrls);
                    }

                    if (error)
                    {
                        Task<string> task2 = Task<string>.Factory.StartNew(() =>
                        {
                            return string.Empty;
                        });
                        return task2;
                    }
                }

                if (url.StartsWith("http://goo.gl/"))
                {
                    url = realUrlForGooglUrls + ".md";
                    realURL = url;
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = contentType;
                request.Method = "GET";
                request.ContinueTimeout = 2000;
                request.Proxy = null;

                Task<WebResponse> task = Task.Factory.FromAsync(
                    request.BeginGetResponse,
                    asyncResult => request.EndGetResponse(asyncResult),
                    (object)null);

                Console.WriteLine("\t URL: " + url);
                return task.ContinueWith(t => ReadStreamFromResponse(t.Result));
            }
            catch (Exception ex)
            {
                Task<string> task2 = Task<string>.Factory.StartNew(() =>
                {
                    return string.Empty;
                });

                return task2;
            }
        }

        private static string ReadStreamFromResponse(WebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            {

                // string Charset = responseStream.CharacterSet;
                // Encoding encoding = Encoding.GetEncoding(Charset);
                // using (StreamReader sr = new StreamReader(responseStream, System.Text.Encoding.UTF8))
                // {
                //     //Need to return this response 
                //     string strContent = sr.ReadToEnd();
                //     return strContent;
                // }

                using (StreamReader sr = new StreamReader(responseStream))
                {
                    //Need to return this response 
                    string strContent = sr.ReadToEnd();
                    return strContent;
                }
            }
        }


        public static Task<Stream> MakeAsyncRequestStream(string url, string contentType)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = contentType;
            request.Method = "GET";
            request.ContinueTimeout = 2000;
            request.Proxy = null;

            Task<WebResponse> task = Task.Factory.FromAsync(
                request.BeginGetResponse,
                asyncResult => request.EndGetResponse(asyncResult),
                (object)null);

            return task.ContinueWith(t => GetStreamFromResponse(t.Result));
        }
        private static Stream GetStreamFromResponse(WebResponse response)
        {
            return response.GetResponseStream();
        }



    }
}