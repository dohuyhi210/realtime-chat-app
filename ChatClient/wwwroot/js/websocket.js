// ==============================================
// FILE: wwwroot/js/websocket.js
// Mô tả: WebSocket client (Real-time communication)
// ==============================================

// WebSocket instance
let socket = null;
let reconnectAttempts = 0;
let maxReconnectAttempts = 5;
let reconnectDelay = 3000; // 3 giây

// WebSocket URL
const WS_URL = (window.APP_CONFIG?.WS_BASE_URL || 'ws://172.20.10.2:5000') + '/ws';;

// ===== AUTHENTICATION =====
function getToken() {
    return localStorage.getItem('token');
}

// ===== WEBSOCKET CONNECTION =====

// Kết nối WebSocket THỰC TẾ
function connectWebSocket() {
    const token = getToken();

    if (!token) {
        console.error('No token found. Cannot connect WebSocket.');
        return;
    }

    try {
        // Connect với token trong query string
        socket = new WebSocket(`${WS_URL}?token=${token}`);

        // Event: Connection opened
        socket.onopen = handleWebSocketOpen;

        // Event: Message received
        socket.onmessage = handleWebSocketMessage;

        // Event: Error
        socket.onerror = handleWebSocketError;

        // Event: Connection closed
        socket.onclose = handleWebSocketClose;

        console.log('🔄 Attempting WebSocket connection...');

    } catch (error) {
        console.error('WebSocket connection error:', error);
    }
}

// ===== INITIALIZATION =====  
function initWebSocket() {
    console.log('🚀 Initializing WebSocket...');
    connectWebSocket();
}

// ===== WEBSOCKET EVENT HANDLERS =====

// Handle: Connection opened
function handleWebSocketOpen(event) {
    console.log('✅ WebSocket connected');
    reconnectAttempts = 0;

    // Update UI: connection status
    updateConnectionStatus(true);

    // Callback: onWebSocketConnected (định nghĩa trong chat.js)
    if (typeof onWebSocketConnected === 'function') {
        onWebSocketConnected();
    }
}

// Handle: Message received
// Handle: Message received
function handleWebSocketMessage(event) {
    try {
        const message = JSON.parse(event.data);
        console.log('📨 WebSocket message received:', message);

        const messageType = message.Type || message.type;
        const messageData = message.Data || message.data;

        console.log('🔹 Message type:', messageType);
        console.log('🔹 Message data:', messageData);

        // Route message based on type
        switch (messageType) {
            case 'private_message':
                console.log('🔹 Handling PRIVATE MESSAGE');
                handlePrivateMessage(messageData);
                break;

            case 'group_message':
                console.log('🔹 Handling GROUP MESSAGE');
                handleGroupMessage(messageData);
                break;

            case 'user_online':
                console.log('🔹 Handling USER ONLINE');
                handleUserOnline(messageData);
                break;

            case 'user_offline':
                console.log('🔹 Handling USER OFFLINE');
                handleUserOffline(messageData);
                break;

            case 'typing':
                console.log('🔹 Handling TYPING');
                handleTypingIndicator(messageData);
                break;

            default:
                console.warn('❓ Unknown message type:', messageType);
        }

    } catch (error) {
        console.error('❌ Error handling WebSocket message:', error);
    }
}
// Handle: Error
function handleWebSocketError(error) {
    console.error('❌ WebSocket error:', error);
    updateConnectionStatus(false);
}

// Handle: Connection closed
function handleWebSocketClose(event) {
    console.log('🔴 WebSocket disconnected');
    updateConnectionStatus(false);

    // Attempt to reconnect
    attemptReconnect();
}

// ===== RECONNECTION =====

// Attempt to reconnect
function attemptReconnect() {
    if (reconnectAttempts >= maxReconnectAttempts) {
        console.error('Max reconnect attempts reached. Please refresh the page.');
        showConnectionError('Mất kết nối. Vui lòng tải lại trang.');
        return;
    }

    reconnectAttempts++;
    console.log(`Attempting to reconnect... (${reconnectAttempts}/${maxReconnectAttempts})`);

    setTimeout(() => {
        connectWebSocket();
    }, reconnectDelay);
}

// ===== SEND MESSAGES =====

// Gửi tin nhắn cá nhân
function sendPrivateMessage(receiverId, content) {
    if (!socket || socket.readyState !== WebSocket.OPEN) {
        console.error('WebSocket not connected');
        return false;
    }

    const message = {
        type: 'private_message',
        data: {
            receiverId: receiverId,
            content: content
        }
    };

    socket.send(JSON.stringify(message));
    return true;
}

