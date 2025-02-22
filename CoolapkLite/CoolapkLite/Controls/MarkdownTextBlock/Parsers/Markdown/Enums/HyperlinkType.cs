// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CoolapkLite.Parsers.Markdown
{
    /// <summary>
    /// Specifies the type of Hyperlink that is used.
    /// </summary>
    [Obsolete(Constants.ParserObsoleteMsg)]
    public enum HyperlinkType
    {
        /// <summary>
        /// A hyperlink surrounded by angle brackets (e.g. "http://www.reddit.com").
        /// </summary>
        BracketedUrl,

        /// <summary>
        /// A fully qualified hyperlink (e.g. "http://www.reddit.com").
        /// </summary>
        FullUrl,

        /// <summary>
        /// A URL without a scheme (e.g. "www.reddit.com").
        /// </summary>
        PartialUrl,

        /// <summary>
        /// An email address (e.g. "test@reddit.com").
        /// </summary>
        Email,

        /// <summary>
        /// A subreddit link (e.g. "/r/news").
        /// </summary>
        Subreddit,

        /// <summary>
        /// A user link (e.g. "/u/quinbd").
        /// </summary>
        User,
    }
}