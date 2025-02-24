// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CoolapkLite.Parsers.Core;
using CoolapkLite.Parsers.Markdown.Helpers;
using System;
using System.Collections.Generic;

namespace CoolapkLite.Parsers.Markdown.Inlines
{
    /// <summary>
    /// Represents a span containing strikethrough text.
    /// </summary>
    [Obsolete(Constants.ParserObsoleteMsg)]
    public class StrikethroughTextInline : MarkdownInline, IInlineContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StrikethroughTextInline"/> class.
        /// </summary>
        public StrikethroughTextInline()
            : base(MarkdownInlineType.Strikethrough)
        {
        }

        /// <summary>
        /// Gets or sets The contents of the inline.
        /// </summary>
        public IList<MarkdownInline> Inlines { get; set; }

        /// <summary>
        /// Returns the chars that if found means we might have a match.
        /// </summary>
        internal static void AddTripChars(List<InlineTripCharHelper> tripCharHelpers)
        {
            tripCharHelpers.Add(new InlineTripCharHelper() { FirstChar = '~', Method = InlineParseMethod.Strikethrough });
        }

        /// <summary>
        /// Attempts to parse a strikethrough text span.
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed strikethrough text span, or <c>null</c> if this is not a strikethrough text span. </returns>
        internal static InlineParseResult Parse(string markdown, int start, int maxEnd)
        {
            // Check the start sequence.
            if (start >= maxEnd - 1 || markdown.Substring(start, 2) != "~~")
            {
                return null;
            }

            // Find the end of the span.
            int innerStart = start + 2;
            int innerEnd = Helpers.Common.IndexOf(markdown, "~~", innerStart, maxEnd);
            if (innerEnd == -1)
            {
                return null;
            }

            // The span must contain at least one character.
            if (innerStart == innerEnd)
            {
                return null;
            }

            // The first character inside the span must NOT be a space.
            if (ParseHelpers.IsMarkdownWhiteSpace(markdown[innerStart]))
            {
                return null;
            }

            // The last character inside the span must NOT be a space.
            if (ParseHelpers.IsMarkdownWhiteSpace(markdown[innerEnd - 1]))
            {
                return null;
            }

            // We found something!
            StrikethroughTextInline result = new StrikethroughTextInline();
            result.Inlines = Helpers.Common.ParseInlineChildren(markdown, innerStart, innerEnd);
            return new InlineParseResult(result, start, innerEnd + 2);
        }

        /// <summary>
        /// Converts the object into it's textual representation.
        /// </summary>
        /// <returns> The textual representation of this object. </returns>
        public override string ToString()
        {
            return Inlines == null ? base.ToString() : "~~" + string.Join(string.Empty, Inlines) + "~~";
        }
    }
}