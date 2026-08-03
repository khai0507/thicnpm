# Service Boundary Design

## 1. Service List

| Service | Port | Database | Primary Responsibility |
| :--- | :--- | :--- | :--- |
| **api-gateway** | 8080 | (No DB) | Single entry point, routing, preliminary authentication, CORS |
| **auth-service** | 8081 | `auth_db` | User and Student management, login, JWT generation and validation |
| **course-service** | 8082 | `course_db` | Course management, search, pagination, available seats management |
| **registration-service** | 8083 | `registration_db` | Registration management, invokes `course-service` for enrollment |

## 2. Data Ownership Principles

* **Database per Service:** Each service has its own dedicated database. No service is allowed to directly access another service's database.
* **API Communication:** Any request to retrieve or modify data owned by another service must go through REST APIs.
* **Concrete Example:** `registration-service` does NOT contain a Course table; it only stores `courseId` (numeric ID, with no foreign key constraint in the DB).

## 3. Gateway Routing Table (Planned)

| Route | Forward To | Notes |
| :--- | :--- | :--- |
| `/api/auth/**` | `http://localhost:8081` | Public for login, remaining endpoints require JWT |
| `/api/courses/**` | `http://localhost:8082` | Public GET requests, POST/PUT/DELETE require ADMIN role |
| `/api/registrations/**` | `http://localhost:8083` | Requires JWT (STUDENT/ADMIN roles) |
| `/api/public/courses` | `http://localhost:8082` | Authenticated via API Key for external partners |