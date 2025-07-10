# 🏆 Sports Betting API

Una API REST completa para apuestas deportivas desarrollada con .NET 8, Entity Framework Core y PostgreSQL, con despliegue en producción en AWS EC2 usando Docker y Cloudflare.

## 📋 Tabla de Contenidos

- [🏆 Sports Betting API](#-sports-betting-api)
  - [📋 Tabla de Contenidos](#-tabla-de-contenidos)
  - [✨ Características Principales](#-características-principales)
  - [🛠️ Tecnologías Utilizadas](#️-tecnologías-utilizadas)
  - [🏗️ Arquitectura del Sistema](#️-arquitectura-del-sistema)
  - [🚀 Configuración y Desarrollo Local](#-configuración-y-desarrollo-local)
    - [Prerrequisitos](#prerrequisitos)
    - [Instalación](#instalación)
    - [Variables de Entorno](#variables-de-entorno)
  - [🐳 Docker y Contenedores](#-docker-y-contenedores)
    - [Desarrollo Local](#desarrollo-local)
    - [Producción](#producción)
  - [🔧 Configuración de Base de Datos](#-configuración-de-base-de-datos)
  - [🔐 Autenticación y Autorización](#-autenticación-y-autorización)
  - [📊 API Endpoints](#-api-endpoints)
    - [Health Check](#health-check)
    - [Autenticación](#autenticación)
    - [Usuarios](#usuarios)
    - [Eventos](#eventos)
    - [Apuestas](#apuestas)
  - [🧪 Testing](#-testing)
  - [🌐 Despliegue en Producción](#-despliegue-en-producción)
    - [AWS EC2 Deployment](#aws-ec2-deployment)
    - [Configuración SSL con Cloudflare](#configuración-ssl-con-cloudflare)
    - [Nginx Reverse Proxy](#nginx-reverse-proxy)
  - [🔒 Seguridad](#-seguridad)
  - [📈 Monitoreo y Logs](#-monitoreo-y-logs)
  - [🔄 CI/CD](#-cicd)
  - [📚 Documentación API](#-documentación-api)
  - [🐛 Troubleshooting](#-troubleshooting)
  - [🤝 Contribución](#-contribución)
  - [📄 Licencia](#-licencia)

## ✨ Características Principales

- 🏆 **Sistema completo de apuestas deportivas**
- 🔐 **Autenticación JWT segura**
- 💰 **Gestión de balance de usuarios**
- 🎯 **Sistema de odds dinámicas**
- 📊 **Validación de apuestas en tiempo real**
- 🔒 **Transacciones atómicas**
- 🌐 **API RESTful con documentación Swagger**
- 🐳 **Containerización completa con Docker**
- ☁️ **Despliegue en producción en AWS EC2**
- 🔐 **SSL/TLS con Cloudflare**
- 📈 **Health checks y monitoreo**
- 🧪 **Suite de tests comprehensiva**

## 🛠️ Tecnologías Utilizadas

### Backend
- **.NET 9** - Framework principal
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL 15** - Base de datos principal
- **Redis** - Cache y sesiones
- **JWT** - Autenticación y autorización

### DevOps y Infraestructura
- **Docker & Docker Compose** - Containerización
- **Nginx** - Reverse proxy y load balancer
- **AWS EC2** - Hosting en la nube
- **Cloudflare** - CDN, DNS y SSL

### Herramientas de Desarrollo
- **Swagger** - Documentación de API
- **EF Migrations** - Gestión de esquema de BD

## 🏗️ Arquitectura del Sistema

### Arquitectura de Producción (AWS EC2)
```
Internet Users
      │
      ▼ HTTPS
┌─────────────────┐
│   Cloudflare    │ (SSL Termination, CDN, DNS)
│ (Flexible Mode) │
└─────────────────┘
      │ HTTP
      ▼
┌─────────────────┐
│   AWS EC2       │ [server-ip]:80,443
│ Amazon Linux    │
└─────────────────┘
      │
      ▼
┌─────────────────┐ Container: nginx
│      Nginx      │ Ports: 80→80, 443→443
│ (Reverse Proxy) │ Network: frontend + backend
└─────────────────┘
      │ proxy_pass :5000
      ▼
┌─────────────────┐ Container: api
│  .NET 9 API     │ Port: 5000 (internal)
│ Sports Betting  │ Network: backend only
└─────────────────┘
      │
   ┌──┴──┐
   ▼     ▼
┌─────┐ ┌─────┐
│ DB  │ │Redis│ Containers: postgres, redis
│:5432│ │:6379│ Network: backend only
└─────┘ └─────┘
```

### Arquitectura de Desarrollo Local
```
Developer Machine
      │
      ▼ http://localhost:5000
┌─────────────────┐
│ Docker Desktop  │
└─────────────────┘
      │
      ▼
┌─────────────────┐ Container: api
│  .NET 9 API     │ Port: 5000→5000
│   Development   │ Hot reload enabled
│   Environment   │ Swagger UI enabled
└─────────────────┘
      │
      ▼
┌─────────────────┐ Container: postgres
│   PostgreSQL    │ Port: 5432→5432
│ (Development)   │ Volume: postgres_data
└─────────────────┘

```

## 🚀 Configuración y Desarrollo Local

### Prerrequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL](https://www.postgresql.org/download/) (opcional, se puede usar Docker)
- [Git](https://git-scm.com/)

### Instalación

1. **Clonar el repositorio:**
```bash
git clone <repository-url>
cd SportsBetting.Backend/SportsBetting.Api
```

2. **Restaurar dependencias:**
```bash
dotnet restore
```

3. **Configurar base de datos:**
```bash
dotnet ef database update
```

4. **Ejecutar con Docker (Recomendado):**
```bash
docker-compose up -d
```

5. **O ejecutar localmente:**
```bash
dotnet run
```

### Variables de Entorno

Crear archivo `.env` basado en `.env.example`:

```env
# Base de datos
POSTGRES_DB=sportsbetting_dev
POSTGRES_USER=sportsbetting_user
POSTGRES_PASSWORD=your_secure_password

# JWT
JWT_SECRET=your_jwt_secret_key_very_long_and_secure
JWT_ISSUER=SportsBettingAPI
JWT_AUDIENCE=SportsBettingClients
JWT_EXPIRATION_DAYS=1

# Usuario
INITIAL_BALANCE=1000.00
MIN_BET_AMOUNT=1.00
MAX_BET_AMOUNT=10000.00

# Redis
REDIS_PASSWORD=your_redis_password
```

## 🐳 Docker y Contenedores

### Desarrollo Local

```bash
# Iniciar todos los servicios
docker-compose up -d

# Ver logs
docker-compose logs -f

# Parar servicios
docker-compose down

# Rebuild
docker-compose up -d --build
```

### Producción

El proyecto incluye configuraciones específicas para producción:

- `docker-compose.prod.yml` - Configuración con Docker Swarm secrets
- `docker-compose.final-prod.yml` - Configuración simplificada para EC2
- Configuraciones optimizadas de Nginx
- Health checks y resource limits

## 🔧 Configuración de Base de Datos

### Migraciones

```bash
# Crear nueva migración
dotnet ef migrations add MigrationName

# Aplicar migraciones
dotnet ef database update

# Revertir migración
dotnet ef database update PreviousMigrationName
```

### Datos de Ejemplo

El sistema incluye datos de ejemplo que se cargan automáticamente:

- **Eventos deportivos**: Manchester United vs Chelsea, Real Madrid vs Barcelona, Liverpool vs Arsenal
- **Tipos de apuesta**: Win, Draw, Lose con odds específicas
- **Usuario de prueba**: Para testing de endpoints

## 🔐 Autenticación y Autorización

### JWT Token

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Respuesta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiration": "2025-07-11T10:30:00Z",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "balance": 1000.00
  }
}
```

### Uso del Token

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

## 📊 API Endpoints

### Health Check

```http
GET /health
```

**Respuesta:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-07-10T04:28:46.016191Z",
  "environment": "Production",
  "version": "1.0.0",
  "dependencies": {
    "database": "Healthy",
    "redis": "Healthy"
  }
}
```

### Autenticación

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/auth/register` | Registro de usuario |
| POST | `/api/auth/login` | Inicio de sesión |
| POST | `/api/auth/logout` | Cerrar sesión |
| GET | `/api/auth/profile` | Perfil del usuario |

### Usuarios

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/users/balance` | Consultar balance |
| PUT | `/api/users/balance` | Actualizar balance |

### Eventos

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/events` | Listar eventos disponibles |
| GET | `/api/events/{id}` | Obtener evento específico |
| GET | `/api/events/{id}/odds` | Obtener odds del evento |

### Apuestas

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/bets` | Realizar apuesta |
| GET | `/api/bets` | Historial de apuestas |
| GET | `/api/bets/{id}` | Detalle de apuesta |


### Test Collection Incluida

- ✅ **Health Check Tests**
- ✅ **Authentication Tests**
- ✅ **User Registration Tests**
- ✅ **Balance Management Tests**
- ✅ **Events API Tests**
- ✅ **Betting System Tests**
- ✅ **Integration Tests**

### Archivo de Testing HTTP

El proyecto incluye `SportsBetting.Api.http` con requests preconfigurados para testing manual de todos los endpoints.

## 🌐 Despliegue en Producción

### AWS EC2 Deployment

El proyecto está configurado para desplegarse en AWS EC2:

- **Instancia**: Amazon Linux 2023
- **IP Pública**: [Configure en scripts/deploy-to-ec2.sh]
- **Dominio**: [Configure tu dominio]
- **SSL**: Cloudflare 

### Configuración SSL con Cloudflare

1. **DNS Configuration**:
   - Tipo: A
   - Nombre: [tu-subdomain]
   - Valor: [server-ip]
   - Proxy: ✅ Activado

2. **SSL Mode**: Flexible

### Nginx Reverse Proxy

Configuración optimizada para:
- Rate limiting
- Security headers
- Gzip compression
- Health checks
- Load balancing

## 🔒 Seguridad

### Medidas Implementadas

- 🔐 **JWT Authentication** con tokens seguros
- 🛡️ **Rate Limiting** en endpoints críticos
- 🔒 **HTTPS** obligatorio en producción
- 🚫 **CORS** configurado apropiadamente
- 🔑 **Secrets management** con Docker Swarm
- 🛡️ **SQL Injection protection** con Entity Framework
- 📝 **Input validation** en todos los endpoints
- 🔐 **Password hashing** con BCrypt

### Security Headers

```
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
```

## 📈 Monitoreo y Logs

### Health Checks

- **Database connectivity**
- **Redis availability**
- **API responsiveness**
- **Memory usage**

### Logging

- **Structured logging** con Serilog
- **Different log levels** por ambiente
- **Error tracking** y alertas
- **Performance monitoring**

## 🔄 CI/CD

### Comandos Útiles

```bash
# Build para producción
docker build -t sportsbetting-api .

# Deploy automático
./scripts/deploy-to-ec2.sh

# Backup de base de datos
./scripts/backup.sh

# Actualizar SSL
./scripts/setup-letsencrypt-cloudflare.sh
```

## 📚 Documentación API

### Swagger UI

- **Desarrollo**: http://localhost:5000/swagger
- **Producción**: Deshabilitado por seguridad

## 🐛 Troubleshooting

### Problemas Comunes

1. **Error de conexión a base de datos**:
```bash
# Verificar contenedor PostgreSQL
docker-compose logs postgres

# Verificar conexión
docker exec -it sportsbetting_postgres_prod psql -U sportsbetting_prod_user -d sportsbetting_prod
```

2. **JWT Token inválido**:
```bash
# Verificar configuración JWT en .env
JWT_SECRET=tu_clave_secreta_muy_larga
```

3. **SSL/HTTPS issues**:
```bash
# Verificar certificados
docker exec -it sportsbetting_nginx_prod nginx -t

# Renovar certificados
./scripts/setup-letsencrypt-cloudflare.sh
```

### Logs

```bash
# Ver logs de la API
docker-compose logs -f api

# Ver logs de Nginx
docker-compose logs -f nginx

# Ver logs del sistema
docker-compose logs -f
```

## 🤝 Contribución

1. Fork el proyecto
2. Crear feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add AmazingFeature'`)
4. Push a la branch (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

### Estándares de Código

- **C# Coding Standards**
- **Entity Framework best practices**
- **RESTful API design**
- **Comprehensive testing**
- **Security first approach**

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver el archivo `LICENSE` para más detalles.

---

## 🔗 Enlaces Útiles

- **API en Producción**: https://[tu-dominio]
- **Health Check**: https://[tu-dominio]/health
- **Documentación**: http://localhost:5000/swagger (solo desarrollo)
