namespace ProxyChecker.Domain.Entity
{
    public class ProxyResult
    {
        public int Index { get; set; }
        public string? Proxy { get; set; }
        public long Time { get; set; }
        public EProxyStatus Status { get; set; }
        public string? Error { get; set; }
    }
}
