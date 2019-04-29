﻿using JetBrains.Annotations;

namespace MihailYartsev.HttpClientGenerator
{
    /// <summary>
    /// Generates client proxies for <see cref="Refit"/> interfaces
    /// </summary>
    [PublicAPI]
    public interface IHttpClientGenerator
    {
        /// <summary>
        /// Generates the proxy
        /// </summary>
        TInterface Generate<TInterface>();
    }
}