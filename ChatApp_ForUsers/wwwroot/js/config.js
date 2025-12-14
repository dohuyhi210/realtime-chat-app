// ==============================================
// FILE: wwwroot/js/config.js
// Mô tả: Cấu hình chung cho toàn bộ app
// ==============================================


const APP_CONFIG = {
    API_BASE_URL: 'http://192.168.88.1:5000/api',
    WS_BASE_URL: 'ws://192.168.88.1:5000',
    APP_NAME: 'ChatApp',
    VERSION: '1.0.0'
};

// Export để các file khác sử dụng
window.APP_CONFIG = APP_CONFIG;