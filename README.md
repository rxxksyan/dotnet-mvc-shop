<div align="center">

# SmartphoneShop

ASP.NET Core MVC интернет-магазин смартфонов.

[![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_MVC-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF_Core_8-03530F?style=flat-square&logo=entity-framework)](https://learn.microsoft.com/ef/)
[![MySQL](https://img.shields.io/badge/MySQL_8-4479A1?style=flat-square&logo=mysql&logoColor=fff)](https://www.mysql.com/)
[![Identity](https://img.shields.io/badge/Identity-512BD4?style=flat-square&logo=dotnet)](https://learn.microsoft.com/aspnet/identity/)
[![Serilog](https://img.shields.io/badge/Serilog-FF7019?style=flat-square&logo=serilog)](https://serilog.net/)
[![SkiaSharp](https://img.shields.io/badge/SkiaSharp-29B6F6?style=flat-square&logo=skia)](https://github.com/mono/SkiaSharp)
[![OpenXml](https://img.shields.io/badge/OpenXml-217346?style=flat-square&logo=microsoft-excel)](https://github.com/OfficeDev/Open-XML-SDK)
[![Bootstrap](https://img.shields.io/badge/Bootstrap_5-7952B3?style=flat-square&logo=bootstrap)](https://getbootstrap.com/)

</div>

---

## Функциональность

| Категория | Возможности |
|---|---|
| **Каталог** | Пагинация, фильтрация по бренду/цене/ОЗУ, сортировка, поиск |
| **Корзина** | Работает для гостей (сессия) и авторизованных (БД). При входе корзина гостя сливается с сохранённой |
| **Заказы** | Оформление с выбором адреса/доставки, история заказов в личном кабинете |
| **Избранное** | Добавление/удаление, просмотр списка избранного |
| **Сравнение** | Сравнение характеристик нескольких товаров в одной таблице |
| **Отзывы** | Рейтинг + текст, привязка к пользователю и товару |
| **Ремонт** | Подача заявки на ремонт, отслеживание статуса, комментарии мастера |
| **Закупки** | Заявки на оптовую закупку для юр. лиц |
| **Экспертные мнения** | Сравнение двух товаров экспертом (развёрнутый обзор) |
| **Админ-панель** | CRUD товаров, управление заказами/заявками/ремонтом/пользователями, назначение ролей |

## Стек технологий

**Backend:** .NET 9 · ASP.NET Core MVC · C# · Entity Framework Core 8 · Pomelo MySQL Provider  
**Auth:** ASP.NET Core Identity (роли: Admin, Expert, ProductAdmin, RepairSpecialist, User)  
**Logging:** Serilog (консоль + ежедневные rolling-файлы)  
**Reporting:** SkiaSharp (графики) · DocumentFormat.OpenXml (Excel-отчёты)  
**Pagination:** X.PagedList  
**Frontend:** Bootstrap 5 · jQuery · jQuery Validation · Razor Views

## Архитектура

Трёхуровневая: `Core → Infrastructure → Web` (зависимость только вниз, Infrastructure и Web не зависят друг от друга).

```
SmartphoneShop.sln
│
├── SmartphoneShop.Core               # Доменный слой (net9.0)
│   ├── Entities/                      #  13 сущностей: Smartphone, Order, Cart, Review ...
│   ├── Enums/                         #  OrderStatus, RepairStatus, UserRole
│   └── Interfaces/                    #  8 контрактов репозиториев
│
├── SmartphoneShop.Infrastructure      # Слой данных (зависит от Core)
│   ├── Data/                          #  AppDbContext + SeedData
│   ├── Migrations/                    #  4 миграции EF Core
│   └── Repositories/                  #  GenericRepository<T> + 8 конкретных реализаций
│
└── SmartphoneShop.Web                 # Слой представления (зависит от Core + Infrastructure)
    ├── Controllers/                   #  17 контроллеров
    ├── Models/                        #  ViewModel'и: CartItemModel, SmartphoneFormViewModel ...
    ├── Views/                         #  22 директории с Razor-шаблонами
    ├── Services/                      #  ChartDrawer (SkiaSharp), ReportGenerator (OpenXml)
    ├── Helpers/                       #  FormatHelper
    ├── Extensions/                    #  SessionExtensions (работа с корзиной в сессии)
    └── Program.cs                     #  Точка входа, DI-контейнер, middleware
```

## Модель данных

Основные сущности (все в `SmartphoneShop.Core/Entities/`):

- **Smartphone** — товар (модель, бренд, цена, характеристики, фото, флаги)
- **Cart / CartItem** — корзина пользователя + позиции
- **Order / OrderItem** — заказ + состав заказа
- **Review** — отзыв с рейтингом
- **ComparisonList / ComparisonItem** — список сравнения
- **Favorite** — избранное
- **PurchaseOrder** — заявка на оптовую закупку
- **RepairRequest** — заявка на ремонт
- **ExpertOpinion** — экспертное мнение (сравнение двух моделей)
- **AppUser** — пользователь (расширенный IdentityUser)

Связи: Smartphone → OrderItem/Review/CartItem/ComparisonItem/Favorite (1:N), AppUser → Order/RepairRequest/Review/Cart/ComparisonList/PurchaseOrder (1:N).

## Настройка и запуск

### Требования

- .NET 9 SDK
- MySQL 8+ (XAMPP / Docker / сервер)

### Установка

```bash
git clone https://github.com/rxxksyan/dotnet-mvc-shop.git
cd dotnet-mvc-shop
dotnet run -p SmartphoneShop.Web
```

База данных создаётся автоматически при первом запуске (миграции применяются в `SeedData.cs`).
Строка подключения — `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=smartphone_shop;User=root;Password=;"
  }
}
```

### Учётные записи по умолчанию

| Роль | Email | Пароль |
|---|---|---|
| **Администратор** | `admin@smartshop.com` | `Admin123!` |

Остальные роли (Expert, ProductAdmin, RepairSpecialist) назначаются через админ-панель после входа.
