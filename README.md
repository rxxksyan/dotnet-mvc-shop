# SmartphoneShop

ASP.NET Core MVC интернет-магазин смартфонов.

![.NET 9](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?logo=mysql&logoColor=fff)
![EF Core](https://img.shields.io/badge/EF%20Core-8-512BD4?logo=dotnet)
![Serilog](https://img.shields.io/badge/Serilog-FFC107?logo=serilog)

---

### Возможности

- Каталог с пагинацией и фильтрацией (бренд, цена, ОЗУ)
- Корзина (гость / авторизованный, слияние при логине)
- Оформление и история заказов
- Избранное, сравнение товаров, отзывы
- Заявки на ремонт
- Админ-панель: CRUD товаров, управление заказами и заявками
- Отчёты: Excel (OpenXml) + графики (SkiaSharp)

### Архитектура

```
SmartphoneShop.sln
├── Core            Сущности, интерфейсы, enum'ы
├── Infrastructure  DbContext, миграции, репозитории
└── Web             Контроллеры, представления, статика
```

Трёхуровневая: `Core → Infrastructure → Web` (зависимость только вниз).

### Быстрый старт

```
dotnet run -p SmartphoneShop.Web
```

**Требования:** .NET 9 SDK, MySQL 8+ (XAMPP).

БД создаётся автоматически при первом запуске через `MigrateAsync()`.
Строка подключения — `appsettings.json` (по умолчанию root:""@localhost:3306).

Учётная запись администратора создаётся сидом:
- **Email:** `admin@shop.com`
- **Пароль:** `Admin123!`
