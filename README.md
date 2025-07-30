# pos.api
# POS.API - Point of Sale System

## Overview
Enterprise-grade Point of Sale (POS) API built with .NET 8, Clean Architecture, and CQRS pattern.

## Architecture
- **Clean Architecture** with Domain, Application, Infrastructure, and API layers
- **CQRS with MediatR** for command and query separation
- **Entity Framework Core** for data persistence
- **JWT Authentication** integration with CoreAuthBackend
- **Docker support** for containerized deployment

## Project Structure
```
POS.API/
├── src/
│   ├── POS.API/           # Web API layer
│   ├── POS.Application/   # Business logic & MediatR handlers
│   ├── POS.Domain/        # Core entities & business rules
│   └── POS.Infrastructure/ # Data access & external services
├── tests/
│   ├── POS.Tests.Unit/
│   └── POS.Tests.Integration/
└── docker-compose.yml
```

## Features
- 🛒 **Product Management** - SKU, categories, pricing, inventory
- 💳 **Sales Processing** - Transaction handling, payment processing
- 👥 **Customer Management** - Customer profiles and purchase history
- 📊 **Inventory Tracking** - Stock levels, adjustments, reporting
- 📈 **Reporting** - Sales analytics and business insights
- 🔐 **Authentication** - JWT-based security with role permissions
- 🏪 **Multi-store Support** - Support for multiple locations

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server or SQL Server Express
- Docker (optional)

### Development Setup
```bash
# Clone repository
git clone <repository-url>
cd POS.API

# Restore packages
dotnet restore

# Update database
dotnet ef database update --project src/POS.Infrastructure --startup-project src/POS.API

# Run application
dotnet run --project src/POS.API
```

### Docker Setup
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f pos-api
```

## API Endpoints

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Sales
- `POST /api/sales` - Create new sale
- `GET /api/sales` - Get sales history
- `GET /api/sales/{id}` - Get sale details
- `POST /api/sales/{id}/payment` - Process payment

### Reports
- `GET /api/reports/sales` - Sales reports
- `GET /api/reports/inventory` - Inventory reports

## Configuration

### Environment Variables
```bash
ConnectionStrings__DefaultConnection=<database-connection>
AuthService__BaseUrl=<auth-service-url>
JwtSettings__SecretKey=<jwt-secret>
```

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=POSDB;Trusted_Connection=true;"
  },
  "AuthService": {
    "BaseUrl": "http://localhost:5001"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Authentication
This API integrates with CoreAuthBackend for authentication and authorization:
- JWT tokens for API access
- Role-based permissions (Cashier, Manager, Admin)
- User activity auditing

## Testing
```bash
# Run unit tests
dotnet test tests/POS.Tests.Unit

# Run integration tests
dotnet test tests/POS.Tests.Integration

# Run all tests
dotnet test
```

## Deployment

### Docker Production
```bash
# Build production image
docker build -t pos-api:latest -f src/POS.API/Dockerfile .

# Run with production configuration
docker run -d -p 8080:80 --name pos-api pos-api:latest
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add <MigrationName> --project src/POS.Infrastructure --startup-project src/POS.API

# Update production database
dotnet ef database update --project src/POS.Infrastructure --startup-project src/POS.API
```

## Contributing
1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Create Pull Request

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support
For support and questions:
- Create an issue in this repository
- Contact the development team

## Changelog
See [CHANGELOG.md](CHANGELOG.md) for version history and updates.
EOF
```

## 5. Create Additional Project Files

### Create LICENSE file
```bash
cat > LICENSE << 'EOF'
MIT License

Copyright (c) 2024 POS.API Project

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.