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
    /// Represents a span containing subscript text.
    /// </summary>
    [Obsolete(Constants.ParserObsoleteMsg)]
    public class SubscriptTextInline : MarkdownInline, IInlineContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptTextInline"/> class.
        /// </summary>
        public SubscriptTextInline()
            : base(MarkdownInlineType.Subscript)
        {
        }

        /// <summary>
        /// Gets or sets the contents of the inline.
        /// </summary>
        public IList<MarkdownInline> Inlines { get; set; }

        /// <summary>
        /// Returns the chars that if found means we might have a match.
        /// </summary>
        internal static void AddTripChars(List<InlineTripCharHelper> tripCharHelpers)
        {
            tripCharHelpers.Add(new InlineTripCharHelper() { FirstChar = '<', Method = InlineParseMethod.Subscript });
        }

        /// <summary>
        /// Attempts to parse a subscript text span.
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed subscript text span, or <c>null</c> if this is not a subscript text span. </returns>
        internal static InlineParseResult Parse(string markdown, int start, int maxEnd)
        {
            // Check the first character.
            // e.g. "<sub>……</sub>"
            if (maxEnd - start < 5)
            {
                return null;
            }

            if (markdown.Substring(start, 5) != "<sub>")
            {
                return null;
            }

            int innerStart = start + 5;
            int innerEnd = Helpers.Common.IndexOf(markdown, "</sub>", innerStart, maxEnd);

            // if don't have the end character or no character between start and end
            if (innerEnd == -1 || innerEnd == innerStart)
            {
                return null;
            }

            // No match if the character after the caret is a space.
            if (ParseHelpers.IsMarkdownWhiteSpace(markdown[innerStart]) || ParseHelpers.IsMarkdownWhiteSpace(markdown[innerEnd - 1]))
            {
                return null;
            }

            // We found something!
            SubscriptTextInline result = new SubscriptTextInline();
            result.Inlines = Helpers.Common.ParseInlineChildren(markdown, innerStart, innerEnd);
            return new InlineParseResult(result, start, innerEnd + 6);
        }

        /// <summary>
        /// Converts the object into it's textual representation.
        /// </summary>
        /// <returns> The textual representation of this object. </returns>
        public override string ToString()
        {
            return Inlines == null ? base.ToString() : "<sub>" + string.Join(string.Empty, Inlines) + "</sub>";
        }
    }
}