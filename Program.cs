using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApplication
{
    public class Program
    {
        static int webCount = 0;
        private static List<string> alreadyDone = new List<string>();
        private static List<string> urlToProcess = new List<string>();
        private static List<string> noGitHubUrls = new List<string>();
        private static List<string> errorUrls = new List<string>();
        private static void OpenWebBrowser(string url)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.Arguments = url + " t ";
            start.FileName = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
            start.CreateNoWindow = true;
            int exitCode;

            using (Process proc = Process.Start(start))
            {
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
        }

        private static string getCorrectExtension(string text)
        {
            try
            {
                var limit = 500;
                if (text.Length < limit)
                    limit = 50;

                var phrase = text.Substring(0, limit);
                if (phrase.Contains("<html"))
                    return ".HTML";
                else if (phrase.Contains("<?xml"))
                    return ".XML";
                else
                    return ".MD";
            }
            catch (Exception)
            {
                return ".TXT";
            }
        }

        private static void addUrlError(string url)
        {
            if (url.StartsWith("https") || url.StartsWith("http"))
                if (!errorUrls.Contains(url))
                    errorUrls.Add(url);
        }

        private static void ExtractMDFilesFromGitHubRepositoriy(
            string githubRepositoryUrl,
            string pathSaveMDFiles,
            bool isTheFather = false)
        {
            var _uri = new Uri(githubRepositoryUrl);
            var totalSeg = _uri.Segments.Length;
            if (totalSeg <= 2)
            {  //se agregan a la lista errors algo que mo deveria ser error// 
            //.md.md  .md.md .md.md
                            if (githubRepositoryUrl.StartsWith("https://github.com"))
                    return;
            }

            var origin = githubRepositoryUrl;
            var path = pathSaveMDFiles;

            string rawMDUrl = "", owner = "", repo = string.Empty;
            var uriOrigin = new Uri(origin);

            if (isTheFather)
            {
                owner = uriOrigin.Segments[1].Replace("/", ""); ;
                repo = uriOrigin.Segments[2].Replace("/", ""); ;
                rawMDUrl = string.Format("https://raw.githubusercontent.com/{0}/{1}/master/README.md", owner, repo);
            }
            else
            {
                rawMDUrl = githubRepositoryUrl;
                if (githubRepositoryUrl.StartsWith("https://github.com"))
                {
                    if (!githubRepositoryUrl.EndsWith(".mb"))
                        rawMDUrl += ".md";
                }

            }

            if (!isTheFather && alreadyDone.Contains(rawMDUrl))
                return;
            //create the file name
            var fileName = getFileName(uriOrigin);

            //debug
            // rawMDUrl = "http://goo.gl/Eq2c2p";

            //get content from the internet
            string urlToGoogl = string.Empty;
            var task = HttpWebRequestExtensions.MakeAsyncRequest(rawMDUrl, "text/plain", out urlToGoogl);
            string fatherMDText = task.Result;

            if (string.IsNullOrEmpty(fatherMDText))
            {

                if (!errorUrls.Contains(rawMDUrl))
                    addUrlError(rawMDUrl);

                return;
            }

            if (!string.IsNullOrEmpty(urlToGoogl))
                fileName = getFileName(urlToGoogl);

            //concatenate the correct extension for file name
            fileName = fileName + getCorrectExtension(fatherMDText);

            if (fileName.EndsWith(".XML"))
                return;

            try
            { //create, write and save the file with the content fatherMDText
                File.WriteAllText(path + fileName, fatherMDText);
            }
            catch (System.Exception)
            {
                //avoid wrong characters on the file name, replace it with now.ticks
                var otherName = "_unknow" + DateTime.Now.Ticks.ToString();
                otherName = otherName + getCorrectExtension(fatherMDText);
                File.WriteAllText(path + otherName, fatherMDText);
            }

            if (!alreadyDone.Contains(rawMDUrl))
                alreadyDone.Add(rawMDUrl);

            //looping the content line by line           
            using (StringReader reader = new StringReader(fatherMDText))
            {
                int count = 0;
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    count++;
                    // Console.WriteLine("Line {0}: {1}", count, line);

                    //regular expresions to capture string URLs
                    var urlBetWeenParentesis = Regex.Match(line, @"\(([^)]*)\)").Groups[1].Value;
                    var brackets = Regex.Match(line, @"\[(.*?)\]").Groups[1].Value;

                    //regular expresions to capture HTML HREFs
                    var href = Regex.Match(line, "<(a|link).*?href=(\"|')(.+?)(\"|').*?>").Groups[3].Value;
                    if (!string.IsNullOrEmpty(href))
                    {
                        if (href.StartsWith("https://github.com"))
                        {
                            brackets = "some"; //to avoid next if sentence
                            urlBetWeenParentesis = href;
                        }
                    }

                    //avoid line if some rules are executed 
                    if (string.IsNullOrEmpty(brackets))
                        continue;
                    if (string.IsNullOrEmpty(urlBetWeenParentesis))
                        continue;
                    if (urlBetWeenParentesis.Contains("issues"))
                        continue;
                    if (urlBetWeenParentesis.Contains("commits"))
                        continue;

                    // process only github and goo.gl urls
                    if (urlBetWeenParentesis.StartsWith("https://github.com")
                        || urlBetWeenParentesis.StartsWith("http://goo.gl"))
                    {
                        if (urlBetWeenParentesis.Contains("pull")
                         || urlBetWeenParentesis.Contains("compare"))
                        {
                            continue;
                        }

                        webCount++;

                        if (!urlToProcess.Contains(urlBetWeenParentesis))
                            urlToProcess.Add(urlBetWeenParentesis);
                        else
                            continue;

                        // OpenWebBrowser(urlBetWeenParentesis);
                        // if (!urlBetWeenParentesis.StartsWith("http://goo.gl")
                        //     && urlBetWeenParentesis.EndsWith(".md"))
                        //     urlBetWeenParentesis += ".md";

                        ExtractMDFilesFromGitHubRepositoriy(urlBetWeenParentesis, pathSaveMDFiles, false); //recursion
                    }
                    else
                    {
                        if (urlBetWeenParentesis.StartsWith("https") || urlBetWeenParentesis.StartsWith("http"))
                            if (!noGitHubUrls.Contains(urlBetWeenParentesis))
                                noGitHubUrls.Add(urlBetWeenParentesis);
                    }
                }
            }

            File.WriteAllLines(path + "_errors.txt", errorUrls);
            File.WriteAllLines(path + "_niGitHubs.txt", noGitHubUrls);
            File.WriteAllLines(path + "_done.txt", alreadyDone);
        }

        private static string getFileName(string urlString)
        {
            var uri_ = new Uri(urlString);
            return getFileName(uri_);
        }
        private static string getFileName(Uri uriOrigin)
        {
            try
            {
                var totalSeg = uriOrigin.Segments.Length;
                var lastPart = uriOrigin.Segments[totalSeg - 1];
                var lastLastPart = uriOrigin.Segments[totalSeg - 2];
                var fileName = lastPart + "_" + lastLastPart.Remove(lastLastPart.Length - 1);
                return fileName;
            }
            catch (Exception ex)
            {
                return "_unknow";
            }
        }

        public static void Main(string[] args)
        {
            var path = @"C:\MDs\";
            var tmpList = File.ReadLines(path + "_done.txt");
            alreadyDone = new List<string>(tmpList);

            tmpList = File.ReadLines(path + "_niGitHubs.txt");
            noGitHubUrls = new List<string>(tmpList);

            tmpList = File.ReadLines(path + "_errors.txt");
            errorUrls = new List<string>(tmpList);

            List<string> urls = new List<string>();
            urls.Add("https://facebook.github.io/react/docs/installation.html");
          
            // urls.Add("https://github.com/EddyVerbruggen/cordova-plugin-3dtouch");

            foreach (string url in urls)
            {
                ExtractMDFilesFromGitHubRepositoriy(url, path, false);
            }

            Console.WriteLine("\tWebs created: " + webCount);
            Console.WriteLine("\tError URLs:");
            foreach (string url in errorUrls)
            {
                Console.WriteLine("\t\t" + url);
            }

            Console.WriteLine("");
            Console.WriteLine("\tNo GitHub URLs:");
            foreach (string url in noGitHubUrls)
            {
                Console.WriteLine("\t\t" + url);
            }
        }
    }
}