// Gửi tin nhắn nhóm
function sendGroupMessage(groupId, content) {
    if (!socket || socket.readyState !== WebSocket.OPEN) {
        console.error('WebSocket not connected');
        return false;
    }

    const message = {
        type: 'group_message',
        data: {
            groupId: groupId,
            content: content
        }
    };

    socket.send(JSON.stringify(message));
    return true;
}

// Gửi typing indicator
function sendTypingIndicator(receiverId, groupId, isTyping) {
    if (!socket || socket.readyState !== WebSocket.OPEN) {
        return;
    }

    const message = {
        type: 'typing',
        data: {
            receiverId: receiverId,
            groupId: groupId,
            isTyping: isTyping
        }
    };

    socket.send(JSON.stringify(message));
}

// Gửi mark read
function sendMarkRead(senderId) {
    if (!socket || socket.readyState !== WebSocket.OPEN) {
        return;
    }

    const message = {
        type: 'mark_read',
        data: {
            senderId: senderId
        }
    };

    socket.send(JSON.stringify(message));
}

// ===== MESSAGE HANDLERS (Callbacks cho chat.js) =====

// Handle: Private message received
function handlePrivateMessage(data) {
    console.log('🎯 handlePrivateMessage CALLED:', data);

    // Callback được định nghĩa trong chat.js
    if (typeof onPrivateMessageReceived === 'function') {
        console.log('✅ Calling onPrivateMessageReceived');
        onPrivateMessageReceived(data);
    } else {
        console.error('❌ onPrivateMessageReceived function not found!');
    }
}
// Handle: Group message received
function handleGroupMessage(data) {
    // Callback được định nghĩa trong chat.js
    if (typeof onGroupMessageReceived === 'function') {
        onGroupMessageReceived(data);
    }
}

// Handle: User online
function handleUserOnline(data) {
    // Callback được định nghĩa trong chat.js
    if (typeof onUserStatusChanged === 'function') {
        onUserStatusChanged(data.userId, true);
    }
}

// Handle: User offline
function handleUserOffline(data) {
    // Callback được định nghĩa trong chat.js
    if (typeof onUserStatusChanged === 'function') {
        onUserStatusChanged(data.userId, false);
    }
}

// Handle: Typing indicator
function handleTypingIndicator(data) {
    // Callback được định nghĩa trong chat.js
    if (typeof onTypingIndicator === 'function') {
        onTypingIndicator(data);
    }
}

// ===== UI UPDATES =====

// Update connection status UI
function updateConnectionStatus(isConnected) {
    const statusElement = document.getElementById('connectionStatus');

    if (statusElement) {
        if (isConnected) {
            statusElement.textContent = '🟢 Đã kết nối';
            statusElement.className = 'connection-status online';
        } else {
            statusElement.textContent = '🔴 Mất kết nối';
            statusElement.className = 'connection-status offline';
        }
    }
}

// Show connection error
function showConnectionError(message) {
    // Có thể hiển thị modal hoặc alert
    alert(message);
}

// ===== DISCONNECT =====

// Disconnect WebSocket
// ===== DISCONNECT =====

// Disconnect WebSocket
function disconnectWebSocket() {
    console.log('🔌 Disconnecting WebSocket...');

    if (socket) {
        // Gửi logout message đến server (tùy chọn)
        if (socket.readyState === WebSocket.OPEN) {
            const logoutMessage = {
                type: 'user_logout',
                data: { userId: getCurrentUser()?.id }
            };
            socket.send(JSON.stringify(logoutMessage));
        }

        // Đóng kết nối
        socket.close();
        socket = null;
        console.log('✅ WebSocket disconnected');
    }

    // Reset reconnect attempts
    reconnectAttempts = 0;
}

// ===== CHECK CONNECTION =====

// Check if WebSocket is connected
function isWebSocketConnected() {
    return socket && socket.readyState === WebSocket.OPEN;
}

// ===== GLOBAL EXPORTS =====
window.initWebSocket = initWebSocket;
window.connectWebSocket = connectWebSocket;
window.sendPrivateMessage = sendPrivateMessage;
window.sendGroupMessage = sendGroupMessage;
window.sendTypingIndicator = sendTypingIndicator;
window.sendMarkRead = sendMarkRead;
window.disconnectWebSocket = disconnectWebSocket;
window.isWebSocketConnected = isWebSocketConnected;