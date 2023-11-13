﻿/*
 * Copyright (C) 2009 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /// <summary>
    /// The phone number pattern used by {@link #find}, similar to
    /// <c> PhoneNumberUtil.VALID_PHONE_NUMBER</c>, but with the following differences:
    /// <ul>
    ///   <li>All captures are limited in order to place an upper bound to the text matched by the pattern. </li>
    /// </ul>
    /// <ul>
    ///   <li>Leading punctuation / plus signs are limited. </li>
    ///   <li>Consecutive occurrences of punctuation are limited. </li>
    ///   <li>Number of digits is limited. </li>
    /// </ul>
    /// <ul>
    ///   <li>No whitespace is allowed at the start or end. </li>
    ///   <li>No alpha digits (vanity numbers such as 1-800-SIX-FLAGS) are currently supported. </li>
    /// </ul>
    /// </summary>
    public partial class PhoneNumberMatcher : IEnumerator<PhoneNumberMatch>
    {
        /// <summary>
        /// Matches strings that look like publication pages. Example:
        /// <pre>Computing Complete Answers to Queries in the Presence of Limited Access Patterns.
        /// Chen Li. VLDB J. 12(3): 211-227 (2003).</pre>
        ///
        /// The string "211-227 (2003)" is not a telephone number.
        /// </summary>
#if NET7_0_OR_GREATER
        [GeneratedRegex(@"\d{1,5}-+\d{1,5}\s{0,4}\(\d{1,4}", InternalRegexOptions.Default)]
        private static partial Regex PubPages();
#else
        private static Regex PubPages() => _pubPages;
        private static readonly Regex _pubPages = new(@"\d{1,5}-+\d{1,5}\s{0,4}\(\d{1,4}", InternalRegexOptions.Default);
#endif

        /// <summary>
        /// Matches strings that look like dates using "/" as a separator. Examples: 3/10/2011, 31/10/96 or
        /// 08/31/95.
        /// </summary>
#if NET7_0_OR_GREATER
        [GeneratedRegex(@"(?>[0-3]?[0-9]/[01]?[0-9]/|[01]?[0-9]/[0-3]?[0-9]/)([12][0-9])?[0-9]{2}", InternalRegexOptions.Default | RegexOptions.ExplicitCapture)]
        private static partial Regex SlashSeparatedDates();
#else
        private static Regex SlashSeparatedDates() => _slashSeparatedDates;
        private static readonly Regex _slashSeparatedDates = new(@"(?:(?:[0-3]?\d/[01]?\d)|(?:[01]?\d/[0-3]?\d))/(?:[12]\d)?\d{2}", InternalRegexOptions.Default);
#endif

        /// <summary>
        /// Matches timestamps. Examples: "2012-01-02 08:00". Note that the reg-ex does not include the
        /// trailing ":\d\d" -- that is covered by TIME_STAMPS_SUFFIX.
        /// </summary>
#if NET7_0_OR_GREATER
        [GeneratedRegex("[12][0-9]{3}[-/]?[01][0-9][-/]?[0-3][0-9] [0-2][0-9]$", InternalRegexOptions.Default)]
        private static partial Regex TimeStamps();
#else
        private static Regex TimeStamps() => _timeStamps;
        private static readonly Regex _timeStamps = new("[12][0-9]{3}[-/]?[01][0-9][-/]?[0-3][0-9] [0-2][0-9]$", InternalRegexOptions.Default);
#endif

        const string openingParens = "(\\[\uFF08\uFF3B";
        const string closingParens = ")\\]\uFF09\uFF3D";
        const string nonParens = "[^" + openingParens + closingParens + "]";

        /*
        * An opening bracket at the beginning may not be closed, but subsequent ones should be.  It's
        * also possible that the leading bracket was dropped, so we shouldn't be surprised if we see a
        * closing bracket first. We limit the sets of brackets in a phone number to four.
        */
#if NET7_0_OR_GREATER
        [GeneratedRegex("^(?>[" + openingParens + "]?)(?>" + nonParens + "+)(?>([" + closingParens + "]" + nonParens + "*)?)" +
            "(?>([" + openingParens + "](?>" + nonParens + "+)[" + closingParens + "]){0,3})(?>" + nonParens + "*)$", InternalRegexOptions.Default | RegexOptions.ExplicitCapture)]
        private static partial Regex MatchingBrackets();
