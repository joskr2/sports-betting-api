#!/bin/bash

# =============================================================================
# COMPREHENSIVE ENDPOINT TESTING - SPORTS BETTING API
# =============================================================================
# Tests all endpoints locally and in production
# Backend Security & Performance Analysis
# Date: 2025-07-15

set -euo pipefail

# Configuration
LOCAL_API_URL="http://localhost:5001/api"
PROD_API_URL="https://hf3bbankw5wc2uovwju4m6zvku0zuozj.lambda-url.us-east-2.on.aws/api"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="${SCRIPT_DIR}/test-results"
LOG_FILE="${RESULTS_DIR}/comprehensive-test-$(date +%Y%m%d_%H%M%S).log"

# Test data
TEST_EMAIL="testuser_$(date +%s)@example.com"
TEST_PASSWORD="TestSecure123!"
TEST_FULL_NAME="Test User Comprehensive"
JWT_TOKEN=""
BET_ID=""
EVENT_ID=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
LOCAL_TESTS=0
PROD_TESTS=0

# Create results directory
mkdir -p "$RESULTS_DIR"

# Logging function
log() {
    local level="$1"
    local message="$2"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    case "$level" in
        "INFO")
            echo -e "${BLUE}[INFO]${NC} $timestamp - $message" | tee -a "$LOG_FILE"
            ;;
        "SUCCESS")
            echo -e "${GREEN}[SUCCESS]${NC} $timestamp - $message" | tee -a "$LOG_FILE"
            ;;
        "ERROR")
            echo -e "${RED}[ERROR]${NC} $timestamp - $message" | tee -a "$LOG_FILE"
            ;;
        "TEST")
            echo -e "${CYAN}[TEST]${NC} $timestamp - $message" | tee -a "$LOG_FILE"
            ;;
        "WARN")
            echo -e "${YELLOW}[WARN]${NC} $timestamp - $message" | tee -a "$LOG_FILE"
            ;;
    esac
}

# Test utility functions
start_test() {
    local test_name="$1"
    ((TOTAL_TESTS++))
    log "TEST" "🧪 Starting: $test_name"
    echo "───────────────────────────────────────────────────────────────"
}

pass_test() {
    local test_name="$1"
    ((PASSED_TESTS++))
    log "SUCCESS" "✅ PASSED: $test_name"
    echo ""
}

fail_test() {
    local test_name="$1"
    local reason="$2"
    ((FAILED_TESTS++))
    log "ERROR" "❌ FAILED: $test_name - $reason"
    echo ""
}

# HTTP request function with comprehensive response analysis
make_request() {
    local method="$1"
    local url="$2"
    local data="$3"
    local auth_header="$4"
    local expected_status="$5"
    
    local response_file="${RESULTS_DIR}/response_$(date +%s).json"
    local headers_file="${RESULTS_DIR}/headers_$(date +%s).txt"
    
    # Build curl command
    local curl_cmd="curl -s -w '%{http_code}' -X $method '$url'"
    
    if [[ -n "$auth_header" ]]; then
        curl_cmd="$curl_cmd -H 'Authorization: Bearer $auth_header'"
    fi
    
    curl_cmd="$curl_cmd -H 'Content-Type: application/json' -H 'Accept: application/json'"
    
    if [[ -n "$data" ]]; then
        curl_cmd="$curl_cmd -d '$data'"
    fi
    
    curl_cmd="$curl_cmd -D '$headers_file' -o '$response_file'"
    
    # Execute request
    local http_code
    http_code=$(eval "$curl_cmd")
    
    # Parse response
    local response_body=""
    if [[ -f "$response_file" ]]; then
        response_body=$(cat "$response_file")
    fi
    
    # Validate response
    if [[ "$http_code" == "$expected_status" ]]; then
        echo "✅ HTTP $http_code (Expected: $expected_status)"
        if [[ -n "$response_body" ]]; then
            echo "📄 Response Body:"
            echo "$response_body" | jq . 2>/dev/null || echo "$response_body"
        fi
        
        # Extract token if it's a login response
        if [[ "$url" == *"/auth/login"* && "$http_code" == "200" ]]; then
            JWT_TOKEN=$(echo "$response_body" | jq -r '.data.token' 2>/dev/null || echo "")
            log "INFO" "JWT Token extracted (length: ${#JWT_TOKEN})"
        fi
        
        # Extract IDs from responses
        if [[ "$response_body" == *"\"id\""* ]]; then
            local id=$(echo "$response_body" | jq -r '.data.id // .id' 2>/dev/null || echo "")
            if [[ -n "$id" && "$id" != "null" ]]; then
                if [[ "$url" == *"/events"* ]]; then
                    EVENT_ID="$id"
                elif [[ "$url" == *"/bets"* ]]; then
                    BET_ID="$id"
                fi
            fi
        fi
        
        return 0
    else
        echo "❌ HTTP $http_code (Expected: $expected_status)"
        if [[ -n "$response_body" ]]; then
            echo "📄 Response Body:"
            echo "$response_body" | jq . 2>/dev/null || echo "$response_body"
        fi
        return 1
    fi
    
    # Cleanup
    rm -f "$response_file" "$headers_file"
}

