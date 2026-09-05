[README.md — Clothing Store.md](https://github.com/user-attachments/files/31870126/README.md.Clothing.Store.md)
# 🛍️ Clothing Store — E-commerce Web Application

A full-stack **e-commerce clothing store web application** built with **ASP.NET Core MVC** and **Entity Framework Core**.

The project provides a complete online shopping workflow, including product browsing, authentication, shopping cart, checkout, order management, product reviews, vouchers, and online payment integration.

> 🎓 This project was developed as a practical software engineering project to apply concepts of **ASP.NET Core MVC, Entity Framework Core, SQL Server, Authentication, REST APIs, and third-party payment services**.

---

## 📌 Overview

**Clothing Store** is an online fashion shopping platform inspired by modern e-commerce websites.

The system supports two main types of users:

- **Customer** — browse products, manage cart, place orders and track order history.
- **Administrator** — manage products, categories, orders, vouchers and other store data.

The application follows the **MVC (Model–View–Controller)** architecture and uses **Entity Framework Core Code First** for database access.

---

## ✨ Features

### 👤 Authentication & Authorization

- User registration and login
- Logout
- Password hashing
- Session-based user information
- Role-based access between customers and administrators
- Google authentication integration
- Authentication validation and authorization

### 🛒 Product & Shopping

- Browse products
- View product details
- Filter products by category
- Product image upload
- Product quantity management
- Add products to cart
- Update cart quantity
- Remove products from cart
- Calculate cart subtotal and total

### 🎟️ Voucher / Coupon

- Apply discount codes during checkout
- Validate voucher availability
- Calculate discounted order total
- Prevent invalid or expired vouchers

### 📦 Order Management

Customers can:

- Create orders
- Select shipping address
- Select payment method
- View order details
- View order history
- Track order status

Administrators can:

- View orders
- Update order status
- Manage order information

### 💳 Payment

The project was designed to support multiple payment methods:

| Payment Method | Status |
|---|---|
| Cash on Delivery (COD) | ✅ |
| PayPal | ✅ / Integration |
| MoMo | 🔧 Integration |
| VNPay | 🔧 Integration |
| Credit/Debit Card | 🔧 Planned / Provider-dependent |

Third-party payment services are integrated through their respective APIs/payment gateways rather than implementing payment processing directly inside the application.

### ⭐ Product Reviews

- Customers can submit product reviews
- Store rating and review information
- Display product reviews

### 🖼️ Product Image Management

Product images are uploaded through `IFormFile` and stored under:

```text
wwwroot/images/
```

The application validates uploaded image files before saving them.

---

## 🏗️ Architecture

The project follows the **ASP.NET Core MVC architecture**:

```text
┌───────────────────────────────┐
│            Views              │
│       Razor / HTML / CSS      │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│         Controllers           │
│    Request / Business Flow    │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│            Models             │
│     Entities / ViewModels     │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│      Entity Framework Core    │
│          DbContext            │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│          SQL Server           │
└───────────────────────────────┘
```

---

## 🧰 Technologies

### Backend

- **C#**
- **ASP.NET Core MVC**
- **Entity Framework Core**
- **LINQ**
- **Dependency Injection**
- **ASP.NET Core Authentication & Authorization**

### Frontend

- **Razor Views**
- **HTML5**
- **CSS3**
- **JavaScript**
- **Bootstrap**

### Database

- **Microsoft SQL Server**
- **Entity Framework Core Code First**
- **EF Core Migrations**

### External Services / APIs

- Google OAuth
- PayPal
- MoMo
- VNPay
- Email service

### Development Tools

- Visual Studio
- SQL Server Management Studio
- Git
- GitHub

---

## 🗄️ Database Design

The application uses a relational database managed through **Entity Framework Core Code First**.

Main entities include:

```text
User
Account
Product
Category
Order
OrderDetail
PaymentMethod
Voucher
Review
Address
Cart
CartItem
```

High-level relationship:

```text
User
 │
 ├── Address
 │
 ├── Order
 │     └── OrderDetail
 │             └── Product
 │
 ├── Cart
 │     └── CartItem
 │             └── Product
 │
 └── Review
       └── Product

Category
 └── Product

Voucher
 └── Order

PaymentMethod
 └── Order
```

---

## 🔐 Authentication Flow

The application separates user information from account/login information.

A simplified authentication flow:

```text
User
  │
  ▼
Login / Register
  │
  ▼
Account Validation
  │
  ▼
Password Verification
  │
  ▼
Authentication
  │
  ▼
Session / Claims
  │
  ▼
Authorized Resources
```

External authentication such as Google OAuth can be used to authenticate users through a third-party identity provider.

---

## 💰 Checkout Flow

The checkout process follows this general workflow:

```text
Browse Products
      │
      ▼
Add to Cart
      │
      ▼
View Cart
      │
      ▼
Apply Voucher
      │
      ▼
Enter / Select Shipping Address
      │
      ▼
Select Payment Method
      │
      ▼
Create Order
      │
      ▼
Payment Processing
      │
      ▼
Order Confirmation
```

---

## ⚙️ Getting Started

### 1. Prerequisites

Make sure the following software is installed:

- .NET SDK 8
- Visual Studio 2022
- SQL Server
- SQL Server Management Studio
- Git

Verify .NET:

```bash
dotnet --version
```

---

### 2. Clone the repository

```bash
git clone https://github.com/YOUR_USERNAME/YOUR_REPOSITORY.git

cd YOUR_REPOSITORY
```

---

### 3. Configure the database

Update the connection string in:

```text
appsettings.json
```

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Clothing_Store;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

> Replace `YOUR_SERVER` with your local SQL Server instance.

---

### 4. Apply Entity Framework migrations

Open the Package Manager Console in Visual Studio:

```powershell
Update-Database
```

Or using the .NET CLI:

```bash
dotnet ef database update
```

If migrations have not been created yet:

```bash
dotnet ef migrations add InitialCreate
```

---

### 5. Configure external services

If you want to use external authentication or payment services, configure their credentials through configuration/environment variables.

For example:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
  }
}
```

**Do not commit real API keys, OAuth secrets, payment credentials, or connection strings containing passwords to GitHub.**

Use:

- User Secrets
- Environment Variables
- `.gitignore`
- Deployment platform secret management

---

### 6. Run the application

Using Visual Studio:

```text
Open .sln
        ↓
