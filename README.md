# Calendar Widget — планер для Windows

Нативный виджет-планер на **WinUI 3** с разделами: время/дата, календарь, задачи, напоминания, настройки.

## Возможности

- Перетаскивание и изменение размера мышью
- Разделы через боковую навигацию (не всё на одном экране)
- Прогресс выполнения задачи (0–100%)
- Глобальные хоткеи: **Win+Shift+Q** (быстрое добавление), **Win+Shift+P** (показать/скрыть)
- Мини-окно быстрого добавления поверх всех окон
- Иконка в системном трее
- Локальная SQLite-база с заделом под облачную синхронизацию
- Toast-уведомления для напоминаний (при запущенном приложении)

## Требования

- Windows 10 1809+ (рекомендуется Windows 11)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 с workload **.NET Desktop Development** и **Windows App SDK**

## Сборка

```powershell
cd e:\prikol\Calendar
dotnet restore
dotnet build Calendar\Calendar.csproj -c Release
```

Запуск:

```powershell
dotnet run --project Calendar\Calendar.csproj
```

Или откройте `Calendar.sln` в Visual Studio и нажмите F5.

## Данные

База: `%LocalAppData%\CalendarWidget\calendar.db`

## Структура решения

- `Calendar` — WinUI 3 UI
- `Calendar.Core` — модели и интерфейсы
- `Calendar.Data` — SQLite
- `Calendar.Platform` — хоткеи, автозапуск, планировщик напоминаний