# Test Health Endpoints
test_health_endpoints() {
    local api_url="$1"
    local env_name="$2"
    
    log "INFO" "Testing Health Endpoints - $env_name"
    
    # Test /health
    start_test "Health Check - $env_name"
    if make_request "GET" "$api_url/../health" "" "" "200"; then
        pass_test "Health Check - $env_name"
    else
        fail_test "Health Check - $env_name" "Health endpoint failed"
    fi
    
    # Test /health/simple
    start_test "Simple Health Check - $env_name"
    if make_request "GET" "$api_url/../health/simple" "" "" "200"; then
        pass_test "Simple Health Check - $env_name"
    else
        fail_test "Simple Health Check - $env_name" "Simple health endpoint failed"
    fi
}

# Test Authentication Endpoints
test_auth_endpoints() {
    local api_url="$1"
    local env_name="$2"
    
    log "INFO" "Testing Authentication Endpoints - $env_name"
    
    # Test Registration
    start_test "User Registration - $env_name"
    local register_data="{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASSWORD\",\"fullName\":\"$TEST_FULL_NAME\"}"
    if make_request "POST" "$api_url/auth/register" "$register_data" "" "201"; then
        pass_test "User Registration - $env_name"
    else
        fail_test "User Registration - $env_name" "Registration failed"
    fi
    
    # Test Login
    start_test "User Login - $env_name"
    local login_data="{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASSWORD\"}"
    if make_request "POST" "$api_url/auth/login" "$login_data" "" "200"; then
        pass_test "User Login - $env_name"
    else
        fail_test "User Login - $env_name" "Login failed"
        return 1
    fi
    
    # Test Token Validation
    start_test "Token Validation - $env_name"
    local token_data="{\"token\":\"$JWT_TOKEN\"}"
    if make_request "POST" "$api_url/auth/validate-token" "$token_data" "" "200"; then
        pass_test "Token Validation - $env_name"
    else
        fail_test "Token Validation - $env_name" "Token validation failed"
    fi
    
    # Test Profile (Authenticated)
    start_test "Get Profile - $env_name"
    if make_request "GET" "$api_url/auth/profile" "" "$JWT_TOKEN" "200"; then
        pass_test "Get Profile - $env_name"
    else
        fail_test "Get Profile - $env_name" "Profile endpoint failed"
    fi
    
    # Test Logout
    start_test "User Logout - $env_name"
    if make_request "POST" "$api_url/auth/logout" "" "$JWT_TOKEN" "200"; then
        pass_test "User Logout - $env_name"
    else
        fail_test "User Logout - $env_name" "Logout failed"
    fi
}

