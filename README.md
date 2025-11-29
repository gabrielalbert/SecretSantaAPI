# TaskGame Backend

ASP.NET Core 8.0 Web API for the TaskGame application.

## Setup

1. Install .NET 8 SDK
2. Install PostgreSQL
3. Update connection string in `appsettings.json`
4. Run migrations:
   ```powershell
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
5. Run the application:
   ```powershell
   dotnet run
   ```

## API Documentation

Access Swagger UI at: `http://localhost:5000/swagger`

## Project Structure

- `Controllers/` - API endpoints
- `Models/` - Database entities
- `Data/` - Database context
- `Services/` - Business logic
- `DTOs/` - Data transfer objects

## Database Schema

- Users
- Tasks (TaskItems)
- TaskAssignments
- TaskSubmissions
- SubmissionFiles
