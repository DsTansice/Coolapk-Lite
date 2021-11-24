﻿using CoolapkLite.Core.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace CoolapkLite.Helpers
{
    internal static class DataHelper
    {
        //public static Task Search(this Core.Providers.SearchListProvider provider, string keyWord)
        //{
        //    return provider.Search(keyWord, GetCoolapkCookies(UriHelper.BaseUri));
        //}

#pragma warning disable 0612
        public static async Task<BitmapImage> GetImageAsync(string uri)
        {
            Windows.Storage.StorageFolder folder = await ImageCacheHelper.GetFolderAsync(ImageType.Captcha);
            Windows.Storage.StorageFile file = await folder.CreateFileAsync(Utils.GetMD5(uri));

            Stream s = await NetworkHelper.GetStreamAsync(new Uri(uri), NetworkHelper.GetCoolapkCookies(new Uri(uri)));

            using (Stream ss = await file.OpenStreamForWriteAsync())
            {
                await s.CopyToAsync(ss);
            }

            return new BitmapImage(new Uri(file.Path));
        }
#pragma warning restore 0612

        public static string ConvertUnixTimeStampToReadable(this double time)
        {
            return ConvertUnixTimeStampToReadable(time, DateTime.Now);
        }

        public static string ConvertUnixTimeStampToReadable(this double time, DateTime baseTime)
        {
            (Utils.TimeIntervalType type, object obj) = Utils.ConvertUnixTimeStampToReadable(time, baseTime);
            switch (type)
            {
                case Utils.TimeIntervalType.MonthsAgo:
                    return $"{((DateTime)obj).Year}/{((DateTime)obj).Month}/{((DateTime)obj).Day}";

                case Utils.TimeIntervalType.DaysAgo:
                    return $"{((TimeSpan)obj).Days}天前";

                case Utils.TimeIntervalType.HoursAgo:
                    return $"{((TimeSpan)obj).Hours}小时前";

                case Utils.TimeIntervalType.MinutesAgo:
                    return $"{((TimeSpan)obj).Minutes}分钟前";

                case Utils.TimeIntervalType.JustNow:
                    return "刚刚";

                default:
                    return string.Empty;
            }
        }

        //public static async Task MakeLikeAsync(ICanChangeLikModel model, Windows.UI.Core.CoreDispatcher dispatcher, SymbolIcon like, SymbolIcon coloredLike)
        //{
        //    if (model == null) { return; }

        //    bool isReply = model is FeedReplyModel;
        //    Uri u = UriHelper.GetUri(
        //        model.Liked ? UriType.OperateUnlike : UriType.OperateLike,
        //        isReply ? "Reply" : string.Empty,
        //        model.Id);
        //    (bool isSucceed, JToken result) = await GetDataAsync(u, true);
        //    if (!isSucceed) { return; }

        //    JObject o = result as JObject;
        //    await dispatcher?.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        //    {
        //        model.Liked = !model.Liked;
        //        if (isReply)
        //        {
        //            model.Likenum = o.ToString().Replace("\"", string.Empty);
        //        }
        //        else if (o != null)
        //        {
        //            model.Likenum = $"{o.Value<int>("count")}";
        //        }

        //        if (like != null)
        //        {
        //            like.Visibility = model.Liked ? Visibility.Visible : Visibility.Collapsed;
        //        }
        //        if (coloredLike != null)
        //        {
        //            coloredLike.Visibility = model.Liked ? Visibility.Collapsed : Visibility.Visible;
        //        }
        //    });
        //}
    }
}
