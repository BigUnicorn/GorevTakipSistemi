# Contributing to Görev Takip Sistemi

First off, thank you for considering contributing to Görev Takip Sistemi! It's people like you that make this tool such a great project.

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct. Please be respectful and considerate to others.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the issue tracker as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

*   Use a clear and descriptive title for the issue to identify the problem.
*   Describe the exact steps which reproduce the problem in as many details as possible.
*   Provide specific examples to demonstrate the steps. Include links to files or GitHub projects, or copy/pasteable snippets, which you use in those examples.

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When you are creating an enhancement suggestion, please include as many details as possible:

*   Use a clear and descriptive title for the issue to identify the suggestion.
*   Provide a step-by-step description of the suggested enhancement in as many details as possible.
*   Explain why this enhancement would be useful to most users.

### Pull Requests

*   Please create a new branch for each feature or bug fix.
*   Make sure your code adheres to our "Zero Warnings/Errors" policy. Run `dotnet build -warnaserror` and `npm run lint` before committing.
*   Ensure all unit and integration tests pass successfully (`dotnet test` & `npm run test`).
*   Update the `README.md` and related documentation (like `yapilanlar.md`, `gelistirmeler.md`) with details of changes to the interface or architecture.
*   Include clear, descriptive commit messages.

## Development Setup

If you want to contribute to the code, you will need to set up a local development environment.

### Prerequisites

*   .NET 10.0 SDK
*   Node.js (v18+)
*   Docker & Docker Compose (Required for Integration Tests and running Redis/PostgreSQL)

### Backend (API) Setup

1.  Navigate to the project root.
2.  Start required databases using Docker: `docker-compose up -d db redis`
3.  Restore and build the project: `dotnet build`
4.  Run tests: `dotnet test`
5.  Run the API: `dotnet run --project GorevTakip.API`

### Frontend (Next.js) Setup

1.  Navigate to the `gorev-takip-frontend` directory.
2.  Install dependencies: `npm install`
3.  Run the development server: `npm run dev`

### "Zero Warnings/Errors" Policy

This project strictly adheres to a zero warning and error policy. Any pull request introducing a linter warning (ESLint) or a compiler warning (C#) will automatically be rejected by our CI/CD pipelines.

## License

By contributing, you agree that your contributions will be licensed under its PolyForm Noncommercial License 1.0.0.
