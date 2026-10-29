// Use with macOS Automatic Proxy Configuration.
// run-gost exposes an HTTP/SOCKS auto proxy on this local address.
function FindProxyForURL(url, host) {
    host = host.toLowerCase();
    if (host.charAt(host.length - 1) === ".") host = host.slice(0, -1);
    var domains = [
        "github.com",
        "githubusercontent.com",
        "githubassets.com",
        "github.io",
        "github.blog",
        "githubstatus.com",
        "githubcopilot.com"
    ];
    for (var i = 0; i < domains.length; i++) {
        var domain = domains[i];
        if (host === domain ||
            host.slice(-(domain.length + 1)) === "." + domain) {
            return "PROXY 127.0.0.1:51080";
        }
    }
    return "DIRECT";
}
