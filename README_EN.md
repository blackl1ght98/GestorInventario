# Installation Guide for the Inventory Manager Project

# 📑 Table of Contents (under construction)
- ⚠️ Requirements to run with Docker
- ⚠️ Requirements to run locally
- 🔑 HTTPS Certificate Generation
- 🐳 Getting Started for Docker Execution
- 📝 Test Credentials
- Getting Started Locally
   - 📂 Restore Database
   - ⚙️ Scaffold-DbContext
       - 🔑 Scaffold-DbContext with username and password (recommended)
   - 🔐 Configure User Secrets
   - ⚙️ Modify GestorInventarioContext.cs    
- 🐳 Common Issues (Docker / Visual Studio / WSL)
- ✨ Features
- 🆕 What's New
- 🧠 Important Notes

# ⚠️ Requirements to run with Docker
Before starting, make sure you have the following installed:
-  [Docker Desktop](https://www.docker.com/products/docker-desktop/)
-  [Git](https://git-scm.com/)

# ⚠️ Requirements to run locally
Before starting, make sure you have the following installed:
-  [Visual Studio 2022](https://visualstudio.microsoft.com/) 
-  [Git](https://git-scm.com/)  
-  [SQL Server](https://www.microsoft.com/es-es/download/details.aspx?id=104781)
-  [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup) to manage the DB


# 🐳 Getting Started for Docker Execution
1. Clone the repository with the command:
```sh
git clone https://github.com/blackl1ght98/GestorInventario
```
2. Run the Installer
To have everything configured, run the **install.ps1** script. This script is a guided installer that will tell you what values to enter.
```powershell
./install.ps1
```
If you are on Linux, run:
```sh
sudo ./install-linux.sh
```
**NOTE**: This script will create a .env file upon completion. This file will contain the value of each environment variable. If you also want to run it locally in addition to Docker, this .env file will be very helpful for filling out the secrets file.

# Test Credentials
- **Email**: keupa@yopmail.com
- **Password**: 1a2a3a4a5
- These test credentials belong to the administrator user.
 Once installed, restart Docker and you will be able to start it.

# Getting Started Locally:
## 📂 Restore the Backup
The first step we need to take is restoring the database. To do this, follow these steps:
1. Open **SSMS**
2. Check the **Trust server certificate** box
3. Click Connect
4. In the **Object Explorer**, right-click on **Databases** → **Restore database**. If you have the program in English, the option to restore the database is called **Database** -> **Restore database**.
5. To avoid problems with the database restoration, move the `.bak` file to the path `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\`. The `MSSQL17.SQLEXPRESS` folder may vary depending on the version you have, but this is the typical path.
6. Once in the restore window, do the following:
      - Select **Device**.
      - Click the `...` button
      - Click **Add** and locate the `.bak` file in the `Backup` folder.
      - Confirm with **OK**.
      - Click **OK** again to complete the restoration.



## 🔐 Configure User Secrets

To access the **User Secrets** file in Visual Studio 2022:  
`Right-click on the project > Manage User Secrets`.

Then, add the following values in JSON format:

```json
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "ConnectionStringLocal": "127.0.0.1:6379"
  },


  "AuthMode": "AsymmetricDynamic",
  "LoginMode": "MfaLogin",

  "JWT": {
    "PublicKey": "",
    "PrivateKey": "",
    "Issuer": "GestorInvetarioEmisor",
    "Audience": "GestorInventarioCliente",
    "ClaveJWT": "IntroduceClaveLargaergoherofiygkeuidgrf7ieurygf97836trf98egfiuytrf"
  },


  "DataBaseConection": {
    "DBHost": "localhost\SQLEXPRESS",
    "DockerDbHost": "SQL-Server-Local",
    "DBName": "GestorInventario",
    "DBUserName": "sa",
    "DBPassword": "SQL#1234"
  },
  "App": {
    "BaseUrl": "https://localhost:7056",
    "DockerUrl": "https://localhost:8080"

  },
  "CallMeBot": {
    "TelegramUser": ""
  },
  "Paypal": {
    "ClientId": "",
    "ClientSecret": "",
    "BaseUrl": "https://api-m.sandbox.paypal.com/",
    "ReturnUrls": {
      "Development": "https://localhost:7056/Payment/Success",
      "Docker": "https://localhost:8081/Payment/Success"
    },
    "CancelUrls": {
      "Development": "https://localhost:7056/Payment/Cancel",
      "Docker": "https://localhost:8081/Payment/Cancel"
    }

  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UserName": "",
    "PassWord": ""
  }
}
```
If you have run the **install.ps1** script, this script will have created an environment variables file for you. You can use this file to fill in the missing values in the secrets file.


## ⚙️ Scaffold-DbContext
The scaffold will only be executed if the database changes. As long as the database does not change, the scaffold will not be executed.
To run this command, follow these steps:


1. Open **Visual Studio**
2. Open the console from: `View > Other Windows > Package Manager Console`.  
3. Run the command:

```sh
Scaffold-DbContext "Data Source=localhost\SQLEXPRESS;Initial Catalog=GestorInventario;User ID=sa;Password=SQL#1234;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -ContextDir ../GestorInventario.Infrastructure/Data -ContextNamespace "GestorInventario.Infrastructure.Data" -Namespace "GestorInventario.Domain.Models" -force -Project GestorInventario.Domain -NoOnConfiguring 
```


# 🐳 Common Issues (Docker / Visual Studio / WSL)
## Visual Studio and Docker
If you **do not have Docker Desktop installed**, Visual Studio may show a compilation error when trying to interpret the `docker-compose` file.

To fix this, follow these steps:

1. Open **Visual Studio** and go to the **Solution Explorer**.  
2. **Right-click** on the `docker-compose` project.  
3. Select **"Unload Project"**.  
4. Rebuild the project → the error will no longer appear.

If you later install **Docker Desktop**, you can re-enable `docker-compose` by right-clicking on the project and selecting **"Reload Project"**.  

# Problem Installing Docker
To fix this problem, follow these steps:
1. Go to the C drive in Windows and enable the option to view hidden files.
2. Go to the folder called **ProgramData**
3. Inside that folder you will see a folder called **DockerDescktop**
4. Delete that folder
With these steps completed, the installation will finish.

# Problem Starting Docker (WSL)
Docker itself tells us to run the following command in the terminal:
```sh
wsl --update
```
but if this doesn't solve it, what we will do is download the latest version of WSL from the Microsoft repository: [WSL](https://github.com/microsoft/WSL/releases), install the latest version of the program, and the problem will be solved.

## Solution to Database Restore Error on Linux (Arch)
To fix this problem, run the following commands:
```sh
  sudo pacman -S git-lfs
  git lfs install
  git lfs pull

```
This will add the missing data to the .bak file.

# ✨ Features

The **Inventory Manager** project offers a wide range of features to efficiently manage inventory:

- **Data Management**: Allows CRUD (Create, Read, Update, Delete) operations.
- **Robust Authentication**: The authentication system is based on token generation and offers three authentication methods: Symmetric Authentication, Asymmetric Authentication with fixed public and private key, Asymmetric Authentication with dynamic public and private key.
- **Report Generation**: Users can download reports in PDF format for order history and products.
- **Email Notifications**: The system sends email notifications when a product's stock is low.
- **User Registration and Login**: Users can register and access the system. When a new user registers, a confirmation email is sent.
- **User Administration Panel**: The project includes a user administration panel to manage user accounts.
- **Role-Based System**: Access to different levels of the system is controlled through a role-based system.
- **PayPal Payment Gateway**: The project includes the implementation of a PayPal payment gateway.
- **Password Reset**: Both the user and the administrator can reset the password. If it is a user, they can only reset their own, and an administrator can reset everyone's.
- **Authentication Flexibility**: Users can switch between authentication modes effectively by commenting and uncommenting the corresponding code.
- **User Enable/Disable**: The administrator can enable or disable one or more users.
- **Docker**: Necessary configuration to integrate with Docker.
- **Redis**: Necessary configuration for it to work correctly with Redis.
- **Refund Function**: Now includes the function to refund an order.
- **Creation of Plans and Products with PayPal**: Currently includes the functionality to create products and plans in PayPal.
- **Plan Subscription Function**: Currently includes the possibility to subscribe to plans.
- **View Plans**: Users can view the plans they can subscribe to.
- **View Subscribers**: The administrator can see how many subscribers there are.
- **View Products**: The administrator can view the products associated with the plans.
- **Price Change in Plans**: The administrator can change the price of the plans.

# 🆕 What's New
- **Partial Refunds**: This new feature allows returning part of the products from an order. This feature will be visible as long as the order placed has more than one product.
- **Barcode Generation**: New feature that simulates a store environment.
- **Add Shipping Information**: With this new functionality, we can add information about which company is in charge of delivering the order.
- **Subscription Activation**: The administrator can activate a cancelled or suspended subscription.
- **Suspend Subscription**: The user can suspend their own subscription, and the administrator can suspend all of them.
- **Cancel Subscription**: The user can cancel their own subscription, and the administrator can cancel any subscription.
- **Add Tracking Information to Orders**: The administrator can add tracking information to orders.
- **MFA Implemented**: Two-factor authentication implemented globally.
- **Telegram Notifications**: Notifies the user of important events.

  # 🧠 Important Notes

- ✅ Project tested on **Windows 10** and **Windows 11**.  
- ⚠️ **Not tested on Linux or MacOS** (may require additional adjustments).  
- 🔧 It is recommended to install and use **SQL Server Express** with **SQL Server Management Studio** (SSMS).  
- 🔑 Keep credentials and JWT keys in **User Secrets** or environment variables (not in the source code) when integrating new ones.  
- 💳 PayPal integration works in **sandbox mode** by default.  
- 🌐 If you want to go to production, remember to change `Mode: sandbox` → `Mode: live` and register your real credentials in PayPal Developer.
