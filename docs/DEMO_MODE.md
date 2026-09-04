# Demo mode

`POST /api/auth/demo` creates an isolated six-hour session and authentication cookie. Seeding runs job-alert HTML fixtures through `SourceParserRegistry`, then creates workspace-owned job source items, three base CV templates, and a versioned Markdown candidate-facts document.

Demo mode cannot connect Gmail or invoke real OpenAI. Its deterministic generator selects overlaps between the job description and seeded candidate skills, then fills the chosen CV template. It uses the same job architecture and real Tectonic compiler as real mode. Initial quotas are ten generation and ten compile jobs per session.
