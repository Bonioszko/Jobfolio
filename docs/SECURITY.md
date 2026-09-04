# Security

- Demo authentication uses an HTTP-only, SameSite=Lax cookie and a random session identifier.
- Every owned lookup uses both resource ID and workspace key to prevent cross-session access.
- Demo creation is rate limited and sessions expire after six hours.
- Email bodies are retained only for sanitized demo fixtures; real email bodies must not be persisted or logged.
- TeX is size-limited and statically screened, then compiled by Tectonic in untrusted, offline mode in the compiler process.
- PDF artifacts are stored outside static web roots and streamed only after workspace authorization.
- Real Gmail/OpenAI credentials are not part of the local demo and must be supplied only to their dedicated future adapters.
