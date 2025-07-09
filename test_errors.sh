#!/bin/bash

echo "=== Error Scenarios Test ==="

# Get fresh token
TOKEN=$(curl -s -X POST "http://localhost:5163/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "testuser@example.com", "password": "SecurePassword123"}' | \
  python3 -c "import sys, json; print(json.load(sys.stdin)['data']['token'])")

echo "Token obtained: ${TOKEN:0:20}..."

# Test 1: Insufficient balance
echo "=== TEST 1: Insufficient Balance ==="
curl -s -X POST "http://localhost:5163/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "selectedTeam": "Real Madrid",
    "amount": 500.00
  }' | python3 -m json.tool

echo ""

# Test 2: Non-existent event
echo "=== TEST 2: Non-existent Event ==="
curl -s -X POST "http://localhost:5163/api/bets" \
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
curl -s -X POST "http://localhost:5163/api/bets" \
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
curl -s -X POST "http://localhost:5163/api/bets" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "selectedTeam": "Real Madrid",
    "amount": 0.50
  }' | python3 -m json.tool

echo ""

echo "=== Error Tests Complete ==="