---
description: Release history and maintenance guidelines
---

# Changelog

Smartstore maintains human‑readable release notes to track new features,
improvements, and bug fixes. The full history lives in the repository root as
[`changelog.md`](../../changelog.md) and follows a simple structure:

```markdown
## Smartstore 6.2.1

### New Features
- Add a payment provider for easyCredit purchase on account.

### Improvements
- FileManager: Enabled language‑dependent tabs.

### Bugfixes
- Attribute combination image could not be selected on product edit page.
```

## Versioning

Smartstore uses semantic versioning (`MAJOR.MINOR.PATCH`). A new major version
introduces breaking changes, a minor version adds functionality, and patch
versions contain only fixes.

## Updating the changelog

When contributing changes that affect users:

1. Open the root `changelog.md`.
2. Add entries under the upcoming release heading, grouped by **New Features**,
   **Improvements**, or **Bugfixes**.
3. Keep descriptions short and reference issue numbers if available.

Module authors can include a `changelog.md` in their module directory using the
same format so that release notes remain discoverable.