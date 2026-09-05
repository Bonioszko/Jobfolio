# Authentication

Real users sign in with Google OpenID Connect authorization code flow with PKCE. The API validates
Google's ID token, requires a verified email, checks the configured allowlist, and issues an
HTTP-only application cookie. Google tokens are not stored in the browser or application cookie.

Application sign-in is separate from the Gmail worker's mailbox authorization.

## Google Cloud setup

Create an OAuth client of type **Web application**. For local development, add this authorized
redirect URI exactly:

```text
http://localhost:5121/signin-google
```

The existing Gmail worker may use a Desktop OAuth client, but the API sign-in flow requires a Web
application client whose redirect URI is configured explicitly.

## Local API configuration

Keep the client secret and account-specific allowlist in .NET user secrets:

```powershell
dotnet user-secrets --project src/App.Api set "Authentication:Google:Enabled" "true"
dotnet user-secrets --project src/App.Api set "Authentication:Google:ClientId" "YOUR_WEB_CLIENT_ID"
dotnet user-secrets --project src/App.Api set "Authentication:Google:ClientSecret" "YOUR_WEB_CLIENT_SECRET"
dotnet user-secrets --project src/App.Api set "Authentication:Google:AllowedUsers:0:Email" "YOUR_GOOGLE_EMAIL"
dotnet user-secrets --project src/App.Api set "Authentication:Google:AllowedUsers:0:WorkspaceId" "user-one"
```

`WorkspaceId` must match the suffix of the workspace used by the Gmail importer. Existing jobs
stored under `user:user-one` therefore require `user-one` here.

Run the API and frontend, open `http://localhost:5173`, and select **Sign in with Google**. An active
demo session also shows a **Sign in** action in the workspace header.

For another deployment, configure `Authentication:Google:FrontendUrl` and register that API's exact
`/signin-google` callback URL with Google. Production frontend URLs must use HTTPS.