# Test Events Endpoints
test_events_endpoints() {
    local api_url="$1"
    local env_name="$2"
    
    log "INFO" "Testing Events Endpoints - $env_name"
    
    # Test Get Events
    start_test "Get Events - $env_name"
    if make_request "GET" "$api_url/events" "" "" "200"; then
        pass_test "Get Events - $env_name"
    else
        fail_test "Get Events - $env_name" "Get events failed"
    fi
    
    # Test Get Events with Pagination
    start_test "Get Events with Pagination - $env_name"
    if make_request "GET" "$api_url/events?page=1&pageSize=5" "" "" "200"; then
        pass_test "Get Events with Pagination - $env_name"
    else
        fail_test "Get Events with Pagination - $env_name" "Pagination failed"
    fi
    
    # Test Create Event (Authenticated)
    start_test "Create Event - $env_name"
    local event_data="{\"name\":\"Test Event $(date +%s)\",\"teamA\":\"Team A\",\"teamB\":\"Team B\",\"eventDate\":\"$(date -u -d '+2 hours' +%Y-%m-%dT%H:%M:%SZ)\",\"oddsTeamA\":1.5,\"oddsTeamB\":2.5}"
    if make_request "POST" "$api_url/events" "$event_data" "$JWT_TOKEN" "201"; then
        pass_test "Create Event - $env_name"
    else
        fail_test "Create Event - $env_name" "Event creation failed"
    fi
    
    # Test Get Event Details (using ID 1 as fallback)
    local test_event_id="${EVENT_ID:-1}"
    start_test "Get Event Details - $env_name"
    if make_request "GET" "$api_url/events/$test_event_id" "" "" "200"; then
        pass_test "Get Event Details - $env_name"
    else
        fail_test "Get Event Details - $env_name" "Get event details failed"
    fi
    
    # Test Event Stats
    start_test "Get Event Stats - $env_name"
    if make_request "GET" "$api_url/events/$test_event_id/stats" "" "" "200"; then
        pass_test "Get Event Stats - $env_name"
    else
        fail_test "Get Event Stats - $env_name" "Event stats failed"
    fi
    
    # Test Event Availability
    start_test "Check Event Availability - $env_name"
    if make_request "GET" "$api_url/events/$test_event_id/availability" "" "" "200"; then
        pass_test "Check Event Availability - $env_name"
    else
        fail_test "Check Event Availability - $env_name" "Event availability check failed"
    fi
    
    # Test Search Events
    start_test "Search Events - $env_name"
    if make_request "GET" "$api_url/events/search?page=1&pageSize=10" "" "" "200"; then
        pass_test "Search Events - $env_name"
    else
        fail_test "Search Events - $env_name" "Event search failed"
    fi
}

# Test Bets Endpoints
test_bets_endpoints() {
    local api_url="$1"
    local env_name="$2"
    
    log "INFO" "Testing Bets Endpoints - $env_name"
    
    # Test Preview Bet
    start_test "Preview Bet - $env_name"
    local preview_data="{\"eventId\":1,\"selectedTeam\":\"Team A\",\"amount\":10.0}"
    if make_request "POST" "$api_url/bets/preview" "$preview_data" "$JWT_TOKEN" "200"; then
        pass_test "Preview Bet - $env_name"
    else
        fail_test "Preview Bet - $env_name" "Bet preview failed"
    fi
    
    # Test Create Bet
    start_test "Create Bet - $env_name"
    local bet_data="{\"eventId\":1,\"selectedTeam\":\"Team A\",\"amount\":10.0}"
    if make_request "POST" "$api_url/bets" "$bet_data" "$JWT_TOKEN" "201"; then
        pass_test "Create Bet - $env_name"
    else
        fail_test "Create Bet - $env_name" "Bet creation failed"
    fi
    
    # Test Get My Bets
    start_test "Get My Bets - $env_name"
    if make_request "GET" "$api_url/bets/my-bets" "" "$JWT_TOKEN" "200"; then
        pass_test "Get My Bets - $env_name"
    else
        fail_test "Get My Bets - $env_name" "Get my bets failed"
    fi
    
    # Test Get My Stats
    start_test "Get My Bet Stats - $env_name"
    if make_request "GET" "$api_url/bets/my-stats" "" "$JWT_TOKEN" "200"; then
        pass_test "Get My Bet Stats - $env_name"
    else
        fail_test "Get My Bet Stats - $env_name" "Get bet stats failed"
    fi
    
    # Test Get Bet History
    start_test "Get Bet History - $env_name"
    if make_request "GET" "$api_url/bets/history?page=1&pageSize=10" "" "$JWT_TOKEN" "200"; then
        pass_test "Get Bet History - $env_name"
    else
        fail_test "Get Bet History - $env_name" "Get bet history failed"
    fi
    
    # Test Get Specific Bet (using ID 1 as fallback)
    local test_bet_id="${BET_ID:-1}"
    start_test "Get Specific Bet - $env_name"
    if make_request "GET" "$api_url/bets/$test_bet_id" "" "$JWT_TOKEN" "200"; then
        pass_test "Get Specific Bet - $env_name"
    else
        fail_test "Get Specific Bet - $env_name" "Get specific bet failed"
    fi
}

