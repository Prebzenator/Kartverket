# Kartverket
Repository for **group 12** in IS-200, 201, 202.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
**Quick Overview:** 

A robust MVC web application for registering and managing aviation obstacles. The system features role-based access control (Pilots, Registry Admins), interactive maps (Leaflet/GeoJSON), and a complete review workflow (Draft -> Pending -> Approved). It uses the "Post-Redirect-Get" pattern and persists data to a MariaDB database running in Docker.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Prerequisites:**

* **.NET 9 SDK** (Updated from .NET 8)
* **Docker Desktop** (Required for containerization)
* **MariaDB / MySQL Client** (Optional for direct DB inspection)


**System Architecture**

The application is built on **ASP.NET Core 9** using a strict Model-View-Controller (MVC) pattern.

* **Controllers:** Manage application flow and enforce Authorization.
    * `PilotController`: Handles organization-specific reporting and crew resource management.
    * `AdminObstacleController`: Manages the approval/rejection workflow for Registry Administrators.
    * `ObstacleController`: Handles the public/pilot data entry forms.
* **Views (Razor):** Server-side rendered HTML using Tag Helpers and Bootstrap for responsive design.
* **Models:**
    * `ObstacleData`: The core entity containing GeoJSON geometry, status enums (`Pending`, `Approved`), and audit trails.
    * `ApplicationUser`: Extended Identity user with `Organization` and `MustChangePassword` flags.
* **Data Access:** Direct usage of **Entity Framework Core** (replacing the repository pattern) for efficient querying and automatic migrations.
* **Leaflet:** Client-side JS library for rendering maps and picking coordinates (Latitude/Longitude).

**Request flow (PRG)**
1. **GET** `/Obstacle/DataForm` --> Render form + Map
2. **POST** `/Obstacle/DataForm` --> Validate Input --> Save to DB --> Redirect
3. **GET** `/Obstacle/Overview` --> Load from DB --> Show Confirmation & Map Marker
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
**PROJECT STRUCTURE:**

Controllers/
  HomeController.cs          --> # Landing page & Error handling
  
  AccountController.cs       --> # Login, Register, Password Management
  
  ObstacleController.cs      --> # Creation & Editing logic
  
  PilotController.cs         --> # Pilot Dashboard & Org. Visibility
  
  AdminObstacleController.cs --> # Admin Dashboard & Review Logic

Data/
  ApplicationDbContext.cs    --> # EF Core Database Context
 
  RoleSeeder.cs             --> # Auto-creates Roles & System Admin on startup

Models/
  ObstacleData.cs            --> # Database Entity & Validation Rules
  
  ApplicationUser.cs         --> # Extended User Profile
  
  ObstacleCategory.cs        --> # Lookup table for obstacle types

Views/
  
  Obstacle/
    
    DataForm.cshtml          --> # Main Reporting Form
  
  AdminObstacle/
    
    Dashboard.cshtml         --> # Admin Filtering & Sorting Interface
  
  Pilot/
    
    Log.cshtml               --> # Pilot's "My Reports" & Organization View
  
  Shared/
   
    _Layout.cshtml           --> # Global Template (Navbar, Alerts, Leaflet)

Docker/

  Dockerfile                 --> # Multi-stage build definition
  
  docker-compose.yml         --> # Orchestration for App + MariaDB


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


**Drift / Usage**

The application is fully containerized for consistent deployment!!

To use this application you can do the following:

* Check the prerequisites
* Clone this repository
* Make sure your 3306:3306 port is available
If your 3306:3306 port can't be made available, you have to change the code and manually choose a port yourself.
* Open the WebApplication1.sln
* After this you are ready to press Docker Compose, or run: docker compose up --build

First Run: The system will automatically apply EF Core Migrations and seed the initial System Administrator user.
Access the app at http://localhost:5000.
