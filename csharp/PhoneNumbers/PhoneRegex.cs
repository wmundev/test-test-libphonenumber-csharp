﻿/*
 * Copyright (C) 2011 Patrick Mezard
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
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhoneRegex
    {
        private readonly string pattern;
        private readonly Lazy<Regex> regex;
        private readonly Lazy<Regex> allRegex;
        private readonly Lazy<Regex> beginRegex;

        private static readonly ConcurrentDictionary<string, PhoneRegex> cache = new();

        internal static PhoneRegex Get(string regex) => cache.GetOrAdd(regex, k => new PhoneRegex(k));

        public PhoneRegex(string pattern)
        {
            this.pattern = pattern;

            regex = new Lazy<Regex>(() => new Regex(this.pattern, RegexOptions.CultureInvariant), true);
            allRegex = new Lazy<Regex>(() => new Regex($"^(?:{this.pattern})$", RegexOptions.CultureInvariant), true);
            beginRegex = new Lazy<Regex>(() => new Regex($"^(?:{this.pattern})", RegexOptions.CultureInvariant), true);
        }

        [Obsolete("This is an internal implementation detail not meant for public use")]
        public PhoneRegex(string pattern, RegexOptions options)
        {
            this.pattern = pattern;

            regex = new Lazy<Regex>(() => new Regex(pattern, options), true);
            allRegex = new Lazy<Regex>(() => new Regex($"^(?:{pattern})$", options), true);
            beginRegex = new Lazy<Regex>(() => new Regex($"^(?:{pattern})", options), true);
        }

        public bool IsMatch(string value) => regex.Value.IsMatch(value);
        public Match Match(string value) => regex.Value.Match(value);
        public string Replace(string value, string replacement) => regex.Value.Replace(value, replacement);

        public bool IsMatchAll(string value) => allRegex.Value.IsMatch(value);
        public Match MatchAll(string value) => allRegex.Value.Match(value);

        public bool IsMatchBeginning(string value) => beginRegex.Value.IsMatch(value);
        public Match MatchBeginning(string value) => beginRegex.Value.Match(value);
    }
}
