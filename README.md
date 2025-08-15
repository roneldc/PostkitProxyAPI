# Postkit Proxy API

An **ASP.NET Core Minimal API** serving as the **Backend-for-Frontend (BFF)** for the Postkit UI.  
Its main purpose is to handle client requests securely, act as a proxy to backend services, and centralize integration logic.

---

## 🚀 Features

- **Backend-for-Frontend Architecture**  
  Designed to serve Postkit UI (Blazor UI client with tailored endpoints).
- **Proxy Request Handling**  
  Forwards requests to backend APIs while managing authentication, headers, and query parameters.

- **Centralized API Integration**  
  Acts as a single entry point for multiple backend services.

- **Security**  
  Supports authentication headers and can enforce additional API key checks.

- **Minimal API**  
  Lightweight and fast to start up, using the latest ASP.NET Core features.

---

## Tech Stack

**Backend**

- .NET 8 Minimal API
- HttpClientFactory
- C# 12

---

## Running Locally

```
dotnet restore
dotnet run
```

## License

MIT

## Author

John Ronel Dela Cruz  
Full-stack Software Engineer
