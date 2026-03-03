const Bridge = (() => {
    const listeners = {};

    function send(type, payload = {}) {
        // Sends a JSON string to C#
        window.external.sendMessage(JSON.stringify({ type, payload }));
    }

    function on(type, callback) {
        if (!listeners[type]) listeners[type] = [];
        listeners[type].push(callback);
    }

    // Photino triggers this when C# replies
    window.external.receiveMessage(raw => {
        try {
            const msg = JSON.parse(raw);
            if (listeners[msg.type]) {
                listeners[msg.type].forEach(cb => cb(msg.payload));
            } else {
                console.warn("Unhandled message from C#:", msg.type);
            }
        } catch (e) {
            console.error("Bridge parse error:", e);
        }
    });

    return { send, on };
})();