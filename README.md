# SmartphoneShop

ASP.NET Core MVC интернет-магазин смартфонов.

**Стек:** .NET 9 · ASP.NET Core MVC · EF Core 8 + MySQL (Pomelo) · Identity · Serilog · SkiaSharp · OpenXml

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
