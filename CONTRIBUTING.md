# Contributing to .NET Outbox Pattern

Thank you for your interest in contributing to the .NET Outbox Pattern project! We welcome contributions from the community and appreciate your help in making this project better.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Submitting Pull Requests](#submitting-pull-requests)
- [Code Style Guidelines](#code-style-guidelines)
- [Reporting Issues](#reporting-issues)
- [License](#license)

## Code of Conduct

This project adheres to the Contributor Covenant Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## Getting Started

1. **Fork the Repository**: Click the "Fork" button on GitHub to create your own copy of the repository.

2. **Clone Your Fork**: 
   ```bash
   git clone https://github.com/YOUR-USERNAME/dotnet-outbox-pattern.git
   cd dotnet-outbox-pattern
   ```

3. **Add Upstream Remote**:
   ```bash
   git remote add upstream https://github.com/sarmkadan/dotnet-outbox-pattern.git
   ```

4. **Keep Your Fork Updated**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

## Development Setup

### Prerequisites

- **.NET SDK 10.0** or later: [Download .NET](https://dotnet.microsoft.com/download)
- **Git**: For version control
- **Docker** (optional): For running services in containers
- **PostgreSQL** (optional): For database integration testing

### Environment Setup

1. **Restore Dependencies**:
   ```bash
   make restore
   # or
   dotnet restore
   ```

2. **Build the Project**:
   ```bash
   make build
   # or
   dotnet build
   ```

3. **Database Setup** (if needed):
   ```bash
   make migrate
   # or
   dotnet ef database update
   ```

4. **Run the Application**:
   ```bash
   make run
   # or
   dotnet run
   ```

### Using Docker

For a complete local environment with database:

```bash
make docker-up        # Start all services
make docker-logs      # View logs
make docker-down      # Stop all services
```

## Making Changes

### Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-number-description
```

Use descriptive branch names that clearly indicate the feature or fix.

### Write Code

Follow the code style guidelines below. Keep changes focused and atomic—each commit should represent a single logical unit of work.

### Keep Author Headers Intact

All C# source files contain author headers at the top. **Preserve these headers exactly as they are**—do not modify or remove them:

```csharp
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================
```

## Testing

### Run Tests

```bash
make test
# or
dotnet test
```

### Test Coverage

- Write unit tests for new functionality
- Ensure all existing tests continue to pass
- Aim for reasonable test coverage of your changes
- Use clear, descriptive test names that explain the expected behavior

### Manual Testing

For features that affect the API or user-facing functionality, test manually:

1. Start the application: `make run`
2. Use the provided examples in `/examples` as reference
3. Test the API endpoints using tools like:
   - **curl**: `curl -X GET https://localhost:5001/api/outbox/messages`
   - **Postman**: Import endpoints and test workflows
   - **HTTP Client**: Use VS Code REST Client extension

### Code Quality Analysis

```bash
make lint
# or
dotnet build
```

## Submitting Pull Requests

### Before Submitting

1. **Sync with Main**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Run Full Test Suite**:
   ```bash
   make full-test
   ```

3. **Format Code**:
   ```bash
   make format
   # or
   dotnet format
   ```

### Create the Pull Request

1. Push your branch to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. Go to the original repository and click "New Pull Request"

3. **Provide a Clear Description**:
   - Reference the issue number (e.g., "Fixes #123")
   - Explain what changes you made and why
   - Describe how to test the changes
   - List any breaking changes

### PR Guidelines

- Keep PRs focused on a single feature or fix
- Provide meaningful commit messages
- Update documentation if needed
- Ensure all tests pass
- Address review feedback promptly

## Code Style Guidelines

### C# Conventions

Follow these conventions when writing C# code:

1. **Naming**:
   - Classes, methods, properties: `PascalCase`
   - Private fields: `_camelCase`
   - Constants: `UPPER_CASE`
   - Local variables: `camelCase`

2. **XML Documentation**:
   - Add XML doc comments to public classes, methods, and properties
   - Use `/// <summary>` for descriptions
   - Use `/// <param>` for parameters
   - Use `/// <returns>` for return values
   - Example:
     ```csharp
     /// <summary>
     /// Publishes a message to the outbox.
     /// </summary>
     /// <param name="message">The message to publish</param>
     /// <returns>The published message ID</returns>
     public async Task<Guid> PublishAsync(OutboxMessage message)
     {
         // implementation
     }
     ```

3. **File Organization**:
   - One public class per file (with exceptions for related small types)
   - Group using statements at the top
   - Use appropriate namespaces matching folder structure
   - Keep files reasonably sized (aim for < 500 lines)

4. **Testing**:
   - Write tests alongside features
   - Use descriptive test method names: `MethodName_Condition_ExpectedResult`
   - Arrange-Act-Assert (AAA) pattern
   - Test both happy paths and edge cases

5. **Code Formatting**:
   - Use `.editorconfig` rules (automatically applied via `make format`)
   - 4-space indentation
   - Max line length: 120 characters (soft limit)
   - Use `var` when type is obvious from context

6. **Error Handling**:
   - Use custom exceptions defined in `Exceptions/OutboxExceptions.cs`
   - Log errors appropriately using Serilog
   - Handle specific exceptions rather than catching `Exception`

### Code Review Comments

When reviewing code or receiving feedback:
- Be respectful and constructive
- Ask clarifying questions
- Suggest improvements with examples
- Acknowledge valid points and thank reviewers

## Reporting Issues

### GitHub Issues

Report bugs, feature requests, and documentation issues via [GitHub Issues](https://github.com/sarmkadan/dotnet-outbox-pattern/issues).

### When Reporting a Bug

Include:
- Clear, descriptive title
- Detailed description of the issue
- Steps to reproduce
- Expected behavior vs. actual behavior
- .NET version and OS information
- Relevant code snippets or logs
- Screenshots (if applicable)

### For Security Vulnerabilities

**Do NOT open a public issue**. See [SECURITY.md](SECURITY.md) for responsible disclosure procedures.

### Feature Requests

Include:
- Clear description of the feature
- Use case and motivation
- Possible implementation approach (optional)
- Examples or mockups (if applicable)

## License

By contributing to this project, you agree that your contributions will be licensed under its MIT License. See [LICENSE](LICENSE) for details.

## Getting Help

- **Documentation**: Check [docs/](docs/) folder for guides
- **Examples**: See [examples/](examples/) for usage examples
- **Issues**: Search existing issues for similar questions
- **Discussions**: Check GitHub Discussions (if enabled)

## Thank You!

Thank you for contributing to making the .NET Outbox Pattern project better. Your efforts help the entire community benefit from more reliable, production-ready code.

---

For any questions, please open an issue or reach out to the maintainers. Happy coding! 🚀