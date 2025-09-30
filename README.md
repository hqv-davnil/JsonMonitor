# Garden Tools Connection Monitor

A WPF application that monitors garden tools connection status by reading a JSON file.

## What it does

Displays "real-time" connection status for:
- Chainsaw
- Blower  
- Hedge Trimmer

The app automatically checks for status changes every 2 seconds.

## How to run

Use the buttons to:
   - **Check Status** - Manual refresh
   - **Start Monitoring** - Begin automatic monitoring
   - **Stop Monitoring** - Stop automatic monitoring

## JSON File Format

The app reads `data.json` with this format:

**File location:** `src\JsonMonitor.WpfApp\bin\Debug\net6.0-windows\data.json`

```json
{
  "title": "Garden Tools Connection Monitor",
  "lastModified": "2025-09-29T18:30:00Z",
  "items": [
    {
      "name": "Chainsaw",
      "value": "Connected",
      "timestamp": "2025-09-29T18:30:00Z"
    }
  ]
}
```

**To test file monitoring:**
1. Start the app and click "Start Monitoring"
2. Open the `data.json` file in Notepad
3. Change any value (e.g., "Connected" → "Disconnected")
4. Save the file
5. The changes should then be visible in GUI