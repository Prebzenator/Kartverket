# Kartverket
Repository for **group 12** in IS-200, 201, 202.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
**Quick Overview:** 

A robust MVC web application for registering and managing aviation obstacles. The system features role-based access control (Pilots, Registry Admins), interactive maps (Leaflet/GeoJSON), and a complete review workflow (Draft -> Pending -> Approved). It uses the "Post-Redirect-Get" pattern and persists data to a MariaDB database running in Docker.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Prerequisites:**

* **.NET 9 SDK**
* **Docker Desktop** (Required for containerization)
* **MariaDB / MySQL Client** (Optional for direct DB inspection)


**Drift / Usage**

The application is fully containerized for consistent deployment!!

To use this application you can do the following:

* Check the prerequisites
* Clone this repository
* Make sure your 3306:3306 port is available
If your 3306:3306 port can't be made available, you have to change the code and manually choose a port yourself.
* Open the WebApplication1.sln
* After this you are ready to press Docker Compose, or run: docker compose up --build
* Depending on your IDE settings, you might need to manually remove the automatic containers that starts. If you face this issue, you simply remove the running containers from Docker desktop, then use "docker compose up --build" in the terminal

**First Run** 
The system will automatically apply EF Core Migrations and seed the initial System Administrator user.
Access the app at http://localhost:5000.

To access the Administrator accounts, you need to log into the System Administrator account. This account has both System- and Registry Administrator rights. This is also the only way to generate new Administrator accounts. After you've logged into this account, you can create new users (both pilots and Registry Administrators). After you create a user this way, you get a temporary password, which you will need the first time you log into the account (same as for System Administrator).
For safety reasons, we recommend changing the System Administrator password on first login. 
You will find the initial EMAIL and PASSWORD for System Administrator in RoleSeeder.cs

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**System Architecture**

The application is built on **ASP.NET Core 9** using a strict Model-View-Controller (MVC) pattern.

* **Controllers:** Manage application flow and enforce Authorization.
    * `PilotController`: Handles organization-specific reporting and crew resource management.
    * `AdminObstacleController`: Manages the approval/rejection workflow for Registry Administrators.
    * `ObstacleController`: Handles the public/pilot data entry forms.
    * `AccountController`: Manage user registration and user information
    * `AdminController`: Gives Admins possibility to see map, and System Admin can create users.
    * `HomeController`: Makes the initial page correct for different users.
* **Views (Razor):** Server-side rendered HTML using Tag Helpers and Bootstrap for responsive design.
* **Models:**
    * `ObstacleData`: The core entity containing GeoJSON geometry, status enums (`Pending`, `Approved`), and audit trails.
    * `ApplicationUser`: Extended Identity user with `Organization` and `MustChangePassword` flags.
    * `ObstacleCategory`: Extends further with CategoryID and Category Name
* **Data Access:** Direct usage of **Entity Framework Core** (replacing the repository pattern) for efficient querying and automatic migrations.
* **Leaflet:** Client-side JS library for rendering maps and picking coordinates (Latitude/Longitude).

**Request flow (PRG)**
1. **GET** `/Obstacle/DataForm` --> Render form + Map
2. **POST** `/Obstacle/DataForm` --> Validate Input --> Save to DB --> Redirect
3. **GET** `/Obstacle/Overview` --> Load from DB --> Show Confirmation & Map Marker
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
**PROJECT STRUCTURE:**

Controllers/
  HomeController.cs
  AccountController.cs
  ObstacleController.cs
  PilotController.cs
  AdminObstacleController.cs
  AdminController.cs

Data/
  ApplicationDbContext.cs
  RoleSeeder.cs
  DesignTimeDbContextFactory.cs
  Migrations/  (EF Core migration files)

Models/
  ObstacleData.cs
  ApplicationUser.cs
  ObstacleCategory.cs
  ErrorViewModel.cs

ViewModels/
  AdminDashBoardViewModel.cs
  ChangePasswordViewModel.cs
  CreateUserViewModel.cs
  ForceChangePasswordViewModel.cs
  LoginViewModel.cs
  ObstacleViewModel.cs
  RegisterViewModel.cs

Views/
  Account/
    ChangePassword.cshtml
    ForceChangePassword.cshtml
    Login.cshtml
    Register.cshtml

  Admin/
    AdminMap.cshtml
    CreateUser.cshtml
    CreateUserSuccess.cshtml

  AdminObstacle/
    Dashboard.cshtml
    ViewReport.cshtml

  Home/
    Index.cshtml
    Privacy.cshtml

  Obstacle/
    DataForm.cshtml
    Overview.cshtml
    Review.cshtml

  Pilot/
    Log.cshtml
    OrganizationReports.cshtml
    ViewReport.cshtml

  Shared/
    _Layout.cshtml
    _Layout.cshtml.css
    _LoginPartial.cshtml
    _ValidationScriptsPartial.cshtml
    Error.cshtml

