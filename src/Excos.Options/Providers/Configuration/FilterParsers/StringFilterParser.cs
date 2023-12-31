﻿// Copyright (c) Marian Dziubiak and Contributors.
// Licensed under the Apache License, Version 2.0

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Excos.Options.Abstractions;
using Excos.Options.Filtering;
using Microsoft.Extensions.Configuration;

namespace Excos.Options.Providers.Configuration.FilterParsers;

internal class StringFilterParser : IFeatureFilterParser
{
    public bool TryParseFilter(IConfiguration configuration, [NotNullWhen(true)] out IFilteringCondition? filteringCondition)
    {
        var pattern = configuration.Get<string?>();
        if (pattern == null)
        {
            filteringCondition = null;
            return false;
        }

        if (pattern.StartsWith('^'))
        {
            filteringCondition = new RegexFilteringCondition(pattern);
            return true;
        }
        else if (pattern.Contains('*'))
        {
            filteringCondition = new RegexFilteringCondition(Regex.Escape(pattern).Replace("\\*", ".*"));
            return true;
        }
        else
        {
            filteringCondition = new StringFilteringCondition(pattern);
            return true;
        }
    }
}
