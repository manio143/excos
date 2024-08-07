﻿// Copyright (c) Marian Dziubiak and Contributors.
// Licensed under the Apache License, Version 2.0

using System.IO.Hashing;
using System.Runtime.InteropServices;
using Excos.Options.Abstractions;
using Excos.Options.Utils;
using Microsoft.Extensions.Options.Contextual.Provider;

namespace Excos.Options.Contextual;

/// <summary>
/// Context receiver for determining allocation spot based on context.
/// </summary>
[PrivatePool]
internal partial class AllocationContextReceiver : IOptionsContextReceiver, IDisposable
{
    private string _allocationUnit;
    private string _salt;
    private string _value = string.Empty;

    private AllocationContextReceiver(string allocationUnit, string salt)
    {
        _allocationUnit = allocationUnit;
        _salt = salt;
    }

    public void Receive<T>(string key, T value)
    {
        if (string.Equals(key, _allocationUnit, StringComparison.InvariantCultureIgnoreCase))
        {
            _value = value?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Compute an allocation spot (floating point value between 0 and 1) for the identifier from context.
    /// </summary>
    public double GetIdentifierAllocationSpot(IAllocationHash hash)
    {
        return hash.GetAllocationSpot(_salt, _value);
    }

    public void Dispose() => Return(this);

    private void Clear() => _value = string.Empty;
}