Root files
  Program.cs
  WebApplication1.sln
  WebApplication1.csproj
  README.md
  Dockerfile
  docker-compose.yml
  appsettings.json
  wwwroot/ (static assets: css, images, favicon)


--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Testing & Results**

<img width="910" height="584" alt="Screenshot 2025-11-21 172426" src="https://github.com/user-attachments/assets/3945c8ce-db28-4e86-8c90-e19831a29bc4" />

The system enforces strict aviation safety rules.
* **Result:** Attempting to register an obstacle height > 1000m triggers a server-side validation error (seen above).



![Map demonstration3 - Made with Clipchamp](https://github.com/user-attachments/assets/d529c289-5124-47d3-b015-64b6a22eb8fc)

**Scenario 2: Map Integration**
* **Result:** Clicking the map automatically populates the Latitude (`59.41`) and Longitude (`9.07`) fields.
* **Result:** Existing reports render their geometry correctly as GeoJSON layers


<img width="626" height="560" alt="Screenshot 2025-11-21 173634" src="https://github.com/user-attachments/assets/04c17d77-940c-4431-9a6c-8a6b67bcdddb" />

A page confirming that an obstacle has succesfully been registerted. 



<img width="1294" height="642" alt="Screenshot 2025-11-21 174006" src="https://github.com/user-attachments/assets/64fcfa57-ab13-44d4-99c6-660090b6eea5" />

**Scenario 3: Organization Visibility (Crew Resource Management)**
* **Test:** A pilot from "Norsk Luftambulanse" logs in.
* **Result:** They can see *all* reports filed by other pilots in their organization (via `PilotController.Log`), preventing duplicate registrations of the same obstacle.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Unit-, security- and integration testing**

This repository includes three test suites covering unit, security and end‑to‑end integration scenarios. The tests are organized to make it easy to run fast unit checks locally, exercise security rules in isolation, and run full integration/E2E flows against a test host.
To run these tests, you can either run the test through your IDE setup (right click the folder in solution explorer, or select wanted testproject in build). Alternatively, you can run all or select which one you wan of these:
dotnet test tests/WebApplication1.Tests.Unit/WebApplication1.Tests.Unit.csproj
dotnet test tests/WebApplication1.Tests.Security/WebApplication1.Tests.Security.csproj
dotnet test tests/WebApplication1.Tests.Integration/WebApplication1.Tests.Integration.csproj

**Structure**
tests/
  README.md
  WebApplication1.Tests.Integration/
    AuthTests.cs
    IntegrationFactory.cs
    ObstacleE2ETests.cs
    TestAuthHandler.cs
    WebApplication1.Tests.Integration.csproj

  WebApplication1.Tests.Security/
    IntegrationFactory.cs
    SecurityTests.cs
    TestAuthHandler.cs
    WebApplication1.Tests.Security.csproj

  WebApplication1.Tests.Unit/
    Controllers/
      ObstacleControllerTests.cs
    Fixtures/
      InMemoryDbFixture.cs
    Helpers/
      MockHelpers.cs
      TestTempDataProvider.cs
    UnitTest1.cs
    WebApplication1.Tests.Unit.csproj

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Security Overview**

This project includes several built‑in protections against common web vulnerabilities. Here are some of them:

- **CSRF protection**: All POST endpoints that modify data use anti‑forgery validation. See `[ValidateAntiForgeryToken]` on controller POST actions (for example `ObstacleController.DataForm`) and `@Html.AntiForgeryToken()` in `Views/Obstacle/DataForm.cshtml`.

- **XSS protection**: Razor views use automatic HTML encoding for model output (`asp-for`, `@Model.Property`), preventing reflected/stored XSS when rendering user input. Avoid `Html.Raw(...)` unless the content is explicitly sanitized and trusted. Inspect `Views/` for any raw output.

- **SQL injection protection**: Data access is implemented with Entity Framework Core (`ApplicationDbContext`), which uses parameterized queries for LINQ/DbSet operations. Avoid raw SQL; if `FromSqlRaw` or `ExecuteSqlRaw` are used, ensure parameters are passed safely.

- **Authentication and authorization**: Controllers are protected with `[Authorize]` and role checks (e.g., `Registry Administrator` and `System Administrator`). See `Controllers/` for role‑restricted endpoints. Security tests are located under `tests/WebApplication1.Tests.Security`.

**Where to verify**:  
- CSRF: `Controllers/*` (POST actions) and `Views/Obstacle/DataForm.cshtml`.  
- XSS: `Views/` — check for `Html.Raw` or unencoded output.  
- SQL injection: `Data/ApplicationDbContext.cs` and any usages of raw SQL in the codebase.  
- Tests: `tests/WebApplication1.Tests.Security` and `tests/WebApplication1.Tests.Integration` for automated checks.

Note: These protections rely on correct usage of the framework. For production, consider adding a Content Security Policy (CSP) header, input sanitization for rich text, and regular security reviews.