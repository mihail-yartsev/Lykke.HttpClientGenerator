﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using MihailYartsev.HttpClientGenerator.Caching;
using MihailYartsev.HttpClientGenerator.Infrastructure;
using Refit;

namespace MihailYartsev.HttpClientGenerator
{
    /// <summary>
    /// Generates client proxies for <see cref="Refit"/> interfaces
    /// </summary>
    /// <remarks>
    /// By default adds custom headers, caching for attribute-marked methods and retries.
    /// To disable caching provide empty callsWrappers.
    /// To disable retries provide null for the retryStrategy.
    /// </remarks>
    public class HttpClientGenerator : IHttpClientGenerator
    {
        private readonly string _rootUrl;
        private readonly RefitSettings _refitSettings;
        private readonly List<ICallsWrapper> _wrappers;

        /// <summary>
        /// Kicks-off the fluent interface of building a configured <see cref="HttpClientGenerator"/>.<br/>
        /// By default it adds an autogenerated User-Agent header (from application metadata);
        /// retries with a linear policy: 6 times with a delay of 5 seconds;
        /// has the caching ability (to enable it add the <see cref="ClientCachingAttribute"/>
        /// to the method and specify the time).<br/>
        /// Api-key header is not added by default. To add it - use <see cref="HttpClientGeneratorBuilder.WithApiKey"/>. 
        /// </summary>
        public static HttpClientGeneratorBuilder BuildForUrl(string rootUrl)
        {
            return new HttpClientGeneratorBuilder(rootUrl);
        }

        /// <inheritdoc />
        public HttpClientGenerator(string rootUrl, IEnumerable<ICallsWrapper> callsWrappers,
            IEnumerable<DelegatingHandler> httpDelegatingHandlers)
        {
            _rootUrl = rootUrl;
            var httpMessageHandler = CreateHttpMessageHandler(httpDelegatingHandlers.ToList().GetEnumerator());
            _refitSettings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            _wrappers = callsWrappers.ToList();
        }

        /// <summary>
        /// Generates the proxy
        /// </summary>
        public TInterface Generate<TInterface>()
        {
            return WrapIfNeeded(RestService.For<TInterface>(_rootUrl, _refitSettings));
        }

        /// <summary>
        /// Constructs <see cref="HttpMessageHandler"/> from an enumerable of delegating handlers 
        /// </summary>
        private static HttpMessageHandler CreateHttpMessageHandler(IEnumerator<DelegatingHandler> handlersEnumerator)
        {
            if (handlersEnumerator.MoveNext())
            {
                var current = handlersEnumerator.Current;
                current.InnerHandler = CreateHttpMessageHandler(handlersEnumerator);
                return current;
            }
            else
            {
                // if no more handlers found - add the handler actually making the calls
                return new HttpClientHandler();
            }
        }

        private T WrapIfNeeded<T>(T obj)
        {
            return _wrappers.Count > 0
                ? AopProxy.Create(obj, _wrappers.Select(w => (AopProxy.MethodCallHandler) w.HandleMethodCall).ToArray())
                : obj;
        }
    }
}