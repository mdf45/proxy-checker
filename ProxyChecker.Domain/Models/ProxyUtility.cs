using System.Net;
using System.Text.RegularExpressions;

namespace ProxyChecker.Domain.Models
{
    public class ProxyUtility
    {
        public const string DefaultFormat = "ip:port:user:pass";

        private const string TypeFormat = @"(?<type>\w+)";
        private const string IpFormat = @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})";
        private const string PortFormat = @"(?<port>\d{1,5})";
        private const string UserFormat = @"(?<user>\w+)";
        private const string PassFormat = @"(?<pass>\w+)";

        private readonly string _format;
        private readonly string _defType;

        public ProxyUtility(string format, string defType = "http")
        {
            _format = format
                .Replace("type", TypeFormat)
                .Replace("ip", IpFormat)
                .Replace("port", PortFormat)
                .Replace("user", UserFormat)
                .Replace("pass", PassFormat);

            _defType = defType;
        }

        /// <summary>
        /// Parse proxy string to <c>IWebProxy</c>
        /// </summary>
        /// <param name="proxyStr"></param>
        /// <param name="format">type, ip, port, user, pass</param>
        /// <param name="defType"></param>
        /// <returns></returns>
        public WebProxy? ParseFromString(string proxyStr)
        {
            if (string.IsNullOrEmpty(proxyStr) || string.IsNullOrEmpty(_format))
                return null;

            var regex = new Regex($@"^{_format}$");

            var match = regex.Match(proxyStr);

            var typeGroup = match.Groups["type"];
            var ipGroup = match.Groups["ip"];
            var portGroup = match.Groups["port"];
            var userGroup = match.Groups["user"];
            var passGroup = match.Groups["pass"];

            var type = typeGroup?.Success != true || string.IsNullOrEmpty(typeGroup?.Value)
                ? _defType
                : typeGroup.Value;

            _ = int.TryParse(portGroup?.Value ?? "0", out var port);
            var address = $"{type}://{ipGroup?.Value ?? "0.0.0.0"}:{port}";

            WebProxy proxy
                = new(address, false, null, userGroup.Success && passGroup.Success ? new NetworkCredential(userGroup.Value, passGroup.Value) : null);

            return proxy;
        }
    }
}
