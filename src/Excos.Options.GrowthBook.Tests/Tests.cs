// Copyright (c) Marian Dziubiak and Contributors.
// Licensed under the Apache License, Version 2.0

using Excos.Options.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Excos.Options.GrowthBook.Tests;

public class Tests
{
    private const string Payload =
    """
    {
        "status": 200,
        "features": {
            "newlabel": {
                "defaultValue": {
                    "MyOptions": {
                        "Label": "Old"
                    }
                },
                "rules": [
                    {
                        "condition": {
                            "country": {
                                "$in": [
                                    "US",
                                    "UK"
                                ]
                            }
                        },
                        "coverage": 0.2,
                        "hashAttribute": "id",
                        "namespace": [
                            "anonymous",
                            0.2,
                            0.8
                        ],
                        "seed": "label",
                        "hashVersion": 2,
                        "variations": [
                            {
                                "MyOptions": {
                                    "Label": "Old"
                                }
                            },
                            {
                                "MyOptions": {
                                    "Label": "New"
                                }
                            }
                        ],
                        "weights": [
                            0.5,
                            0.5
                        ],
                        "key": "label",
                        "meta": [
                            {
                                "key": "0",
                                "name": "Old"
                            },
                            {
                                "key": "1",
                                "name": "New"
                            }
                        ],
                        "phase": "0",
                        "name": "LabelComparison"
                    }
                ]
            },
            "gbdemo-checkout-layout": {
                "defaultValue": "current",
                "rules": [
                    {
                        "condition": {
                            "is_employee": true
                        },
                        "force": "dev"
                    },
                    {
                        "coverage": 1,
                        "seed": "gbdemo-checkout-layout",
                        "hashVersion": 2,
                        "variations": [
                            "current",
                            "dev-compact",
                            "dev"
                        ],
                        "weights": [
                            0.3334,
                            0.3333,
                            0.3333
                        ],
                        "key": "gbdemo-checkout-layout",
                        "meta": [
                            {
                                "key": "0",
                                "name": "Current"
                            },
                            {
                                "key": "1",
                                "name": "Dev-Compact"
                            },
                            {
                                "key": "2",
                                "name": "Dev"
                            }
                        ],
                        "phase": "0",
                        "name": "gbdemo-checkout-layout"
                    },
                    {
                        "condition": {
                            "employee": true
                        },
                        "force": "dev",
                        "coverage": 0.25,
                        "hashAttribute": "id"
                    }
                ]
            },
            "filtered": {
                "defaultValue": {},
                "rules": [
                    {
                        "condition": {
                            "id": {
                                "$exists": true
                            },
                            "browser": {
                                "$ne": "1",
                                "$eq": "3"
                            },
                            "deviceId": {
                                "$gt": "5"
                            },
                            "company": {
                                "$regex": "a.*c"
                            },
                            "country": {
                                "$exists": false
                            },
                            "Tags": {
                                "$size": 0,
                                "$elemMatch": {
                                    "$eq": "A"
                                }
                            },
                            "version": {
                                "$veq": "1.2.3"
                            }
                        },
                        "force": {}
                    }
                ]
            }
        },
        "dateUpdated": "2024-01-02T21:22:10.743Z"
    }
    """;

    [Fact]
    public async Task FeaturesAreParsed()
    {
        var host = BuildHost(new GrowthBookOptions());
        var provider = (GrowthBookFeatureProvider)host.Services.GetRequiredService<IFeatureProvider>();

        var features = (await provider.GetFeaturesAsync(default)).ToList();

        Assert.Equal(3, features.Count);

        Assert.Equal("newlabel", features[0].Name);
        Assert.Equal(2, features[0].Variants.Count);
        Assert.Equal("label:0", features[0].Variants[0].Id);
        Assert.Equal("label:1", features[0].Variants[1].Id);
        Assert.Equal(2, features[0].Variants[0].Filters.Count);
        Assert.True(features[0].Variants[0].Filters.Contains("country"));
        Assert.Equal("country", features[0].Variants[0].Filters["country"].PropertyName);
        var inFilter = Assert.IsType<InFilter>(Assert.Single(features[0].Variants[0].Filters["country"].Conditions));
        Assert.True(inFilter.IsSatisfiedBy("US"));
        Assert.True(inFilter.IsSatisfiedBy("UK"));

        Assert.Equal("gbdemo-checkout-layout", features[1].Name);

        Assert.Equal("filtered", features[2].Name);
    }

    [Fact]
    public async Task ConfigurationIsSetUp()
    {
        var host = BuildHost(new GrowthBookOptions());
        await host.StartAsync();
        var config = host.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("Old", config.GetValue<string>("MyOptions:Label"));
        Assert.Equal("current", config.GetValue<string>("gbdemo-checkout-layout"));
    }

    // These tests seem to be using a different format to what the API returns...
    //[Theory]
    //[MemberData(nameof(Cases.EvalConditions), MemberType = typeof(Cases))]
    //public void EvalConditions_Test(string name, JsonElement condition, JsonElement attributes, bool expected)
    //{
    //    var filter = FilterParser.ParseFilters(condition);
    //}

    [Theory]
    [MemberData(nameof(Cases.Hash), MemberType = typeof(Cases))]
    public void Hash_Test(string seed, string identifier, int version, double? result)
    {
        var algorithm = new GrowthBookHash(version);
        var hash = algorithm.GetAllocationSpot(seed, identifier);
        Assert.Equal(result, hash == -1 ? null : hash);
    }

    [Theory]
    [MemberData(nameof(Cases.VersionCompareEQ), MemberType = typeof(Cases))]
    public void VersionCompareEQ_Test(string left, string right, bool match)
    {
        Assert.True(ComparisonVersionStringFilter.TryParse(left, out var version));
        var algorithm = new ComparisonVersionStringFilter(i => i == 0, version);
        Assert.Equal(match, algorithm.IsSatisfiedBy(right));
    }

    private IHost BuildHost(GrowthBookOptions options)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureExcosWithGrowthBook()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IOptionsMonitor<GrowthBookOptions>>(_ => new OptionsMonitor<GrowthBookOptions>(options));
                services.AddSingleton<ILogger<GrowthBookFeatureProvider>, MockLogger<GrowthBookFeatureProvider>>();
                services.AddSingleton<IHttpClientFactory>(_ => new MockHttpClientFactory(new MockHandler(Payload)));
            });

        return builder.Build();
    }

    private class MockLogger<T> : ILogger<T>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    private class OptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;
        public OptionsMonitor(T value) => _value = value;
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly DelegatingHandler _handler;
        public MockHttpClientFactory(DelegatingHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler);
        }
    }

    private class MockHandler : DelegatingHandler
    {
        private readonly string _content;
        public MockHandler(string content) => _content = content;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();
            response.Content = new StringContent(_content);
            return Task.FromResult(response);
        }
    }
}
