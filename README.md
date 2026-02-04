# Jedox Translator

A minimal translation management tool built with ASP.NET Core and PHP, featuring CRUD operations for text resources across multiple languages.

## Architecture

### Backend (C# / ASP.NET Core)
- **JedoxTranslator.API**: Minimal API project with RESTful endpoints
- **JedoxTranslator.Core**: Domain models, services, repositories, and EF Core data access
- **JedoxTranslator.Tests**: xUnit tests for core functionality

### Frontend (PHP)
Modern PHP web application with OpenID Connect authentication and responsive UI.

### Database
PostgreSQL for data persistence with EF Core migrations.

### Authentication
Microsoft Entra ID (Azure AD) with OpenID Connect authentication flow.

## Features

### Backend API
- List all SIDs
- Get translations by SID (all languages)
- Create new SID with default text and optional translations
- Update translation for specific SID and language
- Delete SID and all translations
- Get all translations for a specific language

### Frontend
- Login page with OpenID Connect authentication
- View page with language selector and translation list
- Edit page for managing translations per language
- Create, update, and delete translations
- Multi-language support with 25+ languages

## Quick Start

### Prerequisites
- Docker and Docker Compose
- Azure AD tenant with app registrations configured

### Configuration Files

**Important:** Configuration files containing sensitive credentials are required to run the application.

Both configuration files are **git-ignored** and will not be found in the repository. The contents and setup instructions for these files will be provided separately.

1. **Backend Configuration**: `Backend/JedoxTranslator.API/appsettings.json`
   - Contains Azure AD configuration, database connection strings, and API settings
   - This file must be created manually with the content provided separately
   
2. **Frontend Configuration**: `Frontend/php/.env`
   - Contains Azure AD credentials and API endpoint URLs
   - This file must be created manually with the content provided separately

**Note:** These files are intentionally excluded from version control for security purposes.

### Running the Application

1. Clone the repository
2. Navigate to the project root directory
3. Create the configuration files mentioned above
4. Run the following command:

```bash
docker-compose up --build
```

### Access the Application

Once running, the application will be available at:

- **PHP Frontend**: http://localhost:8080
- **API**: http://localhost:5000
- **Scalar API Documentation**: http://localhost:5000/scalar/v1 (Development mode only)
- **PostgreSQL**: localhost:5432

### Authentication

The application uses Microsoft Entra ID for authentication. When prompted to log in, use your Azure AD credentials.

## Security

The application implements multiple security layers:

- **Session Security**: HttpOnly cookies prevent JavaScript access to session tokens
- **XSS Prevention**: All user input is escaped with `htmlspecialchars()` to prevent cross-site scripting attacks
- **CSRF Protection**: OAuth state parameter validates redirects and prevents cross-site request forgery
- **Token Expiration**: Sessions automatically expire after 1 hour of inactivity
- **Input Validation**: SIDs are validated before database operations to prevent malicious input
- **PKCE (Proof Key for Code Exchange)**: OAuth extension prevents authorization code interception attacks
- **Bearer Token Authentication**: All API requests require valid JWT tokens from Azure AD

## API Endpoints

All endpoints require authentication via Bearer token.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/translations/sids` | Get all SIDs |
| GET | `/api/v1/translations/{sid}` | Get all translations for a SID |
| GET | `/api/v1/translations?langId={langId}` | Get all translations for a language |
| POST | `/api/v1/translations` | Create new SID with translations |
| PUT | `/api/v1/translations/{sid}/source` | Update source English text |
| PUT | `/api/v1/translations/{sid}/{langId}` | Update translation |
| DELETE | `/api/v1/translations/{sid}/{langId}` | Delete specific translation |
| DELETE | `/api/v1/translations/{sid}` | Delete SID and all translations |

## Response Format

All API responses follow the standardized format:

```json
{
  "data": {},
  "isSuccess": true,
  "errors": []
}
```

## License

This project is provided as-is for evaluation purposes.
