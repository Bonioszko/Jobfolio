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
dotnet user-secrets --project src/App.GmailSync set "Gmail:AccountEmail" "YOUR_GOOGLE_EMAIL"
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

When `Gmail:AccountEmail` is configured, the worker reads the authenticated Gmail profile before
importing messages and rejects a token belonging to another mailbox. Cloud deployments always set
this value from the allowlisted email mapped to the job's workspace.

## Personal Cloud Run job

The low-cost personal deployment executes one synchronization pass and exits by setting:

```text
Gmail__Enabled=true
Gmail__RunOnce=true
```

For a non-interactive Cloud Run Job, configure `Gmail:OAuth:ClientId`,
`Gmail:OAuth:ClientSecret`, and `Gmail:OAuth:RefreshToken` together. The client secret and
refresh token are injected from Secret Manager. Local development keeps using the desktop
authorization flow and `FileDataStore`; cloud jobs never attempt to open a browser or persist
credentials on their ephemeral filesystem.

## Adding a second cloud mailbox

Each mailbox has an independent workspace, refresh-token secret, Cloud Run job, and scheduler.
The two accounts may use the same Gmail label name; labels are resolved inside each mailbox.

1. In the second Gmail account, create the configured label (for example, `Job alerts`) and apply
   it to the alert messages that should be imported.
2. If the Google Auth Platform audience is in Testing, add the second address under **Audience →
   Test users** before authorizing it. Gmail read-only refresh tokens issued while an external app
   remains in Testing expire after seven days; use an appropriate production or internal audience
   for durable scheduled access.
3. Obtain a separate offline refresh token using the existing Desktop OAuth client. For the local
   authorization run, use a new `Gmail:OAuth:UserKey` and token-store directory so the first
   account's cached grant cannot be reused. Select the second Google account in the consent flow.
4. Add the second email to `allowed_users` with a new, stable workspace ID. Add a matching
   `gmail_sync_accounts` entry with `enabled = false`, then run `terraform plan` and
   `terraform apply`. This first stage grants application sign-in and creates the new empty Secret
   Manager container without deploying a Gmail job that lacks a token.
5. Put only the second account's refresh token in
   `jobparser-gmail-refresh-token-<account-key>`. Never reuse the first account's token and never
   put either token in Terraform variables.
6. Change the second Gmail account entry to `enabled = true`, review another plan, and apply it.
   Terraform creates `jobparser-gmail-sync-<account-key>` and its schedule.
7. Execute that Cloud Run job once manually and confirm that postings appear only after signing in
   as the second user.

The account key is an infrastructure name such as `second`; it is not the email address. The
`workspace_id` must exactly match the value mapped from the second email in `allowed_users`.

For example, keep the Desktop OAuth JSON path in user secrets, then start a one-off local grant with
overrides that cannot reuse the first account's cached credential:

```bash
dotnet run --project src/App.GmailSync -- \
  --Gmail:Enabled=true \
  --Gmail:RunOnce=true \
  --Gmail:AccountEmail=SECOND_GOOGLE_EMAIL \
  --Gmail:WorkspaceKey=user:second-user \
  --Gmail:Labels:0="Job alerts" \
  --Gmail:OAuth:UserKey=second \
  --Gmail:OAuth:TokenStoreDirectory=/absolute/private/path/gmail-token-second
```

The browser consent must be completed while signed in as `SECOND_GOOGLE_EMAIL`. The worker rejects
the grant before import if a different account is selected. Locate the resulting JSON credential in
the private token-store directory, copy only its `refresh_token` value to a temporary private text
file, upload that file to the second account's Secret Manager container, and delete the temporary
file after successful rollout. Do not print the token in a terminal or paste it into shell history.

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
