﻿using CoolapkLite.Models.Update;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace CoolapkLite.Helpers
{
    public static class UpdateHelper
    {
        private const string KKPP_API = "https://v2.kkpp.cc/repos/{0}/{1}/releases/latest";
        private const string GITHUB_API = "https://api.github.com/repos/{0}/{1}/releases/latest";

        public static async Task<UpdateInfo> CheckUpdateAsync(string username, string repository, PackageVersion currentVersion = new PackageVersion())
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(repository))
            {
                throw new ArgumentNullException(nameof(repository));
            }

            HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", username);
            string url = string.Format(GITHUB_API, username, repository);
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            UpdateInfo result = JsonConvert.DeserializeObject<UpdateInfo>(responseBody);

            if (result != null)
            {
                if (currentVersion.Major <= 0 && currentVersion.Minor <= 0 && currentVersion.Build <= 0 && currentVersion.Revision <= 0)
                {
                    currentVersion = Package.Current.Id.Version;
                }

                SystemVersionInfo newVersionInfo = GetAsVersionInfo(result.TagName);
                int major = currentVersion.Major <= 0 ? 0 : currentVersion.Major;
                int minor = currentVersion.Minor <= 0 ? 0 : currentVersion.Minor;
                int build = currentVersion.Build <= 0 ? 0 : currentVersion.Build;
                int revision = currentVersion.Revision <= 0 ? 0 : currentVersion.Revision;

                SystemVersionInfo currentVersionInfo = new(major, minor, build, revision);

                return new UpdateInfo
                {
                    Changelog = result?.Changelog,
                    CreatedAt = Convert.ToDateTime(result?.CreatedAt),
                    Assets = result.Assets,
                    IsPreRelease = result.IsPreRelease,
                    PublishedAt = Convert.ToDateTime(result?.PublishedAt),
                    TagName = result.TagName,
                    ApiUrl = result?.ApiUrl,
                    ReleaseUrl = result?.ReleaseUrl,
                    IsExistNewVersion = newVersionInfo > currentVersionInfo
                };
            }

            return null;
        }

        private static SystemVersionInfo GetAsVersionInfo(string version)
        {
            List<int> nums = GetVersionNumbers(version).Split('.').Select(int.Parse).ToList();

            return nums.Count <= 1
                ? new SystemVersionInfo(nums[0], 0, 0, 0)
                : nums.Count <= 2
                    ? new SystemVersionInfo(nums[0], nums[1], 0, 0)
                    : nums.Count <= 3
                                    ? new SystemVersionInfo(nums[0], nums[1], nums[2], 0)
                                    : new SystemVersionInfo(nums[0], nums[1], nums[2], nums[3]);
        }

        private static string GetVersionNumbers(string version)
        {
            string allowedChars = "01234567890.";
            return new string(version.Where(allowedChars.Contains).ToArray());
        }
    }
}
