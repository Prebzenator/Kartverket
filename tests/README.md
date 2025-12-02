# Tests for WebApplication1

This folder contains the automated tests for the WebApplication1 project.  
The test suite is divided into three projects: **Unit**, **Integration**, and **Security**.

---

## Test project overview

```
tests/
  WebApplication1.Tests.Unit/
  WebApplication1.Tests.Integration/
  WebApplication1.Tests.Security/
```

### 1. Unit tests
Small, isolated tests such as:
- ModelState behavior in `ObstacleController`
- Helper methods (`UnitConverter`)
- Basic validation logic

Does **not** use database or authentication.

#### Implemented tests:
**`DataForm_Post_InvalidModelState_ReturnsViewWithErrors`**
- Sends invalid form input to the controller action
- Verifies `ModelState` contains expected errors
- Ensures the action returns the view with the same model (no DB interaction)

**`UnitConverter_ToMeters_InputInCentimeters_ReturnsCorrectValue`**
- Tests conversion logic for typical and edge inputs
- Verifies numeric accuracy and handling of zero/null inputs

**`CreateUserViewModelValidator_InvalidEmail_AddsModelError`**
- Validates view model rules for user registration
- Ensures invalid email or missing required fields produce expected validation errors

**`ObstacleController_Action_CallsExpectedServiceMethods`**
- Mocks dependent services (`ILogger`, repository/service interfaces, `ITempDataProvider`)
- Verifies controller calls into services with correct parameters and expected number of invocations

---

### 2. Integration tests
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

### 3. Security tests
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

## How to run tests

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

## Summary

This test suite covers:
- Form submission and database flow (integration)
- Draft creation logic
- Ownership authorization (Forbid)
- Admin role protection
- CSRF protection
- Small isolated logic via unit tests

All tests run without modifying production code.
