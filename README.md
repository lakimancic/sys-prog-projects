# 🧠 C# Concurrent Programming & Reactive Systems

This repository contains three independent C# solutions demonstrating different concurrency and data-processing paradigms:

1. **Project01 – Multithreaded Web Server (Threads & ThreadPool)**
2. **Project02 – Asynchronous Web Server (Tasks & Async/Await)**
3. **Project03 – Reactive Programming with Yelp Fusion API**

Each project is implemented as a standalone Visual Studio solution and can be run using `dotnet run` from its directory.

---

## 📁 Project01 – Multithreaded Web Server (Threads & ThreadPool)

### 📝 Description
A console web server that handles HTTP GET requests using **threads and ThreadPool**. It:
- Logs all requests and processing details
- Caches responses in memory to speed up repeated requests
- Allows searching for songs/albums using the Spotify API

### 🧩 Structure
- `Program.cs` – Entry point
- `HttpServer/HttpServer.cs` – Handles HTTP requests
- `Caches/SpotifyCache.cs` – Thread-safe caching
- `Spotify/SpotifyFetcher.cs` – Calls Spotify API
- `Models/` – Contains data models (Album, Artist, Track, CacheEntry)

### ▶️ Run
```bash
cd Project01
dotnet run
```

### ⚙️ Requirements
- .NET 9.0 or newer
- Spotify API access token in `.env` file
- NuGet packages: DotNetEnv, Serilog, Sprache, Microsoft.Extensions.Configuration

---

## 📁 Project02 – Asynchronous Web Server (Tasks & Async/Await)

### 📝 Description
Reimplements Project01 using **Tasks** and **async/await** for scalable and non-blocking request handling.  
ThreadPool and synchronization mechanisms are still used where Tasks do not apply.

### 🧩 Structure
- `Program.cs` – Entry point
- `HttpServer/HttpServer.cs` – Asynchronous HTTP server
- `Caches/SpotifyCache.cs` – Async-safe caching
- `Spotify/SpotifyFetcher.cs` – Calls Spotify API asynchronously
- `Models/` – Same as Project01

### ▶️ Run
```bash
cd Project02
dotnet run
```

### ⚙️ Requirements
- .NET 9.0 or newer
- Spotify API access token in `.env` file
- NuGet packages: DotNetEnv, Serilog, Sprache, Microsoft.Extensions.Configuration

---

## 📁 Project03 – Reactive Programming with Yelp Fusion API

### 📝 Description
A reactive console application using **Reactive Extensions**. It:
- Fetches restaurant reviews using Yelp Fusion API
- Processes reviews as observable streams
- Performs **topic modeling** on reviews using `SharpEntropy`

### 🧩 Structure
- `Program.cs` – Entry point
- `Reactive/HttpServer.cs` – Reactive request handling
- `Reactive/ResultObserver.cs` – Observes and processes incoming data
- `Reactive/TopicModeler.cs` – Performs topic modeling
- `Reactive/YelpFetchCall.cs` – Calls Yelp API
- `Utils/ObserverListExtensions.cs` – Helper for reactive collections
- `Yelp/YelpFetcher.cs` – Handles Yelp API interaction
- `Models/` – Review and User models

### ▶️ Run
```bash
cd Project03
dotnet run
```

### ⚙️ Requirements
- .NET 9.0 or newer
- Yelp API access key in `.env` file
- NuGet packages: DotNetEnv, Serilog, System.Reactive, SharpEntropy, Newtonsoft.Json, Microsoft.ML

---

## 🔗 Common Notes
- Each project is standalone and can be run independently using `dotnet run` in its folder.
- Server logs are written to `logs/` inside each project.
- API keys should be stored in a `.env` file in the project root.
- Projects are structured using a modular approach with separate folders for Models, HttpServer, Spotify/Yelp API calls, and caching.