Build Solution
        ↓
Run / Debug
```

Or using CLI:

```bash
dotnet restore
dotnet build
dotnet run
```

The application will then be available through the local development URL shown by ASP.NET Core.

---

## 📁 Project Structure

A simplified project structure:

```text
MTKPM_Clothing_Store_web/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── ProductsController.cs
│   ├── CartController.cs
│   ├── OrdersController.cs
│   ├── PaymentsController.cs
│   └── ...
│
├── Models/
│   ├── User.cs
│   ├── Account.cs
│   ├── Product.cs
│   ├── Category.cs
│   ├── Order.cs
│   ├── OrderDetail.cs
│   ├── Voucher.cs
│   ├── Review.cs
│   └── ...
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Services/
│   ├── Payment/
│   ├── Email/
│   └── ...
│
├── ViewModels/
│   └── ...
│
├── Views/
│   ├── Account/
│   ├── Products/
│   ├── Cart/
│   ├── Orders/
│   ├── Payments/
│   └── Shared/
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   └── ...
│
├── Migrations/
│
├── appsettings.json
├── Program.cs
└── MTKPM_Clothing_Store_web.csproj
```

> The exact structure may differ depending on the current version of the project.

---

## 🔌 API & Third-Party Integration

This project demonstrates how a web application can integrate external services through APIs.

Examples include:

### Google OAuth

Used for:

```text
User
 ↓
Google Login
 ↓
Google OAuth
 ↓
Authentication Callback
 ↓
Application Login
```

### Payment Gateways

Payment providers such as PayPal, MoMo and VNPay are integrated as external payment services.

The application sends the required transaction information to the payment provider and handles the returned payment result.

This allows the application to separate:

```text
Application Business Logic
          │
          ▼
Payment Service
          │
          ▼
External Payment API
```

---

## 🧪 Testing

Before deploying the application, the following workflows should be tested:

### Authentication

- [ ] Register account
- [ ] Login
- [ ] Logout
- [ ] Invalid login
- [ ] Google login
- [ ] Authorization

### Product

- [ ] Browse products
- [ ] Search/filter
- [ ] View product details
- [ ] Upload product image
- [ ] Create/update/delete product

### Cart

- [ ] Add product
- [ ] Update quantity
- [ ] Remove product
- [ ] Calculate total

### Checkout

- [ ] Apply voucher
- [ ] Select address
- [ ] Select payment method
- [ ] Create order
- [ ] Clear cart after successful checkout

### Payment

- [ ] COD
- [ ] PayPal
- [ ] MoMo
- [ ] VNPay
- [ ] Payment success/failure handling

### Order

- [ ] View order history
- [ ] View order details
- [ ] Update order status

---

## 🔒 Security Considerations

The project follows several basic security practices:

- Passwords should never be stored as plain text.
- Sensitive API credentials should not be committed to source control.
- User authorization should be checked before accessing administrative functionality.
- Uploaded files should be validated before being stored.
- Database access is handled through Entity Framework Core.
- Production credentials should be stored using secure configuration mechanisms.

Before publishing the repository publicly, check the repository for:

```text
API Keys
Client Secrets
Database Passwords
Payment Credentials
OAuth Secrets
Email Passwords
```

---

## 🚀 Future Improvements

Possible improvements for future versions:

- [ ] Implement ASP.NET Core Identity
- [ ] Improve role-based authorization
- [ ] Add product search with advanced filtering
- [ ] Add pagination
- [ ] Improve admin dashboard
- [ ] Add sales analytics
- [ ] Add product stock management
- [ ] Add email notifications
- [ ] Add automated unit/integration tests
- [ ] Improve payment transaction handling
- [ ] Add Docker support
- [ ] Deploy to a cloud platform
- [ ] Implement CI/CD with GitHub Actions
- [ ] Improve application logging and monitoring
- [ ] Add Redis caching
- [ ] Add API layer for mobile/frontend clients

---

## 🎯 What I Learned

Through this project, I practiced:

- Designing relational databases
- Entity Framework Core Code First
- Database migrations
- ASP.NET Core MVC architecture
- Dependency Injection
- Authentication and authorization
- Session management
- CRUD operations
- Shopping cart implementation
- Checkout and order processing
- Payment gateway integration
- OAuth authentication
- File upload handling
- Form validation
- ViewModels
- Git and GitHub
- Debugging real-world application errors

---

## 👨‍💻 Author

**Bùi Thiện Tâm**

Software Engineering Student  
HUFLIT

GitHub: `https://github.com/THD28136`

**Trần Quốc Vương**

Software Engineering Student  
HUFLIT

GitHub: `https://github.com/zuongne`

**Nguyễn Ngọc Gia Hưng**

Software Engineering Student  
HUFLIT

GitHub: `https://github.com/hung3823242-bit`

---

## 📄 License

This project was developed for educational and portfolio purposes.

---

## ⭐ Acknowledgements

This project was developed as part of the author's learning journey in:

- Web Application Development
- Software Engineering
- Database Design
- ASP.NET Core
- E-commerce System Development
