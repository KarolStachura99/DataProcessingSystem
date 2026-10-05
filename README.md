# DataProcessingSystem

REST API do asynchronicznej analizy plików CSV z danymi sprzedaży.

## Problem

Analiza dużego pliku może trwać długo. Gdyby serwer wykonywał ją w trakcie
żądania HTTP, klient czekałby na odpowiedź, a przy dłuższej operacji
połączenie zostałoby przerwane.

## Rozwiązanie

API przyjmuje plik i od razu zwraca identyfikator zadania (`202 Accepted`).
Przetwarzanie odbywa się w tle, a klient sprawdza stan zadania po identyfikatorze.

Statusy zadania: `Pending` → `Processing` → `Completed` lub `Failed`.

## Technologie

- .NET 10, ASP.NET Core Minimal API
- Entity Framework Core + SQLite
- BackgroundService do przetwarzania w tle

## Uruchomienie

```
dotnet run
```

Baza danych tworzy się automatycznie przy starcie aplikacji.

Wysłanie przykładowego pliku:

```
curl -L -F "file=@samples/sprzedaz.csv" http://127.0.0.1:5056/tasks
```

Sprawdzenie wyniku (w miejsce `{id}` wstaw identyfikator z odpowiedzi):

```
curl -L http://127.0.0.1:5056/tasks/{id}
```

Przykładowe żądania znajdują się też w pliku `DataProcessingSystem.http`.

## API

| Metoda | Adres | Opis |
|---|---|---|
| POST | `/tasks` | Przyjmuje plik CSV (`multipart/form-data`, pole `file`, maks. 5 MB) |
| GET | `/tasks/{id}` | Zwraca status i wynik zadania |

Format CSV: nagłówek, a potem wiersze `produkt,ilosc,cena`, np. `Kawa,3,12.50`.

Wynik analizy: liczba wierszy, łączna ilość, łączny przychód, produkt
z największym przychodem oraz przychód każdego produktu. Niepoprawny wiersz
kończy zadanie statusem `Failed` z opisem błędu i numerem wiersza.

## Decyzje projektowe

- **SQLite zamiast serwera bazy danych:** projekt uruchamia się bez instalowania
  dodatkowych programów. Dzięki EF Core zmiana na PostgreSQL wymaga zmiany
  jednego wywołania (`UseSqlite` → `UseNpgsql`).
- **Analizator CSV oddzielony od workera:** logika analizy nie zależy od bazy
  ani serwera, więc można ją testować osobno.
- **Walidacja pliku przed zapisem:** puste pliki, pliki większe niż 5 MB
  i pliki bez rozszerzenia `.csv` są odrzucane z kodem `400`.

## Możliwe usprawnienia

- testy jednostkowe analizatora
- ponawianie nieudanych zadań i obsługa zadań przerwanych w stanie `Processing`
  (np. po awarii serwera)
- obsługa wielu workerów jednocześnie
- Docker i PostgreSQL