# Foundry Console V2 (Python)

Python port of [`FoundryConsoleV2`](../FoundryConsoleV2). It calls the model directly, using the `openai` SDK's Responses API against the Foundry unified AI services endpoint (`…/openai/v1`).

It runs the same four examples as the C# console:

1. It asks for the current weather without giving any data, so the model can't answer.
2. It asks the model to make something up.
3. It passes raw Open-Meteo JSON in and gets a string back.
4. It passes raw Open-Meteo JSON in and gets strict JSON back (`json_schema`, `strict: true`).

Examples 3 and 4 fetch geocoding and current weather straight from Open-Meteo instead of calling Core's `GetLatLongEvent` and `GetPublicWeatherCurrentEvent`.

## Run

```bash
cd FoundryConsoleV2python
pip install -e ".[dev]"
cp .env.example .env   # set AZURE_FOUNDRY_PROD_KEY
foundry-console-v2-python   # or: python foundry_console_v2.py
```

## Test

```bash
python -m pytest -q
```
