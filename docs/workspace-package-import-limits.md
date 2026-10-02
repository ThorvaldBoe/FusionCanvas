# Workspace package import limits

FusionCanvas treats workspace packages as untrusted input. The ZIP reader rejects a package before import when its compressed or declared decompressed resource usage exceeds these limits:

| Resource | Limit |
| --- | ---: |
| Compressed package file | 256 MiB |
| Manifest (uncompressed) | 1 MiB |
| ZIP entries, including `manifest.json` and `workspace.db` | 10,000 |
| Any one uncompressed entry | 256 MiB |
| All uncompressed entries combined | 1 GiB |
| Uncompressed-to-compressed ratio for a non-empty entry | 1,000:1 |

The reader validates ZIP metadata before deserializing the manifest or extracting the embedded database. It also wraps every entry stream with the same per-entry and aggregate read budget so a corrupt archive cannot bypass the declared sizes while it is being decompressed. Cancellation, parse failure, validation failure, and resource-limit rejection dispose the archive and remove temporary import data. The import service separately rolls back managed files created before a later import failure.

Resource-limit rejection is shown to callers as:

> The workspace package exceeds the supported resource limits.

This intentionally avoids exposing archive internals while giving the user a distinct explanation from a generally unreadable or corrupt package.
