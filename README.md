# NodeRunner - Hytopia Node Manager  
Version: 1.0.4  
Author: BlockchainEazy

![NodeRunner_M15dI4Jz4m](https://github.com/user-attachments/assets/c55ea0bf-9588-4bc2-b62c-5c6e3ae69513)

## ⚠️ Disclaimer – Not Affiliated with Hytopia
*This software is NOT created, endorsed, or affiliated with Hytopia in any way.  
It is an independent tool developed by BlockchainEazy to help users easily manage and run their nodes on their own machines.*

---

## 📥 Installation & Setup

### 1️⃣ Clone the Repository

Open a terminal or PowerShell and run:

```bash
git clone https://github.com/Eazyny/NodeRunner-1.0.0.git
cd NodeRunner-1.0.0
```

---

### 2️⃣ Install Dependencies

Make sure you have [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) installed.

Then, restore dependencies by running:

```bash
dotnet restore
```

---

### 3️⃣ Download Required Files

The validation engine and guardian executable are required to run the nodes.

Download them from the official Hychain GitHub:  
[🔗 HYCHAIN Guardian Node Software](https://github.com/HYCHAIN/guardian-node-software/releases/tag/0.0.1)

Place the following into the `scripts` folder:

- `guardian-cli-win.exe` *(Windows users)*
- `guardian-cli-linux` *(Linux users — if available)*
- `validation-engine/` folder *(containing jit + replay.wasm)*

---

## ⚙️ Configuring the Scripts

Inside the `scripts` folder, you'll find three script files that must be configured before use:

### 1️⃣ `check.bat` / `check.sh` – Check rewards

```bash
guardian-cli-win.exe guardian reward-to-claim <node_number_1,node_number_2,...>
```

Modify the file and replace the node list with your own node IDs.

---

### 2️⃣ `claim.bat` / `claim.sh` – Claim rewards

```bash
guardian-cli-win.exe guardian claim-rewards <your_private_key> --owned-keys --approved-keys
```

Replace `<your_private_key>` with the wallet private key managing your nodes.

⚠️ **Do not use a wallet with funds. Use a cold wallet or Delegate.xyz setup.**

---

### 3️⃣ `start.bat` / `start.sh` – Start nodes

```bash
guardian-cli-win.exe guardian run <your_private_key> --loop-interval-ms 3600000
```

Again, replace `<your_private_key>` with your node wallet key.

⚠️ **Do not use a wallet that holds assets.**

---

## 🚀 Running the Application (Development)

Run directly from terminal:

```bash
dotnet build
dotnet run
```

---

## 🛠️ Create Executables

### ▶️ For Windows:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Result:

```plaintext
bin\Release\net8.0\win-x64\publish\NodeRunner.exe
```

---

<details>
<summary><strong>🐧 How to Run NodeRunner on Linux</strong></summary>

<br>

### 📦 Requirements

- .NET 6+ or 8+ SDK
- `.sh` versions of `start`, `check`, and `claim` scripts

---

### 📁 Setup Example

If you've created your `.sh` scripts, run the commands below to make them executable or else you will get a permission denied error:

```bash
chmod +x /path/to/your-scripts/start.sh
chmod +x /path/to/your-scripts/check.sh
chmod +x /path/to/your-scripts/claim.sh
```

---

### 🧱 Build for Linux

```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

Navigate to the output folder:

```bash
cd bin/Release/net8.0/linux-x64/publish
./NodeRunner
```

---

### 🖱 In-App Setup

Once NodeRunner is open:

1. Click **"Select Scripts Folder"**
2. Choose the folder with your `.sh` files
3. Click **Run Nodes / Check / Claim**

✅ NodeRunner will automatically detect Linux and run `.sh` instead of `.bat`.

</details>

---

## 📄 Version History

| Version | Highlights |
|---------|------------|
| `v1.0.4` | ✅ Linux/macOS support, auto `.sh` detection, cross-platform process killing |
| `v1.0.3` | 🧠 Auto-restart on critical error, balance parsing, wallet info |
| `v1.0.2` | 📊 Auto-updating pending balance, improved UI |
| `v1.0.1` | 🛠 Initial stable Avalonia release |

---

## ⚠️ Legal Disclaimer

This software is provided **as-is**, without any warranties or guarantees.  
By using this tool, you agree to the following:

- This is not official Hytopia software.
- You are responsible for your private keys and credentials.
- Use a separate wallet with no funds.
- The developer is not responsible for loss, damage, or misconfiguration.
- This is for educational and personal use only.

---

## 🙌 Support & Contributions

- Encounter an issue? Open a GitHub [Issue](https://github.com/Eazyny/NodeRunner-1.0.0/issues)
- Contributions welcome — fork the repo and submit a pull request!
