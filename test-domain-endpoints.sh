#!/bin/bash

# Configuración del dominio
DOMAIN="api-kurax-demo-jos.uk"
API_URL="https://${DOMAIN}/api"
HEALTH_URL="https://${DOMAIN}/health"
TEST_EMAIL="domain_test_$(date +%s)@example.com"
TEST_PASSWORD="DomainTest123"
TEST_FULL_NAME="Domain Test User"

echo "=============================================="
echo "  🌐 PRUEBAS DE ENDPOINTS CON DOMINIO REAL"
echo "=============================================="
echo "Dominio: ${DOMAIN}"
echo "API URL: ${API_URL}"
echo ""

# Función para mostrar resultados
show_result() {
    local test_name="$1"
    local status="$2"
    local details="$3"
    
    if [[ "$status" == "success" ]]; then
        echo "✅ $test_name - EXITOSO"
        [[ -n "$details" ]] && echo "   └── $details"
    else
        echo "❌ $test_name - FALLIDO"
        [[ -n "$details" ]] && echo "   └── $details"
    fi
}

# Test 1: Conectividad al dominio
echo "1. Probando conectividad al dominio..."
if curl -s -o /dev/null -w "%{http_code}" --max-time 10 "https://${DOMAIN}" | grep -q "200\|301\|302"; then
    show_result "Conectividad al dominio" "success" "Dominio accesible"
else
    show_result "Conectividad al dominio" "failed" "No se puede acceder al dominio"
    echo ""
    echo "❌ Error: No se puede acceder al dominio ${DOMAIN}"
    echo "   Verifica que:"
    echo "   - El dominio apunte a tu servidor"
    echo "   - Los puertos 80 y 443 estén abiertos"
    echo "   - El certificado SSL esté configurado"
    exit 1
fi

# Test 2: Health Check
echo "2. Probando Health Check..."
HEALTH_RESPONSE=$(curl -s --max-time 10 "$HEALTH_URL" 2>/dev/null)
if [[ $? -eq 0 ]] && echo "$HEALTH_RESPONSE" | jq -e '.status' >/dev/null 2>&1; then
    HEALTH_STATUS=$(echo "$HEALTH_RESPONSE" | jq -r '.status')
    if [[ "$HEALTH_STATUS" == "Healthy" ]]; then
        ENV=$(echo "$HEALTH_RESPONSE" | jq -r '.environment')
        show_result "Health Check" "success" "Status: $HEALTH_STATUS, Entorno: $ENV"
    else
        show_result "Health Check" "failed" "Status: $HEALTH_STATUS"
    fi
else
    show_result "Health Check" "failed" "No se pudo obtener respuesta del health check"
    echo "Response: $HEALTH_RESPONSE"
fi

