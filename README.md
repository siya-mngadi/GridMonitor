# Grid Monitor

GridMonitor is a solution for monitoring, managing, and alerting on grid resource usage. It is designed for extensibility, reliability, and ease of integration with external systems.

## Features
- Real-time grid status monitoring
- Load reduction alerting (configurable thresholds and channels)
- User and subscription management
- Municipality, suburb, and schedule management
- API key management
- Extensible alerting (webhooks, WhatsApp, etc.)

## Architecture
The solution is organized into several projects:

- **Domain**: Core business entities, value objects, and interfaces
- **Application**: Application services, alert engine, and background workers
- **Infrastructure**: Data access, external integrations, and notification services
- **Api**: RESTful API for external access
- **Worker**: Background services for grid and schedule synchronization
- **Tests.Unit**: Unit tests for core logic


## Authentication (Keycloak Integration)
GridMonitor uses Keycloak for authentication and authorization. All API endpoints are protected and require a valid JWT access token issued by your Keycloak server.

### Keycloak Setup
- Configure a Keycloak realm, client, and user roles for GridMonitor.
- Set the Keycloak authority (issuer URL), client ID, and audience in your `appsettings.json` or environment variables.
- Example configuration:
  ```json   
  "Keycloak": {
    "realm": "gridmonitor-realm",
    "auth-server-url": "http://localhost:8080/",
    "ssl-required": "none",
    "resource": "grid-client",
    "verify-token-audience": false,
  }
  ```
- The API validates incoming JWT tokens against the configured Keycloak server.

### User Flow
1. Users authenticate via Keycloak and obtain a JWT access token.
2. The token is sent in the `Authorization: Bearer <token>` header for API requests.
3. The API validates the token and authorizes access based on roles and claims.

## Usage
1. Clone the repository
2. Configure your database, Keycloak, and external services (see Configuration)
3. Build and run the solution using Visual Studio 2026 or `dotnet` CLI

## Configuration
Configuration is managed via appsettings.json and environment variables. Key settings include:
- Database connection strings
- API keys and authentication (see Keycloak section above)

## Build & Test
To build the solution:
```sh
dotnet build
```

To run tests:
```sh
dotnet test
```

## TODOs
- [ ] Implement load reduction alerting (critical before public release)
- [ ] Review and address all TODO comments left in the codebase
- [ ] Add comprehensive unit and integration tests
- [ ] Write user and developer documentation
- [ ] Prepare for public release (security, code quality, documentation)

## Load Reduction Alerting
A key feature pending implementation is the load reduction alerting system. This should:
- Detect when load reduction will take place (data source?)
- Notify operators or trigger automated responses
- Be configurable and extensible for different alerting channels (email, SMS, webhooks, WhatsApp, etc.)

## Contributing
Contributions are welcome! Please review open issues and TODOs before submitting pull requests.

## License

MIT License