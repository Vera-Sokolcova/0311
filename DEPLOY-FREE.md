# Безкоштовний деплой без AWS

Дві частини, обидві без AWS і (зазвичай) без банківської картки:

| Частина | Де | Файли |
|---|---|---|
| Frontend (Angular) | GitHub Pages (або Netlify) | `.github/workflows/deploy-pages.yml`, `netlify.toml` |
| Backend (.NET) | Render, free web service | `Dockerfile.backend`, `render.yaml` |

## Як працює backend без AWS
Той самий код Lambda-обробників запускається як звичайний веб-сервер (`dnapp/local-host`).
Режим вмикає змінна `LOCAL_MODE=true` (у Lambda вона не задана, тож AWS-поведінка не змінилась):
- DynamoDB замінено in-memory сховищем `Local/LocalDb.cs` (репозиторії `Local/InMemoryRepositories.cs`);
- Cognito замінено вбудованою автентифікацією `Local/LocalAuth.cs` (JWT, групи Customer/Waiter);
- S3, SQS і звіти вимкнені (повертають 500 «not available in local mode»).

Дані скидаються при перезапуску й засіваються заново. Тестові акаунти:

| Роль | Email | Пароль |
|---|---|---|
| Owner (Customer) | `owner@example.com` | `Owner123!` |
| Waiter | `waiter@example.com` | `Waiter123!` |

Демо-бронювання власника: `demo-reservation` (з порожнім замовленням).

## Кроки
1. Створіть публічний репозиторій на GitHub і залийте в корінь все, що в архіві/проєкті
   (`backend`, `frontend`, `.github`, `Dockerfile.backend`, `render.yaml`).
2. **Backend:** на render.com → New → Blueprint → оберіть репозиторій (підхопиться `render.yaml`).
   Після збірки отримаєте URL виду `https://restaurant-api.onrender.com`. Перевірте `GET <url>/health`.
3. **Frontend:** у GitHub: Settings → Pages → Source: *GitHub Actions*;
   Settings → Secrets and variables → Actions → *Variables* → додайте `BACKEND_URL` = URL з кроку 2
   (без слеша в кінці). Push у `main` запустить workflow.
   Без змінної `BACKEND_URL` workflow збирає демо на моках (`?demo`) — це запасний варіант.
4. Відкрийте `https://<user>.github.io/<repo>/`, увійдіть як Owner, у кошику натисніть
   «Group Pre-Order: guests' dishes» або відкрийте `/reservations/demo-reservation/group-order`.

## Обмеження
- Безкоштовний інстанс Render засинає після ~15 хв простою; перший запит прокидає його 30–60 с.
- **Код бекенду не компілювався в середовищі, де я його писала (там немає dotnet).**
  Якщо збірка на Render впаде, відкрийте її лог і надішліть мені помилки: це зазвичай дрібні виправлення.
- Лише частина API перевірена логікою, а не запуском: логін/реєстрація, бронювання, групове замовлення,
  страви, локації, столи. Завантаження зображень, звіти та email-сповіщення не працюють.
- Для AQA Direction 1 цього достатньо: API та UI тести можна запускати на цих URL.
