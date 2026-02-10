# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build test clean run restore migrate docker-build docker-up docker-down format lint

# Variables
DOTNET := dotnet
SOLUTION := DotnetOutboxPattern.csproj
CONFIGURATION := Release
VERSION := $(shell grep '<Version>' $(SOLUTION) | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/')

# Color output
BLUE := \033[0;34m
GREEN := \033[0;32m
NC := \033[0m # No Color

help:
	@echo "$(BLUE)Outbox Pattern - Make Commands$(NC)"
	@echo ""
	@echo "Usage: make [command]"
	@echo ""
	@echo "Commands:"
	@echo "  $(GREEN)build$(NC)           Build the project"
	@echo "  $(GREEN)test$(NC)            Run all tests"
	@echo "  $(GREEN)clean$(NC)           Clean build artifacts"
	@echo "  $(GREEN)restore$(NC)         Restore NuGet packages"
	@echo "  $(GREEN)run$(NC)             Run the application"
	@echo "  $(GREEN)migrate$(NC)         Run database migrations"
	@echo "  $(GREEN)format$(NC)          Format code with dotnet-format"
	@echo "  $(GREEN)lint$(NC)            Run code quality analysis"
	@echo "  $(GREEN)docker-build$(NC)    Build Docker image"
	@echo "  $(GREEN)docker-up$(NC)       Start Docker containers"
	@echo "  $(GREEN)docker-down$(NC)     Stop Docker containers"
	@echo "  $(GREEN)docker-logs$(NC)     View Docker logs"
	@echo "  $(GREEN)pack$(NC)            Create NuGet package"
	@echo "  $(GREEN)docs$(NC)            Generate API documentation"
	@echo "  $(GREEN)health$(NC)          Check API health"

# Build
build: restore
	@echo "$(BLUE)Building project...$(NC)"
	@$(DOTNET) build -c $(CONFIGURATION)
	@echo "$(GREEN)Build complete!$(NC)"

# Restore dependencies
restore:
	@echo "$(BLUE)Restoring dependencies...$(NC)"
	@$(DOTNET) restore
	@echo "$(GREEN)Dependencies restored!$(NC)"

# Run tests
test: build
	@echo "$(BLUE)Running tests...$(NC)"
	@$(DOTNET) test -c $(CONFIGURATION) --no-build --verbosity normal
	@echo "$(GREEN)Tests complete!$(NC)"

# Clean build artifacts
clean:
	@echo "$(BLUE)Cleaning build artifacts...$(NC)"
	@$(DOTNET) clean -c $(CONFIGURATION)
	@rm -rf bin obj
	@echo "$(GREEN)Clean complete!$(NC)"

# Run application
run: build
	@echo "$(BLUE)Starting application...$(NC)"
	@$(DOTNET) run -c $(CONFIGURATION)

# Database migrations
migrate:
	@echo "$(BLUE)Running database migrations...$(NC)"
	@$(DOTNET) ef database update
	@echo "$(GREEN)Migrations complete!$(NC)"

# Format code
format:
	@echo "$(BLUE)Formatting code...$(NC)"
	@$(DOTNET) format
	@echo "$(GREEN)Code formatted!$(NC)"

# Code quality analysis
lint:
	@echo "$(BLUE)Running code quality analysis...$(NC)"
	@$(DOTNET) build -c $(CONFIGURATION)
	@echo "$(GREEN)Lint complete!$(NC)"

# Create NuGet package
pack: clean build
	@echo "$(BLUE)Creating NuGet package...$(NC)"
	@$(DOTNET) pack -c $(CONFIGURATION) -o ./nupkg
	@echo "$(GREEN)Package created: ./nupkg/$(NC)"
	@ls -la ./nupkg/

# Docker commands
docker-build:
	@echo "$(BLUE)Building Docker image...$(NC)"
	@docker build -t dotnet-outbox-pattern:$(VERSION) .
	@echo "$(GREEN)Docker image built: dotnet-outbox-pattern:$(VERSION)$(NC)"

docker-up:
	@echo "$(BLUE)Starting Docker containers...$(NC)"
	@docker-compose up -d
	@echo "$(GREEN)Containers started!$(NC)"
	@docker-compose ps

docker-down:
	@echo "$(BLUE)Stopping Docker containers...$(NC)"
	@docker-compose down
	@echo "$(GREEN)Containers stopped!$(NC)"

docker-logs:
	@docker-compose logs -f api

docker-shell:
	@docker-compose exec api /bin/bash

# Health check
health:
	@echo "$(BLUE)Checking API health...$(NC)"
	@curl -k https://localhost:5001/health || echo "$(RED)API is not responding$(NC)"

# API statistics
stats:
	@echo "$(BLUE)Getting API statistics...$(NC)"
	@curl -k https://localhost:5001/api/outbox/statistics 2>/dev/null | jq .

# Database connection test
db-test:
	@echo "$(BLUE)Testing database connection...$(NC)"
	@$(DOTNET) ef dbcontext info

# List migrations
db-migrations:
	@echo "$(BLUE)Available migrations:$(NC)"
	@$(DOTNET) ef migrations list

# Rollback last migration
db-rollback:
	@echo "$(BLUE)Rolling back last migration...$(NC)"
	@$(DOTNET) ef database update --help | grep -A 5 "Examples"
	@echo "Manually specify: $(DOTNET) ef database update <PreviousMigration>"

# Generate API documentation
docs:
	@echo "$(BLUE)Generating API documentation...$(NC)"
	@mkdir -p docs/api
	@echo "Documentation already in ./docs folder"
	@ls -la ./docs/

# Version info
version:
	@echo "$(BLUE)Project Version: $(GREEN)$(VERSION)$(NC)"
	@echo "$(BLUE).NET Version:$(NC)"
	@$(DOTNET) --version

# Help targets
.PHONY: version db-test db-migrations db-rollback stats health docker-shell

# Default target
.DEFAULT_GOAL := help

# Compound targets
full-build: clean restore build test
	@echo "$(GREEN)Full build complete!$(NC)"

full-test: docker-down docker-up test
	@echo "$(GREEN)Full test suite complete!$(NC)"

release: full-build pack
	@echo "$(GREEN)Release build complete! Package ready in ./nupkg/$(NC)"

deploy-local: docker-down docker-build docker-up
	@echo "$(GREEN)Local deployment complete! API at https://localhost:5001$(NC)"

.PHONY: full-build full-test release deploy-local
