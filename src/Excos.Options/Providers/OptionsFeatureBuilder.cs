﻿// Copyright (c) Marian Dziubiak and Contributors.
// Licensed under the Apache License, Version 2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using Excos.Options.Abstractions;
using Excos.Options.Abstractions.Data;
using Excos.Options.Filtering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Excos.Options.Providers;

/// <summary>
/// Builder for creating feature configurations using the Options framework.
/// </summary>
public sealed class OptionsFeatureBuilder
{
    private readonly OptionsBuilder<FeatureCollection> _optionsBuilder;

    internal Feature Feature { get; }

    internal OptionsFeatureBuilder(OptionsBuilder<FeatureCollection> optionsBuilder, string featureName, string providerName)
    {
        _optionsBuilder = optionsBuilder;
        Feature = new Feature
        {
            Name = featureName,
            ProviderName = providerName,
        };
    }

    /// <summary>
    /// Saves the feature to the collection.
    /// </summary>
    /// <returns>Options builder for further method chaining.</returns>
    public OptionsBuilder<FeatureCollection> Save() =>
        _optionsBuilder.Configure(features => features.Add(Feature));
}

/// <summary>
/// Builder for creating feature filters using the Options framework.
/// </summary>
public sealed class OptionsFeatureFilterBuilder
{
    internal OptionsFeatureBuilder FeatureBuilder { get; }

    internal Filter Filter { get; }

    internal OptionsFeatureFilterBuilder(OptionsFeatureBuilder featureBuilder, string propertyName)
    {
        FeatureBuilder = featureBuilder;
        Filter = new Filter
        {
            PropertyName = propertyName,
        };
    }

    /// <summary>
    /// Saves the filter to the feature.
    /// </summary>
    /// <returns>Feature builder for further method chaining.</returns>
    public OptionsFeatureBuilder SaveFilter()
    {
        FeatureBuilder.Feature.Filters.Add(Filter);
        return FeatureBuilder;
    }
}

