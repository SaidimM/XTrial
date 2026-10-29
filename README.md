# Trial X

A 2D motorcycle trials game with five tracks, a desert-inspired visual style, saved progression and personal best times.

Open **New Tuanjie Project** in Tuanjie 1.11 / Unity 2022.3, open `Assets/Scenes/Canyon.unity`, and press Play.

See the [project guide](<New Tuanjie Project/README.md>) for tracks, controls and verification instructions.

## Push through GOST

With `run-gost` already active, run this from your regular terminal:

```bash
cd /Users/saidim/Downloads/XTrial
./push-github.sh
```

The helper uses the local SOCKS5 listener at `127.0.0.1:51080`. `socks5h` sends DNS resolution through the proxy as well. It checks access first, initializes Git if needed, commits the source files, and pushes to `https://github.com/SaidimM/XTrial.git`. It never force-pushes; an existing remote history may require integration before the push can succeed. GitHub authentication still uses your normal Git credential setup.

The proxy is saved only for this repository's GitHub traffic. Keep `run-gost` running for future fetches/pushes. Remove the setting with:

```bash
git config --local --unset http.https://github.com.proxy
```

If you want the same proxy for all your GitHub repositories, configure it explicitly in your regular terminal:

```bash
git config --global http.https://github.com.proxy socks5h://127.0.0.1:51080
```

This network setting does not change an assistant session's filesystem or network permissions. A restricted session may still be unable to access the local proxy or write Git metadata.
