# NodeRunner - Hytopia Node Manager
Version: 1.0.0
Author: BlockchainEazy

![NodeRunner_M9sqWVhY9t](https://github.com/user-attachments/assets/583c4c6a-2fcc-4371-bf7c-55657dc64f2d)


## ⚠️ Disclaimer – Not Affiliated with Hytopia
*This software is NOT created, endorsed, or affiliated with Hytopia in any way.
It is an independent tool developed by BlockchainEazy to help users easily manage and run their nodes on their own machines.*

## 📥 Installation & Setup

1️⃣ Clone the Repository

Open a terminal or PowerShell and run:

*git clone https://github.com/Eazyny/NodeRunner-1.0.0.git
cd NodeRunner-1.0.0*

2️⃣ Install Dependencies

Make sure you have .NET 8 SDK installed.

Then, restore dependencies by running:

*dotnet restore*

3️⃣ Download Required Files

The validation engine and guardian executable are required to run the nodes.

Download them from official Hychain GitHub:
[🔗 HYCHAIN Guardian Node Software](https://github.com/HYCHAIN/guardian-node-software/releases/tag/0.0.1)

Add the following files to the scripts folder:

guardian-cli-win.exe

validation-engine (folder containing jit and replay.wasm)


## ⚙️ Configuring the Scripts
Inside the scripts folder, there are three .bat files that must be configured before using the app.

1️⃣ **check.bat**: Used to check pending rewards for your nodes.

Modify the file and replace (add node numbers here) with your node IDs (comma-separated, no spaces).

guardian-cli-win.exe guardian reward-to-claim <node_number_1,node_number_2,...>


2️⃣ **claim.bat**: Used to claim node rewards.

Replace <your_private_key> with the private key of the wallet managing the nodes.

guardian-cli-win.exe guardian claim-rewards <your_private_key> --owned-keys --approved-keys

⚠️ DO NOT use a wallet that holds valuable assets. Use Delegate.xyz if your nodes are in a vault.


3️⃣ **start.bat**: Starts running the nodes.

Replace <your_private_key> with your node wallet's private key.

guardian-cli-win.exe guardian run <your_private_key> --loop-interval-ms 3600000

⚠️ DO NOT use a wallet that holds valuable assets.


## 🚀 Running the Application

Run via Terminal (Use without creating EXE but need to keep VS Code open)

To start the app, use:

*dotnet build*
*dotnet run*

## Generate an Executable (.exe)

To create a Windows executable file:

*dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true*

This will generate a standalone .exe file inside:

bin\Release\net8.0\win-x64\publish\NodeRunner.exe


⚠️ Disclaimer
This software is provided as-is, without any warranties or guarantees. By using this tool, you acknowledge and accept the following:

*This software is NOT made by, endorsed by, or affiliated with Hytopia.
You are responsible for safeguarding your private keys. The developer assumes no responsibility for compromised or misused credentials.
The tool is open-source and provided for educational purposes only.
Use at your own risk. The developer is not liable for any losses or damages resulting from the use of this software.
🛠️ Support & Contributions
If you encounter any issues, please open a GitHub Issue.
Contributions are welcome! Feel free to fork the repo and submit a pull request.*