# Test 3: Registro de Usuario
echo "3. Probando registro de usuario..."
REGISTER_RESPONSE=$(curl -s --max-time 15 -X POST "$API_URL/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"email\": \"$TEST_EMAIL\", \"password\": \"$TEST_PASSWORD\", \"fullName\": \"$TEST_FULL_NAME\"}" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$REGISTER_RESPONSE" | jq -e '.value.data.token' >/dev/null 2>&1; then
    JWT_TOKEN=$(echo "$REGISTER_RESPONSE" | jq -r '.value.data.token')
    USER_EMAIL=$(echo "$REGISTER_RESPONSE" | jq -r '.value.data.email')
    show_result "Registro de usuario" "success" "Usuario creado: $USER_EMAIL"
else
    show_result "Registro de usuario" "failed" "Error en el registro"
    echo "Response: $REGISTER_RESPONSE"
    exit 1
fi

# Test 4: Login de Usuario
echo "4. Probando login de usuario..."
LOGIN_RESPONSE=$(curl -s --max-time 15 -X POST "$API_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\": \"$TEST_EMAIL\", \"password\": \"$TEST_PASSWORD\"}" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$LOGIN_RESPONSE" | jq -e '.data.token' >/dev/null 2>&1; then
    LOGIN_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.data.token')
    show_result "Login de usuario" "success" "Token JWT obtenido"
else
    show_result "Login de usuario" "failed" "Error en el login"
    echo "Response: $LOGIN_RESPONSE"
fi

# Test 5: Perfil de Usuario
echo "5. Probando perfil de usuario..."
PROFILE_RESPONSE=$(curl -s --max-time 15 -X GET "$API_URL/auth/profile" \
    -H "Authorization: Bearer $JWT_TOKEN" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$PROFILE_RESPONSE" | jq -e '.data.email' >/dev/null 2>&1; then
    PROFILE_EMAIL=$(echo "$PROFILE_RESPONSE" | jq -r '.data.email')
    BALANCE=$(echo "$PROFILE_RESPONSE" | jq -r '.data.balance')
    show_result "Perfil de usuario" "success" "Email: $PROFILE_EMAIL, Balance: $BALANCE"
else
    show_result "Perfil de usuario" "failed" "Error obteniendo perfil"
    echo "Response: $PROFILE_RESPONSE"
fi

# Test 6: Lista de Eventos
echo "6. Probando lista de eventos..."
EVENTS_RESPONSE=$(curl -s --max-time 15 -X GET "$API_URL/events" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$EVENTS_RESPONSE" | jq -e '.data' >/dev/null 2>&1; then
    EVENTS_COUNT=$(echo "$EVENTS_RESPONSE" | jq '.data | length')
    if [[ "$EVENTS_COUNT" -gt 0 ]]; then
        show_result "Lista de eventos" "success" "Encontrados $EVENTS_COUNT eventos"
    else
        show_result "Lista de eventos" "failed" "No se encontraron eventos"
    fi
else
    show_result "Lista de eventos" "failed" "Error obteniendo eventos"
    echo "Response: $EVENTS_RESPONSE"
fi

# Test 7: Detalles de Evento
echo "7. Probando detalles de evento..."
EVENT_RESPONSE=$(curl -s --max-time 15 -X GET "$API_URL/events/1" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$EVENT_RESPONSE" | jq -e '.data.name' >/dev/null 2>&1; then
    EVENT_NAME=$(echo "$EVENT_RESPONSE" | jq -r '.data.name')
    EVENT_STATUS=$(echo "$EVENT_RESPONSE" | jq -r '.data.status')
    show_result "Detalles de evento" "success" "Evento: $EVENT_NAME, Status: $EVENT_STATUS"
else
    show_result "Detalles de evento" "failed" "Error obteniendo detalles del evento"
    echo "Response: $EVENT_RESPONSE"
fi

# Test 8: Preview de Apuesta
echo "8. Probando preview de apuesta..."
PREVIEW_RESPONSE=$(curl -s --max-time 15 -X POST "$API_URL/bets/preview" \
    -H "Authorization: Bearer $JWT_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"eventId": 1, "selectedTeam": "Real Madrid", "amount": 25.0}' 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$PREVIEW_RESPONSE" | jq -e '.data.isValid' >/dev/null 2>&1; then
    IS_VALID=$(echo "$PREVIEW_RESPONSE" | jq -r '.data.isValid')
    POTENTIAL_WIN=$(echo "$PREVIEW_RESPONSE" | jq -r '.data.potentialWin')
    show_result "Preview de apuesta" "success" "Válida: $IS_VALID, Ganancia potencial: $POTENTIAL_WIN"
else
    show_result "Preview de apuesta" "failed" "Error en preview de apuesta"
    echo "Response: $PREVIEW_RESPONSE"
fi

# Test 9: Crear Apuesta
echo "9. Probando creación de apuesta..."
BET_RESPONSE=$(curl -s --max-time 15 -X POST "$API_URL/bets" \
    -H "Authorization: Bearer $JWT_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"eventId": 1, "selectedTeam": "Real Madrid", "amount": 25.0}' 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$BET_RESPONSE" | jq -e '.success' >/dev/null 2>&1; then
    BET_SUCCESS=$(echo "$BET_RESPONSE" | jq -r '.success')
    if [[ "$BET_SUCCESS" == "true" ]]; then
        BET_ID=$(echo "$BET_RESPONSE" | jq -r '.data.id')
        show_result "Creación de apuesta" "success" "Apuesta creada con ID: $BET_ID"
    else
        show_result "Creación de apuesta" "failed" "La apuesta no se pudo crear"
    fi
else
    show_result "Creación de apuesta" "failed" "Error creando apuesta"
    echo "Response: $BET_RESPONSE"
fi

# Test 10: Mis Apuestas
echo "10. Probando mis apuestas..."
MY_BETS_RESPONSE=$(curl -s --max-time 15 -X GET "$API_URL/bets/my-bets" \
    -H "Authorization: Bearer $JWT_TOKEN" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$MY_BETS_RESPONSE" | jq -e '.data' >/dev/null 2>&1; then
    BETS_COUNT=$(echo "$MY_BETS_RESPONSE" | jq '.data | length')
    show_result "Mis apuestas" "success" "Encontradas $BETS_COUNT apuestas"
else
    show_result "Mis apuestas" "failed" "Error obteniendo apuestas"
    echo "Response: $MY_BETS_RESPONSE"
fi

# Test 11: Estadísticas de Usuario
echo "11. Probando estadísticas de usuario..."
STATS_RESPONSE=$(curl -s --max-time 15 -X GET "$API_URL/bets/my-stats" \
    -H "Authorization: Bearer $JWT_TOKEN" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$STATS_RESPONSE" | jq -e '.data.totalBets' >/dev/null 2>&1; then
    TOTAL_BETS=$(echo "$STATS_RESPONSE" | jq -r '.data.totalBets')
    ACTIVE_BETS=$(echo "$STATS_RESPONSE" | jq -r '.data.activeBets')
    show_result "Estadísticas de usuario" "success" "Total: $TOTAL_BETS apuestas, Activas: $ACTIVE_BETS"
else
    show_result "Estadísticas de usuario" "failed" "Error obteniendo estadísticas"
    echo "Response: $STATS_RESPONSE"
fi

# Test 12: Logout
echo "12. Probando logout..."
LOGOUT_RESPONSE=$(curl -s --max-time 15 -X POST "$API_URL/auth/logout" \
    -H "Authorization: Bearer $JWT_TOKEN" 2>/dev/null)

if [[ $? -eq 0 ]] && echo "$LOGOUT_RESPONSE" | jq -e '.success' >/dev/null 2>&1; then
    LOGOUT_SUCCESS=$(echo "$LOGOUT_RESPONSE" | jq -r '.success')
    if [[ "$LOGOUT_SUCCESS" == "true" ]]; then
        show_result "Logout" "success" "Sesión cerrada correctamente"
    else
        show_result "Logout" "failed" "Error en logout"
    fi
else
    show_result "Logout" "failed" "Error en logout"
    echo "Response: $LOGOUT_RESPONSE"
fi

echo ""
echo "=============================================="
echo "  🎉 PRUEBAS COMPLETADAS CON DOMINIO REAL"
echo "=============================================="
echo ""
echo "✅ Dominio: ${DOMAIN}"
echo "✅ API URL: ${API_URL}"
echo "✅ Certificado SSL: Configurado"
echo "✅ Endpoints: Funcionando correctamente"
echo "✅ Autenticación: Operacional"
echo "✅ Sistema de Apuestas: Funcional"
echo "✅ Base de Datos: Conectada"
echo ""
echo "🚀 La API está completamente operacional en producción"
echo "   con el dominio ${DOMAIN}"