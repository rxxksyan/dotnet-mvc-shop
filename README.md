<div align="center">

# SmartphoneShop

ASP.NET Core MVC интернет-магазин смартфонов с системой ремонта.

![.NET 9](https://img.shields.io/badge/.NET%209-512BD4?logo=dotnet&logoColor=fff)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core%20MVC-0078D4?logo=dotnet&logoColor=fff)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?logo=mysql&logoColor=fff)
![EF Core 8](https://img.shields.io/badge/EF%20Core%208-5C2D91?logoColor=fff)
![Identity](https://img.shields.io/badge/Identity-512BD4?logo=dotnet&logoColor=fff)
![Serilog](https://img.shields.io/badge/Serilog-FF7019?logoColor=fff)
![jsPDF](https://img.shields.io/badge/jsPDF-EC432A?logo=javascript&logoColor=fff)

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
| **Ремонт** | Подача заявки на ремонт, отслеживание статуса, комментарии мастера, запчасти, гарантия, PDF-документы (квитанция, гарантийный талон) |
| **Закупки** | Заявки на оптовую закупку для юр. лиц |
| **Экспертные мнения** | Сравнение двух товаров экспертом (развёрнутый обзор) |
| **Админ-панель** | CRUD товаров, управление заказами/заявками/ремонтом/пользователями/запчастями, назначение ролей |

## Стек технологий

```text
Backend      ── .NET 9 · ASP.NET Core MVC · C# · EF Core 8 · Pomelo MySQL
Auth/Logging ── Identity · Serilog
Reporting    ── jsPDF + html2canvas (PDF-документы)
Frontend     ── Vanilla CSS · Vanilla JS · Razor Views · Google Fonts (Montserrat)
```

## Архитектура

Трёхуровневая: `Core → Infrastructure → Web` (зависимость только вниз, Infrastructure и Web не зависят друг от друга).

```
SmartphoneShop.sln
│
├── SmartphoneShop.Core               # Доменный слой (net9.0)
│   ├── Entities/                      #  15 сущностей: Smartphone, Order, Cart, RepairRequest, SparePart ...
│   ├── Enums/                         #  OrderStatus, RepairStatus, UserRole
│   └── Interfaces/                    #  9 контрактов репозиториев
│
├── SmartphoneShop.Infrastructure      # Слой данных (зависит от Core)
│   ├── Data/                          #  AppDbContext + SeedData
│   ├── Migrations/                    #  11 миграций EF Core
│   └── Repositories/                  #  GenericRepository<T> + 9 конкретных реализаций
│
└── SmartphoneShop.Web                 # Слой представления (зависит от Core + Infrastructure)
    ├── Controllers/                   #  18 контроллеров
    ├── Models/                        #  ViewModel'и: CartItemModel, SmartphoneFormViewModel ...
    ├── Views/                         #  19 директорий с Razor-шаблонами
    ├── Services/                      #  RussianIdentityErrorDescriber
    ├── Helpers/                       #  FormatHelper
    ├── Extensions/                    #  SessionExtensions (работа с корзиной в сессии)
    └── wwwroot/js/documents.js        #  Генерация PDF-документов (квитанция, гарантийный талон)
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
- **RepairRequest** — заявка на ремонт (статус, диагностика, запчасти, гарантия, SN/IMEI)
- **SparePart / RepairSparePart** — запчасти и связь с заявкой на ремонт
- **ExpertOpinion** — экспертное мнение (сравнение двух моделей)
- **AppUser** — пользователь (расширенный IdentityUser)

Связи: Smartphone → OrderItem/Review/CartItem/ComparisonItem/Favorite (1:N), AppUser → Order/RepairRequest/Review/Cart/ComparisonList/PurchaseOrder (1:N), RepairRequest → RepairSparePart (1:N).

## Ремонт

Полный цикл обработки заявок на ремонт:

1. **Клиент** — подаёт заявку (описание + фото), выбирает тип (по гарантии / платный)
2. **Мастер** — назначает статус, проводит диагностику, добавляет запчасти, устанавливает цену
3. **Клиент** — видит комментарии мастера, подтверждает или отказывается от ремонта
4. **PDF-документы** — квитанция (описание + стоимость) и гарантийный талон (условия гарантии)

Особенности:
- Гарантийный ремонт: стоимость запчастей и работ = 0 BYN
- При отказе клиента: отдельный статус "Заберите смартфон из сервиса"
- Серийный номер (SN/IMEI) отображается в заявке

## Настройка и запуск

### Требования

- .NET 9 SDK
- MySQL 8+ (XAMPP / Docker / сервер)

### Быстрый старт

```bash
# 1. Клонировать
git clone https://github.com/rxxksyan/dotnet-mvc-shop.git
cd dotnet-mvc-shop

# 2. (Опционально) Поправить строку подключения к MySQL
#    отредактировать SmartphoneShop.Web/appsettings.json

# 3. Собрать и запустить
dotnet run --project SmartphoneShop.Web
```

База данных и таблицы создаются автоматически при первом запуске (миграции + SeedData).
Строка подключения — `appsettings.json` (отредактируй под свой MySQL):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=smartphone_shop;User=root;Password=;"
  }
}
```

### Учётные записи по умолчанию

Все пароли: `bmw850850`

| Роль | Email | Описание |
|---|---|---|
| **Admin** | `admin@smartshop.com` | Полный доступ к админ-панели |
| **User** | `testuser@test.com` | Обычный покупатель |
| **ProductAdmin** | `prodadmin2@test.com` | Управление каталогом товаров |
| **RepairSpecialist** | `repair@smartshop.com` | Управление заявками на ремонт |

> Сервер запускается на `http://localhost:5023` (см. `Properties/launchSettings.json`).
