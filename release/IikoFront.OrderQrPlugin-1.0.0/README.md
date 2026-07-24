# IikoFront.OrderQrPlugin

Плагин для `iikoFront 9.4.6`, который подписывается на `BillChequePrinting`, собирает payload по заказу и добавляет QR-код в нижнюю часть гостевого счёта. Дополнительно плагин может сам инициировать печать счета в момент начала приготовления.

## Поддерживаемые версии

- `iikoFront`: `9.4.6`
- `Front API`: `Resto.Front.Api.V9Preview7`
- Версия пакета: `9.4.6046-alpha`

Связка версий выбрана под установленный `iikoFront 9.4.6`: для линейки `iikoRMS 9.4` официальный API-контракт находится в ветке `V9Preview7`.

## Что делает плагин

- Загружает актуальный заказ через `PluginContext.Operations.GetOrderById(orderId)`.
- Подписывается на `GetKitchenOrderChanged(false)` и реагирует на `CookingStarted`.
- Включает в payload только неудалённые корневые позиции.
- Поддерживает `IOrderProductItem`, модификаторы и базовую обработку `IOrderCompoundItem`.
- Печатает отсутствующие значения как ASCII-символ `-`.
- Не блокирует печать гостевого счёта при любой ошибке.
- Пишет стандартный лог и JSONL-аудит попыток.
- Для доставки вызывает `PrintDeliveryBill(...)`, для заказа от стола вызывает `PrintBillCheque(...)`.

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
2. Подготовьте папку плагина с файлами:
   `IikoFront.OrderQrPlugin.dll`, `manifest.xml`, `README.md`.
3. Скопируйте папку плагина в каталог `Plugins` iikoFront.
4. Не копируйте `Resto.Front.Api.V9Preview7.dll` вместе с плагином.
5. Разместите `order-qr-settings.json` в каталоге, который возвращает `PluginContext.Integration.GetConfigsDirectoryPath()`.
6. Перезапустите iikoFront.

## LicenseModuleId

В репозитории и release-пакете сейчас установлен:

```text
LicenseModuleId = 21016318
```

Он указан в двух местах:

