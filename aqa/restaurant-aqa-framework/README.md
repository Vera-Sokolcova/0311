# Restaurant AQA framework

> **Обраний напрям: AQA, напрям 1 — груповий pre-order** (рівні L1 перегляд учасників, L2 підтвердження, L3 видалення учасника).
> **Підхід:** тестуємо власний розгорнутий застосунок «Green & Tasty» (фронтенд на GitHub Pages, бекенд на Render).
> API-тести покривають авторизацію, валідацію і підготовку даних; UI-тести — основні сценарії власника.
> Кожен сценарій створює власні дані (нового клієнта, бронювання на випадковий слот, гостей) і прибирає їх після себе.

## Групові сценарії

| Feature | Рівень | Що перевіряє |
|---|---|---|
| `GroupPreOrderView.feature` | L1 | власник бачить учасників, страви, суми по кожному і загальну; відмітка «ще обирає»; учасник без страв; порожня група; доступ лише власнику (інший клієнт, гість, офіціант, неавторизований, неіснуюча група) |
| `GroupPreOrderConfirmation.feature` | L2 | консолідація однакових страв, ігнорування непідтверджених кошиків, замовлення бачить офіціант (`SubmittedByCustomer`); усі причини відмови окремими сценаріями; повторне підтвердження; не-Owner |
| `GroupPreOrderRemoval.feature` | L3 | видалення одного з кількох, видалення підтвердженого учасника, видалення останнього, учасник іншої групи (400), неіснуючий (404), не-Owner; наскрізний сценарій із офіціантом |
| `GroupPreOrderOnTheWebsite.feature` | UI | сторінка групового замовлення: учасники й суми, підтвердження, видалення гостя, офіціант бачить замовлення |

Запуск тільки групових сценаріїв: `dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=group-preorder]"`.
За рівнем: `Category=l1|l2|l3`; лише API: `Category=api`; лише UI: `Category=ui`.

### Поведінка, яку ДЗ не визначає (обрана і протестована)

| Ситуація | Поведінка |
|---|---|
| Не власник групи (інший клієнт, офіціант) | `403`, повідомлення «Only the reservation owner can perform this action.» |
| Без входу, або гість зі своїм session-токеном | `401` (гість — не користувач системи) |
| Повторне підтвердження вже підтвердженої групи | `409` |
| Група без жодного підтвердженого кошика / без учасників | `400` «There are no submitted guest sessions in the group.» |
| Група без учасників | `participants: []`, `total: 0` |
| Видалення останнього учасника | `200`, порожній список, `total: 0` |
| Учасник іншої групи | `400` |
| Неіснуючий учасник або група | `404` |

Не автоматизовано: відмова «у підтверджених кошиках немає жодної страви» — вона недосяжна, бо бекенд не дає підтвердити порожній кошик і не дає змінити кошик після підтвердження.

### Що потрібно для запуску

- `UI_URL`, `API_URL` — адреси вашого розгорнутого фронтенду й бекенду (без кінцевого `/`).
- `WAITER_USERNAME`, `WAITER_PASSWORD` — обліковий запис офіціанта (`waiter@example.com` / `Waiter123!` у демо-даних).
- `TESTTABLE_ID` — стіл на ≥ 4 місця, біля якого працює офіціант, наприклад `table-3`.
- Безкоштовний бекенд «засинає»: першій відповіді після простою потрібно до кількох хвилин, CI спочатку «будить» його (крок `Wake up the backend`).

End-to-end tests for the Green & Tasty restaurant application: Reqnroll (Gherkin) + TUnit + Playwright for .NET, with the Screenplay pattern.

Guidance for extending the suite (writing Gherkin, steps, Screenplay, test data, working with an AI assistant) is in [CLAUDE.md](CLAUDE.md).

## Run the tests

```bash
cp .env.example .env        # then fill in your own values (see "Configuration")
dotnet build
dotnet run --no-build -- --report-trx                          # everything
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=smoke]"   # by tag
```

Everything a run produces lands in `TestResults/`: the TUnit report and TRX, the Reqnroll report in `reqnroll/`, and Playwright traces in `traces/` (open one with `npx playwright show-trace <file>.zip`).

Some scenarios are expected to fail (`@known-defect`, bugs in the application) or are skipped (`@wip`, no admin account configured). That is intentional.

## Quality gates

