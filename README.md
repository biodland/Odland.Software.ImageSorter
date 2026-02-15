# ImageSorter

ImageSorter is a command-line tool for organizing and sorting images from a source directory into a target directory based on configurable criteria such as date, name, or file size. It supports a wide range of image formats, including standard and RAW formats, and offers flexible directory structures and renaming options.

## Features
- Sort images by date taken, file name, or file size
- Supports standard (JPG, PNG, GIF, etc.) and RAW (NEF, CR2, ARW, DNG, RAW) formats
- Customizable directory structure (e.g., Year/Month/Day)
- Optional file renaming based on sorting criteria
- Overwrite or keep existing files
- Dry run mode for previewing changes
- Progress and error reporting

## Usage

```
ImageSorter source="<source_path>" target="<target_path>" sortby="<criteria>" [options]
```

### Required Arguments
- `source=<path>`: Directory containing images to sort
- `target=<path>`: Directory where sorted images will be placed
- `sortby=<criteria>`: Sort criteria: `date`, `name`, or `size`

### Optional Arguments
- `structure=<fmt>`: Directory structure (e.g., `YYYY\\MM\\` for date sorting)
- `rename=<bool>`: Rename files based on sorting criteria (`true`/`false`, default: `false`)
- `overwrite=<bool>`: Overwrite existing files (`true`/`false`, default: `false`)
- `dryrun=<bool>`: Perform a dry run without moving files (`true`/`false`, default: `false`)
- `keeporiginal=<bool>`: Keep original files (copy instead of move, default: `true`)

### Examples
- Sort by date into year/month folders:
  ```
  ImageSorter source="C:\Photos" target="C:\Sorted" sortby="date" structure="YYYY\\MM\\"
  ```
- Sort by date and rename files:
  ```
  ImageSorter source="C:\Photos" target="C:\Sorted" sortby="date" structure="YYYY\\MM\\" rename="true"
  ```
- Dry run to preview changes:
  ```
  ImageSorter source="C:\Photos" target="C:\Sorted" sortby="date" dryrun="true"
  ```
- Sort by name with overwrite:
  ```
  ImageSorter source="C:\Photos" target="C:\Sorted" sortby="name" overwrite="true"
  ```

## Supported Formats
- **Standard:** JPG, JPEG, PNG, GIF, BMP, TIFF, TIF, WEBP
- **RAW:** NEF (Nikon), CR2 (Canon), ARW (Sony), DNG (Adobe), RAW

## Directory Structure Tokens
For date sorting, you can use tokens in the `structure` argument:
- `YYYY` or `YEAR`: 4-digit year
- `MM` or `MONTHNUM`: 2-digit month
- `DD` or `DAYNUM`: 2-digit day
- `HH`: 2-digit hour
- `mm`: 2-digit minute
- `ss`: 2-digit second

Example: `YYYY\\MM\\DD\\` → `2024\06\15\`

## Building
This project is a .NET application. To build:

```
dotnet build
```

## License
MIT License

---

For more help, run:
```
ImageSorter --help
```
