// Copyright (c) Marian Dziubiak and Contributors.
// Licensed under the Apache License, Version 2.0

using Excos.Options.Abstractions;
using Excos.Options.Abstractions.Data;
using Microsoft.Extensions.Options;

namespace Excos.Options.Providers;

internal class OptionsFeatureProvider : IFeatureProvider
{
    private readonly IOptionsMonitor<FeatureCollection> _options;

    public OptionsFeatureProvider(IOptionsMonitor<FeatureCollection> options)
    {
        _options = options;
    }

    public ValueTask<IEnumerable<Feature>> GetFeaturesAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<IEnumerable<Feature>>(_options.CurrentValue);
    }
}
