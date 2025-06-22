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

YAML-MBE Tool (YMT) is a command-line tool based off [SydMontague's DSCS_Tools](https://github.com/SydMontague/DSCSTools) to extract and repack `.mbe` files into YAML format making them easier to read and modify. It is built using C# .Net 8.0 and leverages the `YamlDotNet` library for YAML parsing and serialization. The tool supports multi-threading for performance 2.5x the speed of extracting using DSCS_Tools.


## Limitations

Currently YMT does not support extracting `data` mbes. This is due to the fact this tool uses Classes to represent the data in the MBE files rather than structure jsons, and the data mbes are not structured in a way that can be easily represented by classes. This may change in the future, but for now, you will need to use DSCS_Tools to extract data mbes.
##  Getting Started

###  Prerequisites

Before getting started with YAML_MBE_Tool, ensure your runtime environment meets the following requirements:

- **Language:** C# .Net 8.0
- **Package Manager:** Nuget


###  Installation

Install YAML_MBE_Tool using one of the following methods:

**Build from source:**

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


###  Usage

```sh


  mbeextract    Extract a .mbe file or a directory of them into YAML.

  mbepack       Repack a YAML file or directory of YAML files into a .mbe file.

  help          Display more information on a specific command.

  version       Display version information.

  -v, --verbose           (Default: false) Prints all messages to standard output.

  -m, --smt               (Default: false) Enables MultiThreading.

  --disableprogressbar    (Default: false) Disables the progress bar.

  --help                  Display this help screen.

  --version               Display version information.
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