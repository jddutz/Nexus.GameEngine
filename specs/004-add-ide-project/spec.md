# Feature Specification: Add IDE project (Nexus IDE)

**Feature Branch**: `001-add-ide-project`  
**Created**: 2025-11-09  
**Status**: Draft  
**Input**: User description: "Add a new project to the solution, IDE. The default namespace should be Nexus.IDE. This project will be used to create templates. The IDE should use the Nexus engine as a means of fully testing the engine itself. For core functionality, the app should launch. Copy TestApp startup pattern, but provide a different startup template (NexusIDE). The app should show a black window with 'Welcome to the Nexus' centered on screen."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Launch IDE (Priority: P1)

As a developer, I want to launch the Nexus IDE application so I can visually exercise the engine and verify templates.

**Why this priority**: This delivers immediate value by providing an in-repo application that exercises the engine end-to-end and serves as a manual test harness.

**Independent Test**: Build and run the `Nexus.IDE` project and confirm the application window appears and displays a centered "Welcome to the Nexus" message.

**Acceptance Scenarios**:

1. **Given** the solution builds successfully, **When** I run the `Nexus.IDE` application, **Then** a window opens within 5 seconds and displays a black background with the text "Welcome to the Nexus" centered on-screen.
2. **Given** the app is running, **When** I close the window, **Then** the application exits cleanly with no unhandled exceptions.

---

### User Story 2 - Template-driven startup (Priority: P2)

As a developer, I want the IDE app to use a startup template named `NexusIDE` so I can iterate on templates and UI components in this project.

**Why this priority**: Using a template mirrors how consumers will use the engine and allows validating source-generator-produced templates end-to-end.

**Independent Test**: Inspect the `Nexus.IDE` project and confirm it contains a `Templates.NexusIDE` runtime template; run the app and observe the same visual outcome.

**Acceptance Scenarios**:

1. **Given** the `Nexus.IDE` template exists, **When** the app is launched, **Then** the application calls `application.Run(..., Templates.NexusIDE)` and the welcome text appears.

---

### User Story 3 - Project integration (Priority: P3)

As a maintainer, I want the IDE project added to the solution and referencing the engine and source generators so it builds in CI and can exercise analyzers/generators.

**Why this priority**: Ensures the project is part of the repository and is validated by standard build tooling.

**Independent Test**: Run `dotnet build Nexus.GameEngine.sln` and confirm the `Nexus.IDE` project compiles successfully.

**Acceptance Scenarios**:

1. **Given** the new project files are added, **When** the solution is built, **Then** `Nexus.IDE` and the full solution build succeed.

### Edge Cases

- Running on machines without a Vulkan runtime should fail gracefully (the GameEngine already reports errors); document in notes if additional handling is needed.
- If source generators or analyzers are not restored, the IDE should still build (generators may be referenced as analyzers) — build failures should surface clearly.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Repository MUST include a new executable project `src/IDE/Nexus.IDE.csproj` with default namespace `Nexus.IDE`.
- **FR-002**: `Nexus.IDE` MUST reference `src/GameEngine/GameEngine.csproj` and the repository's `SourceGenerators` and `Analyzers` projects (mirroring TestApp references).
- **FR-003**: The app MUST call the engine startup API with a startup template named `Templates.NexusIDE`.
- **FR-004**: The `Templates.NexusIDE` runtime template MUST create a minimal UI that results in a black window with the text "Welcome to the Nexus" centered on-screen.
- **FR-005**: The new project MUST be added to the solution file `Nexus.GameEngine.sln` so it is built by the standard build task.

### Key Entities *(include if feature involves data)*

- **IDE Project**: The new executable project (csproj), contains Program.cs and Templates.
- **Startup Template (NexusIDE)**: A runtime component template that defines the root UI for the IDE app.
- **Window / Message**: The runtime instance that displays the welcome message.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `dotnet build Nexus.GameEngine.sln` completes successfully with the `Nexus.IDE` project included.
- **SC-002**: Running the `Nexus.IDE` executable opens a window and displays "Welcome to the Nexus" centered on-screen within 5 seconds, on a machine with required graphics support.
- **SC-003**: Application exits cleanly when the window is closed (no unhandled exceptions in logs).
- **SC-004**: Spec and checklist files are present at `specs/001-add-ide-project/spec.md` and `specs/001-add-ide-project/checklists/requirements.md` and the checklist items are actionable.

## Assumptions

- The IDE project will reuse the repository's established engine conventions for target framework and package dependencies unless instructed otherwise.
- The engine's source generators and analyzers should be referenced the same way as in `TestApp` so generated template types are available at compile-time.
- This task does not implement a full IDE — only a minimal runtime app used to exercise the engine and templates.

## Next Steps / Notes

- Implement the project files (`Nexus.IDE.csproj`, `Program.cs`, `Templates.cs`) and add them to the solution.  
- Build the solution to validate compilation.  
- If additional visual polish or UI scaffolding is desired, create follow-up tasks after verifying basic app launch.

---

Return: SUCCESS (spec ready for planning)
