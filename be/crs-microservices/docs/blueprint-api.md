# API Blueprint - Course Registration System

---

## 1. auth-service (Port: 8081, Gateway Prefix: `/api/auth`)

| Method | Endpoint | Description | Requirement |
| :--- | :--- | :--- | :--- |
| **POST** | `/auth/login` | Authenticate user and return JWT | Public |
| **POST** | `/auth/register` | Register a new user account (optional) | Public |

---

## 2. course-service (Port: 8082, Gateway Prefix: `/api/courses`)

### External API (Exposed to Client / Gateway)
| Method | Endpoint | Description | Requirement |
| :--- | :--- | :--- | :--- |
| **GET** | `/courses` | Retrieve course list with search and pagination | Public |
| **GET** | `/courses/{id}` | Retrieve course details by ID | Public |
| **POST** | `/courses` | Create a new course | ADMIN |
| **PUT** | `/courses/{id}` | Update existing course information | ADMIN |
| **DELETE** | `/courses/{id}` | Remove a course | ADMIN |

### Internal API (Invoked by `registration-service`, NOT exposed to Gateway)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| **PATCH** | `/internal/courses/{id}/reserve-seat` | Check available seats and decrement `availableSeats` (transactional) |
| **PATCH** | `/internal/courses/{id}/release-seat` | Increment `availableSeats` by 1 upon registration cancellation |

---

## 3. registration-service (Port: 8083, Gateway Prefix: `/api/registrations`)

| Method | Endpoint | Description | Requirement |
| :--- | :--- | :--- | :--- |
| **POST** | `/registrations` | Register for a course (invokes `course-service` internally) | STUDENT |
| **GET** | `/registrations/my` | Retrieve my registered course list | STUDENT |
| **DELETE** | `/registrations/{id}` | Cancel registration (invokes `release-seat` internally) | STUDENT / ADMIN |