# Course Registration System (CRS) - Microservices Architecture

Hệ thống Đăng ký Môn học (Course Registration System - CRS) được xây dựng theo kiến trúc Microservices phân tán với Spring Boot, Spring Cloud Gateway, JWT Security và React/Vite Frontend.

---

## 1. Danh sách Dịch vụ & Cổng (Service Architecture)

| Dịch vụ | Cổng (Port) | Cơ sở dữ liệu | Tiền tố API Gateway | Vai trò chính |
| :--- | :--- | :--- | :--- | :--- |
| **`api-gateway`** | `8080` | Không có DB | `/` | Cổng vào duy nhất, điều hướng routing, CORS, bộ lọc AuthHeaderFilter & ApiKeyFilter. |
| **`auth-service`** | `8081` | `auth_db` | `/api/auth` | Quản lý người dùng, mã hóa BCrypt, đăng nhập & cấp phát JWT HS256. |
| **`course-service`** | `8082` | `course_db` | `/api/courses` | Quản lý môn học, phân trang & tìm kiếm, API giữ/hoàn chỗ nội bộ. |
| **`registration-service`**| `8083` | `registration_db` | `/api/registrations` | Đăng ký & hủy môn học, kết nối REST tới `course-service`. |
| **`crs-frontend`** | `5173` | N/A | N/A | Giao diện người dùng Web React / Vite / TypeScript. |

---

## 2. Chuẩn bị Cơ sở dữ liệu (Database Setup)

Trước khi khởi chạy các microservices, hãy tạo 3 cơ sở dữ liệu MySQL:

```sql
CREATE DATABASE auth_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE course_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE registration_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

> **Ghi chú Bảo mật**: Chuỗi bí mật JWT dùng chung giữa các service được khai báo trong `application.properties`:
> `jwt.secret=CRS-Microservices-Secret-Key-Nam-3-Hoc-Ky-2026-Doi-Trong-Thuc-Te`
> *(Lưu ý: TODO đổi jwt.secret trước khi deploy môi trường thực tế).*

---

## 3. Tài khoản Mẫu (Seeded Users)

Tài khoản mẫu tự động sinh khi `auth-service` khởi chạy lần đầu:

- **Quản trị viên (ADMIN)**: `admin` / `admin123`
- **Sinh viên (STUDENT)**: `student1` / `student123`

---

## 4. Hướng dẫn Khởi chạy Hệ thống

### 4.1. Khởi chạy Backend Microservices (Java Spring Boot)

Chạy các lệnh sau ở các cửa sổ Terminal riêng biệt:

```bash
# 1. Khởi chạy auth-service (Port 8081)
cd auth-service
./mvnw spring-boot:run

# 2. Khởi chạy course-service (Port 8082)
cd course-service
./mvnw spring-boot:run

# 3. Khởi chạy registration-service (Port 8083)
cd registration-service
./mvnw spring-boot:run

# 4. Khởi chạy api-gateway (Port 8080)
cd api-gateway
./mvnw spring-boot:run
```

### 4.2. Khởi chạy Frontend (React + Vite)

```bash
cd crs-frontend
npm install
npm run dev
```
Giao diện sẽ lắng nghe tại: `http://localhost:5173`.

---

## 5. Kiểm thử Postman (Postman Collection)

File Postman collection có sẵn tại [`CRS_Buoi_04.postman_collection.json`](./CRS_Buoi_04.postman_collection.json) hỗ trợ test toàn bộ API qua Cổng Gateway `http://localhost:8080`:

1. **POST** `/api/auth/login` (Admin/Student) -> Lấy JWT Token.
2. **GET** `/api/courses` -> Xem danh sách môn công khai (No token required).
3. **POST** `/api/courses` (Không token) -> **401 Unauthorized** (Chặn tại Gateway).
4. **POST** `/api/courses` (Token Student) -> **403 Forbidden** (Chặn tại `course-service`).
5. **POST** `/api/courses` (Token Admin) -> **201 Created** (Tạo môn học).
6. **POST** `/api/registrations` (Token Student) -> **201 Created** (Đăng ký học phần).
7. **GET** `/api/public/courses` (Header `X-API-KEY: crs-partner-key-2026`) -> **200 OK** (API Đối tác).