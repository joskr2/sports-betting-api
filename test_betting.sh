#!/bin/bash

# Sports Betting API Test Script
echo "=== Sports Betting API Test Suite ==="

# Get authentication token
echo "Getting authentication token..."
TOKEN=$(curl -s -X POST "http://localhost:5002/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "cleantest@example.com", "password": "SecurePassword123"}' | \
  python3 -c "import sys, json; print(json.load(sys.stdin)['data']['token'])")

if [ -z "$TOKEN" ]; then
    echo "ERROR: Failed to get authentication token"
    exit 1
fi

echo "Token obtained successfully (length: ${#TOKEN})"
echo ""

# Test 1: Preview Bet
echo "=== TEST 1: Preview Bet ==="
curl -s -X POST "http://localhost:5002/api/bets/preview" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "selectedTeam": "Real Madrid",
    "amount": 50.00
  }' > preview_response.json

if [ -s preview_response.json ]; then
    echo "✅ Preview bet response received"
    python3 -m json.tool < preview_response.json
else
    echo "❌ Preview bet failed - no response"
fi
echo ""

# Test 2: Create Bet
echo "=== TEST 2: Create Bet ==="
curl -s -X POST "http://localhost:5002/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "selectedTeam": "Real Madrid",
    "amount": 50.00
  }' > create_response.json

if [ -s create_response.json ]; then
    echo "✅ Create bet response received"
    python3 -m json.tool < create_response.json
else
    echo "❌ Create bet failed - no response"
fi
echo ""

# Test 3: Get My Bets
echo "=== TEST 3: Get My Bets ==="
curl -s -X GET "http://localhost:5002/api/bets/my-bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Accept: application/json" > mybets_response.json

if [ -s mybets_response.json ]; then
    echo "✅ Get my bets response received"
    python3 -m json.tool < mybets_response.json
else
    echo "❌ Get my bets failed - no response"
fi
echo ""

# Test 4: Get Bet Statistics
echo "=== TEST 4: Get Bet Statistics ==="
curl -s -X GET "http://localhost:5002/api/bets/my-stats" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Accept: application/json" > stats_response.json

if [ -s stats_response.json ]; then
    echo "✅ Get bet statistics response received"
    python3 -m json.tool < stats_response.json
else
    echo "❌ Get bet statistics failed - no response"
fi
echo ""

# Test 5: Get Bet History
echo "=== TEST 5: Get Bet History ==="
curl -s -X GET "http://localhost:5002/api/bets/history?page=1&pageSize=10" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Accept: application/json" > history_response.json

if [ -s history_response.json ]; then
    echo "✅ Get bet history response received"
    python3 -m json.tool < history_response.json
else
    echo "❌ Get bet history failed - no response"
fi
echo ""

# Cleanup
rm -f preview_response.json create_response.json mybets_response.json stats_response.json history_response.json

echo "=== Test Suite Complete ==="