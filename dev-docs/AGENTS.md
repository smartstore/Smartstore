# AGENTS Guidelines for technical documentation

Purpose: This folder contains GitBook documentation sources. Docs only. No product build or tests required.

## Terminology
- Always use the words frontend and backend when talking of the storefront and the back office.
- Don't speak of page fragments when something is rendered into a widget zone. Use the correct term 'widget zone'.

## Structure
- File types: `.md` (optional front matter), images under `assets/`.
- Navigation file: use the existing `SUMMARY.md` and keep order/hierarchy consistent.
- Filenames: `kebab-case`, ASCII, short, stable. Never change slugs!

## Writing rules
- One H1 per page. Short intro paragraph.
- Clear, imperative headings. Short sentences.
- Use relative links within the repo. No `blob/<branch>` URLs.
- Code examples minimal and runnable. Use language-tagged code fences.

