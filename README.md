# Kartverket
Repository for **group 12** in IS-200, 201, 202.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
**Quick Overview:** 

A simple web app to register obstacles by clicking on a map (leaflet integrated). After submitting, you see a confirmation page with your desired inputs and a map marker. The app uses a "Post-Redirect-Get" pattern and can persist to MariaDB. 

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Prerequisites:**

* .NET 8 SDK
* MariaDB/MySQL
* Docker Desktop


**System Architecture**

The different pieces involved in the application: 
* Controller (C#): Recieves requests, validates, saves/loads data, chooses which view to render.
* View (Razor): The HTML page. Uses "Tag Helpers" for forms/links.
* Model (ObstacleData): The data (form fields) + validation rules.
* Repository: A small class that handles database reads/writes (keeps DB code out of controllers).
* Leaflet: Renders the map. Clicking on the map sets latitude/longitude into the form.
* Layout: Shared wrapper (navbar, Bootstrap, Icons, Leaflet, dark mode (optional)) around all pages.

**Request flow** (PRG)
GET /Obstacle/Dataform --> Show form + map

POST /Obstacle/DataForm --> Validate --> Save --> Redirect to:

GET /Obstacle/Overview --> load from DB --> show details + map marker

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
**PROJECT STRUCTRURE:**

Controllers/
 
  HomeController.cs         --> # Home/Privacy
  
  ObstacleController.cs     --> # Form (GET/POST) + Overview (GET)

Data/
 
  ObstacleRepository.cs     --> # DB save/load (MySqlConnector)

Models/
  
  ObstacleData.cs          -->  # Model/validation

Views/
 
  Obstacle/
  DataForm.cshtml         --> # Form + Leaflet
  Overview.cshtml         --> # Confirmation + map marker
 
  Home/
   
   Index.cshtml, Privacy.cshtml
  
  Shared/
    
  _Layout.cshtml           --> # Shared head/footer, Leaflet/Bootstrap, dark mode

wwwroot/css/site.css       --> # Small theme tweaks (light/dark)


--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Testing & Results**


<img width="640" height="871" alt="image" src="https://github.com/user-attachments/assets/716fc7a6-8863-4782-89e8-cce58bc04915" />

Leaving fields empty will result in error messages in the form. Error message also shows when the height is over a value of 1000 meters.


![interactive map - Made with Clipchamp](https://github.com/user-attachments/assets/5135819c-8e22-4e22-ae85-e26fea54cb16)

Shows a working interactive map. Markers appear, and Latitude/Longitude fields fill in. 


<img width="645" height="751" alt="image" src="https://github.com/user-attachments/assets/8a10d6fa-2f6e-4f42-bc28-362040096a31" />

A page confirming that an obstacle has succesfully been registerted. 


**Drift / Usage**

To use this application you can do the following:

* Check the prerequisites
* Clone this repository
* Make sure your 3306:3306 port is available
If your 3306:3306 port can't be made available, you have to change the code and manually choose a port yourself.
* After this you are ready to press Docker Compose, or run: docker compose up --build