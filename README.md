# Pavane

Pavane is a local server for the mobile client of a discontinued symphonic turn-based RPG, built for offline game preservation. It speaks the formats the client expects, so a locally held copy of the game can run against a server on your own machine instead of the shut-down official servers.

Pavane contains no game assets, client code, or game content of its own; it is an independent server implementation.

## What it does

With Pavane running and the client pointed at it, the game authenticates, signs in as a local guest, completes its login sequence, and loads into the game world. Pavane implements the account/SDK layer, the agreement and asset endpoints, the realm list, and the encrypted gRPC game API the client uses after sign-in.

## Components

- **Pavane.SdkServer** - HTTP (`:5000`) and optional HTTPS (`:8443`) endpoints: account/SDK init, agreement pages, the realm list, asset info, and the master-data password. Also exposes offline CLI helpers for encoding and decoding the master-data password.
- **Pavane.GameServer** - the post-sign-in game API over HTTP/2 gRPC (`:5001`), with each message in the client's Packer encryption. Handles the authorize and login flow and returns a minimal starter player state.
- **Pavane.Protocol** - the client's protocol buffer schema generated as C#.
- **Pavane.Common** - shared encoding: the Packer codec, the master-data password scheme, and logging.
- **Pavane.DispatchServer** - placeholder for future use.

## Requirements

- .NET 10 SDK

## Build

```bash
dotnet build -c Release
```

## Run

Start each server in its own terminal:

```bash
dotnet run -c Release --project Pavane.SdkServer
```

```bash
dotnet run -c Release --project Pavane.GameServer
```

### Configuration

The servers read optional settings from environment variables:

- `PAVANE_MASTERDATA_PWD` - the plaintext master-data archive password to serve (SdkServer). Left unset, no password is served.
- `PAVANE_HTTPS_PFX` - path to a PKCS#12 certificate to enable the HTTPS listener on `:8443` (SdkServer).
- `PAVANE_GAME_PFX` - path to a PKCS#12 certificate to serve the game gRPC endpoint over TLS (GameServer). Left unset, the endpoint is cleartext HTTP/2.

## Status

The account, sign-in, and login path works end to end: the client reaches the in-game loading screen. Completing the lobby requires the game's master-data content tables, which are not included.

## Tools

The `tools/` directory holds small standalone utilities:

- **mkzip** - builds the master-data archive from a list of table names, with optional per-table content overrides.
- **logintest** - a test client that calls the login gRPC methods directly and prints the decoded response.

## acknowledgments

[cosette-rs](https://github.com/cosette-rs/cosette-rs)