| Where | What runs |
|---|---|
| Git hooks (local) | `pre-commit`: `dotnet format` check and Gherkin lint on staged files · `commit-msg`: Conventional Commits · `pre-push`: build with analyzers |
| `aqa-quality.yml` | on every push and pull request: no generated files tracked, format, build (warnings are errors), Gherkin lint, test discovery |
| `aqa-tests.yml` | the scenarios themselves, nightly and on demand (Actions → AQA Tests → Run workflow) |

Enable the hooks once per clone, from the repository root:

```bash
dotnet tool restore --tool-manifest aqa/restaurant-aqa-framework/.config/dotnet-tools.json
git config core.hooksPath aqa/restaurant-aqa-framework/.husky
```

The hooks only act on commits and pushes that touch `aqa/restaurant-aqa-framework`. Gherkin lint needs Node.js.

## Configuration

There is no shared environment and no committed `.env`: copy `.env.example` to `.env` and fill it in with the data of **your own** deployment (see "Testing your own deployment"). `.env` is git-ignored; never commit it.

Settings come from `.env` or real environment variables (variables win):

| Setting | Meaning |
|---|---|
| `UI_URL` | where the frontend is served |
| `API_URL` | base URL of the backend API, including the stage (for example `…/develop`) |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | a waiter account that already exists in the environment under test |
| `TESTTABLE_ID`, `TESTTABLE_WAITER_*` | a table and the waiter assigned to it (only for scenarios that need them) |
| `ADMIN_USERNAME`, `ADMIN_PASSWORD` | an admin account; without it the admin scenarios are skipped |

The full list, with defaults, is in `.env.example`.

## Testing your own deployment

### If you deployed the backend / frontend / DevOps homework

1. `cp .env.example .env` and set `UI_URL` and `API_URL` to your deployment (API URL includes the stage, for example `…/develop`).
2. Waiters cannot be created by signing up, so create a waiter (and, for the admin scenarios, an admin) in *your* environment and put its credentials in `WAITER_*` / `ADMIN_*`. Optionally add a table and its assigned waiter (`TESTTABLE_*`).
3. Run `dotnet build` and a small slice first (`--treenode-filter "/*/*/*/*[Category=smoke]"`).

### If you have not deployed your own homework

If you deployed the original application, test that: set `UI_URL` / `API_URL` as above. Otherwise point the framework at a comparable public site (Booking, Airbnb, OpenTable, …) and write all the layers for it yourself: Gherkin features, step definitions, Screenplay tasks and questions, page objects with locators, and API clients where the site has a usable API. Use only flows you are allowed to automate (no purchases, no real bookings, respect the site's terms), and set `UI_URL` to that site.

1. **Generate a skeleton with Playwright codegen** (see "Presetup instructions"): record one flow, for example search → open a result → check details. Save the generated code to a scratch file, it is only raw material.
2. **Split it across the layers**: locators and small actions go to `Ui/` page objects, the flow becomes a task in `Screenplay/Tasks/`, the read-backs become questions in `Screenplay/Questions/`, the business wording goes to `Features/` and thin glue to `StepDefinitions/`.
3. **Replace weak locators.** Codegen often emits positional or text-fragile selectors. Prefer role + accessible name, label, test id, then CSS (see [CLAUDE.md](CLAUDE.md)); check them in the Playwright inspector.
4. Tag scenarios `@ui` / `@api`, keep them independent, and run `npx gherkin-lint -c .gherkin-lintrc "Features/*.feature"`.

### CI on your own public repository

Publish your work in your own public Git repository (the link is the deliverable). Workflows are disabled in a fresh fork until you enable them. Add the same values under Settings → Secrets and variables → Actions:

- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, `AQA_TESTTABLE_ID`
- **Secrets:** `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`, `AQA_ADMIN_USERNAME`, `AQA_ADMIN_PASSWORD`, `AQA_TESTTABLE_WAITER_USERNAME`, `AQA_TESTTABLE_WAITER_PASSWORD`

`aqa-quality.yml` runs on every push and pull request; `aqa-tests.yml` (nightly and on demand) tests whatever those variables point at. Never put credentials in the repository, in `.env.example` or in test code.

## Presetup instructions

This framework contains PlaywriteInstaller script so you don't need to setup it manually.
For full BDD highlighting in your IDE you may need to install Reqnroll extension https://docs.reqnroll.net/latest/ide-integrations/index.html
To start the Playwright code generator open Powershell in the framework directory and enter the line:
```bash
bin/Debug/net10.0/playwright.ps1 codegen "<your UI_URL or the public site you test>"
```
Build first so `playwright.ps1` exists. The URL is optional.
