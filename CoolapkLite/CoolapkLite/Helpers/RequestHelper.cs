﻿using CoolapkLite.Models.Feeds;
using CoolapkLite.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using mtuc = Microsoft.Toolkit.Uwp.Connectivity;

namespace CoolapkLite.Helpers
{
    public static class RequestHelper
    {
        private static bool IsInternetAvailable => mtuc.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable;
        private static readonly Dictionary<Uri, Dictionary<int, (DateTime date, string data)>> ResponseCache = new Dictionary<Uri, Dictionary<int, (DateTime, string)>>();
        private static readonly object locker = new object();

        internal static readonly Timer CleanCacheTimer = new Timer(o =>
        {
            if (IsInternetAvailable)
            {
                DateTime now = DateTime.Now;
                lock (locker)
                {
                    foreach (KeyValuePair<Uri, Dictionary<int, (DateTime date, string data)>> i in ResponseCache)
                    {
                        int[] needDelete = (from j in i.Value
                                            where (now - j.Value.date).TotalMinutes > 2
                                            select j.Key).ToArray();
                        foreach (int item in needDelete)
                        {
                            _ = ResponseCache[i.Key].Remove(item);
                        }
                    }
                }
            }
        }, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

        public static async Task<(bool isSucceed, JToken result)> GetDataAsync(Uri uri, bool isBackground = false, bool forceRefresh = true)
        {
            string json = string.Empty;
            (int page, Uri uri) info = uri.GetPage();
            (bool isSucceed, JToken result) result;

            void ReadCache()
            {
                lock (locker)
                {
                    json = ResponseCache[info.uri][info.page].data;
                }
                result = GetResult(json);
            }

            if (forceRefresh && IsInternetAvailable)
            {
                lock (locker)
                {
                    ResponseCache.Remove(info.uri);
                }
            }

            bool isCached = false;

            lock (locker)
            {
                isCached = ResponseCache.ContainsKey(info.uri)
                && ResponseCache[info.uri].ContainsKey(info.page)
                && (!IsInternetAvailable || (DateTime.Now - ResponseCache[info.uri][info.page].date).TotalMinutes <= 2);
            }

            if (!isCached)
            {
                json = await NetworkHelper.GetSrtingAsync(uri, NetworkHelper.GetCoolapkCookies(uri), "XMLHttpRequest", isBackground);
                result = GetResult(json);
                if (!result.isSucceed) { return result; }
                lock (locker)
                {
                    ResponseCache.Add(info.uri, new Dictionary<int, (DateTime date, string data)>());
                    ResponseCache[info.uri].Add(info.page, (DateTime.Now, json));
                }
            }
            else
            {
                ReadCache();
            }
            return result;
        }

        public static async Task<(bool isSucceed, string result)> GetStringAsync(Uri uri, bool isBackground = false, bool forceRefresh = true)
        {
            string json = string.Empty;
            (int page, Uri uri) info = uri.GetPage();
            (bool isSucceed, string result) result;

            (bool isSucceed, string result) GetResult()
            {
                if (string.IsNullOrEmpty(json))
                {
                    UIHelper.ShowInAppMessage(MessageType.Message, "加载失败");
                    return (false, null);
                }
                else { return (true, json); }
            }

            if (forceRefresh && IsInternetAvailable)
            {
                lock (locker)
                {
                    ResponseCache.Remove(info.uri);
                }
            }

            bool isCached = false;

            lock (locker)
            {
                isCached = ResponseCache.ContainsKey(info.uri)
                && ResponseCache[info.uri].ContainsKey(info.page)
                && (!IsInternetAvailable || (DateTime.Now - ResponseCache[info.uri][info.page].date).TotalMinutes <= 2);
            }

            if (!isCached)
            {
                json = await NetworkHelper.GetSrtingAsync(uri, NetworkHelper.GetCoolapkCookies(uri), "XMLHttpRequest", isBackground);
                result = GetResult();
                if (!result.isSucceed) { return result; }
                lock (locker)
                {
                    ResponseCache.Add(info.uri, new Dictionary<int, (DateTime date, string data)>());
                    ResponseCache[info.uri].Add(info.page, (DateTime.Now, json));
                }
            }
            else
            {
                lock (locker)
                {
                    json = ResponseCache[info.uri][info.page].data;
                }
                result = GetResult();
                if (!result.isSucceed) { return result; }
            }
            return result;
        }

        public static async Task<(bool isSucceed, JToken result)> PostDataAsync(Uri uri, HttpContent content = null, bool isBackground = false)
        {
            string json = string.Empty;
            (bool isSucceed, JToken result) result;

            json = await NetworkHelper.PostAsync(uri, content, NetworkHelper.GetCoolapkCookies(uri), isBackground);
            result = GetResult(json);

            return result;
        }

        public static async Task<(bool isSucceed, string result)> PostStringAsync(Uri uri, HttpContent content = null, bool isBackground = false)
        {
            string json = string.Empty;
            (bool isSucceed, string result) result;

            (bool isSucceed, string result) GetResult()
            {
                if (string.IsNullOrEmpty(json))
                {
                    UIHelper.ShowInAppMessage(MessageType.Message, "加载失败");
                    return (false, null);
                }
                else { return (true, json); }
            }

            json = await NetworkHelper.PostAsync(uri, content, NetworkHelper.GetCoolapkCookies(uri), isBackground);
            result = GetResult();

            return result;
        }

        private static (int page, Uri uri) GetPage(this Uri uri)
        {
            Regex pageregex = new Regex(@"([&|?])page=(\d+)(\??)");
            if (pageregex.IsMatch(uri.ToString()))
            {
                int pagenum = Convert.ToInt32(pageregex.Match(uri.ToString()).Groups[2].Value);
                Uri baseuri = new Uri(pageregex.Match(uri.ToString()).Groups[3].Value == "?" ? pageregex.Replace(uri.ToString(), pageregex.Match(uri.ToString()).Groups[1].Value) : pageregex.Replace(uri.ToString(), string.Empty));
                return (pagenum, baseuri);
            }
            else
            {
                return (0, uri);
            }
        }

        private static (bool isSucceed, JToken result) GetResult(string json)
        {
            if (string.IsNullOrEmpty(json)) { return (false, null); }
            JObject o;
            try { o = JObject.Parse(json); }
            catch
            {
                UIHelper.ShowInAppMessage(MessageType.Message, "加载失败");
                return (false, null);
            }
            if (!o.TryGetValue("data", out JToken token) && o.TryGetValue("message", out JToken message))
            {
                UIHelper.ShowInAppMessage(MessageType.Message, message.ToString());
                return (false, null);
            }
            else { return (!string.IsNullOrEmpty(token.ToString()), token); }
        }

        public static string GetId(JToken token, string _idName)
        {
            return token == null
                ? string.Empty
                : (token as JObject).TryGetValue(_idName, out JToken jToken)
                    ? jToken.ToString()
                    : (token as JObject).TryGetValue("entityId", out JToken v1)
                                    ? v1.ToString()
                                    : (token as JObject).TryGetValue("id", out JToken v2) ? v2.ToString() : throw new ArgumentException(nameof(_idName));
        }

#pragma warning disable 0612
        public static async Task<BitmapImage> GetImageAsync(string uri, bool isBackground = false)
        {
            StorageFolder folder = await ImageCacheHelper.GetFolderAsync(ImageType.Captcha);
            StorageFile file = await folder.CreateFileAsync(DataHelper.GetMD5(uri));

            Stream s = await NetworkHelper.GetStreamAsync(new Uri(uri), NetworkHelper.GetCoolapkCookies(new Uri(uri)), "XMLHttpRequest", isBackground);

            using (Stream ss = await file.OpenStreamForWriteAsync())
            {
                await s.CopyToAsync(ss);
            }

            return new BitmapImage(new Uri(file.Path));
        }
#pragma warning restore 0612

        public static async Task ChangeLikeAsync(this ICanChangeLikeModel model, CoreDispatcher dispatcher)
        {
            if (model == null) { return; }

            bool isReply = model is FeedReplyModel;
            Uri u = UriHelper.GetUri(
                model.Liked ? UriType.OperateUnlike : UriType.OperateLike,
                isReply ? "Reply" : string.Empty,
                model.ID);
            (bool isSucceed, JToken result) = await PostDataAsync(u, null, true);
            if (!isSucceed) { return; }

            JObject o = result as JObject;
            await dispatcher.AwaitableRunAsync(() =>
            {
                model.Liked = !model.Liked;
                if (isReply)
                {
                    model.LikeNum = Convert.ToInt32(o.ToString().Replace("\"", string.Empty));
                }
                else if (o != null)
                {
                    model.LikeNum = o.Value<int>("count");
                }
            });
        }

        public static async void ChangeFollow(this ICanFollowModel model, CoreDispatcher dispatcher)
        {
            UriType type = model.Followed ? UriType.OperateUnfollow : UriType.OperateFollow;

            (bool isSucceed, _) = await PostDataAsync(UriHelper.GetUri(type, model.UID), null, true);
            if (!isSucceed) { return; }

            await dispatcher.AwaitableRunAsync(() => model.Followed = !model.Followed);
        }
    }
}
