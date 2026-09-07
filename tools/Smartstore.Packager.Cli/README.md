# Smartstore.Packager.Cli

Command line front-end for the **Smartstore Packager**. It creates the same deployable
module and theme packages as the WinForms application `Smartstore.Packager`, but without
a user interface, so packages can be built from a script or a CI job.

Both applications share their extension discovery and packaging code, which lives in
`Smartstore.Packager.Common`. Neither the package format nor the file naming differs
between them.

```powershell
Smartstore.Packager.Cli.exe pack `
  --root "D:\Build\Community.6.5.0.win-x64" `
  --output "D:\Build\packages" `
  --extension "Smartstore.PayPal"
```

`--help` prints the full usage text. Repeat `--extension` to package several extensions,
or pass `--all` to package everything found below `--root`.

The complete reference — options, further examples and exit codes — is documented in
[Deploying modules](../../dev-docs/compose/modules/deploying-modules.md). Keep that page
authoritative and do not duplicate it here.