# Test Security Scenarios
test_security_scenarios() {
    local api_url="$1"
    local env_name="$2"
    
    log "INFO" "Testing Security Scenarios - $env_name"
    
    # Test Unauthorized Access
    start_test "Unauthorized Access Test - $env_name"
    if make_request "GET" "$api_url/auth/profile" "" "" "401"; then
        pass_test "Unauthorized Access Test - $env_name"
    else
        fail_test "Unauthorized Access Test - $env_name" "Should return 401"
    fi
    
    # Test Invalid Token
    start_test "Invalid Token Test - $env_name"
    if make_request "GET" "$api_url/auth/profile" "" "invalid-token" "401"; then
        pass_test "Invalid Token Test - $env_name"
    else
        fail_test "Invalid Token Test - $env_name" "Should return 401"
    fi
    
    # Test Invalid Data
    start_test "Invalid Registration Data - $env_name"
    local invalid_data="{\"email\":\"invalid-email\",\"password\":\"123\"}"
    if make_request "POST" "$api_url/auth/register" "$invalid_data" "" "400"; then
        pass_test "Invalid Registration Data - $env_name"
    else
        fail_test "Invalid Registration Data - $env_name" "Should return 400"
    fi
}

# Main execution
main() {
    echo "================================================================="
    echo "    🚀 COMPREHENSIVE SPORTS BETTING API TESTING SUITE"
    echo "================================================================="
    echo ""
    
    log "INFO" "Starting comprehensive endpoint testing..."
    log "INFO" "Local API: $LOCAL_API_URL"
    log "INFO" "Production API: $PROD_API_URL"
    echo ""
    
    # Test LOCAL Environment
    echo "🏠 TESTING LOCAL ENVIRONMENT"
    echo "================================="
    
    test_health_endpoints "$LOCAL_API_URL" "LOCAL"
    test_auth_endpoints "$LOCAL_API_URL" "LOCAL"
    test_events_endpoints "$LOCAL_API_URL" "LOCAL"
    test_bets_endpoints "$LOCAL_API_URL" "LOCAL"
    test_security_scenarios "$LOCAL_API_URL" "LOCAL"
    
    LOCAL_TESTS=$PASSED_TESTS
    
    # Reset for production tests
    JWT_TOKEN=""
    BET_ID=""
    EVENT_ID=""
    
    echo ""
    echo "🌩️  TESTING PRODUCTION ENVIRONMENT (AWS Lambda)"
    echo "================================================="
    
    test_health_endpoints "$PROD_API_URL" "PRODUCTION"
    test_auth_endpoints "$PROD_API_URL" "PRODUCTION"
    test_events_endpoints "$PROD_API_URL" "PRODUCTION"
    test_bets_endpoints "$PROD_API_URL" "PRODUCTION"
    test_security_scenarios "$PROD_API_URL" "PRODUCTION"
    
    PROD_TESTS=$((PASSED_TESTS - LOCAL_TESTS))
    
    # Final Report
    echo ""
    echo "================================================================="
    echo "                    📊 FINAL TEST REPORT"
    echo "================================================================="
    echo "Total Tests:     $TOTAL_TESTS"
    echo "Passed:          $PASSED_TESTS"
    echo "Failed:          $FAILED_TESTS"
    echo "Success Rate:    $((PASSED_TESTS * 100 / TOTAL_TESTS))%"
    echo ""
    echo "Local Tests:     $LOCAL_TESTS"
    echo "Production Tests: $PROD_TESTS"
    echo ""
    echo "Log File:        $LOG_FILE"
    echo "================================================================="
    
    if [[ $FAILED_TESTS -eq 0 ]]; then
        log "SUCCESS" "🎉 All tests passed! API is ready for production."
        exit 0
    else
        log "ERROR" "❌ Some tests failed. Check the log for details."
        exit 1
    fi
}

# Execute main function
main "$@"