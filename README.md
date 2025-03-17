# Welcome to the Windows Advanced Settings repo

This repository contains the source code for:

* [Windows Advanced Settings](https://aka.ms/WindowsAdvancedSettings)

## Installing and running Windows Advanced Settings

> **Note**: Windows Advanced Settings requires Windows 11 21H2 (build 22000) or later.

### Microsoft Store

You can also install the Windows Advanced Settings directly from its [Microsoft Store listing](https://aka.ms/WindowsAdvancedSettings).

### Other install methods

#### Via GitHub

For users who are unable to install the Windows Advanced Settings from the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/WindowsAdvancedSettings/releases).

---

## Windows Advanced Settings overview

Please take a few minutes to review the overview below before diving into the code:

---

## Documentation

Documentation for the Windows Advanced Settings can be found at https://aka.ms/WindowsAdvancedSettingsDocs.

---

## Contributing

We are excited to work alongside you, our amazing community, to build and enhance the Windows Advanced Settings!

***BEFORE you start work on a feature/fix***, please read & follow our [Contributor's Guide](https://github.com/microsoft/WindowsAdvancedSettings/blob/main/CONTRIBUTING.md) to help avoid any wasted or duplicate effort.

## Communicating with the team

The easiest way to communicate with the team is via GitHub issues.

Please file new issues, feature requests and suggestions, but **DO search for similar open/closed preexisting issues before creating a new issue.**

If you would like to ask a question that you feel doesn't warrant an issue (yet), please reach out to us via Twitter:

* Kayla Cinnamon, Senior Product Manager: [@cinnamon_msft](https://twitter.com/cinnamon_msft)
* Clint Rutkas, Principal Product Manager: [@clintrutkas](https://twitter.com/clintrutkas)

## Developer guidance

* You must be running Windows 11 21H2 (build >= 10.0.22000.0) to run Dev Home
* You must [enable Developer Mode in the Windows Settings app](https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development)

## Building the code

* Clone the repository
* Uninstall the Preview version of the Windows Advanced Settings (Dev Home has a hard time choosing which extension to use if two versions exist)
* Open `WindowsAdvancedSettings.sln` in Visual Studio 2022 or later and build from the IDE, or run `build\scripts\build.ps1` from a Visual Studio command prompt.

## Code of conduct

We welcome contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
