# Testing

## Backend (.NET)

**Framework:** xUnit + Moq  
**Target:** .NET 10.0  
**Location:** `Arcadeia.Tests/`

### Running

```bash
# All tests
dotnet test

# Specific project
dotnet test Arcadeia.Tests/Arcadeia.Tests.csproj

# With JUnit XML output
dotnet test --logger "junit;LogFilePath=test-results/dotnet-junit.xml"
```

### Test Suites

| Suite | Tests | Description |
|---|---|---|
| `MediaContainerPathTests` | 28 | Static path parsing — Windows/Unix formats, UNC paths, edge cases |
| `MediaContainerFlagsTests` | 23 | Flag management (Favorite, Deleted) — set/unset, case-insensitive parsing, serialization |
| `MediaContainerModelTests` | 23 | `Models.MediaContainer` data model — equality, comparison, DateTime truncation, `Differences()` |
| `MediaFileExtensionTests` | 19 | File extension extraction and content-type detection |
| `ResolutionTypeTests` | 22 | Resolution parsing, comparison, equality operators, `GetHashCode` |
| `PlatformTests` | 13 | Cross-platform behavior — executable extensions, path/root separators |
| **Total** | **128** | |

### Adding Tests

- Use `[Fact]` for single-case tests, `[Theory]` + `[InlineData]` for parameterized ones.
- Keep tests focused on public methods and pure logic; avoid mocking constructor chains.

---

## Frontend (React/Vite)

**Framework:** Vitest + jsdom  
**Location:** `UI/src/`

### Running

```bash
cd UI
npm test
```

### Test Files

| File | Tests | Description |
|---|---|---|
| `src/App.test.jsx` | 1 | App renders without crashing (jsdom, full Redux + Router setup) |
| `src/features/ui/slice.test.js` | 11 | `queueUpload` URL deduplication logic |
| **Total** | **12** | |

### Adding Tests

- Place test files alongside the source they test, named `*.test.js(x)`.
- Use `// @vitest-environment jsdom` at the top of files that render React components.
- Use `vi.mock()` for external dependencies (SignalR, etc.).

---

## CI

Two jobs run in the `test` stage on every pipeline:

| Job | Image | Reports |
|---|---|---|
| `test:backend` | `dotnet-core-build` | `test-results/dotnet-junit.xml` |
| `test:frontend` | `dotnet-core-build` | `test-results/ui-junit.xml` |

Both jobs upload JUnit XML via `artifacts.reports.junit` (with `when: always`), so pass/fail details appear in the GitLab pipeline UI even when tests fail.
