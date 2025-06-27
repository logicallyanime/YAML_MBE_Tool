<p align="center"><h1 align="center">YAML-MBE Tool (YMT)</h1></p>
<p align="center">
	<img src="https://img.shields.io/github/license/logicallyanime/YAML_MBE_Tool?style=default&logo=opensourceinitiative&logoColor=white&color=0080ff" alt="license">
	<img src="https://img.shields.io/github/last-commit/logicallyanime/YAML_MBE_Tool?style=default&logo=git&logoColor=white&color=0080ff" alt="last-commit">
	<img src="https://img.shields.io/github/languages/top/logicallyanime/YAML_MBE_Tool?style=default&color=0080ff" alt="repo-top-language">
	<img src="https://img.shields.io/github/languages/count/logicallyanime/YAML_MBE_Tool?style=default&color=0080ff" alt="repo-language-count">
</p>
<p align="center"><!-- default option, no dependency badges. -->
</p>
<p align="center">
	<!-- default option, no dependency badges. -->
</p>
<br>


##  Overview

YAML-MBE Tool (YMT) is a command-line tool based off [SydMontague's DSCS_Tools](https://github.com/SydMontague/DSCSTools) to extract and repack `.mbe` files into YAML format making them easier to read and modify. It is built using .Net 8.0 and leverages the `YamlDotNet` library for YAML parsing and serialization. The tool supports multi-threading for performance up to 3x the speed of extracting and packing using DSCS_Tools.


## Limitations

Currently YMT does not support extracting `data` mbes. This is due to the fact this tool uses Classes to represent the data in the MBE files rather than structure jsons, and the data mbes are not structured in a way that can be easily represented by classes. This may change in the future, but for now, you will need to use DSCS_Tools to extract data mbes.
##  Getting Started

###  Prerequisites

Before getting started with YAML_MBE_Tool, ensure your runtime environment meets the following requirements:

For Building:
- **Language:** C# .Net 8.0
- **Package Manager:** Nuget

For Using:
- **Framework** [.Net 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)


###  Building from Source


1. Clone the YAML_MBE_Tool repository:
```sh
❯ git clone https://github.com/logicallyanime/YAML_MBE_Tool
```

2. Navigate to the project directory:
```sh
❯ cd YAML_MBE_Tool
```

3. Install the project dependencies:


**Using `nuget`** &nbsp; [<img align="center" src="https://img.shields.io/badge/C%23-239120.svg?style={badge_style}&logo=c-sharp&logoColor=white" />](https://docs.microsoft.com/en-us/dotnet/csharp/)

```sh
❯ dotnet restore
```

Build YAML_MBE_Tool using the following command:
**Using `nuget`** &nbsp; [<img align="center" src="https://img.shields.io/badge/C%23-239120.svg?style={badge_style}&logo=c-sharp&logoColor=white" />](https://docs.microsoft.com/en-us/dotnet/csharp/)

```sh
❯ dotnet build
```

### Download from Release

You can always find the latest release and download binaries on the [Releases page](https://github.com/logicallyanime/YAML_MBE_Tool/releases/latest).

## Command Line Usage

```
DSCSTools - Digimon Story Cyber Sleuth MBE File Tool

USAGE:
  DSCSTools <command> [options]

COMMANDS:
  mbeextract    Extract .mbe file(s) to YAML format
  mbepack       Pack YAML file(s) back to .mbe format

GLOBAL OPTIONS:
  -v, --verbose              Enable verbose output
  -t, --Multithread <bool>   Enable/disable multithreading (default: true)
  --DisableProgressBar       Disable the progress bar
  -m, --isPatch <bool>       Extract/pack as patch format (default: true)
  --help                     Display help information
  --version                  Display version information

EXTRACT COMMAND:
  DSCSTools mbeextract <source> [target] [options]

  ARGUMENTS:
    source                   Source .mbe file or directory containing .mbe files
    target                   Target directory for extracted YAML files (optional, defaults to ./Converted)

  OPTIONS:
    -l, --lang <languages>   Specify languages to extract (e.g., jpn,eng,ger)

PACK COMMAND:
  DSCSTools mbepack <source> [target] [options]

  ARGUMENTS:
    source                   Source YAML file or directory containing YAML files
    target                   Target directory for packed .mbe files (optional, defaults to ./Converted)

EXAMPLES:
  DSCSTools mbeextract message.mbe
  DSCSTools mbeextract ./mbe_files/ ./extracted/
  DSCSTools mbepack message.yaml ./output/
  DSCSTools mbeextract message.mbe -l jpn,eng --verbose
```

## Usage Guide with Examples

### Basic Operations

#### 1. Extract a Single MBE File

```bash
# Extract one .mbe file to YAML (outputs to ./Converted by default)
DSCSTools mbeextract message_001.mbe

# Extract to specific directory
DSCSTools mbeextract message_001.mbe ./my_extracts/
```

#### 2. Extract Multiple MBE Files (Directory)

```bash
# Extract all .mbe files from a directory
DSCSTools mbeextract ./mbe_files/

# Extract to specific output directory
DSCSTools mbeextract ./mbe_files/ ./extracted_yamls/
```

#### 3. Pack YAML Back to MBE

```bash
# Pack a single YAML file back to .mbe
DSCSTools mbepack message_001.yaml

# Pack with specific output location
DSCSTools mbepack message_001.yaml ./packed_mbe/

# Pack entire directory of YAML files
DSCSTools mbepack ./yaml_files/ ./output_mbe/
```

### Language-Specific Operations

#### 4. Extract Specific Languages Only

```bash
# Extract only Japanese and English
DSCSTools mbeextract message.mbe -l jpn,eng

# Extract all available languages
DSCSTools mbeextract message.mbe -l all

# Extract multiple languages with custom output
DSCSTools mbeextract ./mbe_files/ ./multilang_output/ -l jpn,eng,ger,kor
```

#### 5. Supported Language Codes

Pick one, or multiple idc

| Language | Supported Codes |
|----------|----------------|
| Japanese | `jp`, `ja`, `jpn` |
| English | `us`, `usa`, `en`, `eng` |
| Chinese | `cn`, `chn`, `zh`, `zho`, `cdo`, `cjy`, `cmn`, `cnp`, `csp`, `czh`, `czo`, `gan`, `hak`, `hnm`, `hsn`, `luh`, `lzh`, `mnp`, `nan`, `sjc`, `wuu`, `yue`, `dng` |
| Korean | `kr`, `ko`, `kor` |
| German | `ger`, `de`, `deu` |
| English Censored | `engc`, `eng_censored` |

**Examples:**
```bash
# Japanese variants
DSCSTools mbeextract message.mbe -l jp
DSCSTools mbeextract message.mbe -l ja
DSCSTools mbeextract message.mbe -l jpn

# Multiple languages
DSCSTools mbeextract message.mbe -l jpn,eng,ger,kor
```

### Performance and Output Control

#### 6. Disable Multithreading (for troubleshooting)

```bash
# Single-threaded processing
DSCSTools mbeextract ./large_directory/ -t false

# Also works for packing
DSCSTools mbepack ./yaml_files/ -t false
```

#### 7. Verbose Output for Debugging

```bash
# Get detailed processing information
DSCSTools mbeextract message.mbe --verbose

# Combine with other options
DSCSTools mbeextract ./mbe_files/ ./output/ -l jpn,eng --verbose -t false
```

#### 8. Disable Progress Bar

```bash
# For cleaner output in scripts/logs
DSCSTools mbeextract ./mbe_files/ --DisableProgressBar

# Combine with verbose for detailed logs without progress bar
DSCSTools mbeextract ./mbe_files/ --verbose --DisableProgressBar
```

### Patch vs Full Format

#### 9. Extract as Full Format (Non-Patch)

```bash
# Extract complete data structure
DSCSTools mbeextract message.mbe -m false

# Useful for viewing all table data
DSCSTools mbeextract ./story_files/ ./full_extract/ -m false
```

#### 10. Pack from Full Format

```bash
# Pack from complete YAML structure
DSCSTools mbepack full_message.yaml -m false
```

### Batch Processing Examples

#### 11. Process Multiple Directories

**Windows:**
```batch
for /d %i in (chapter_*) do DSCSTools mbeextract "%i" "extracted_%i"
```

**Linux/Mac:**
```bash
for dir in chapter_*/; do DSCSTools mbeextract "$dir" "extracted_$dir"; done
```

#### 12. Chain Extract and Modify Operations

```bash
# Extract, then pack after modification
DSCSTools mbeextract original_messages.mbe ./temp_yaml/
# ... edit YAML files ...
DSCSTools mbepack ./temp_yaml/ ./modified_mbe/
```

### Advanced Use Cases

#### 13. Translation Workflow

```bash
# Extract source files with all languages
DSCSTools mbeextract ./source_mbe/ ./translation_work/ -l all --verbose

# After translation, pack with patch format
DSCSTools mbepack ./translation_work/ ./translated_mbe/ -m true
```

#### 14. Quality Assurance Testing

```bash
# Extract with verbose logging for QA
DSCSTools mbeextract ./test_files/ ./qa_output/ --verbose --DisableProgressBar -l jpn,eng

# Single-threaded for consistent results
DSCSTools mbepack ./qa_yaml/ ./qa_mbe/ -t false --verbose
```

### Error Handling and Troubleshooting

#### 15. Common Issues and Solutions

```bash
# If multithreading causes issues, disable it
DSCSTools mbeextract problematic_files/ -t false --verbose

# For memory-intensive operations, disable progress bar
DSCSTools mbeextract large_files/ --DisableProgressBar -t false

# Check specific file with full verbose output
DSCSTools mbeextract single_problem_file.mbe --verbose -m false
```

#### 16. Validation Workflow

```bash
# Extract original
DSCSTools mbeextract original.mbe ./temp1/

# Pack it back  
DSCSTools mbepack ./temp1/ ./temp2/

# Extract the packed version to compare
DSCSTools mbeextract ./temp2/original.mbe ./temp3/

# Compare temp1 and temp3 YAML files for validation
```

## File Structure Examples

### Patch Format YAML (Default)
```yaml
- id: "1"
  speakerId: 2000
  voiceFn: 6969
  name: "Protagonist"
  msg:
    jpn: "こんにちは"
    eng: "Hello"
    ger: "Hallo"
- id: "2"
  speakerId: 2
  voiceFn: 9696
  name: "Agumon"
  msg:
    jpn: "元気ですか？"
    eng: "How are you?"
  ...
```

### Full Format YAML (Non-Patch)
```yaml
Sheet1:
- ID: 1
  Speaker: 2000
  Japanese: "こんにちは"
  English: "Hello"
  Chinese: ""
  EnglishCensored: ""
  Korean: ""
  German: "Hallo"
- ID: 2
  Speaker: 2
  Japanese: "元気ですか？"
  English: "How are you?"
  Chinese: ""
  EnglishCensored: ""
  Korean: ""
  German: ""
```


##  Contributing

- **🐛 [Report Issues](https://github.com/logicallyanime/YAML_MBE_Tool/issues)**: Submit bugs found or log feature requests for `YAML_MBE_Tool`
- **💡 [Submit Pull Requests](https://github.com/logicallyanime/YAML_MBE_Tool/blob/main/CONTRIBUTING.md)**: Review open PRs, and submit your own PRs.

---

##  Special Thanks

* [SydMontague](https://github.com/SydMontague) for the original [DSCS_Tools](https://github.com/SydMontague/DSCSTools) project, which inspired this tool.

---

## Other Tools
* [SimpleDSCSModManager](https://github.com/Pherakki/SimpleDSCSModManager) by Pherakki
* [Blender-Tools-for-DSCS](https://github.com/Pherakki/Blender-Tools-for-DSCS/) by Pherakki
* [NutCracker](https://github.com/SydMontague/NutCracker)
  * a decompiler for the game's Squirrel script files