/// <summary>
/// Extension methods for configuring Excos features using the Options framework.
/// </summary>
public static class OptionsFeatureProviderBuilderExtensions
{
    /// <summary>
    /// Adds the Excos Options framework based feature provider to the services collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddExcosOptionsFeatureProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IFeatureProvider), typeof(OptionsFeatureProvider), ServiceLifetime.Singleton));
        return services;
    }

    /// <summary>
    /// Creates a feature builder using the specified feature name.
    /// </summary>
    /// <param name="services">Services collection.</param>
    /// <param name="featureName">Feature name.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder BuildFeature(this IServiceCollection services, string featureName)
        => services.BuildFeature(featureName, Assembly.GetCallingAssembly().GetName()?.Name ?? nameof(OptionsFeatureBuilder));

    /// <summary>
    /// Creates a feature builder using the specified feature name and a custom provider name.
    /// </summary>
    /// <param name="services">Services collection.</param>
    /// <param name="featureName">Feature name.</param>
    /// <param name="providerName">Provider name.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder BuildFeature(this IServiceCollection services, string featureName, string providerName)
    {
        services.AddExcosOptionsFeatureProvider();
        return new OptionsFeatureBuilder(
            services.AddOptions<FeatureCollection>(),
            featureName,
            providerName);
    }

    /// <summary>
    /// Creates a feature builder using the specified feature name.
    /// </summary>
    /// <param name="optionsBuilder">Options builder.</param>
    /// <param name="featureName">Feature name.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder BuildFeature(this OptionsBuilder<FeatureCollection> optionsBuilder, string featureName) =>
        optionsBuilder.BuildFeature(featureName, Assembly.GetCallingAssembly().GetName()?.Name ?? nameof(OptionsFeatureBuilder));

    /// <summary>
    /// Creates a feature builder using the specified feature name and a custom provider name.
    /// </summary>
    /// <param name="optionsBuilder">Options builder.</param>
    /// <param name="featureName">Feature name.</param>
    /// <param name="providerName">Provider name.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder BuildFeature(this OptionsBuilder<FeatureCollection> optionsBuilder, string featureName, string providerName)
    {
        optionsBuilder.Services.AddExcosOptionsFeatureProvider();
        return new OptionsFeatureBuilder(optionsBuilder, featureName, providerName);
    }

    /// <summary>
    /// Configures the feature properties.
    /// </summary>
    /// <param name="optionsFeatureBuilder">Builder.</param>
    /// <param name="action">Configuration callback.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder Configure(this OptionsFeatureBuilder optionsFeatureBuilder, Action<Feature> action)
    {
        action(optionsFeatureBuilder.Feature);
        return optionsFeatureBuilder;
    }

    /// <summary>
    /// Starts building a filter for the feature for a specific property.
    /// </summary>
    /// <param name="optionsFeatureBuilder">Builder.</param>
    /// <param name="propertyName">Property being filtered.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureFilterBuilder WithFilter(this OptionsFeatureBuilder optionsFeatureBuilder, string propertyName) =>
        new OptionsFeatureFilterBuilder(optionsFeatureBuilder, propertyName);

    /// <summary>
    /// No-op method for chaining of filter conditions.
    /// </summary>
    /// <param name="builder">Builder.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureFilterBuilder Or(this OptionsFeatureFilterBuilder builder) => builder; // no-op

    /// <summary>
    /// Adds a condition to the filter that checks if the property matches the specified value.
    /// </summary>
    /// <param name="builder">Builder.</param>
    /// <param name="value">String to match (case-insensitive, culture-invariant).</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureFilterBuilder Matches(this OptionsFeatureFilterBuilder builder, string value)
    {
        builder.Filter.Conditions.Add(new StringFilteringCondition(value));
        return builder;
    }

    /// <summary>
    /// Adds a condition to the filter that checks if the property matches the specified regular expresion.
    /// </summary>
    /// <param name="builder">Builder.</param>
    /// <param name="pattern">Regex to match (case-insensitive, culture-invariant).</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureFilterBuilder RegexMatches(this OptionsFeatureFilterBuilder builder, string pattern)
    {
        builder.Filter.Conditions.Add(new RegexFilteringCondition(pattern));
        return builder;
    }

    /// <summary>
    /// Adds a condition to the filter that checks if the property fits into the specified range.
    /// </summary>
    /// <param name="builder">Builder.</param>
    /// <param name="range">Range to check the value against.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureFilterBuilder InRange<T>(this OptionsFeatureFilterBuilder builder, Range<T> range)
        where T : IComparable<T>, ISpanParsable<T>
    {
        builder.Filter.Conditions.Add(new RangeFilteringCondition<T>(range));
        return builder;
    }

    /// <summary>
    /// Sets up an A/B experiment for the feature for a specific <typeparamref name="TOptions"/> configuration.
    /// </summary>
    /// <typeparam name="TOptions">Options type to configure.</typeparam>
    /// <param name="optionsFeatureBuilder">Builder.</param>
    /// <param name="configureA">Configuration callback taking the options object and section name (variant A).</param>
    /// <param name="configureB">Configuration callback taking the options object and section name (variant B).</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder ABExperiment<TOptions>(this OptionsFeatureBuilder optionsFeatureBuilder, Action<TOptions, string> configureA, Action<TOptions, string> configureB)
    {
        optionsFeatureBuilder.Feature.Variants.Add(new Variant
        {
            Id = "A",
            Allocation = new Allocation(new Range<double>(0, 0.5, RangeType.IncludeStart)),
            Configuration = new CallbackConfigureOptions<TOptions>(configureA),
        });
        optionsFeatureBuilder.Feature.Variants.Add(new Variant
        {
            Id = "B",
            Allocation = new Allocation(new Range<double>(0.5, 1, RangeType.IncludeBoth)),
            Configuration = new CallbackConfigureOptions<TOptions>(configureB),
        });

        return optionsFeatureBuilder;
    }

    /// <summary>
    /// Sets up a feature rollout for a specific percentage of the population for a specific <typeparamref name="TOptions"/> configuration.
    /// </summary>
    /// <typeparam name="TOptions">Options type to configure.</typeparam>
    /// <param name="optionsFeatureBuilder">Builder.</param>
    /// <param name="percentage">Rollout percentage (0-100%)</param>
    /// <param name="configure">Configuration callback taking the options object and section name.</param>
    /// <returns>Builder.</returns>
    public static OptionsFeatureBuilder Rollout<TOptions>(this OptionsFeatureBuilder optionsFeatureBuilder, double percentage, Action<TOptions, string> configure)
    {
        optionsFeatureBuilder.Feature.Variants.Add(new Variant
        {
            Id = "Rollout",
            Allocation = Allocation.Percentage(percentage),
            Configuration = new CallbackConfigureOptions<TOptions>(configure),
        });

        return optionsFeatureBuilder;
    }
}

internal sealed class CallbackConfigureOptions<TDesignatedOptions> : IConfigureOptions
{
    private readonly Action<TDesignatedOptions, string> _configure;

    public CallbackConfigureOptions(Action<TDesignatedOptions, string> configure)
    {
        _configure = configure;
    }

    public void Configure<TOptions>(TOptions input, string section) where TOptions : class
    {
        if (typeof(TOptions) == typeof(TDesignatedOptions))
        {
            _configure(Unsafe.As<TOptions, TDesignatedOptions>(ref input), section);
        }
    }
}
