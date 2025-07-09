#!/bin/bash

echo "=== Error Scenarios Test ==="

# Get fresh token
TOKEN=$(curl -s -X POST "http://localhost:5002/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "cleantest@example.com", "password": "SecurePassword123"}' | \
  python3 -c "import sys, json; print(json.load(sys.stdin)['data']['token'])")

echo "Token obtained: ${TOKEN:0:20}..."

# Test 1: Insufficient balance
echo "=== TEST 1: Insufficient Balance ==="
# First check current balance
CURRENT_BALANCE=$(curl -s -X GET "http://localhost:5002/api/auth/profile" -H "Authorization: Bearer ${TOKEN}" | python3 -c "import sys, json; print(json.load(sys.stdin)['data']['balance'])")
echo "Current balance: $${CURRENT_BALANCE}"

# Try to bet more than current balance
INSUFFICIENT_AMOUNT=$(python3 -c "print(${CURRENT_BALANCE} + 100)")
echo "Trying to bet: $${INSUFFICIENT_AMOUNT} (more than available balance)"

curl -s -X POST "http://localhost:5002/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{
    \"eventId\": 1,
    \"selectedTeam\": \"Real Madrid\",
    \"amount\": ${INSUFFICIENT_AMOUNT}
  }" | python3 -m json.tool

echo ""

# Test 2: Non-existent event
echo "=== TEST 2: Non-existent Event ==="
curl -s -X POST "http://localhost:5002/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 999,
    "selectedTeam": "Real Madrid",
    "amount": 50.00
  }' | python3 -m json.tool

echo ""

# Test 3: Invalid team
echo "=== TEST 3: Invalid Team ==="
curl -s -X POST "http://localhost:5002/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "selectedTeam": "InvalidTeam",
    "amount": 50.00
  }' | python3 -m json.tool

echo ""

# Test 4: Invalid amount
echo "=== TEST 4: Invalid Amount ==="
curl -s -X POST "http://localhost:5002/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "selectedTeam": "Real Madrid",
    "amount": 0.50
  }' | python3 -m json.tool

echo ""

echo "=== Error Tests Complete ==="