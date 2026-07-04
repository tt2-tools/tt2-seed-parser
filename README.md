# tt2-seed-parser

A two-component service for Tap Titans 2 that automatically ingests raid seed files shared on Discord and exposes them through a REST API.

## Architecture

```
Discord channel  →  discord-bot  →  /data volume  →  api
```

- **discord-bot** (Python): watches a Discord channel, downloads any `.json` seed file attachments to a shared volume, and marks processed messages with a 👍 reaction.
- **api** (.NET 10): reads seed files from the shared volume, keeps the most recently active one in memory, and serves raid data over HTTP.

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/seed/meta` | Metadata about the currently loaded seed (filename, loaded_at, validity window) |
| GET | `/seed/raids` | All raids in the active seed |
| GET | `/seed/raids/{tier}/{level}` | A specific raid by tier and level |

The API automatically selects the seed with the most recent `valid_from` that is currently within its validity window (`valid_from ≤ now < expire_at`). Expired and superseded seeds are deleted automatically.

## Getting started

### Prerequisites

- Docker & Docker Compose
- A Discord bot token with access to the target channel

### Configuration

Copy `.env.example` to `.env` and fill in the values:

```env
DISCORD_TOKEN=your_bot_token_here
TARGET_CHANNEL_ID=your_channel_id_here
```

### Run

```bash
docker compose up -d
```

The API is available at `http://localhost:5001`.

## Seed file format

Seed files are JSON documents with the following structure:

```json
{
  "valid_from": "2024-01-01T00:00:00Z",
  "expire_at":  "2024-01-08T00:00:00Z",
  "raids": [
    {
      "tier": 1,
      "level": 1,
      "spawn_sequence": ["enemy_a", "enemy_b"],
      "titans": [
        {
          "enemy_id": "enemy_a",
          "enemy_name": "Lojak",
          "current_hp": 1000,
          "total_hp": 1000,
          "parts": [
            { "part_id": "head", "current_hp": 200, "total_hp": 200, "cursed": false }
          ],
          "area_debuffs": null,
          "cursed_debuffs": null
        }
      ],
      "area_buffs": null
    }
  ]
}
```

## License

MIT — see [LICENSE](LICENSE).
