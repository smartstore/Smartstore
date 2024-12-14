# IconGenerator

**IconGenerator** is a tool designed to help you create and export a subset of icons from an SVG symbol collection. While optimized for Bootstrap icons, the tool can be used with any SVG collection that follows the required format.

The primary purpose of **IconGenerator** is to:
- **Update existing subsets** based on new icons in the remote file.
- **Create completely new subsets**: The new subset can include icons from any of the loaded files (remote, local, or subset), allowing you to create a subset that is larger or smaller than the remote file, depending on your needs.

## Features

- Load and compare **remote** (external), **local** (previous local copy), and **subset** (last created) SVG symbol collections.
- Visualize icons with badges:
  - **NEW**: Icons found only in the remote collection.
  - **SUBSET**: Icons already present in the subset.
- Select icons from **all loaded files** (remote, local, and subset).
- Filter icons by symbol ID, the **NEW** badge, or selected icons.
- Export the selected icons as an SVG subset file with **prettified** SVG code for better readability.

## Prerequisites

To use **IconGenerator**, the following requirements must be met:
- The tool requires at least a **remote** SVG symbol file (e.g., from Bootstrap) containing the latest icons.
- You can optionally load a **local** file (your existing local copy) and a **subset** file (your last created subset).
- Each symbol in the SVG files must contain:
  - A valid **ID** attribute.
  - Properly defined **viewBox** and **SVG drawing data** (e.g., `<path>`, `<rect>`, etc.).

### Sample SVG Symbol File

Here is a sample of an SVG symbol file with two icons. Each symbol contains an ID, viewBox, and simple SVG path data:

```xml
<svg xmlns="http://www.w3.org/2000/svg">
  <symbol id="icon1" viewBox="0 0 16 16">
    <path d="M1 1h14v14H1z" />
  </symbol>
  <symbol id="icon2" viewBox="0 0 16 16">
    <path d="M8 0L16 16H0L8 0z" />
  </symbol>
</svg>
```

## How to Use

1. **Load SVG Files**: 
   - Select at least the **remote** file and press the "Read files" button.
   - The tool will load and display all the icons.
   - Icons found only in the remote file will be labeled with a **NEW** badge.
   - Icons present in both the **remote** and **subset** files will have a **SUBSET** badge and will be pre-selected.

2. **Select Icons**:
   - Manually select or deselect the icons you want to include in the subset.

3. **Filter Icons**:
   - Filter icons by symbol ID, or by the **NEW** or **SUBSET** badges.

4. **Export Subset**:
   - Once at least one icon is selected, press the **Export** button to generate the new subset.
   - A dialog will display the **prettified** SVG code of the new subset.
   - You can optionally download the subset as a file by clicking the provided download link.