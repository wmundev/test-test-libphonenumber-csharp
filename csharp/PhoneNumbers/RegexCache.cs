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
using System.ComponentModel;

namespace PhoneNumbers
{
    [Obsolete("This is an internal implementation detail not meant for public use"), EditorBrowsable(EditorBrowsableState.Never)]
    public class RegexCache
    {
        public RegexCache(int size) { }
        public PhoneRegex GetPatternForRegex(string regex) => PhoneRegex.Get(regex);
        public bool ContainsRegex(string regex) => false;
    }
}