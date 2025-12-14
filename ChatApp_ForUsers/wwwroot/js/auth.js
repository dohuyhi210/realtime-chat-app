// ==============================================
// FILE: wwwroot/js/auth.js
// Mô tả: Logic đăng nhập và đăng ký
// ==============================================



// ===== UTILITY FUNCTIONS =====

// Hiển thị alert
function showAlert(message, type = 'error') {
    const alert = document.getElementById('alert');
    alert.textContent = message;
    alert.className = `auth-alert auth-alert-${type} show`;

    // Auto hide sau 5 giây
    setTimeout(() => {
        alert.classList.remove('show');
    }, 5000);
}

// Hiển thị error cho input
function showFieldError(fieldId, message) {
    const input = document.getElementById(fieldId);
    const errorSpan = document.getElementById(`${fieldId}Error`);

    input.classList.add('error');
    errorSpan.textContent = message;
    errorSpan.classList.add('show');
}

// Xóa tất cả errors
function clearErrors() {
    document.querySelectorAll('.auth-form-input').forEach(input => {
        input.classList.remove('error');
    });

    document.querySelectorAll('.error-message').forEach(span => {
        span.classList.remove('show');
        span.textContent = '';
    });
}

// Lưu user info vào localStorage
function saveUserInfo(token, user) {
    localStorage.setItem('token', token);
    localStorage.setItem('userId', user.id);
    localStorage.setItem('username', user.username);
    localStorage.setItem('nickname', user.nickname);
}

// Check đã login chưa
function checkAuth() {
    const token = localStorage.getItem('token');
    if (token) {
        // Redirect đến chat page
        window.location.href = '/Home/Index'; // ← SỬA DÒNG NÀY
    }
}

// ===== LOGIN PAGE =====

function initLoginPage() {
    // Check nếu đã login
    checkAuth();

    const form = document.getElementById('loginForm');
    const submitBtn = document.getElementById('submitBtn');
    const togglePasswordBtn = document.getElementById('togglePassword');
    const passwordInput = document.getElementById('password');

    // Toggle password visibility
    togglePasswordBtn.addEventListener('click', () => {
        const type = passwordInput.type === 'password' ? 'text' : 'password';
        passwordInput.type = type;
        togglePasswordBtn.textContent = type === 'password' ? '👁️' : '🙈';
    });

    // Form submit
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        clearErrors();

        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;

        // Validation
        if (!username) {
            showFieldError('username', 'Vui lòng nhập tên đăng nhập');
            return;
        }

        if (!password) {
            showFieldError('password', 'Vui lòng nhập mật khẩu');
            return;
        }

        // Disable button và hiển thị loading
        submitBtn.disabled = true;
        submitBtn.classList.add('loading');
        submitBtn.textContent = '';

        try {
            const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            });

            const data = await response.json();

            if (response.ok && data.success) {
                // Login thành công
                saveUserInfo(data.token, data.user);
                showAlert('Đăng nhập thành công! Đang chuyển hướng...', 'success');

                // Redirect sau 1 giây
                setTimeout(() => {
                    window.location.href = '/Home/Index'; // ← SỬA DÒNG NÀY
                }, 1000);
            } else {
                // Login thất bại
                showAlert(data.message || 'Tên đăng nhập hoặc mật khẩu không đúng', 'error');
                submitBtn.disabled = false;
                submitBtn.classList.remove('loading');
                submitBtn.textContent = 'Đăng nhập';
            }
        } catch (error) {
            console.error('Login error:', error);
            showAlert('Không thể kết nối đến server. Vui lòng thử lại sau.', 'error');
            submitBtn.disabled = false;
            submitBtn.classList.remove('loading');
            submitBtn.textContent = 'Đăng nhập';
        }
    });
}

// ===== REGISTER PAGE =====

