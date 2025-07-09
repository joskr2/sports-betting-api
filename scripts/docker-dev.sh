#!/bin/bash
# =============================================================================
# SPORTS BETTING API - DEVELOPMENT DOCKER SCRIPT
# =============================================================================
# Script para manejo del entorno de desarrollo con Docker

set -e

# =============================================================================
# Configuration
# =============================================================================
ENV_FILE=".env.local"
COMPOSE_FILE="docker-compose.yml"
PROJECT_NAME="sportsbetting-dev"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# =============================================================================
# Helper Functions
# =============================================================================
print_header() {
    echo -e "${BLUE}==============================================================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}==============================================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Function to load environment variables safely
load_env() {
    if [ -f "$ENV_FILE" ]; then
        export $(grep -v '^#' $ENV_FILE | grep -v '^$' | xargs)
    fi
}

# =============================================================================
# Pre-flight Checks
# =============================================================================
check_requirements() {
    print_header "Checking Requirements"
    
    # Check if Docker is installed
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker Desktop."
        exit 1
    fi
    
    # Check if Docker Compose is installed
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed. Please install Docker Compose."
        exit 1
    fi
    
    # Check if .env.local exists
    if [ ! -f "$ENV_FILE" ]; then
        print_error "Environment file $ENV_FILE not found."
        exit 1
    fi
    
    # Check if docker-compose.yml exists
    if [ ! -f "$COMPOSE_FILE" ]; then
        print_error "Docker Compose file $COMPOSE_FILE not found."
        exit 1
    fi
    
    print_success "All requirements satisfied"
}

# =============================================================================
# Docker Operations
# =============================================================================
docker_up() {
    print_header "Starting Development Environment"
    
    # Create necessary directories
    mkdir -p logs temp
    
    # Load environment variables
    load_env
    
    # Start services
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME up -d
    
    print_success "Development environment started"
    print_info "API available at: http://localhost:${API_PORT:-5000}"
    print_info "Database available at: localhost:${POSTGRES_EXTERNAL_PORT:-5433}"
    print_info "pgAdmin available at: http://localhost:5050 (use --profile tools)"
    
    # Wait for services to be healthy
    print_info "Waiting for services to be healthy..."
    sleep 10
    
    # Check health
    check_health
}

docker_down() {
    print_header "Stopping Development Environment"
    
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME down
    
    print_success "Development environment stopped"
}

docker_restart() {
    print_header "Restarting Development Environment"
    
    docker_down
    docker_up
}

docker_rebuild() {
    print_header "Rebuilding Development Environment"
    
    # Load environment variables
    load_env
    
    # Stop and remove containers
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME down
    
    # Remove images
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME build --no-cache
    
    # Start again
    docker_up
}

# =============================================================================
# Logging Operations
# =============================================================================
show_logs() {
    print_header "Showing Logs"
    
    if [ -n "$1" ]; then
        print_info "Showing logs for service: $1"
        docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME logs -f $1
    else
        print_info "Showing logs for all services"
        docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME logs -f
    fi
}

# =============================================================================
# Database Operations
# =============================================================================
db_shell() {
    print_header "Opening Database Shell"
    
    # Load environment variables
    load_env
    
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME exec postgres psql -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-sportsbetting_db}
}

db_backup() {
    print_header "Creating Database Backup"
    
    # Load environment variables
    load_env
    
    BACKUP_FILE="backup_$(date +%Y%m%d_%H%M%S).sql"
    
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME exec -T postgres pg_dump -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-sportsbetting_db} > $BACKUP_FILE
    
    print_success "Database backup created: $BACKUP_FILE"
}

db_restore() {
    if [ -z "$1" ]; then
        print_error "Please provide backup file path"
        exit 1
    fi
    
    print_header "Restoring Database from Backup"
    
    # Load environment variables
    load_env
    
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME exec -T postgres psql -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-sportsbetting_db} < $1
    
    print_success "Database restored from: $1"
}

