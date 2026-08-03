# Enigma.HardCopy

Back up small critical files — private keys, KeePass databases, recovery seeds — **to paper**.

Enigma.HardCopy turns a file into a printable A4 PDF of QR codes, and recovers it later from scanned
images or from code contents typed in by hand, with end-to-end SHA-256 integrity verification. Each
printed sheet also carries a plain-text description of the format, so the data can be recovered
without this application.

Cross-platform desktop app (Windows + Linux), built with Avalonia.

## Status

In development — see [`docs/roadmap.md`](docs/roadmap.md).

## Build

```
dotnet build Enigma.HardCopy.slnx
dotnet test --solution Enigma.HardCopy.slnx
```

Requires the .NET 10 SDK (pinned in `global.json`).

## License

MIT — see [LICENSE.md](LICENSE.md).
