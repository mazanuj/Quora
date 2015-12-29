using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using QuoraLib.DataTypes;
using xNet.Net;

namespace QuoraLib.Utilities
{
    public static class Utils
    {
        private static readonly Queue<CaptchaStruct> captchaQueue;

        public static IWebDriver WebDriver { get; set; }
        public static IWebDriver WebDriverQueue { get; set; }

        public static List<PersonStruct> ResultPers { get; set; }
        public static int CaptchaLife { get; set; }
        public static int MaxCaptchaQueue { get; set; }
        public static int CaptchaQueueCount => captchaQueue.Count;
        public static bool IsPermit { get; set; }
        public static string AntigateService { get; set; }

        public static List<PersonStruct> PersonsList { get; set; }

        static Utils()
        {
            PersonsList = new List<PersonStruct>();
            captchaQueue = new Queue<CaptchaStruct>();
            ResultPers = new List<PersonStruct>();
            Informer.RaiseOnQueueChanged(CaptchaQueueCount);
        }

        public static CaptchaStruct CaptchaQueue
        {
            get
            {
                var queue = captchaQueue.Dequeue();
                Informer.RaiseOnQueueChanged(CaptchaQueueCount);
                return queue;
            }
            set
            {
                captchaQueue.Enqueue(value);
                Informer.RaiseOnQueueChanged(CaptchaQueueCount);
            }
        }

        public static CaptchaCollection CaptchaCollectionHelper
        {
            get
            {
                var list = new List<CaptchaStruct>();
                while (CaptchaQueueCount > 0)
                    list.Add(CaptchaQueue);

                return new CaptchaCollection {CaptchaItemsList = list};
            }
            set
            {
                var list = value.CaptchaItemsList;
                foreach (var x in list)
                    CaptchaQueue = x;
            }
        }

        public static HttpRequest GetRequest => new HttpRequest
        {
            Cookies = new CookieDictionary(),
            UserAgent = HttpHelper.ChromeUserAgent(),
            EnableAdditionalHeaders = true,
            EnableEncodingContent = true,
            ConnectTimeout = 360000,
            ReadWriteTimeout = 360000,
            AllowAutoRedirect = true,
            MaximumAutomaticRedirections = 30,
            [HttpHeader.AcceptLanguage] = "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4"
        };

        public static void DisposeWebDrivers()
        {
            try
            {
                WebDriver.Dispose();
                WebDriverQueue.Dispose();
            }
            catch (Exception)
            {
            }
        }

        public static async Task RemoveOldCaptcha(DateTime date)
        {
            await Task.Run(() =>
            {
                var listAll = CaptchaCollectionHelper;

                var list = listAll.CaptchaItemsList;
                if (list.Count == 0)
                    return;

                var newList = list.Where(x => x.Date > date).ToList(); //todo hours
                CaptchaCollectionHelper = new CaptchaCollection {CaptchaItemsList = newList};
            });
        }

        public static async Task<Queue<ProxyStruct>> GetProxyStruct(ProxyStruct str)
        {
            return await Task.Run(() =>
            {
                var queue = new Queue<ProxyStruct>();
                try
                {
                    var host = string.IsNullOrEmpty(str.Host) ? string.Empty : str.Host;
                    var login = string.IsNullOrEmpty(str.Login) ? string.Empty : str.Login;
                    var type = str.Type;

                    foreach (var s in host.Split(','))
                        queue.Enqueue(new ProxyStruct {Host = s, Login = login, Type = type});
                }
                catch (Exception)
                {

                }
                return queue;
            });
        }

        public static async Task<HttpRequest> GetRequestTaskAsync(ProxyStruct str)
        {
            var req = GetRequest;
            return await Task.Run(() =>
            {
                try
                {
                    var host = str.Host;
                    var login = str.Login;
                    var type = str.Type;

                    if (string.IsNullOrEmpty(host) || !host.Contains(":"))
                        return req;

                    var arr = host.Split(':');
                    switch (type)
                    {
                        case ProxyType.Http:
                            req.Proxy = new HttpProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        case ProxyType.Socks4:
                            req.Proxy = new Socks4ProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        case ProxyType.Socks4a:
                            req.Proxy = new Socks4aProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        case ProxyType.Socks5:
                            req.Proxy = new Socks5ProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        default:
                            req.Proxy = new HttpProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                    }

                    if (!string.IsNullOrEmpty(login) && login.Contains(":"))
                    {
                        var arrr = login.Split(':');
                        req.Proxy.Username = arrr[0];
                        req.Proxy.Password = arrr[1];
                    }
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex.Message);
                }
                return req;
            });
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        public static async Task<Queue<ProxyStruct>> GetProxyList(string fileName = "Proxy.txt")
        {
            var list = new Queue<ProxyStruct>();
            var rows = File.ReadAllLines(fileName);

            try
            {
                foreach (var str in rows.Select(row => row.Split('|')).Select(arr => new ProxyStruct
                {
                    Host = arr[0],
                    Login = arr[1],
                    Type = (ProxyType) Enum.Parse(typeof (ProxyType), arr[2])
                }))
                {
                    var t = await GetProxyStruct(str);

                    while (t.Count > 0)
                        list.Enqueue(t.Dequeue());
                }
            }
            catch (Exception ex)
            {
                Informer.RaiseOnResultReceived(ex.Message);
            }

            return list;
        }
    }
}