# =============================================================================
# Health Check Operations
# =============================================================================
check_health() {
    print_header "Checking Service Health"
    
    # Load environment variables
    load_env
    
    # Check if containers are running first
    print_info "Checking if containers are running..."
    if ! docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME ps | grep -q "Up"; then
        print_error "No containers are running. Start the environment first with: $0 up"
        return 1
    fi
    
    # Check API health
    API_URL="http://localhost:${API_PORT:-5000}/health"
    
    print_info "Checking API health at: $API_URL"
    
    if curl -s -f "$API_URL" > /dev/null; then
        print_success "API is healthy"
        
        # Show health details
        echo -e "${GREEN}API Health Response:${NC}"
        curl -s "$API_URL" | jq '.' 2>/dev/null || curl -s "$API_URL"
    else
        print_error "API is not healthy"
        print_info "Check logs with: $0 logs api"
        return 1
    fi
    
    # Check database health
    print_info "Checking database health..."
    
    if docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME exec -T postgres pg_isready -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-sportsbetting_db} > /dev/null 2>&1; then
        print_success "Database is healthy"
    else
        print_error "Database is not healthy"
        print_info "Check logs with: $0 logs postgres"
        return 1
    fi
    
    print_success "All services are healthy!"
}

# =============================================================================
# Development Tools
# =============================================================================
start_with_tools() {
    print_header "Starting Development Environment with Tools"
    
    # Load environment variables
    load_env
    
    # Start with pgAdmin
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME --profile tools up -d
    
    print_success "Development environment with tools started"
    print_info "API available at: http://localhost:${API_PORT:-5000}"
    print_info "Database available at: localhost:${POSTGRES_EXTERNAL_PORT:-5433}"
    print_info "pgAdmin available at: http://localhost:5050"
    print_info "pgAdmin credentials: admin@sportsbetting.com / admin123"
}

clean_all() {
    print_header "Cleaning All Docker Resources"
    
    print_warning "This will remove all containers, images, and volumes for this project"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        # Stop and remove containers
        docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME down -v --remove-orphans
        
        # Remove images
        docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE -p $PROJECT_NAME down --rmi all
        
        # Remove volumes
        docker volume prune -f
        
        print_success "All resources cleaned"
    else
        print_info "Operation cancelled"
    fi
}

# =============================================================================
# Usage Information
# =============================================================================
show_usage() {
    echo -e "${BLUE}Sports Betting API - Development Docker Script${NC}"
    echo
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo
    echo "Commands:"
    echo "  up                Start development environment"
    echo "  down              Stop development environment"
    echo "  restart           Restart development environment"
    echo "  rebuild           Rebuild and restart development environment"
    echo "  logs [service]    Show logs (optionally for specific service)"
    echo "  health            Check service health"
    echo "  db-shell          Open database shell"
    echo "  db-backup         Create database backup"
    echo "  db-restore <file> Restore database from backup"
    echo "  tools             Start with development tools (pgAdmin)"
    echo "  clean             Clean all Docker resources"
    echo "  help              Show this help message"
    echo
    echo "Service names:"
    echo "  api               Sports Betting API"
    echo "  postgres          PostgreSQL Database"
    echo "  pgadmin           pgAdmin Database Management"
    echo
    echo "Examples:"
    echo "  $0 up                    Start development environment"
    echo "  $0 logs api              Show API logs"
    echo "  $0 db-shell              Open database shell"
    echo "  $0 tools                 Start with pgAdmin"
    echo
}

# =============================================================================
# Main Script Logic
# =============================================================================
main() {
    # Check requirements first
    check_requirements
    
    case "${1:-help}" in
        "up")
            docker_up
            ;;
        "down")
            docker_down
            ;;
        "restart")
            docker_restart
            ;;
        "rebuild")
            docker_rebuild
            ;;
        "logs")
            show_logs $2
            ;;
        "health")
            check_health
            ;;
        "db-shell")
            db_shell
            ;;
        "db-backup")
            db_backup
            ;;
        "db-restore")
            db_restore $2
            ;;
        "tools")
            start_with_tools
            ;;
        "clean")
            clean_all
            ;;
        "help"|*)
            show_usage
            ;;
    esac
}

# Run main function
main "$@"
