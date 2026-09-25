# Foundry Console V3 (Python)

Python port of [`FoundryConsoleV3`](../FoundryConsoleV3). It uses the Responses API with **in-process tool callbacks**. The model asks for function calls, this console runs them locally, feeds the outputs back, and loops until the model returns strict JSON.

It exposes two tools, ported from Core's handlers:

| Tool | Source |
| --- | --- |
| `GetLatLong` | Open-Meteo geocoding, up to 5 ranked matches |
| `GetPublicWeatherCurrent` | Open-Meteo `current_weather` |

Unlike the C# console, it leaves out `GetLocation`, the forecast and history tools, and the database-backed tools (`GetCities`, `GetUser`, `AddUserCity`, `DeleteUserCity`). As a result, it needs no `DB_CONNECTION_STRING`.

## Run

```bash
cd FoundryConsoleV3python
pip install -e ".[dev]"
cp .env.example .env   # set AZURE_FOUNDRY_PROD_KEY
foundry-console-v3-python   # or: python foundry_console_v3.py
```

## Test

```bash
python -m pytest -q
```