function initRegisterPage() {
    // Check nếu đã login
    checkAuth();

    const form = document.getElementById('registerForm');
    const submitBtn = document.getElementById('submitBtn');
    const togglePasswordBtn = document.getElementById('togglePassword');
    const toggleConfirmPasswordBtn = document.getElementById('toggleConfirmPassword');
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');

    // Toggle password visibility
    togglePasswordBtn.addEventListener('click', () => {
        const type = passwordInput.type === 'password' ? 'text' : 'password';
        passwordInput.type = type;
        togglePasswordBtn.textContent = type === 'password' ? '👁️' : '🙈';
    });

    toggleConfirmPasswordBtn.addEventListener('click', () => {
        const type = confirmPasswordInput.type === 'password' ? 'text' : 'password';
        confirmPasswordInput.type = type;
        toggleConfirmPasswordBtn.textContent = type === 'password' ? '👁️' : '🙈';
    });

    // Form submit
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        clearErrors();

        const nickname = document.getElementById('nickname').value.trim();
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        // Validation
        let hasError = false;

        if (!nickname || nickname.length < 2) {
            showFieldError('nickname', 'Tên hiển thị phải có ít nhất 2 ký tự');
            hasError = true;
        }

        if (!username || username.length < 3) {
            showFieldError('username', 'Tên đăng nhập phải có ít nhất 3 ký tự');
            hasError = true;
        } else if (!/^[a-zA-Z0-9_]+$/.test(username)) {
            showFieldError('username', 'Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới');
            hasError = true;
        }

        if (!password || password.length < 6) {
            showFieldError('password', 'Mật khẩu phải có ít nhất 6 ký tự');
            hasError = true;
        }

        if (password !== confirmPassword) {
            showFieldError('confirmPassword', 'Mật khẩu xác nhận không khớp');
            hasError = true;
        }

        if (hasError) {
            return;
        }

        // Disable button và hiển thị loading
        submitBtn.disabled = true;
        submitBtn.classList.add('loading');
        submitBtn.textContent = '';

        try {
            const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}/auth/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    username: username,
                    password: password,
                    nickname: nickname
                })
            });

            const data = await response.json();

            if (response.ok && data.success) {
                // Đăng ký thành công
                //saveUserInfo(data.token, data.user);
                showAlert('Đăng ký thành công! Đang chuyển hướng...', 'success');

                // Redirect sau 1 giây
                setTimeout(() => {
                    window.location.href = '/Auth/Login'; // ← SỬA DÒNG NÀY
                }, 1000);
            } else {
                // Đăng ký thất bại
                if (data.errors) {
                    // Hiển thị validation errors từ server
                    Object.keys(data.errors).forEach(field => {
                        const fieldId = field.toLowerCase();
                        showFieldError(fieldId, data.errors[field][0]);
                    });
                } else {
                    showAlert(data.message || 'Đăng ký thất bại. Vui lòng thử lại.', 'error');
                }

                submitBtn.disabled = false;
                submitBtn.classList.remove('loading');
                submitBtn.textContent = 'Đăng ký';
            }
        } catch (error) {
            console.error('Register error:', error);
            showAlert('Không thể kết nối đến server. Vui lòng thử lại sau.', 'error');
            submitBtn.disabled = false;
            submitBtn.classList.remove('loading');
            submitBtn.textContent = 'Đăng ký';
        }
    });
}


// ===== LOGOUT FUNCTION =====

function logout() {
    // Hiển thị confirm dialog
    const confirmLogout = confirm('Bạn có chắc chắn muốn đăng xuất?');

    if (!confirmLogout) {
        return; // Người dùng hủy
    }

    console.log('🚪 Logging out...');

    // 1. Disconnect WebSocket nếu có
    if (typeof disconnectWebSocket === 'function') {
        console.log('🔌 Disconnecting WebSocket...');
        disconnectWebSocket();
    }

    // 2. Clear tất cả data trong localStorage
    console.log('🗑️ Clearing localStorage...');
    localStorage.clear();

    // 3. Clear sessionStorage (nếu có)
    sessionStorage.clear();

    // 4. Redirect về login page
    console.log('🔄 Redirecting to login page...');
    window.location.href = '/Auth/Login';
}

// ===== GLOBAL EXPORT =====
// Đảm bảo hàm logout có thể gọi từ HTML
window.logout = logout;
window.initLoginPage = initLoginPage;
window.initRegisterPage = initRegisterPage;