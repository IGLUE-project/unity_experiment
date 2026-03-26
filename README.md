# Padlock Puzzle for Escapp

A Unity WebGL padlock puzzle that integrates with the [Escapp](https://escapp.es) educational escape room platform.

## Features

- Configurable number of digit positions (1-10)
- Visual padlock with rotating digit wheels
- **Drag/slide interaction** - drag up/down on wheels to change digits
- Shackle animation on correct solution
- Integration with Escapp API for solution validation
- Supports both online (Escapp server) and offline (mock) modes

## Requirements

- Unity 6 (6000.x) or later
- WebGL build support

## Project Structure

```
PadlockPuzzle/
├── Assets/
│   ├── Plugins/WebGL/
│   │   └── EscappBridge.jslib      # JS-Unity bridge
│   ├── Scripts/
│   │   ├── SimplePadlock.cs        # Main padlock controller
│   │   └── Editor/
│   │       └── CreatePadlockScene.cs
│   ├── WebGLTemplates/
│   │   └── EscappTemplate/
│   │       ├── index.html          # WebGL template
│   │       ├── escapp.js           # Escapp client library
│   │       └── escapp.css          # Escapp client styles
│   └── Prefabs/
│       └── WheelPrefab.prefab
├── WebGL/                          # Built WebGL output
│   ├── index.html
│   ├── escapp.js
│   ├── escapp.css
│   └── Build/
└── host.html                       # Local testing page
```

## Building

1. Open the project in Unity
2. Go to **File > Build Settings**
3. Select **WebGL** platform
4. Click **Switch Platform** if needed
5. Set the WebGL Template to **EscappTemplate** in Player Settings
6. In Player Settings > Publishing Settings, set **Compression Format** to **Disabled** (for local testing with Python server)
7. Click **Build** and select the `WebGL` folder as output

## Configuration

The puzzle reads configuration from `window.ESCAPP_APP_SETTINGS` set by the parent page:

```javascript
window.ESCAPP_APP_SETTINGS = {
    solutionLength: 4,                    // Number of digit positions
    escappClientSettings: {
        endpoint: "https://escapp.es/api/escapeRooms/YOUR_ID",
        linkedPuzzleIds: [2],             // Puzzle ID to validate against
        silent: true,                     // Don't show auth popup
        notifications: false,
        user: {
            email: "user@example.com",
            token: "userToken123"
        }
    }
};
```

### Configuration Options

| Property | Description |
|----------|-------------|
| `solutionLength` | Number of digits in the padlock (default: 4) |
| `backgroundImage` | URL to a background image (optional) |
| `backgroundColor` | CSS color for the background (default: `#1a1a2e`) |
| `escappClientSettings.endpoint` | Escapp API endpoint URL |
| `escappClientSettings.linkedPuzzleIds` | Array with the puzzle ID |
| `escappClientSettings.user.email` | User email for authentication |
| `escappClientSettings.user.token` | User token for authentication |
| `escappClientSettings.silent` | Set to `true` to use provided credentials without auth popup |

## Local Testing

### Using the test page

1. Build the WebGL project to the `WebGL` folder
2. Start a local server:
   ```bash
   cd PadlockPuzzle
   python3 -m http.server 8080
   ```
3. Open `http://localhost:8080/host.html`
4. Configure the settings and click **Load Puzzle**

### Embedding in an iframe

```html
<script>
window.ESCAPP_APP_SETTINGS = {
    solutionLength: 4,
    escappClientSettings: {
        endpoint: "https://escapp.es/api/escapeRooms/YOUR_ID",
        linkedPuzzleIds: [2],
        silent: true,
        user: { email: "...", token: "..." }
    }
};
</script>
<iframe src="WebGL/index.html" width="600" height="500"></iframe>
```

## Escapp Integration

The puzzle uses the [escapp_client](https://github.com/IGLUE-project/escapp_client) library to communicate with the Escapp platform.

### How it works

1. Parent page sets `window.ESCAPP_APP_SETTINGS` with configuration
2. Unity WebGL reads the configuration via JavaScript bridge
3. Padlock creates the correct number of digit wheels
4. When user submits, solution is sent to Escapp via `submitPuzzle()`
5. Server response determines if the shackle opens

### API Methods Used

- `submitPuzzle(puzzleId, solution, options, callback)` - Submit and validate solution
- `validate(callback)` - Validate user session

## Customization

### Interaction mode

By default, the padlock uses **drag interaction** - users slide up/down on the digit wheels. To switch to button mode (up/down arrows), set `useDragInteraction = false` on the `SimplePadlock` component.

### Changing the visual style

Edit the padlock prefabs and materials in Unity:
- `Assets/Prefabs/WheelPrefab.prefab` - Digit wheel appearance
- Scene objects in `PadlockScene` - Body, shackle, buttons

### Adding more digits

The number of digits is controlled by `solutionLength` in the configuration. The padlock dynamically creates wheel instances at runtime.

## Troubleshooting

### "Mock mode - no server"
- Check that `ESCAPP_APP_SETTINGS` is accessible from the iframe
- Verify the `endpoint` URL is correct
- Check browser console for cross-origin errors

### Stuck at "Checking..."
- Open Network tab to see if API request was made
- Check console for callback errors
- Verify user credentials are valid

### Auth popup appears
- Set `silent: true` in `escappClientSettings`
- Ensure valid `user.email` and `user.token` are provided

### Build stuck at 90%
- Disable compression in Player Settings > Publishing Settings
- Or configure your server to serve `.br` files with correct MIME type

## License

This project integrates with the Escapp platform. See [Escapp](https://github.com/ging/escapp) for platform licensing.

The escapp_client library is from [IGLUE-project/escapp_client](https://github.com/IGLUE-project/escapp_client).
