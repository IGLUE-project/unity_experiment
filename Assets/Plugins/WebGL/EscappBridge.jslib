mergeInto(LibraryManager.library, {

    // Get the number of positions from config
    GetConfigPositions: function() {
        if (window.PADLOCK_CONFIG && window.PADLOCK_CONFIG.positions) {
            return window.PADLOCK_CONFIG.positions;
        }
        return 4; // default
    },

    // Get the puzzle ID from config
    GetConfigPuzzleId: function() {
        if (window.PADLOCK_CONFIG && window.PADLOCK_CONFIG.puzzleId) {
            return window.PADLOCK_CONFIG.puzzleId;
        }
        return 1; // default
    },

    // Get the test solution (for offline mode)
    GetConfigSolution: function() {
        var solution = '1234';
        if (window.PADLOCK_CONFIG && window.PADLOCK_CONFIG.solution) {
            solution = window.PADLOCK_CONFIG.solution;
        }
        var bufferSize = lengthBytesUTF8(solution) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(solution, buffer, bufferSize);
        return buffer;
    },

    // Get the unlocked text from config
    GetConfigUnlockedText: function() {
        var text = 'UNLOCKED!';
        if (window.PADLOCK_CONFIG && window.PADLOCK_CONFIG.unlockedText) {
            text = window.PADLOCK_CONFIG.unlockedText;
        }
        var bufferSize = lengthBytesUTF8(text) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(text, buffer, bufferSize);
        return buffer;
    },

    // Initialize Escapp (called from Unity)
    EscappInitialize: function() {
        console.log('Escapp bridge initialized from Unity');
    },

    // Submit puzzle solution using checkPuzzle
    EscappSubmitPuzzle: function(puzzleId, solutionPtr, gameObjectNamePtr, callbackMethodPtr) {
        var solution = UTF8ToString(solutionPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        console.log('Checking puzzle:', puzzleId, 'solution:', solution);

        // Use the global function if available (set up in index.html)
        if (window.EscappCheckSolution) {
            window.EscappCheckSolution(puzzleId, solution, gameObjectName, callbackMethod);
            return;
        }

        if (!window.escappInstance) {
            console.error('Escapp not initialized');
            var errorResponse = JSON.stringify({
                success: false,
                message: 'Escapp not initialized'
            });
            SendMessage(gameObjectName, callbackMethod, errorResponse);
            return;
        }

        // Use checkPuzzle instead of submitPuzzle
        window.escappInstance.checkPuzzle(puzzleId, solution, {}, function(success, response) {
            console.log('checkPuzzle response:', success, response);

            var unityResponse = JSON.stringify({
                success: success,
                message: success ? 'Correct!' : (response && response.message ? response.message : 'Wrong combination!'),
                puzzleId: puzzleId.toString()
            });

            SendMessage(gameObjectName, callbackMethod, unityResponse);
        });
    },

    // Check puzzle without saving progress
    EscappCheckPuzzle: function(puzzleId, solutionPtr, gameObjectNamePtr, callbackMethodPtr) {
        var solution = UTF8ToString(solutionPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        if (!window.escappInstance) {
            SendMessage(gameObjectName, callbackMethod, JSON.stringify({ success: false }));
            return;
        }

        window.escappInstance.checkPuzzle(puzzleId, solution, {}, function(success, response) {
            SendMessage(gameObjectName, callbackMethod, JSON.stringify({
                success: success,
                message: response ? response.message : ''
            }));
        });
    },

    // Check if Escapp is ready
    EscappIsInitialized: function() {
        return window.escappInstance !== null && window.escappInstance !== undefined;
    }

});
