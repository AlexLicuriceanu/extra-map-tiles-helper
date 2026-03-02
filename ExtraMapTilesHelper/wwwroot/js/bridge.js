const Bridge = (() => {
    const listeners = {};

    function send(type, payload = {}) {
        window.external.sendMessage(JSON.stringify({ type, payload }));
    }

    function on(type, callback) {
        if (!listeners[type]) listeners[type] = [];
        listeners[type].push(callback);
    }

    function off(type, callback) {
        if (!listeners[type]) return;
        listeners[type] = listeners[type].filter(cb => cb !== callback);
    }

    // Photino calls this when C# sends a message back
    window.external.receiveMessage(raw => {
        try {
            const msg = JSON.parse(raw);
            const handlers = listeners[msg.type];
            if (handlers) {
                handlers.forEach(cb => cb(msg.payload));
            } else {
                console.warn("Unhandled message type:", msg.type);
            }
        } catch (e) {
            console.error("Bridge parse error:", e);
        }
    });

    return { send, on, off };
})();