#else
        private static Regex MatchingBrackets() => _matchingBrackets;
        private static readonly Regex _matchingBrackets = new("^(?>[" + openingParens + "]?)(?>" + nonParens + "+)(?>([" + closingParens + "]" + nonParens + "*)?)" +
            "(?>([" + openingParens + "](?>" + nonParens + "+)[" + closingParens + "]){0,3})(?>" + nonParens + "*)$", InternalRegexOptions.Default | RegexOptions.ExplicitCapture);
#endif

        /// <summary>
        /// Punctuation that may be at the start of a phone number - brackets and plus signs.
        /// </summary>
        private static bool IsLeadClass(char c) => PhoneNumberUtil.IsPlusChar(c) || c is '(' or '[' or '\uFF08' or '\uFF3B';

        /// <summary>
        /// Matches white-space, which may indicate the end of a phone number and the start of something
        /// else (such as a neighbouring zip-code). If white-space is found, continues to match all
        /// characters that are not typically used to start a phone number.
        /// </summary>
#if NET7_0_OR_GREATER
        [GeneratedRegex("\\p{Z}[^" + PhoneNumberUtil.PLUS_CHARS + openingParens + "\\d]*", InternalRegexOptions.Default)]
        private static partial Regex GroupSeparator();
#else
        private static Regex GroupSeparator() => _groupSeparator;
        private static readonly Regex _groupSeparator = new("\\p{Z}[^" + PhoneNumberUtil.PLUS_CHARS + openingParens + "\\d]*", InternalRegexOptions.Default);
#endif

        /* Phone number pattern allowing optional punctuation. */
