# Proxy only GitHub through GOST — no TUN

`run-gost` already provides an HTTP/SOCKS auto proxy on `127.0.0.1:51080`.

Use a PAC file for browsers/system-proxy-aware applications and a host-scoped Git setting for Git. Other sites connect directly. No virtual network interface is needed.

## Browsers and applications that follow macOS proxy settings

1. Keep `run-gost` running.
2. Serve this PAC file locally from a regular terminal:

   ```bash
   python3 -m http.server 18765 --bind 127.0.0.1 --directory /Users/saidim/Downloads/XTrial/Network
   ```

3. In **System Settings → Network → your active connection → Details → Proxies**, enable **Automatic proxy configuration**, using:

   ```text
   http://127.0.0.1:18765/github.pac
   ```

4. Apply the settings and reopen the browser. Avoid a second proxy app or browser extension overriding the PAC configuration. Firefox may need its own **Use system proxy settings** option enabled.

The PAC file matches the listed GitHub domains and all of their subdomains. It includes API/SSH subdomains, raw files, release assets, CDN resources, Pages, blog/status and Copilot. A lookalike domain such as `github.com.example.org` does not match.

Both GOST and the local PAC HTTP server need to be available whenever the browser loads/reloads the PAC file. Automatic operation after login requires startup services for both. This workspace preparation does not install or change startup services or system proxy settings.

## Git in terminals and editors

Git does not normally consume the macOS PAC file. Set its proxy specifically for GitHub HTTPS URLs:

```bash
git config --global http.https://github.com.proxy socks5h://127.0.0.1:51080
git ls-remote https://github.com/SaidimM/XTrial.git
```

This covers Git commands launched from terminals and applications that use that Git configuration, while other Git hosts are unaffected. SSH remotes need a separate SSH host configuration; this HTTP setting applies to HTTPS remotes. A repository-local proxy setting or a client with its own networking implementation can override the global setting.

## Other terminal programs

Not every program supports PAC or macOS proxy settings. curl, for example, needs an explicit proxy option or a wrapper for selective routing:

```bash
curl --proxy socks5h://127.0.0.1:51080 -I https://github.com
```

For applications that ignore PAC, configure their own host-specific proxy support where available. There is no single PAC setting that forces every program to obey domain rules. A rule-based local proxy is another option for programs that accept only one proxy address: route the listed GitHub domains through GOST and finish with a DIRECT rule. Keep broad `HTTP_PROXY`/`HTTPS_PROXY` variables unset if using this PAC + host-scoped Git setup.

## Verify

Visit GitHub and an unrelated site in the browser; use GOST connection logs to confirm only the GitHub requests use the VPS. Run `git ls-remote` without a command-specific proxy option after setting Git's global URL-scoped proxy.

The PAC matching logic was checked locally. Actual routing remains unverified because this assistant session cannot connect to the local proxy. Enabling host proxy settings does not remove independent sandbox restrictions.

## Disable

Turn off Automatic proxy configuration in macOS and remove the Git setting:

```bash
git config --global --unset http.https://github.com.proxy
```
