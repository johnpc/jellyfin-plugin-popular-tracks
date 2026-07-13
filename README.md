# PopularTracks

> Fixes the artist **"Popular"** track ordering in Jellyfin using real Last.fm popularity
> instead of your server's (usually meaningless) local play counts.

On a self-hosted Jellyfin server nobody scrobbles, so `PlayCount` is ~0 for almost everything.
That makes an artist's **"Popular"** row — which the clients build with
`SortBy=PlayCount&SortOrder=Descending` — effectively random. PopularTracks transparently
re-orders that one query by the artist's real
[Last.fm `artist.getTopTracks`](https://www.last.fm/api/show/artist.getTopTracks) ranking, so the
top songs are the songs the world actually plays.

**No client changes.** The clients keep asking for `SortBy=PlayCount`; the server just returns a
better order.

## How it works

1. A dynamically-injected MVC action filter attaches to Jellyfin's `ItemsController.GetItems`
   at startup (approach borrowed from [CrowdMix](https://github.com/johnpc/jellyfin-plugin-crowdmix)).
2. It fires **only** for the artist "Popular" query — audio, `SortBy=PlayCount` descending, scoped
   to an `ArtistIds`/`AlbumArtistIds`. Everything else (library browse, homepage rows, album track
   lists) is left completely untouched.
3. Because Jellyfin applies the request's `Limit` in SQL **before** the plugin sees the result
   (so the native result is just the top-N by garbage PlayCount), the plugin **re-queries the
   artist's full track set**, orders it by Last.fm rank, then re-applies the page's
   `StartIndex`/`Limit` itself.
4. Tracks Last.fm doesn't rank fall to the bottom in their original order. Artists unknown to
   Last.fm (or any network/key failure) **fail open** to the native result — you never get an
   empty or broken list.
5. Results are cached per-artist for a configurable TTL, so there's at most one Last.fm call per
   artist per window.

## Install

1. In Jellyfin: **Dashboard → Plugins → Repositories → Add**, URL:
   `https://raw.githubusercontent.com/johnpc/jellyfin-plugin-popular-tracks/main/manifest.json`
2. **Catalog → PopularTracks → Install**, then restart Jellyfin.
3. **Dashboard → Plugins → PopularTracks**, paste your free
   [Last.fm API key](https://www.last.fm/api/account/create), and Save.

## Configuration

| Setting | Default | Purpose |
|---|---|---|
| Last.fm API Key | — | Required; reads artist popularity |
| Enabled | on | Turn the re-ordering on/off without uninstalling |
| Cache lifetime (hours) | 12 | How long a fetched artist top-tracks list is reused |

## Development

Built with the standard quality rig — strict build (warnings-as-errors), ≥80% unit coverage,
Reqnroll acceptance tests, CRAP ≤15, and a 250-line-per-file cap, all enforced in CI.

```bash
dotnet build --configuration Release /p:TreatWarningsAsErrors=false
dotnet test  Jellyfin.Plugin.PopularTracks.Tests/Jellyfin.Plugin.PopularTracks.Tests.csproj
```
