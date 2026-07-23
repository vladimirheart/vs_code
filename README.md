# IikoFront.OrderQrPlugin

Плагин для `iikoFront 9.4.6`, который подписывается на `BillChequePrinting`, собирает payload по заказу и добавляет QR-код в нижнюю часть гостевого счёта.

## Поддерживаемые версии

- `iikoFront`: `9.4.6`
- `Front API`: `Resto.Front.Api.V9Preview7`
- Версия пакета: `9.4.6046-alpha`

Связка версий выбрана под установленный `iikoFront 9.4.6`: для линейки `iikoRMS 9.4` официальный API-контракт находится в ветке `V9Preview7`.

## Что делает плагин

- Загружает актуальный заказ через `PluginContext.Operations.GetOrderById(orderId)`.
- Включает в payload только неудалённые корневые позиции.
- Поддерживает `IOrderProductItem`, модификаторы и базовую обработку `IOrderCompoundItem`.
- Печатает отсутствующие значения как ASCII-символ `-`.
- Не блокирует печать гостевого счёта при любой ошибке.
- Пишет стандартный лог и JSONL-аудит попыток.

## Структура решения

- `src/IikoFront.OrderQrPlugin` — основная библиотека плагина
- `tests/IikoFront.OrderQrPlugin.Tests` — unit-тесты логики payload/escaping/КБЖУ

## Сборка

Нужна Visual Studio 2022 или совместимый `MSBuild` с NuGet restore.

Пример:

```powershell
MSBuild.exe IikoFront.OrderQrPlugin.sln /restore /p:Configuration=Release
```

Пакет `Resto.Front.Api.V9Preview7` подтягивается из NuGet. Его `targets` автоматически отключает `Copy Local`, поэтому `Resto.Front.Api.V9Preview7.dll` не должен попадать в дистрибутив плагина.

## Установка в iikoFront

1. Соберите `Release`.
2. Скопируйте `IikoFront.OrderQrPlugin.dll` в каталог плагинов iikoFront.
3. Не копируйте `Resto.Front.Api.V9Preview7.dll` вместе с плагином.
4. Разместите `order-qr-settings.json` в каталоге, который возвращает `PluginContext.Integration.GetConfigsDirectoryPath()`.
5. Перезапустите iikoFront.

## Включение и отключение

- Для включения: `"enabled": true`
- Для отключения: `"enabled": false`

Дополнительно можно отключить только печать на гостевом счёте через `"printOnGuestBill": false`.

## Конфигурация

Файл конфигурации: `order-qr-settings.json`

Пример:

```json
{
  "enabled": true,
  "printOnGuestBill": true,
  "payloadVersion": "IIKOQR1",
  "qrSize": "Extralarge",
  "qrCorrection": "Low",
  "treatAllZeroFoodValueAsMissing": true,
  "maxPayloadUtf8BytesWarning": 2500,
  "writeFullPayloadToStandardLog": false,
  "writeJsonlAuditLog": true,
  "includeOrderGuidInPayload": false,
  "includeModifiers": true,
  "includeAllergens": true,
  "includePrintTime": true
}
```

Если файла нет, плагин создаёт его со значениями по умолчанию. Если JSON повреждён, плагин пишет ошибку в стандартный лог и использует безопасные дефолты.

## Где искать логи

- Стандартный лог: через лог плагинов iikoFront (`PluginContext.Log.Info/Warn/Error`)
- JSONL-аудит: каталог `PluginContext.Integration.GetDataStorageDirectoryPath()`
- Формат имени JSONL: `qr-print-attempts-YYYY-MM-DD.jsonl`

## Пример записи стандартного лога

```text
event=QR_PRINT_ATTEMPT status=EXTENSION_RETURNED attemptId=20260723-1542-001 orderId=9f2b... orderNumber=1542 items=2 modifierCount=1 chars=184 utf8Bytes=226 elapsedMs=24
```

## Пример payload

```text
IIKOQR1
O:1542;D:2026-07-23T18:42:11;R:1;C:2
1;N:Филадельфия;Q:1;S:Стандарт;K:420;B:18;J:21;U:43;A:Молоко,Рыба,Соя
M:Доп.сыр[Q:1;K:70;B:4;J:6;U:1]
2;N:Мисо суп;Q:2;S:-;K:-;B:-;J:-;U:-;A:-
M:-
```

## Значение прочерка

Прочерк `-` означает:

- поле отсутствует;
- значение недоступно;
- произошла ошибка чтения поля;
- данные не различимы по текущему API (например, пустой список аллергенов).

## Значение статусов

- `NULL` — `FoodValue == null`
- `ALL_ZERO` — все значения КБЖУ одновременно равны нулю
- `EMPTY_OR_NONE` — пустой список аллергенов, который в текущем API нельзя отличить от незаполненных данных

## Ограничение события печати

Событие `BillChequePrinting` подтверждает только то, что плагин вернул дополнение к чеку. Оно не гарантирует:

- физический выход бумаги;
- успешную генерацию QR устройством;
- факт сканирования QR пользователем.

Поэтому в логах фиксируются `payload`, `characters`, `utf8Bytes`, `itemCount`, `modifierCount` и `attemptId`.

## Безопасное удаление

1. Отключите плагин через `"enabled": false`.
2. Перезапустите iikoFront.
3. Удалите `IikoFront.OrderQrPlugin.dll` из каталога плагинов.
4. При необходимости удалите `order-qr-settings.json` и JSONL-файлы аудита.

## Что ставить вместе с DLL

Минимально:

- `IikoFront.OrderQrPlugin.dll`
- `order-qr-settings.json` — как пример конфигурации

Не нужно ставить вместе с DLL:

- `Resto.Front.Api.V9Preview7.dll`
