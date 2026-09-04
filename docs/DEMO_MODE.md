# Demo mode

`POST /api/auth/demo` creates an isolated six-hour session and authentication cookie. Seeding runs every HTML fixture through `SourceParserRegistry`, then creates workspace-owned source items, templates, and a versioned Markdown rule document.

Demo mode cannot connect Gmail or invoke real OpenAI. It uses the deterministic demo generator but the same job architecture and real Tectonic compiler as real mode. Initial quotas are ten generation and ten compile jobs per session.
