﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.PlatformAbstractions;
using MihailYartsev.HttpClientGenerator.Caching;
using MihailYartsev.HttpClientGenerator.Infrastructure;
using MihailYartsev.HttpClientGenerator.Retries;

namespace MihailYartsev.HttpClientGenerator
{
    /// <summary>
    /// Provides a simple interface for configuring the <see cref="HttpClientGenerator"/> for friquient use-cases
    /// </summary>
    [PublicAPI]
    public class HttpClientGeneratorBuilder
    {
        /// <inheritdoc />
        public HttpClientGeneratorBuilder([NotNull] string rootUrl)
        {
            _rootUrl = rootUrl ?? throw new ArgumentNullException(nameof(rootUrl));
        }

        private string _rootUrl ;
        [CanBeNull] private string _apiKey ;
        [CanBeNull] private IRetryStrategy _retryStrategy  = new LinearRetryStrategy();
        [CanBeNull] private ICachingStrategy _cachingStrategy  = new AttributeBasedCachingStrategy();
        private List<ICallsWrapper> _additionalCallsWrappers  = new List<ICallsWrapper>();
        private List<DelegatingHandler> _additionalDelegatingHandlers  = new List<DelegatingHandler>();

        /// <summary>
        /// Specifies the value of the api-key header to add to the requests.
        /// If not called - no api-key is added. 
        /// </summary>
        public HttpClientGeneratorBuilder WithApiKey([NotNull] string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            return this;
        }

        /// <summary>
        /// Sets the retry stategy used to handle requests failures. If not called - the default one is used.
        /// </summary>
        public HttpClientGeneratorBuilder WithRetriesStrategy([NotNull] IRetryStrategy retryStrategy)
        {
            _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));
            return this;
        }

        /// <summary>
        /// Configures not to use retries
        /// </summary>
        public HttpClientGeneratorBuilder WithoutRetries()
        {
            _retryStrategy = null;
            return this;
        }

        /// <summary>
        /// Configures the caching strategy to use. If not called - the default one is used.
        /// </summary>
        public HttpClientGeneratorBuilder WithCachingStrategy([NotNull] ICachingStrategy cachingStrategy)
        {
            _cachingStrategy = cachingStrategy ?? throw new ArgumentNullException(nameof(cachingStrategy));
            return this;
        }

        /// <summary>
        /// Configures not to use methods results caching
        /// </summary>
        public HttpClientGeneratorBuilder WithoutCaching()
        {
            _cachingStrategy = null;
            return this;
        }

        /// <summary>
        /// Adds an additional method call wrapper
        /// </summary>
        public HttpClientGeneratorBuilder WithAdditionalCallsWrapper([NotNull] ICallsWrapper callsWrapper)
        {
            _additionalCallsWrappers.Add(callsWrapper ?? throw new ArgumentNullException(nameof(callsWrapper)));
            return this;
        }

        /// <summary>
        /// Adds an additional http delegating handler
        /// </summary>
        public HttpClientGeneratorBuilder WithAdditionalDelegatingHandler([NotNull] DelegatingHandler delegatingHandler)
        {
            _additionalDelegatingHandlers.Add(delegatingHandler ?? throw new ArgumentNullException(nameof(delegatingHandler)));
            return this;
        }

        /// <summary>
        /// Creates the configured <see cref="HttpClientGenerator"/> instance
        /// </summary>
        public HttpClientGenerator Create()
        {
            return new HttpClientGenerator(_rootUrl, GetCallsWrappers(), GetDelegatingHandlers());
        }

        private IEnumerable<DelegatingHandler> GetDelegatingHandlers()
        {
            if (_additionalDelegatingHandlers != null)
            {
                foreach (var additionalDelegatingHandler in _additionalDelegatingHandlers)
                {
                    yield return additionalDelegatingHandler;
                }
            }
            
            if (_apiKey != null)
            {
                yield return new ApiKeyHeaderHttpClientHandler(_apiKey);
            }

            yield return new UserAgentHeaderHttpClientHandler(GetDefaultUserAgent());

            if (_retryStrategy != null)
            {
                yield return new RetryingHttpClientHandler(_retryStrategy);
            }
        }

        private IEnumerable<ICallsWrapper> GetCallsWrappers()
        {
            if (_additionalCallsWrappers != null)
            {
                foreach (var additionalCallsWrapper in _additionalCallsWrappers)
                {
                    yield return additionalCallsWrapper;
                }
            }
            
            if (_cachingStrategy != null)
            {
                 yield return new CachingCallsWrapper(_cachingStrategy);
            }
        }

        private static string GetDefaultUserAgent()
        {
            return PlatformServices.Default.Application.ApplicationName + " v" +
                   PlatformServices.Default.Application.ApplicationVersion;
        }
    }
}