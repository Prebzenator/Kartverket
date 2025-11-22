# Tests for WebApplication1

This folder contains the automated tests for the WebApplication1 project.  
The test suite is divided into three projects: **Unit**, **Integration**, and **Security**.

---

## 📁 Test project overview

```
tests/
  WebApplication1.Tests.Unit/
  WebApplication1.Tests.Integration/
  WebApplication1.Tests.Security/
```

### ✔ 1. Unit tests (Truls)
Small, isolated tests such as:
- ModelState behavior in ObstacleController
- Helper methods (UnitConverter)
- Basic validation logic

Does **not** use database or authentication.

---

### ✔ 2. Integration tests (Herman)
These tests use:
- A real ObstacleController
- Fake authenticated Pilot user
- In-memory ApplicationDbContext
- In-memory TempData
- Realistic form data (IFormCollection)

#### Implemented tests:
**`DataForm_Post_Draft_CreatesObstacleInDatabase`**
- Simulates posting the real obstacle form
- Creates a new obstacle in the in-memory database
- Ensures draft reports get status `NotApproved`
- Tests real controller + DB logic end-to-end

**`Get_AdminEndpoint_WithoutOwnership_ReturnsForbid`**
- User tries to edit someone else's obstacle
- Controller returns `ForbidResult`
- Tests the ownership security rule

---

### ✔ 3. Security tests (Herman)
Security tests check **attributes** using reflection.

**`Review_Action_ShouldRequire_RegistryAdministrator_Role`**
- Ensures the `Review` action has:
  `[Authorize(Roles = "Registry Administrator")]`

**`DataForm_Post_ShouldHave_ValidateAntiForgeryToken`**
- Ensures the POST version of `DataForm` has:
  `[ValidateAntiForgeryToken]`
- Confirms CSRF protection is enabled

These tests do not run logic — they verify security configuration.

---

## ▶️ How to run tests

### In Visual Studio
1. Open **Test Explorer**
2. Click **Run All**
3. Or right-click a test project → **Run**

### Command line
From solution root:

```
dotnet test
```

Run only integration tests:

```
dotnet test tests/WebApplication1.Tests.Integration/WebApplication1.Tests.Integration.csproj
```

Run only security tests:

```
dotnet test tests/WebApplication1.Tests.Security/WebApplication1.Tests.Security.csproj
```

---

## ✔ Summary

This test suite covers:
- Form submission and database flow (integration)
- Draft creation logic
- Ownership authorization (Forbid)
- Admin role protection
- CSRF protection
- Small isolated logic via unit tests

All tests run without modifying production code.
