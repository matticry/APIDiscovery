# API Discovery

## Descripción
API Discovery es un servicio web basado en .NET Core con Swagger, que proporciona un sistema de gestión de usuarios y roles. La API permite realizar operaciones CRUD sobre entidades como Usuario, Empresa y Rol, garantizando un manejo adecuado de excepciones y seguridad.

## Tecnologías Utilizadas
- **.NET Core 7+**
- **Entity Framework Core** (EF Core) con SQL Server
- **Swagger** (Documentación de API)
- **Docker** (Para despliegue en contenedores)
- **JWT (JSON Web Token)** para autenticación
- **AutoMapper** (Mapeo de DTOs)

## Arquitectura del Proyecto
```
APIDiscovery/
│── Controllers/        # Controladores de la API
│── Models/             # Modelos de datos
│── Interfaces/         # Interfaces para servicios
│── Services/           # Implementación de servicios
│── Core/               # Configuración de la base de datos
│── Exceptions/         # Manejo centralizado de errores
│── Startup.cs          # Configuración de la aplicación
│── appsettings.json    # Configuración de la aplicación
```

## Instalación y Configuración

### 1️⃣ Clonar el Repositorio
```bash
git clone https://github.com/tu-usuario/API-Discovery.git
cd API-Discovery
```

### 2️⃣ Configurar la Base de Datos
Editar `appsettings.json` y configurar la conexión con SQL Server:
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=APIDiscoveryDB;User Id=sa;Password=TuContraseña;"
}
```

### 3️⃣ Ejecutar Migraciones
```bash
dotnet ef database update
```

### 4️⃣ Ejecutar la API
```bash
dotnet run
```
La API estará disponible en `http://localhost:5000/swagger`.

## Endpoints Principales
### 🟢 Usuarios (`/api/usuarios`)
| Método | Endpoint             | Descripción                        |
|--------|----------------------|------------------------------------|
| GET    | `/api/usuarios`      | Obtiene todos los usuarios        |
| GET    | `/api/usuarios/{id}` | Obtiene un usuario por ID         |
| POST   | `/api/usuarios`      | Crea un nuevo usuario             |
| PUT    | `/api/usuarios/{id}` | Actualiza un usuario              |
| DELETE | `/api/usuarios/{id}` | Elimina un usuario                |

### 🔵 Autenticación (`/api/auth`)
| Método | Endpoint        | Descripción                         |
|--------|----------------|-------------------------------------|
| POST   | `/api/auth/login` | Autentica y devuelve un token JWT  |

## Excepciones y Manejo de Errores
El proyecto implementa excepciones personalizadas:
- `NotFoundException` → 404 Not Found
- `ValidationException` → 400 Bad Request
- `UnauthorizedException` → 401 Unauthorized

Ejemplo de respuesta de error:
```json
{
    "status": 404,
    "message": "Usuario no encontrado."
}
```

## Despliegue en Docker
### 🛠 Crear la Imagen de Docker
```bash
docker build -t api-discovery .
```

### 🚀 Ejecutar el Contenedor
```bash
docker run -d -p 5000:5000 --name api-discovery api-discovery
```

## Autor
📌 **matticry**

## Licencia
Este proyecto está bajo la licencia MIT.

