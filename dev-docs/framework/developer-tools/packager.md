# Smartstore Packager

The Smartstore Packager creates deployable ZIP packages for compiled Smartstore modules and themes. These packages can be uploaded and installed through the plugin manager in the Smartstore backend.

The source code is available on GitHub for the [desktop application](https://github.com/smartstore/Smartstore/tree/main/tools/Smartstore.Packager), the [command-line application](https://github.com/smartstore/Smartstore/tree/main/tools/Smartstore.Packager.Cli), and the [shared packaging library](https://github.com/smartstore/Smartstore/tree/main/tools/Smartstore.Packager.Common).

The Packager is available as:

* `Smartstore.Packager`: a Windows desktop application for interactive use.
* `Smartstore.Packager.Cli`: a command-line application for scripts and automated builds.

Both applications use the same discovery and packaging logic and produce identical packages.

{% hint style="warning" %}
Always package extensions from a compiled build artifact. Do not point the Packager at module or theme source directories.
{% endhint %}

## Prepare the build artifact

Build the extension in **Release** configuration before packaging it. A complete Smartstore build artifact normally has this structure:

```text
Community.<version>.<runtime>/
├── Modules/
│   ├── Smartstore.PayPal/
│   │   ├── module.json
│   │   ├── Smartstore.PayPal.dll
│   │   ├── Views/
│   │   └── wwwroot/
│   └── ...
└── Themes/
    ├── Flex/
    │   ├── theme.config
    │   ├── Views/
    │   └── wwwroot/
    └── ...
```

The Packager recognizes:

* A module by the `module.json` file in its directory.
* A theme by the `theme.config` file in its directory.

Use the build artifact's web root as the Packager's root path. This is the directory containing `Modules`, `Themes`, or both.

## Use the desktop application

Build the `Smartstore.Packager` project from `Smartstore.Tools.sln`, then start `Smartstore.Packager.exe`.

The application remembers the last root and output paths. For a regular community build, the initial paths default to locations similar to:

```text
Root path:   build\artifacts\Community.<version>.win-x64
Output path: build\packages
```

To create packages:

1. Enter the root path of the compiled Smartstore artifact or select it with **Browse**.
2. Enter the directory in which the packages should be created.
3. Select **Read Extensions**.
4. Open the **Plugins** or **Themes** tab.
5. Select one or more extensions.
6. Select **Create Package** or **Create packages**.

The output directory is created automatically if it does not exist.

## Use the command-line application

Use `Smartstore.Packager.Cli` for scripts and automated builds. Build the `Smartstore.Packager.Cli` project from `Smartstore.Tools.sln`, then call the resulting executable with the `pack` command.

```text
Smartstore.Packager.Cli pack --root <path> --output <path> --extension <name>
Smartstore.Packager.Cli pack --root <path> --output <path> --all
Smartstore.Packager.Cli --help
```

### Command options

| Option | Description |
| --- | --- |
| `--root <path>` | Root directory of the compiled build artifact. Required. |
| `--output <path>` | Directory in which package files are created. Required. The directory is created if necessary. |
| `--extension <name>` | Directory name of a module or theme to package. May be repeated. |
| `--all` | Packages every module and theme discovered under the root path. |
| `-h`, `--help` | Displays the command reference. |

Exactly one of `--extension` and `--all` must be specified.

Extension names are matched case-insensitively against their directory names. For example, `smartstore.paypal` matches the `Smartstore.PayPal` directory.

### Package one extension

```powershell
Smartstore.Packager.Cli.exe pack `
  --root "D:\Build\Community.6.5.0.win-x64" `
  --output "D:\Build\packages" `
  --extension "Smartstore.PayPal"
```

### Package several extensions

Repeat `--extension` for each module or theme:

```powershell
Smartstore.Packager.Cli.exe pack `
  --root "D:\Build\Community.6.5.0.win-x64" `
  --output "D:\Build\packages" `
  --extension "Smartstore.PayPal" `
  --extension "Smartstore.Stripe" `
  --extension "Flex"
```

Duplicate extension arguments are ignored.

### Package all extensions

```powershell
Smartstore.Packager.Cli.exe pack `
  --root "D:\Build\Community.6.5.0.win-x64" `
  --output "D:\Build\packages" `
  --all
```

## Process CLI results

For every successfully created package, the CLI writes its absolute path to standard output:

```text
D:\Build\packages\Smartstore.Module.Smartstore.PayPal.6.5.0.zip
D:\Build\packages\Smartstore.Theme.Flex.6.5.0.zip
```

Warnings and errors are written to standard error. This allows scripts to process successful package paths independently from diagnostic messages.

When several extensions are requested, a failure does not stop the remaining extensions from being packaged. The command still returns an error exit code so that an automated build can detect the partial failure.

### Exit codes

| Exit code | Meaning |
| --- | --- |
| `0` | All requested packages were created successfully. |
| `1` | A discovery, manifest-reading, or packaging error occurred. |
| `2` | The command line was invalid. |

A script should check both the exit code and the package paths written to standard output.

```powershell
& .\Smartstore.Packager.Cli.exe pack `
  --root $artifactPath `
  --output $packagePath `
  --all

if ($LASTEXITCODE -ne 0) {
    throw "One or more extension packages could not be created."
}
```

## Install a generated package

Open the plugin manager in the Smartstore backend and upload the generated ZIP file. Smartstore reads the embedded manifest and installs the module or theme into the appropriate directory.

The package installer checks the extension's minimum supported Smartstore version before installation. When updating an existing extension, Smartstore backs up the installed files before applying the package.

Unlike a manual FTP or RDP deployment, package installation does not require stopping the application pool. Smartstore handles the extension update and application restart.
