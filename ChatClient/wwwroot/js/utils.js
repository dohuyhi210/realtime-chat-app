// ==============================================
// FILE: wwwroot/js/utils.js
// Mô tả: Utility functions (Helper - Model layer)
// ==============================================



// ===== AUTHENTICATION =====

// Lấy token từ localStorage
function getToken() {
    return localStorage.getItem('token');
}

// Lấy thông tin user hiện tại
function getCurrentUser() {
    return {
        id: parseInt(localStorage.getItem('userId')),
        username: localStorage.getItem('username'),
        nickname: localStorage.getItem('nickname')
    };
}

// Check authentication
function isAuthenticated() {
    return !!getToken();
}

// Redirect nếu chưa login
function requireAuth() {
    if (!isAuthenticated()) {
        window.location.href = '/Auth/Login';
        return false;
    }
    return true;
}

// ===== API CALLS =====

// Fetch helper với authentication
async function fetchAPI(endpoint, options = {}) {
    const token = getToken();

    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            ...(token && { 'Authorization': `Bearer ${token}` })
        }
    };

    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...options.headers
        }
    };

    try {
        const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}${endpoint}`, mergedOptions);

        // Nếu 401 Unauthorized → logout
        if (response.status === 401) {
            localStorage.clear();
            window.location.href = '/Auth/Login';
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
        throw error;
    }
}

// ===== DATE & TIME FORMATTING =====

// Format timestamp thành "HH:mm"
function formatTime(timestamp) {
    const date = new Date(timestamp);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
}

// Format timestamp thành "DD/MM/YYYY"
function formatDate(timestamp) {
    const date = new Date(timestamp);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

// Format timestamp thành "Hôm nay", "Hôm qua", hoặc "DD/MM/YYYY"
function formatDateDivider(timestamp) {
    const date = new Date(timestamp);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
        return 'Hôm nay';
    } else if (date.toDateString() === yesterday.toDateString()) {
        return 'Hôm qua';
    } else {
        return formatDate(timestamp);
    }
}

// Tính thời gian "offline X phút/giờ/ngày trước"
function formatOfflineTime(lastSeen) {
    const now = new Date();
    const lastSeenDate = new Date(lastSeen);
    const diffMs = now - lastSeenDate;
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMinutes < 1) {
        return 'Vừa xong';
    } else if (diffMinutes < 60) {
        return `${diffMinutes} phút trước`;
    } else if (diffHours < 24) {
        return `${diffHours} giờ trước`;
    } else if (diffDays < 7) {
        return `${diffDays} ngày trước`;
    } else {
        return formatDate(lastSeen);
    }
}

// ===== AVATAR GENERATION =====

// Tạo avatar chữ cái đầu từ nickname
function getAvatarText(nickname) {
    if (!nickname) return '?';

    const words = nickname.trim().split(' ');
    if (words.length >= 2) {
        // Lấy chữ cái đầu của 2 từ đầu
        return (words[0][0] + words[words.length - 1][0]).toUpperCase();
    } else {
        // Lấy 2 chữ cái đầu
        return nickname.substring(0, 2).toUpperCase();
    }
}

// ===== STRING HELPERS =====

// Escape HTML để tránh XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Truncate text với "..."
function truncateText(text, maxLength) {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
}

// ===== DOM HELPERS =====

// Scroll element xuống dưới cùng
function scrollToBottom(element) {
    element.scrollTop = element.scrollHeight;
}

// Scroll element xuống dưới cùng (smooth)
function smoothScrollToBottom(element) {
    element.scrollTo({
        top: element.scrollHeight,
        behavior: 'smooth'
    });
}

// Check element có ở dưới cùng không (trong khoảng 100px)
function isAtBottom(element) {
    return element.scrollHeight - element.scrollTop - element.clientHeight < 100;
}

// ===== NOTIFICATION =====

// Request notification permission
function requestNotificationPermission() {
    if ('Notification' in window && Notification.permission === 'default') {
        Notification.requestPermission();
    }
}

// Show browser notification
function showNotification(title, body) {
    if ('Notification' in window && Notification.permission === 'granted') {
        new Notification(title, {
            body: body,
            icon: '/favicon.ico',
            badge: '/favicon.ico'
        });
    }
}

// Play notification sound
function playNotificationSound() {
    // Tạo notification sound bằng Web Audio API
    const audioContext = new (window.AudioContext || window.webkitAudioContext)();
    const oscillator = audioContext.createOscillator();
    const gainNode = audioContext.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(audioContext.destination);

    oscillator.frequency.value = 800;
    oscillator.type = 'sine';

    gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);

    oscillator.start(audioContext.currentTime);
    oscillator.stop(audioContext.currentTime + 0.5);
}

// ===== DEBOUNCE =====

// Debounce function (để tránh gọi quá nhiều lần)
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ===== STORAGE =====

// Lưu vào localStorage với expiry
function setWithExpiry(key, value, ttl) {
    const now = new Date();
    const item = {
        value: value,
        expiry: now.getTime() + ttl
    };
    localStorage.setItem(key, JSON.stringify(item));
}

// Lấy từ localStorage với expiry check
function getWithExpiry(key) {
    const itemStr = localStorage.getItem(key);
    if (!itemStr) return null;

    const item = JSON.parse(itemStr);
    const now = new Date();

    if (now.getTime() > item.expiry) {
        localStorage.removeItem(key);
        return null;
    }

    return item.value;
}

// ===== VALIDATION =====

// Validate message content
// Validate message content
// Validate message content
function validateMessage(content) {
    console.log('🔍 Validating message:', `"${content}"`);

    // Kiểm tra nếu content là null, undefined, hoặc empty string
    if (!content) {
        console.log('❌ Validation failed: content is null/undefined');
        return {
            valid: false,
            error: 'Tin nhắn không được để trống'
        };
    }

    // Kiểm tra nếu content chỉ chứa khoảng trắng
    const trimmedContent = content.trim();
    if (trimmedContent.length === 0) {
        console.log('❌ Validation failed: content is only whitespace');
        return {
            valid: false,
            error: 'Tin nhắn không được để trống'
        };
    }

    // Kiểm tra độ dài
    if (content.length > 5000) {
        console.log('❌ Validation failed: message too long');
        return {
            valid: false,
            error: 'Tin nhắn không được vượt quá 5000 ký tự'
        };
    }

    console.log('✅ Validation passed');
    return { valid: true };
}

// ===== EXPORT (nếu dùng modules) =====
// Nếu không dùng modules, các functions này sẽ available globally