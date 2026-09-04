# Domain configuration

`config/domain.json` owns user-facing item/document labels, parsed field definitions, and workflow statuses. The API exposes it at `GET /api/config/domain`; clients must not duplicate these values.

The temporary listing vocabulary is confined to configuration, fixtures, templates, parser-edge behavior, and presentation copy.