#if NET7_0_OR_GREATER
        [GeneratedRegex("(?>([" + PhoneNumberUtil.PLUS_CHARS + openingParens + "](?>[" + PhoneNumberUtil.VALID_PUNCTUATION + "]{0,4})){0,2})" +
            "(?>\\d{1,20})([" + PhoneNumberUtil.VALID_PUNCTUATION + "]{0,4}(?>\\d{1,20})){0,20}" +
            "(" + PhoneNumberUtil.ExtnPatternsForMatching + ")?", InternalRegexOptions.Default | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
        private static partial Regex Pattern();
#else
        private static Regex Pattern() => _pattern;
        private static readonly Regex _pattern = new("(?>([" + PhoneNumberUtil.PLUS_CHARS + openingParens + "](?>[" + PhoneNumberUtil.VALID_PUNCTUATION + "]{0,4})){0,2})" +
            "(?>\\d{1,20})([" + PhoneNumberUtil.VALID_PUNCTUATION + "]{0,4}(?>\\d{1,20})){0,20}" +
            "(" + PhoneNumberUtil.ExtnPatternsForMatching + ")?", InternalRegexOptions.Default | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
#endif

        /// <summary>The phone number utility.</summary>
        private readonly PhoneNumberUtil phoneUtil;
        /// <summary>The text searched for phone numbers.</summary>
        private readonly string text;

        /// <summary>The region (country) to assume for phone numbers without an international prefix, possibly null.</summary>
        private readonly string preferredRegion;
        /// <summary>The degree of validation requested.</summary>
        private readonly PhoneNumberUtil.Leniency leniency;
        /// <summary>The maximum number of retries after matching an invalid number.</summary>
        private long maxTries;

        /// <summary>The last successful match, null unless in {@link State#READY}.</summary>
        private PhoneNumberMatch lastMatch;
        /// <summary>The next index to start searching at.</summary>
        private int searchIndex;

        /// <summary>
        /// Creates a new instance. See the factory methods in {@link PhoneNumberUtil} on how to obtain a
        /// new instance.
        /// </summary>
        ///
        /// <param name="util">      the phone number util to use</param>
        /// <param name="text">      the character sequence that we will search, null for no text</param>
        /// <param name="country">   the country to assume for phone numbers not written in international format
        ///                          (with a leading plus, or with the international dialing prefix of the
        ///                          specified region). May be null or "ZZ" if only numbers with a
        ///                          leading plus should be considered.</param>
        /// <param name="leniency">  the leniency to use when evaluating candidate phone numbers</param>
        /// <param name="maxTries">  the maximum number of invalid numbers to try before giving up on the text.
        ///                          This is to cover degenerate cases where the text has a lot of false positives
        ///                          in it. Must be <c> >= 0</c>.</param>
        public PhoneNumberMatcher(PhoneNumberUtil util, string text, string country, PhoneNumberUtil.Leniency leniency,
            long maxTries)
        {
            if (maxTries < 0)
                throw new ArgumentOutOfRangeException();

            phoneUtil = util ?? throw new ArgumentNullException();
            this.text = text ?? "";
            preferredRegion = country;
            this.leniency = leniency;
            this.maxTries = maxTries;
        }

        /// <summary>
        /// Attempts to find the next subsequence in the searched sequence on or after <c>searchIndex</c>
        /// that represents a phone number. Returns the next match, null if none was found.
        /// </summary>
        /// 
        /// <param name="index"> the search index to start searching at</param>
        /// <returns> the phone number match found, null if none can be found</returns>
        private PhoneNumberMatch Find(int index)
        {
            Match matched;
            while (maxTries > 0 && (matched = Pattern().Match(text, index)).Success)
            {
                var start = matched.Index;
                var candidate = text.Substring(start, matched.Length);

                // Check for extra numbers at the end.
                // TODO: This is the place to start when trying to support extraction of multiple phone number
                // from split notations (+41 79 123 45 67 / 68).
                candidate = TrimAfterSecondNumberStart(candidate);

                var match = ExtractMatch(candidate, start);
                if (match != null)
                    return match;

                index = start + candidate.Length;
                maxTries--;
            }

            return null;
        }

        /// <summary>
        /// Helper method to determine if a character is a Latin-script letter or not. For our purposes,
        /// combining marks should also return true since we assume they have been added to a preceding
        /// Latin character.
        /// </summary>
        public static bool IsLatinLetter(char letter)
        {
            // Combining marks are a subset of non-spacing-mark.
            if (!char.IsLetter(letter) && CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                return false;
            return
                letter >= 0x0000 && letter <= 0x007F        // BASIC_LATIN
                || letter >= 0x0080 && letter <= 0x00FF     // LATIN_1_SUPPLEMENT
                || letter >= 0x0100 && letter <= 0x017F     // LATIN_EXTENDED_A
                || letter >= 0x1E00 && letter <= 0x1EFF     // LATIN_EXTENDED_ADDITIONAL
                || letter >= 0x0180 && letter <= 0x024F     // LATIN_EXTENDED_B
                || letter >= 0x0300 && letter <= 0x036F     // COMBINING_DIACRITICAL_MARKS
                ;
        }

        private static bool IsInvalidPunctuationSymbol(char character)
        {
            return character == '%' || CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.CurrencySymbol;
        }

        public static string TrimAfterUnwantedChars(string str)
        {
            var found = -1;
            for (var i = 0; i != str.Length; ++i)
            {
                var character = str[i];
                if (character != '#' && !char.IsLetterOrDigit(character))
                {
                    if (found < 0)
                        found = i;
                }
                else
                {
                    found = -1;
                }
            }
            return found >= 0 ? str.Substring(0, found) : str;
        }

        // Regular expression of characters typically used to start a second phone number for the purposes
        // of parsing. This allows us to strip off parts of the number that are actually the start of
        // another number, such as for: (530) 583-6985 x302/x2303 -> the second extension here makes this
        // actually two phone numbers, (530) 583-6985 x302 and (530) 583-6985 x2303. We remove the second
        // extension so that the first number is parsed correctly.
        private static readonly char[] SecondNumberStartChars = new[] { '\\', '/' };

        internal static string TrimAfterSecondNumberStart(string number)
        {
            var start = 0;
            while ((start = number.IndexOfAny(SecondNumberStartChars, start)) >= 0)
            {
                var i = start;
                while (++i < number.Length && number[i] == ' ') /*skip spaces*/;
                if (i < number.Length && number[i] == 'x')
                    return number.Substring(0, start);
                start = i;
            }
            return number;
        }

        /// <summary>
        /// Attempts to extract a match from a <c>candidate</c> character sequence.
        /// </summary>
        ///
        /// <param name="candidate">the candidate text that might contain a phone number</param>
        /// <param name="offset">the offset of <c>candidate</c> within <see cref="text" /></param>
        /// <returns>the match found, null if none can be found</returns>
        private PhoneNumberMatch ExtractMatch(string candidate, int offset)
        {
            // Skip a match that is more likely a publication page reference or a date.
            if (PubPages().IsMatch(candidate) || SlashSeparatedDates().IsMatch(candidate))
                return null;
            // Skip potential time-stamps.
            if (TimeStamps().IsMatch(candidate))
            {
                var i = offset + candidate.Length;
                if (text.Length >= i + 3 && text[i] == ':' && text[i + 1] is >= '0' and <= '5' && text[i + 2] is >= '0' and <= '9')
                    return null;
            }

            // Try to come up with a valid match given the entire candidate.
            var rawString = candidate;
            var match = ParseAndVerify(rawString, offset);
            return match ?? ExtractInnerMatch(rawString, offset);

            // If that failed, try to find an "inner match" - there might be a phone number within this
            // candidate.
        }

        /// <summary>
        /// Attempts to extract a match from {@code candidate} if the whole candidate does not qualify as a
        /// match.
        /// </summary>
        /// 
        /// <param name="candidate">the candidate text that might contain a phone number</param>
        /// <param name="offset">the current offset of <c>candidate</c> within <see cref="text" /></param>
        /// <returns>the match found, null if none can be found</returns>
        private PhoneNumberMatch ExtractInnerMatch(string candidate, int offset)
        {
            // Try removing either the first or last "group" in the number and see if this gives a result.
            // We consider white space to be a possible indications of the start or end of the phone number.
            var groupMatcher = GroupSeparator().Match(candidate);
            if (groupMatcher.Success)
            {
                // Try the first group by itself.
                var firstGroupOnly = candidate.Substring(0, groupMatcher.Index);
                firstGroupOnly = TrimAfterUnwantedChars(firstGroupOnly);
                var match = ParseAndVerify(firstGroupOnly, offset);
                if (match != null)
                    return match;
                maxTries--;

                var withoutFirstGroupStart = groupMatcher.Index + groupMatcher.Length;
                // Try the rest of the candidate without the first group.
                var withoutFirstGroup = candidate.Substring(withoutFirstGroupStart);
                withoutFirstGroup = TrimAfterUnwantedChars(withoutFirstGroup);
                match = ParseAndVerify(withoutFirstGroup, offset + withoutFirstGroupStart);
                if (match != null)
                    return match;
                maxTries--;

                if (maxTries > 0)
                {
                    var lastGroupStart = withoutFirstGroupStart;
                    while ((groupMatcher = groupMatcher.NextMatch()).Success)
                    {
                        // Find the last group.
                        lastGroupStart = groupMatcher.Index;
                    }
                    var withoutLastGroup = candidate.Substring(0, lastGroupStart);
                    withoutLastGroup = TrimAfterUnwantedChars(withoutLastGroup);
                    if (withoutLastGroup.Equals(firstGroupOnly))
                    {
                        // If there are only two groups, then the group "without the last group" is the same as
                        // the first group. In these cases, we don't want to re-check the number group, so we exit
                        // already.
                        return null;
                    }
                    match = ParseAndVerify(withoutLastGroup, offset);
                    if (match != null)
                        return match;
                    maxTries--;
                }
            }
            return null;
        }

        /// <summary>
        /// Parses a phone number from the {@code candidate} using {@link PhoneNumberUtil#parse} and
        /// verifies it matches the requested {@link #leniency}. If parsing and verification succeed, a
        /// corresponding <see cref="PhoneNumberMatch" /> is returned, otherwise this method returns null.
        /// </summary>
        ///
        /// <param name="candidate">the candidate match</param>
        /// <param name="offset">the offset of <c>candidate</c> within <see cref="text" /></param>
        /// <returns>the parsed and validated phone number match, or null</returns>
        private PhoneNumberMatch ParseAndVerify(string candidate, int offset)
        {
            try
            {
                // Check the candidate doesn't contain any formatting which would indicate that it really
                // isn't a phone number.
                if (!MatchingBrackets().IsMatch(candidate))
                    return null;

                // If leniency is set to VALID or stricter, we also want to skip numbers that are surrounded
                // by Latin alphabetic characters, to skip cases like abc8005001234 or 8005001234def.
                if (leniency >= PhoneNumberUtil.Leniency.VALID)
                {
                    // If the candidate is not at the start of the text, and does not start with phone-number
                    // punctuation, check the previous character.
                    if (offset > 0 && !IsLeadClass(candidate[0]))
                    {
                        var previousChar = text[offset - 1];
                        // We return null if it is a latin letter or an invalid punctuation symbol.
                        if (IsInvalidPunctuationSymbol(previousChar) || IsLatinLetter(previousChar))
                        {
                            return null;
                        }
                    }
                    var lastCharIndex = offset + candidate.Length;
                    if (lastCharIndex < text.Length)
                    {
                        var nextChar = text[lastCharIndex];
                        if (IsInvalidPunctuationSymbol(nextChar) || IsLatinLetter(nextChar))
                        {
                            return null;
                        }
                    }
                }

                var number = phoneUtil.ParseAndKeepRawInput(candidate, preferredRegion);
                if (leniency.Verify(number, candidate, phoneUtil, this))
                {
                    // We used parseAndKeepRawInput to create this number, but for now we don't return the extra
                    // values parsed. TODO: stop clearing all values here and switch all users over
                    // to using rawInput() rather than the rawString() of PhoneNumberMatch.
                    number.CountryCodeSource = 0;
                    number.RawInput = "";
                    number.PreferredDomesticCarrierCode = null;
                    return new PhoneNumberMatch(offset, candidate, number);
                }
            }
            catch (NumberParseException)
            {
                // ignore and continue
            }
            return null;
        }

        /// <summary>
        /// Returns true if the groups of digits found in our candidate phone number match our
        /// expectations.
        /// </summary>
        ///
        /// <param name="util"> </param>
        /// <param name="number"> the original number we found when parsing</param>
        /// <param name="normalizedCandidate"> the candidate number, normalized to only contain ASCII digits,
        ///     but with non-digits (spaces etc) retained</param>
        /// <param name ="expectedNumberGroups"> the groups of digits that we would expect to see if we
        ///     formatted this number</param>
        public delegate bool CheckGroups(PhoneNumberUtil util, PhoneNumber number,
                StringBuilder normalizedCandidate, IList<string> expectedNumberGroups);

        public static bool AllNumberGroupsRemainGrouped(PhoneNumberUtil util,
            PhoneNumber number,
            StringBuilder normalizedCandidate,
            IList<string> formattedNumberGroups)
        {
            var fromIndex = 0;
            // Check each group of consecutive digits are not broken into separate groupings in the
            // normalizedCandidate string.
            for (var i = 0; i < formattedNumberGroups.Count; i++)
            {
                // Fails if the substring of {@code normalizedCandidate} starting from {@code fromIndex}
                // doesn't contain the consecutive digits in formattedNumberGroups[i].
                fromIndex = normalizedCandidate.ToString().IndexOf(formattedNumberGroups[i], fromIndex, StringComparison.Ordinal);
                if (fromIndex < 0)
                {
                    return false;
                }
                // Moves {@code fromIndex} forward.
                fromIndex += formattedNumberGroups[i].Length;
                if (i == 0 && fromIndex < normalizedCandidate.Length)
                {
                    // We are at the position right after the NDC.
                    if (char.IsDigit(normalizedCandidate[fromIndex]))
                    {
                        // This means there is no formatting symbol after the NDC. In this case, we only
                        // accept the number if there is no formatting symbol at all in the number, except
                        // for extensions.
                        var nationalSignificantNumber = util.GetNationalSignificantNumber(number);
                        return normalizedCandidate.ToString().Substring(fromIndex - formattedNumberGroups[i].Length)
                            .StartsWith(nationalSignificantNumber, StringComparison.Ordinal);
                    }
                }
            }
            // The check here makes sure that we haven't mistakenly already used the extension to
            // match the last group of the subscriber number. Note the extension cannot have
            // formatting in-between digits.
            return normalizedCandidate.ToString().Substring(fromIndex).Contains(number.Extension);
        }

        public static bool AllNumberGroupsAreExactlyPresent(PhoneNumberUtil util,
            PhoneNumber number,
            StringBuilder normalizedCandidate,
            IList<string> formattedNumberGroups)
        {
            var candidateGroups = PhoneNumberUtil.NonDigitsPattern().Split(normalizedCandidate.ToString());
            // Set this to the last group, skipping it if the number has an extension.
            var candidateNumberGroupIndex =
                number.HasExtension ? candidateGroups.Length - 2 : candidateGroups.Length - 1;
            // First we check if the national significant number is formatted as a block.
            // We use contains and not equals, since the national significant number may be present with
            // a prefix such as a national number prefix, or the country code itself.
            if (candidateGroups.Length == 1 ||
                candidateGroups[candidateNumberGroupIndex].Contains(
                    util.GetNationalSignificantNumber(number)))
            {
                return true;
            }
            // Starting from the end, go through in reverse, excluding the first group, and check the
            // candidate and number groups are the same.
            for (var formattedNumberGroupIndex = (formattedNumberGroups.Count - 1);
                formattedNumberGroupIndex > 0 && candidateNumberGroupIndex >= 0;
                formattedNumberGroupIndex--, candidateNumberGroupIndex--)
            {
                if (!candidateGroups[candidateNumberGroupIndex].Equals(
                    formattedNumberGroups[formattedNumberGroupIndex]))
                {
                    return false;
                }
            }
            // Now check the first group. There may be a national prefix at the start, so we only check
            // that the candidate group ends with the formatted number group.
            return candidateNumberGroupIndex >= 0 &&
                   candidateGroups[candidateNumberGroupIndex].EndsWith(formattedNumberGroups[0], StringComparison.Ordinal);
        }

        /// <summary>
        /// Helper method to get the national-number part of a number, formatted without any national
        /// prefix, and return it as a set of digit blocks that would be formatted together following
        /// standard formatting rules.
        /// </summary>
        private static IList<string> GetNationalNumberGroups(PhoneNumberUtil util, PhoneNumber number) {
            // This will be in the format +CC-DG1-DG2-DGX;ext=EXT where DG1..DGX represents groups of
            // digits.
            var rfc3966Format = util.Format(number, PhoneNumberFormat.RFC3966);
            // We remove the extension part from the formatted string before splitting it into different
            // groups.
            var endIndex = rfc3966Format.IndexOf(';');
            if (endIndex < 0) {
                endIndex = rfc3966Format.Length;
            }
            // The country-code will have a '-' following it.
            var startIndex = rfc3966Format.IndexOf('-') + 1;
            return rfc3966Format.Substring(startIndex, endIndex - startIndex).Split('-');
        }

        /// <summary>
        /// Helper method to get the national-number part of a number, formatted without any national
        /// prefix, and return it as a set of digit blocks that should be formatted together according to
        /// the formatting pattern passed in.
        /// </summary>
        private static IList<string> GetNationalNumberGroups(PhoneNumberUtil util, PhoneNumber number,
            NumberFormat formattingPattern)
        {
            // If a format is provided, we format the NSN only, and split that according to the separator.
            var nationalSignificantNumber = util.GetNationalSignificantNumber(number);
            return util.FormatNsnUsingPattern(nationalSignificantNumber,
                formattingPattern, PhoneNumberFormat.RFC3966).Split('-');
        }

        public bool CheckNumberGroupingIsValid(
            PhoneNumber number, string candidate, PhoneNumberUtil util, CheckGroups checker)
        {
            // TODO: Evaluate how this works for other locales (testing has been limited to NANPA regions)
            // and optimise if necessary.
            var normalizedCandidate =
                PhoneNumberUtil.NormalizeDigits(new StringBuilder(candidate), true /* keep non-digits */);
            var formattedNumberGroups = GetNationalNumberGroups(util, number);
            if (checker(util, number, normalizedCandidate, formattedNumberGroups))
            {
                return true;
            }
            // If this didn't pass, see if there are any alternate formats that match, and try them instead.
            var alternateFormats =
                MetadataManager.GetAlternateFormatsForCountry(number.CountryCode);
            var nationalSignificantNumber = util.GetNationalSignificantNumber(number);
            if (alternateFormats != null)
            {
                foreach (var alternateFormat in alternateFormats.numberFormat_)
                {
                    if (alternateFormat.LeadingDigitsPatternCount > 0) {
                        // There is only one leading digits pattern for alternate formats.
                        var pattern =
                            PhoneRegex.Get(alternateFormat.GetLeadingDigitsPattern(0));
                        if (!pattern.IsMatchBeginning(nationalSignificantNumber)) {
                            // Leading digits don't match; try another one.
                            continue;
                        }
                    }
                    formattedNumberGroups = GetNationalNumberGroups(util, number, alternateFormat);
                    if (checker(util, number, normalizedCandidate, formattedNumberGroups))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool ContainsMoreThanOneSlash(string candidate)
        {
            var firstSlashIndex = candidate.IndexOf('/');
            return firstSlashIndex > 0 && candidate.Substring(firstSlashIndex + 1).Contains("/");
        }

        public static bool ContainsOnlyValidXChars(
            PhoneNumber number, string candidate, PhoneNumberUtil util)
        {
            // The characters 'x' and 'X' can be (1) a carrier code, in which case they always precede the
            // national significant number or (2) an extension sign, in which case they always precede the
            // extension number. We assume a carrier code is more than 1 digit, so the first case has to
            // have more than 1 consecutive 'x' or 'X', whereas the second case can only have exactly 1 'x'
            // or 'X'. We ignore the character if it appears as the last character of the string.
            for (var index = 0; index < candidate.Length - 1; index++)
            {
                var charAtIndex = candidate[index];
                if (charAtIndex == 'x' || charAtIndex == 'X')
                {
                    var charAtNextIndex = candidate[index + 1];
                    if (charAtNextIndex == 'x' || charAtNextIndex == 'X')
                    {
                        // This is the carrier code case, in which the 'X's always precede the national
                        // significant number.
                        index++;
                        if (util.IsNumberMatch(number, candidate.Substring(index)) != PhoneNumberUtil.MatchType.NSN_MATCH)
                        {
                            return false;
                        }
                        // This is the extension sign case, in which the 'x' or 'X' should always precede the
                        // extension number.
                    }
                    else if (!PhoneNumberUtil.NormalizeDigitsOnly(candidate.Substring(index)).Equals(
                        number.Extension))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsNationalPrefixPresentIfRequired(PhoneNumber number, PhoneNumberUtil util)
        {
            // First, check how we deduced the country code. If it was written in international format, then
            // the national prefix is not required.
            if (number.CountryCodeSource != PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY)
            {
                return true;
            }
            var phoneNumberRegion =
                util.GetRegionCodeForCountryCode(number.CountryCode);
            var metadata = util.GetMetadataForRegion(phoneNumberRegion);
            if (metadata == null)
            {
                return true;
            }
            // Check if a national prefix should be present when formatting this number.
            var nationalNumber = util.GetNationalSignificantNumber(number);
            var formatRule =
                util.ChooseFormattingPatternForNumber(metadata.numberFormat_, nationalNumber);
            // To do this, we check that a national prefix formatting rule was present and that it wasn't
            // just the first-group symbol ($1) with punctuation.
            if (formatRule != null && formatRule.NationalPrefixFormattingRule.Length > 0)
            {
                if (formatRule.NationalPrefixOptionalWhenFormatting)
                {
                    // The national-prefix is optional in these cases, so we don't need to check if it was
                    // present.
                    return true;
                }
                // Remove the first-group symbol.
                var candidateNationalPrefixRule = formatRule.NationalPrefixFormattingRule;
                // We assume that the first-group symbol will never be _before_ the national prefix.
                candidateNationalPrefixRule =
                    candidateNationalPrefixRule.Substring(0, candidateNationalPrefixRule.IndexOf("${1}", StringComparison.Ordinal));
                candidateNationalPrefixRule =
                    PhoneNumberUtil.NormalizeDigitsOnly(candidateNationalPrefixRule);
                if (candidateNationalPrefixRule.Length == 0)
                {
                    // National Prefix not needed for this number.
                    return true;
                }
                // Normalize the remainder.
                var rawInputCopy = PhoneNumberUtil.NormalizeDigitsOnly(number.RawInput);
                var rawInput = new StringBuilder(rawInputCopy);
                // Check if we found a national prefix and/or carrier code at the start of the raw input, and
                // return the result.
                return util.MaybeStripNationalPrefixAndCarrierCode(rawInput, metadata, null);
            }
            return true;
        }

        public PhoneNumberMatch Current => lastMatch;

        object IEnumerator.Current => lastMatch;

        public bool MoveNext()
        {
            lastMatch = Find(searchIndex);
            if (lastMatch != null)
                searchIndex = lastMatch.Start + lastMatch.Length;
            return lastMatch != null;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        void IDisposable.Dispose()
        {
        }
    }
}
