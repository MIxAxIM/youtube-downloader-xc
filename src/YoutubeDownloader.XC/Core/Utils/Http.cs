using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PowerKit.Extensions;

namespace YoutubeDownloader.XC.Core.Utils;

public static class Http
{
    private static readonly DynamicProxy Proxy = DynamicProxy.Install();

    public static HttpClient Client { get; } = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new ThrottlingHandler(new SocketsHttpHandler { UseProxy = true });
        var client = new HttpClient(handler, disposeHandler: true)
        {
            DefaultRequestHeaders =
            {
                // Required by some of the services we're using
                UserAgent =
                {
                    new ProductInfoHeaderValue(
                        "YoutubeDownloader.XC",
                        Assembly.GetExecutingAssembly().TryGetVersionString()
                    ),
                },
            },
        };

        return client;
    }

    public static void ApplyProxy(ProxyConfiguration configuration) => Proxy.Update(configuration);

    private sealed class DynamicProxy : IWebProxy
    {
        private readonly object _gate = new();
        private ProxyConfiguration _configuration = ProxyConfiguration.Disabled;

        public static DynamicProxy Install()
        {
            var proxy = new DynamicProxy();
            HttpClient.DefaultProxy = proxy;
            return proxy;
        }

        public void Update(ProxyConfiguration configuration)
        {
            lock (_gate)
                _configuration = configuration;
        }

        public ICredentials? Credentials
        {
            get
            {
                var configuration = Current;
                if (string.IsNullOrWhiteSpace(configuration.Username))
                    return null;

                return new NetworkCredential(
                    configuration.Username,
                    configuration.Password ?? string.Empty
                );
            }
            set { }
        }

        public Uri? GetProxy(Uri destination) => Current.TryCreateProxyUri() ?? destination;

        public bool IsBypassed(Uri host) => Current.TryCreateProxyUri() is null;

        private ProxyConfiguration Current
        {
            get
            {
                lock (_gate)
                    return _configuration;
            }
        }
    }

    private sealed class ThrottlingHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private const long MinimumThrottledLength = 1_000_000;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (SpeedLimiter.Shared.IsUnlimited || response.Content is null)
                return response;

            var length = response.Content.Headers.ContentLength;
            if (length is < MinimumThrottledLength)
                return response;

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var throttled = new ThrottledStream(stream, SpeedLimiter.Shared);
            var replacement = new StreamContent(throttled);
            foreach (var header in response.Content.Headers)
                replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);

            response.Content = replacement;
            return response;
        }
    }
}
