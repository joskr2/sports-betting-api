#!/bin/bash
# ==============================================================================
# SPORTS BETTING API - PRODUCTION DOCKER SCRIPT
# ==============================================================================
# Script para manejo del entorno de producción con Docker
set -e

# ==============================================================================
# Configuration
# ==============================================================================
PROJECT_NAME="sportsbetting-api"
COMPOSE_FILE="docker-compose.prod.yml"
ENV_FILE=".env"
BACKUP_DIR="./backups"
LOG_DIR="./logs"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ==============================================================================
# Helper Functions
# ==============================================================================
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_requirements() {
    log_info "Checking requirements..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    if [ ! -f "$ENV_FILE" ]; then
        log_error "Environment file $ENV_FILE not found"
        exit 1
    fi
    
    if [ ! -f "$COMPOSE_FILE" ]; then
        log_error "Compose file $COMPOSE_FILE not found"
        exit 1
    fi
    
    log_success "All requirements met"
}

create_directories() {
    log_info "Creating necessary directories..."
    mkdir -p "$BACKUP_DIR"
    mkdir -p "$LOG_DIR"
    log_success "Directories created"
}

backup_database() {
    log_info "Creating database backup..."
    
    if [ ! -d "$BACKUP_DIR" ]; then
        mkdir -p "$BACKUP_DIR"
    fi
    
    BACKUP_FILE="$BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"
    
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" exec -T postgres pg_dump -U ${POSTGRES_USER:-postgres} ${POSTGRES_DB:-sportsbetting_prod} > "$BACKUP_FILE" 2>/dev/null || {
        log_warning "Could not create backup - database might not be running"
        return 1
    }
    
    log_success "Database backup created: $BACKUP_FILE"
}

# ==============================================================================
# Main Functions
# ==============================================================================
start_production() {
    log_info "Starting production environment..."
    
    check_requirements
    create_directories
    
    # Pull latest images
    log_info "Pulling latest images..."
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" pull
    
    # Build and start services
    log_info "Building and starting services..."
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d --build
    
    # Wait for services to be ready
    log_info "Waiting for services to be ready..."
    sleep 30
    
    # Check if services are running
    if docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps | grep -q "Up"; then
        log_success "Production environment started successfully"
        show_status
    else
        log_error "Failed to start production environment"
        show_logs
        exit 1
    fi
}

stop_production() {
    log_info "Stopping production environment..."
    
    # Create backup before stopping
    backup_database
    
    # Stop services
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down
    
    log_success "Production environment stopped"
}

restart_production() {
    log_info "Restarting production environment..."
    
    backup_database
    stop_production
    start_production
}

show_status() {
    log_info "Production environment status:"
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps
}

show_logs() {
    log_info "Showing logs..."
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" logs --tail=50
}

follow_logs() {
    log_info "Following logs (Ctrl+C to exit)..."
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" logs -f
}

update_production() {
    log_info "Updating production environment..."
    
    # Create backup
    backup_database
    
    # Pull latest images
    log_info "Pulling latest images..."
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" pull
    
    # Recreate services with new images
    log_info "Recreating services..."
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d --force-recreate
    
    log_success "Production environment updated"
}

cleanup_production() {
    log_info "Cleaning up production environment..."
    
    # Stop and remove containers
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down -v
    
    # Remove unused images
    docker image prune -f
    
    # Remove unused volumes
    docker volume prune -f
    
    log_success "Production environment cleaned up"
}

health_check() {
    log_info "Performing health check..."
    
    # Check if containers are running
    if ! docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps | grep -q "Up"; then
        log_error "Some services are not running"
        return 1
    fi
    
    # Check API health
    log_info "Checking API health..."
    if curl -f http://localhost:5000/health &> /dev/null; then
        log_success "API is healthy"
    else
        log_warning "API health check failed"
    fi
    
    # Check database connection
    log_info "Checking database connection..."
    if docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" exec -T postgres pg_isready -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-sportsbetting_prod} &> /dev/null; then
        log_success "Database is ready"
    else
        log_warning "Database connection failed"
    fi
}

show_help() {
    echo "Sports Betting API - Production Docker Management"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  start       Start production environment"
    echo "  stop        Stop production environment"
    echo "  restart     Restart production environment"
    echo "  status      Show status of services"
    echo "  logs        Show recent logs"
    echo "  logs-f      Follow logs in real-time"
    echo "  update      Update production environment"
    echo "  cleanup     Clean up containers and images"
    echo "  backup      Create database backup"
    echo "  health      Perform health check"
    echo "  help        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 start        # Start production environment"
    echo "  $0 logs-f       # Follow logs in real-time"
    echo "  $0 health       # Check system health"
}

# ==============================================================================
# Main Script Logic
# ==============================================================================
case "${1:-help}" in
    start)
        start_production
        ;;
    stop)
        stop_production
        ;;
    restart)
        restart_production
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    logs-f)
        follow_logs
        ;;
    update)
        update_production
        ;;
    cleanup)
        cleanup_production
        ;;
    backup)
        backup_database
        ;;
    health)
        health_check
        ;;
    help|*)
        show_help
        ;;
esac 