- [Plugin.cs](/C:/Users/SinicinVV/git_h/vs_code/src/IikoFront.OrderQrPlugin/Plugin.cs:1) — атрибут `PluginLicenseModuleId(...)`
- [manifest.xml](/C:/Users/SinicinVV/git_h/vs_code/src/IikoFront.OrderQrPlugin/manifest.xml:1) — тег `<LicenseModuleId>`

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
  "qrPayloadEncodingMode": "Utf8ViaPrinterCodePage",
  "qrPayloadPrinterCodePage": 866,
  "treatAllZeroFoodValueAsMissing": true,
  "maxPayloadUtf8BytesWarning": 2500,
  "writeFullPayloadToStandardLog": false,
  "writeJsonlAuditLog": true,
  "includeOrderGuidInPayload": false,
  "includeModifiers": true,
  "includeAllergens": true,
  "includePrintTime": true,
  "printOnCookingStart": true,
  "printDeliveryBillOnCookingStart": true,
  "printTableBillOnCookingStart": true,
  "cookingStartInitialDelayMs": 5000,
  "cookingStartRetryDelayMs": 2000,
  "cookingStartMaxAttempts": 30
}
```

- `"printOnCookingStart": true` — включить автопечать на старте готовки.
- `"printDeliveryBillOnCookingStart": true` — печатать доставочный документ при `CookingStarted`.
- `"printTableBillOnCookingStart": true` — печатать гостевой счет заказа от стола при `CookingStarted`.
- `"qrPayloadEncodingMode": "Utf8ViaPrinterCodePage"` — для принтеров с `cp866` сначала преобразовать payload в транспортную строку, чтобы в QR физически попали байты UTF-8.
- `"qrPayloadPrinterCodePage": 866` — кодовая страница принтера, через которую идет упаковка QR payload.
- `"cookingStartInitialDelayMs": 5000` — подождать 5 секунд перед первой попыткой печати, чтобы iiko успела отпустить блокировку заказа.
- `"cookingStartRetryDelayMs": 2000` — ждать 2 секунды между повторными попытками.
- `"cookingStartMaxAttempts": 30` — максимум 30 попыток автопечати на один старт готовки.

Если файла нет, плагин создаёт его со значениями по умолчанию. Если JSON повреждён, плагин пишет ошибку в стандартный лог и использует безопасные дефолты.
Если после сканирования вы видите текст вида `���� �ॢ�⪠`, это почти всегда означает, что принтер записал в QR байты `cp866`, а приложение-сканер попыталось прочитать их как `UTF-8`. Для такого случая и нужен режим `Utf8ViaPrinterCodePage`.
Для заказов от стола это особенно важно: `PrintBillCheque(...)` меняет статус заказа на `Bill`, поэтому на живом событии `CookingStarted` заказ часто ещё занят внутренней edit-session iiko. Отложенная первая попытка и длинное окно ретраев нужны именно для обхода этой блокировки.
Если за время ретраев заказ успел перейти в `Closed` или `Deleted`, плагин завершает автопечать как `SKIPPED`: после закрытия заказа `PrintBillCheque(...)` уже не поддерживается самим API iiko.
Для заказа от стола плагин дополнительно учащает опрос на разблокировку до `250 ms`, чтобы не пропустить короткое окно между снятием блокировки и закрытием заказа.

## Справочник параметров

Ниже перечислены все строки `order-qr-settings.json` и допустимые значения.

- `"enabled"`:
  `true` | `false`
  Полностью включает или выключает плагин.

- `"printOnGuestBill"`:
  `true` | `false`
  Разрешает или запрещает добавление QR в guest bill через `BillChequePrinting`.

- `"payloadVersion"`:
  Сейчас поддерживается строка `IIKOQR1`.
  Лучше не менять, если у вас нет отдельного потребителя payload с другой версией формата.

- `"qrSize"`:
  Допустимые значения: `Tiny`, `Small`, `Medium`, `Large`, `Extralarge`, `Ultra`.
  Значение по умолчанию: `Extralarge`.
  Если указано неизвестное значение, плагин использует `Extralarge`.

- `"qrCorrection"`:
  Допустимые значения: `Low`, `High`.
  Значение по умолчанию: `Low`.
  Если указано неизвестное значение, плагин использует `Low`.

- `"qrPayloadEncodingMode"`:
  Допустимые значения: `Utf8ViaPrinterCodePage`, `Raw`.
  Значение по умолчанию: `Utf8ViaPrinterCodePage`.
  `Utf8ViaPrinterCodePage` нужен для принтеров, которые строят QR из байтов своей локальной кодовой страницы, например `cp866`.
  `Raw` оставляет payload без дополнительного преобразования.

- `"qrPayloadPrinterCodePage"`:
  Любое целое число `> 0`.
  Значение по умолчанию: `866`.
  Используется только вместе с `"qrPayloadEncodingMode": "Utf8ViaPrinterCodePage"`.
  Для вашей текущей настройки принтера должно оставаться `866`.

- `"treatAllZeroFoodValueAsMissing"`:
  `true` | `false`
  Если `true`, набор `K=0, B=0, J=0, U=0` печатается как `-`.
  Если `false`, печатается как `0`.

- `"maxPayloadUtf8BytesWarning"`:
  Любое целое число больше `0`.
  Значение по умолчанию: `2500`.
  Если payload превышает этот порог, плагин пишет предупреждение, но всё равно пытается печатать QR.

- `"writeFullPayloadToStandardLog"`:
  `true` | `false`
  Если `true`, полный payload пишется в стандартный лог плагина.
  Если `false`, в стандартный лог пишутся только служебные поля и статистика.

- `"writeJsonlAuditLog"`:
  `true` | `false`
  Включает или выключает запись JSONL-аудита в каталог `DataStorageDirectory`.

- `"includeOrderGuidInPayload"`:
  `true` | `false`
  Если `true`, в payload добавляется полный GUID заказа.
  Если `false`, GUID не включается.

- `"includeModifiers"`:
  `true` | `false`
  Если `true`, в payload включаются модификаторы блюд.
  Если `false`, строка `M:` для позиции будет `M:-`.

- `"includeAllergens"`:
  `true` | `false`
  Если `true`, плагин вызывает `GetAllergenGroupsByOrderRootItem(...)` и пытается включить аллергены.
  Если `false`, аллергены не вычисляются и в payload выводится `A:-`.

- `"includePrintTime"`:
  `true` | `false`
  Если `true`, в order header включается поле `D:` со временем печати.
  Если `false`, поле `D:` не добавляется.

- `"printOnCookingStart"`:
  `true` | `false`
  Главный флаг автопечати при начале приготовления.
  Если `false`, события `CookingStarted` игнорируются.

- `"printDeliveryBillOnCookingStart"`:
  `true` | `false`
  Если `true`, для `IDeliveryOrder` на `CookingStarted` вызывается `PrintDeliveryBill(...)`.
  Если `false`, доставка на старте готовки не печатается.

- `"printTableBillOnCookingStart"`:
  `true` | `false`
  Если `true`, для обычного заказа от стола на `CookingStarted` вызывается `PrintBillCheque(...)`.
  Если `false`, заказ от стола на старте готовки не печатается.

- `"cookingStartInitialDelayMs"`:
  Любое целое число `>= 0`.
  Значение по умолчанию: `5000`.
  Задержка перед первой попыткой автопечати после события `CookingStarted`.
  Для столов, где часто встречается `EntityAlreadyInUseException`, имеет смысл увеличивать до `8000`-`15000`.

- `"cookingStartRetryDelayMs"`:
  Любое целое число `> 0`.
  Значение по умолчанию: `2000`.
  Пауза между повторными попытками, если iiko ещё держит блокировку заказа.
  Если заказ может быстро закрываться, имеет смысл уменьшать до `500`-`1000`, чтобы не пропустить короткое окно между снятием блокировки и закрытием.
  Для заказа от стола плагин может опрашивать разблокировку чаще этого значения, но сохраняет общее окно ожидания.

- `"cookingStartMaxAttempts"`:
  Любое целое число `> 0`.
  Значение по умолчанию: `30`.
  Сколько раз плагин максимум попробует автопечать одного и того же заказа до окончательного отказа.

Рекомендуемый безопасный стартовый профиль:

```json
{
  "enabled": true,
  "printOnGuestBill": true,
  "payloadVersion": "IIKOQR1",
  "qrSize": "Extralarge",
  "qrCorrection": "Low",
  "qrPayloadEncodingMode": "Utf8ViaPrinterCodePage",
  "qrPayloadPrinterCodePage": 866,
  "treatAllZeroFoodValueAsMissing": true,
  "maxPayloadUtf8BytesWarning": 2500,
  "writeFullPayloadToStandardLog": false,
  "writeJsonlAuditLog": true,
  "includeOrderGuidInPayload": false,
  "includeModifiers": true,
  "includeAllergens": true,
  "includePrintTime": true,
  "printOnCookingStart": true,
  "printDeliveryBillOnCookingStart": true,
  "printTableBillOnCookingStart": true,
  "cookingStartInitialDelayMs": 5000,
  "cookingStartRetryDelayMs": 2000,
  "cookingStartMaxAttempts": 30
}
```

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
