# Gmail Sync

`App.GmailSync` polls Gmail with the read-only scope and imports messages carrying an explicitly configured label. It never changes, marks, moves, or deletes Gmail messages.

## Google setup

1. Create a Google Cloud project and enable the Gmail API.
2. Configure the OAuth consent screen and add the Gmail account as a test user while the app is in testing mode.
3. Create an OAuth client of type **Desktop app** and download its JSON file.
4. Store the JSON outside source control. Do not copy its contents into `appsettings.json`.

On the first enabled run, the worker opens the Google consent flow and requests only `gmail.readonly`. Google refresh-token data is written to the configured local token-store directory. The default `.appdata` directory is git-ignored and intended only for local development; production deployments must use encrypted secret storage.

## Local configuration

Use user secrets rather than committing account-specific values:

```powershell
dotnet user-secrets --project src/App.GmailSync set "Gmail:Enabled" "true"
dotnet user-secrets --project src/App.GmailSync set "Gmail:WorkspaceKey" "user:YOUR_WORKSPACE_ID"
dotnet user-secrets --project src/App.GmailSync set "Gmail:Labels:0" "Job alerts"
dotnet user-secrets --project src/App.GmailSync set "Gmail:OAuth:ClientSecretsPath" "C:\secure\gmail-oauth-client.json"
```

Then run:

```powershell
dotnet run --project src/App.GmailSync
```

`Gmail:Labels` accepts exact Gmail label names or IDs. When several labels are configured, a message under any configured label is eligible, and duplicate message IDs are processed once.

Other settings:

```json
{
  "Gmail": {
    "PollIntervalSeconds": 60,
    "MaxMessagesPerRun": 100,
    "MaxBodyBytes": 2000000,
    "OAuth": {
      "TokenStoreDirectory": ".appdata/gmail-token",
      "UserKey": "primary"
    }
  }
}
```

The workspace key must use the same `user:<workspace ID>` configured for that email under
`Authentication:Google:AllowedUsers` in the API. For example, an API `WorkspaceId` of
`user-one` owns Gmail data under `user:user-one`. Demo workspaces are rejected.

## Processing behavior

For each selected message, the worker:

1. downloads the full MIME message,
2. extracts bounded HTML and plain-text bodies,
3. selects exactly one deterministic provider parser,
4. attempts to fetch each provider job page independently,
5. saves normalized postings and a Gmail message receipt in one EF Core save operation.

The `(workspace, Gmail message ID)` receipt and `(workspace, provider, provider external ID)` posting constraints make repeated polling idempotent. Unsupported and ambiguous messages are recorded without storing their complete email bodies. Failed messages are not marked complete and can be retried on a later poll.

Job-page enrichment is best effort. If one page is unavailable, times out, or no longer
contains recognizable job details, its posting is still imported from the deterministic
email fields and may have a null description. Other postings from the same alert continue
to be enriched. A later manual import can fill the missing details.
