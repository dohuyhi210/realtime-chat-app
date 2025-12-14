// ==============================================
// FILE: wwwroot/js/chat.js
// Mô tả: Chat page logic (Controller trong JavaScript)
// ==============================================

// ===== STATE MANAGEMENT =====

// ===== STATE MANAGEMENT =====
let currentChatUser = null;
let users = [];
let groups = [];
let messages = {};
let typingTimeout = null;

// THÊM: State cho infinite scroll
let currentPage = 1;
let isLoadingMessages = false;
let hasMoreMessages = true;

// ===== INITIALIZATION =====

// Initialize chat page
async function initChatPage() {
    // Check authentication
    if (!requireAuth()) {
        return;
    }

    // Display current user info
    displayCurrentUser();

    // Load users và groups
    await loadUsers();
    await loadGroups();

    // ==== CHỈ GỌI initWebSocket - XÓA dòng connectWebSocket() ====
    if (typeof window.initWebSocket === 'function') {
        window.initWebSocket();
        console.log('✅ WebSocket initialized');
    } else {
        console.error('❌ initWebSocket function not found');
    }

    // Request notification permission
    requestNotificationPermission();

    // Setup event listeners
    setupEventListeners();
}


// Display current user info
function displayCurrentUser() {
    const user = getCurrentUser();

    // KIỂM TRA PHẦN TỬ TỒN TẠI TRƯỚC KHI THAO TÁC
    const nicknameEl = document.getElementById('currentUserNickname');
    const usernameEl = document.getElementById('currentUserUsername');
    const avatarEl = document.getElementById('currentUserAvatar');

    if (!user) {
        console.error('Current user not found');
        return;
    }

    if (nicknameEl) {
        nicknameEl.textContent = user.nickname || 'User';
    } else {
        console.warn('Element currentUserNickname not found');
    }

    if (usernameEl) {
        usernameEl.textContent = `@${user.username || 'user'}`;
    } else {
        console.warn('Element currentUserUsername not found');
    }

    if (avatarEl) {
        avatarEl.textContent = getAvatarText(user.nickname);
    } else {
        console.warn('Element currentUserAvatar not found');
    }
}

// ===== LOAD DATA =====

// Load danh sách users
async function loadUsers() {
    try {
        const response = await fetchAPI('/user');

        if (response && response.success) {
            users = response.users;
            renderUserList(users);
        }
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

// Load danh sách groups
async function loadGroups() {
    try {
        const response = await fetchAPI('/group');

        if (response && response.success) {
            groups = response.groups;
            renderGroupList(groups);
        }
    } catch (error) {
        console.error('Error loading groups:', error);
    }
}

// ===== RENDER UI =====

// Render danh sách users
function renderUserList(userList) {
    const container = document.getElementById('friendsList');

    if (userList.length === 0) {
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #65676b;">Chưa có bạn bè</p>';
        return;
    }

    container.innerHTML = userList.map(user => `
        <div class="user-item" data-user-id="${user.id}" onclick="openChat(${user.id}, '${escapeHtml(user.nickname)}', false)">
            <div class="user-item-avatar">
                ${getAvatarText(user.nickname)}
                <span class="online-indicator ${user.isOnline ? 'online' : 'offline'}"></span>
            </div>
            <div class="user-item-info">
                <h4 class="user-item-name">${escapeHtml(user.nickname)}</h4>
                <p class="user-item-status">${user.isOnline ? 'Đang hoạt động' : user.offlineTimeText}</p>
            </div>
            <span class="unread-badge" id="unread-${user.id}" style="display: none;">0</span>
        </div>
    `).join('');
}

// Render danh sách groups
function renderGroupList(groupList) {
    const container = document.getElementById('groupsList');

    if (groupList.length === 0) {
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #65676b;">Chưa có nhóm</p>';
        return;
    }

    container.innerHTML = groupList.map(group => `
        <div class="user-item" data-group-id="${group.id}" onclick="openChat(${group.id}, '${escapeHtml(group.groupName)}', true)">
            <div class="user-item-avatar">
                ${getAvatarText(group.groupName)}
            </div>
            <div class="user-item-info">
                <h4 class="user-item-name">${escapeHtml(group.groupName)}</h4>
                <p class="user-item-status">${group.memberCount} thành viên</p>
            </div>
            <span class="unread-badge" id="unread-group-${group.id}" style="display: none;">0</span>
        </div>
    `).join('');
}

// ===== OPEN CHAT =====

// Mở chat với user hoặc group
async function openChat(id, name, isGroup) {
    // Save current chat
    currentChatUser = { id, nickname: name, isGroup };

    // Update UI: active state
    document.querySelectorAll('.user-item').forEach(item => {
        item.classList.remove('active');
    });

    const selector = isGroup ? `[data-group-id="${id}"]` : `[data-user-id="${id}"]`;
    document.querySelector(selector)?.classList.add('active');

    // Hide welcome screen, show chat area
    document.getElementById('welcomeScreen').style.display = 'none';
    document.getElementById('chatArea').style.display = 'flex';

    // Update chat header
    document.getElementById('chatHeaderAvatar').textContent = getAvatarText(name);
    document.getElementById('chatHeaderName').textContent = name;

    if (!isGroup) {
        const user = users.find(u => u.id === id);
        document.getElementById('chatHeaderStatus').textContent = user?.isOnline ? 'Đang hoạt động' : user?.offlineTimeText || 'Offline';
    } else {
        const group = groups.find(g => g.id === id);
        document.getElementById('chatHeaderStatus').textContent = `${group?.memberCount || 0} thành viên`;
    }

    // Load chat history
    await loadChatHistory(id, isGroup);

    // Mark messages as read
    if (!isGroup) {
        sendMarkRead(id);
        updateUnreadBadge(id, 0);
    }

    // Focus message input
    document.getElementById('messageInput').focus();
}

// ===== LOAD CHAT HISTORY =====

// Load lịch sử chat với phân trang
async function loadChatHistory(id, isGroup, loadMore = false) {
    if (isLoadingMessages) return;

    isLoadingMessages = true;

    if (loadMore) {
        showLoadingIndicator(true);
    }

    const endpoint = isGroup ? `/message/group/${id}` : `/message/private/${id}`;

    if (loadMore) {
        currentPage++;
    } else {
        currentPage = 1;
        hasMoreMessages = true;
    }

    try {
        const response = await fetchAPI(`${endpoint}?page=${currentPage}&pageSize=50`);

        if (response && response.success) {
            const { messages: newMessages, pagination } = response;

            hasMoreMessages = pagination.hasNextPage;

            console.log('📨 API messages (already in correct order by server):');
            newMessages.forEach((msg, i) => {
                console.log(`  ${i}: ${new Date(msg.timestamp).toLocaleTimeString()} - "${msg.content}"`);
            });

            if (loadMore) {
                // 🔥 SỬA: Thêm tin nhắn cũ vào ĐẦU mảng (giữ đúng thứ tự thời gian)
                messages[id] = [...newMessages, ...messages[id]];
                prependMessages(newMessages);
            } else {
                // LOAD MỚI: thay thế toàn bộ messages
                messages[id] = newMessages;
                renderMessages(messages[id]);
            }

            console.log(`📄 Loaded ${newMessages.length} messages`);
        }
    } catch (error) {
        console.error('Error loading chat history:', error);
        if (loadMore) currentPage--;
    } finally {
        isLoadingMessages = false;
        showLoadingIndicator(false);
    }
}
// ===== RENDER MESSAGES =====

// Render messages - ĐẢM BẢO CŨ TRÊN → MỚI DƯỚI
function renderMessages(messageList) {
    const container = document.getElementById('messagesContainer');
    const currentUser = getCurrentUser();

    if (messageList.length === 0) {
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #65676b;">Chưa có tin nhắn</p>';

        // Vẫn scroll xuống dưới cùng
        setTimeout(() => {
            container.scrollTop = container.scrollHeight;
        }, 50);
        return;
    }

    let html = '';
    let lastDate = null;

    // RENDER THEO ĐÚNG THỨ TỰ TỪ SERVER (CŨ → MỚI)
    messageList.forEach(msg => {
        // Date divider
        const msgDate = formatDateDivider(msg.timestamp);
        if (msgDate !== lastDate) {
            html += `
                <div class="date-divider">
                    <span>${msgDate}</span>
                </div>
            `;
            lastDate = msgDate;
        }

        // Message
        const isSent = msg.senderId === currentUser.id;

        html += `
            <div class="message ${isSent ? 'sent' : 'received'}">
                ${!isSent ? `<div class="message-avatar">${getAvatarText(msg.senderNickname)}</div>` : ''}
                <div class="message-content">
                    ${!isSent && currentChatUser?.isGroup ? `<div class="message-sender">${escapeHtml(msg.senderNickname)}</div>` : ''}
                    <div class="message-bubble">${escapeHtml(msg.content)}</div>
                    <div class="message-time">${formatTime(msg.timestamp)}</div>
                </div>
                ${isSent ? `<div class="message-avatar">${getAvatarText(currentUser.nickname)}</div>` : ''}
            </div>
        `;
    });

    container.innerHTML = html;

    // 🔥 SCROLL XUỐNG TIN NHẮN MỚI NHẤT (DƯỚI CÙNG)
    setTimeout(() => {
        console.log('⬇️ Scrolling to newest message...');
        container.scrollTop = container.scrollHeight;

        // Đảm bảo scroll thành công
        setTimeout(() => {
            if (container.scrollTop < container.scrollHeight - container.clientHeight - 10) {
                console.log('🔄 Re-scrolling to ensure bottom...');
                container.scrollTop = container.scrollHeight;
            }
        }, 200);
    }, 100);
}

// Thêm messages vào đầu chat (cho load more - tin nhắn cũ hơn)
// Thêm messages vào đầu chat (cho load more - tin nhắn cũ hơn)
function prependMessages(messageList) {
    const container = document.getElementById('messagesContainer');
    const currentUser = getCurrentUser();

    if (messageList.length === 0) return;

    // 🔥 QUAN TRỌNG: Đảo ngược mảng để tin nhắn cũ nhất lên đầu
    const reversedMessages = [...messageList].reverse();

    let html = '';
    let lastDate = null;

    // THÊM TIN NHẮN CŨ HƠN VÀO ĐẦU (theo thứ tự đúng)
    reversedMessages.forEach(msg => {
        // Date divider
        const msgDate = formatDateDivider(msg.timestamp);
        if (msgDate !== lastDate) {
            html = `
                <div class="date-divider">
                    <span>${msgDate}</span>
                </div>
            ` + html;
            lastDate = msgDate;
        }

        // Message
        const isSent = msg.senderId === currentUser.id;

        const messageHtml = `
            <div class="message ${isSent ? 'sent' : 'received'}">
                ${!isSent ? `<div class="message-avatar">${getAvatarText(msg.senderNickname)}</div>` : ''}
                <div class="message-content">
                    ${!isSent && currentChatUser?.isGroup ? `<div class="message-sender">${escapeHtml(msg.senderNickname)}</div>` : ''}
                    <div class="message-bubble">${escapeHtml(msg.content)}</div>
                    <div class="message-time">${formatTime(msg.timestamp)}</div>
                </div>
                ${isSent ? `<div class="message-avatar">${getAvatarText(currentUser.nickname)}</div>` : ''}
            </div>
        `;

        html = messageHtml + html;
    });

    // Lưu scroll position trước khi thêm messages
    const oldScrollHeight = container.scrollHeight;
    const oldScrollTop = container.scrollTop;

    // Thêm messages vào đầu
    container.insertAdjacentHTML('afterbegin', html);

    // Giữ nguyên vị trí scroll sau khi thêm messages
    const newScrollHeight = container.scrollHeight;
    container.scrollTop = oldScrollTop + (newScrollHeight - oldScrollHeight);
}

// ===== SEND MESSAGE =====

// Gửi tin nhắn
async function sendMessage() {
    if (!currentChatUser) {
        return;
    }

    const input = document.getElementById('messageInput');
    const content = input.value.trim();

    // Validation
    const validation = validateMessage(content);
    if (!validation.valid) {
        alert(validation.error);
        return;
    }

    // Send via WebSocket
    let success;
    if (currentChatUser.isGroup) {
        success = sendGroupMessage(currentChatUser.id, content);
    } else {
        success = sendPrivateMessage(currentChatUser.id, content);
    }

    if (success) {
        // Clear input
        input.value = '';
        input.style.height = 'auto';
    } else {
        alert('Không thể gửi tin nhắn. Vui lòng kiểm tra kết nối.');
    }
}

// ===== WEBSOCKET CALLBACKS =====

// Callback: WebSocket connected
function onWebSocketConnected() {
    console.log('WebSocket connected. Ready to chat!');
}

// Callback: Private message received
// Callback: Private message received
function onPrivateMessageReceived(data) {
    console.log('🎯 onPrivateMessageReceived called:', data);

    const currentUser = getCurrentUser();
    console.log('👤 Current user ID:', currentUser?.id);

    // Thêm message vào cache
    const chatId = data.SenderId === currentUser.id ? data.ReceiverId : data.SenderId;
    console.log('💬 Chat ID for message:', chatId);
    console.log('📱 Current chat user:', currentChatUser);

    if (!messages[chatId]) {
        messages[chatId] = [];
    }
    messages[chatId].push(data);

    // QUAN TRỌNG: Nếu đang mở chat này → append message ngay lập tức
    if (currentChatUser && currentChatUser.id === chatId && !currentChatUser.isGroup) {
        console.log('📱 Appending message to OPEN chat');
        appendMessage(data);

        // Mark as read
        if (data.SenderId !== currentUser.id) {
            sendMarkRead(data.SenderId);
        }
    } else {
        console.log('📬 Message received but chat is NOT open');
        // Nếu không mở chat này → update unread badge
        if (data.SenderId !== currentUser.id) {
            console.log('🔄 Updating unread badge');
            incrementUnreadBadge(data.SenderId);

            // Show notification
            showNotification(data.SenderNickname, data.content);
            playNotificationSound();
        }
    }
}

// Callback: Group message received
function onGroupMessageReceived(data) {
    const currentUser = getCurrentUser();

    // Thêm message vào cache
    if (!messages[data.groupId]) {
        messages[data.groupId] = [];
    }
    messages[data.groupId].push(data);

    // Nếu đang mở chat nhóm này → render message
    if (currentChatUser && currentChatUser.id === data.groupId && currentChatUser.isGroup) {
        appendMessage(data);
    } else {
        // Nếu không mở → update unread badge
        if (data.senderId !== currentUser.id) {
            incrementUnreadBadge(data.groupId, true);

            // Show notification
            showNotification(`${data.groupName}: ${data.senderNickname}`, data.content);
            playNotificationSound();
        }
    }
}

// Append message to chat (realtime)
// Append message to chat (realtime)
function appendMessage(data) {
    console.log('📝 appendMessage called with:', data);

    const container = document.getElementById('messagesContainer');
    if (!container) {
        console.error('❌ messagesContainer not found!');
        return;
    }

    const currentUser = getCurrentUser();
    const isSent = data.SenderId === currentUser.id;
    const wasAtBottom = isAtBottom(container);

    console.log('🖊️ Creating message HTML...');
    const messageHtml = `
        <div class="message ${isSent ? 'sent' : 'received'}">
            ${!isSent ? `<div class="message-avatar">${getAvatarText(data.SenderNickname)}</div>` : ''}
            <div class="message-content">
                ${!isSent && currentChatUser?.isGroup ? `<div class="message-sender">${escapeHtml(data.SenderNickname)}</div>` : ''}
                <div class="message-bubble">${escapeHtml(data.Content || data.content)}</div>
                <div class="message-time">${formatTime(data.Timestamp || data.timestamp)}</div>
            </div>
            ${isSent ? `<div class="message-avatar">${getAvatarText(currentUser.nickname)}</div>` : ''}
        </div>
    `;

    console.log('📤 Inserting message into container...');
    container.insertAdjacentHTML('beforeend', messageHtml);

    // Auto scroll nếu đang ở dưới cùng
    if (wasAtBottom) {
        console.log('⬇️ Scrolling to bottom...');
        smoothScrollToBottom(container);
    }

    console.log('✅ Message appended successfully');
}

// Callback: User status changed
function onUserStatusChanged(userId, isOnline) {
    // Update user list
    const user = users.find(u => u.id === userId);
    if (user) {
        user.isOnline = isOnline;

        // Update UI
        const userItem = document.querySelector(`[data-user-id="${userId}"]`);
        if (userItem) {
            const indicator = userItem.querySelector('.online-indicator');
            indicator.className = `online-indicator ${isOnline ? 'online' : 'offline'}`;

            const status = userItem.querySelector('.user-item-status');
            status.textContent = isOnline ? 'Đang hoạt động' : 'Offline vừa xong';
        }

        // Update chat header nếu đang chat với user này
        if (currentChatUser && currentChatUser.id === userId && !currentChatUser.isGroup) {
            document.getElementById('chatHeaderStatus').textContent = isOnline ? 'Đang hoạt động' : 'Offline vừa xong';
        }
    }
}

// Callback: Typing indicator
function onTypingIndicator(data) {
    // Chỉ hiển thị nếu đang chat với user này
    if (!currentChatUser) return;

    if (data.receiverId && currentChatUser.id === data.userId && !currentChatUser.isGroup) {
        showTypingIndicator(data.isTyping);
    } else if (data.groupId && currentChatUser.id === data.groupId && currentChatUser.isGroup) {
        showTypingIndicator(data.isTyping);
    }
}

// Show/hide typing indicator
function showTypingIndicator(show) {
    const indicator = document.getElementById('typingIndicator');
    indicator.style.display = show ? 'flex' : 'none';
}

// ===== TYPING INDICATOR (SEND) =====

// Handle typing in input
function handleTyping() {
    if (!currentChatUser) return;

    // Send typing indicator
    if (currentChatUser.isGroup) {
        sendTypingIndicator(null, currentChatUser.id, true);
    } else {
        sendTypingIndicator(currentChatUser.id, null, true);
    }

    // Clear previous timeout
    clearTimeout(typingTimeout);

    // Stop typing after 2 seconds
    typingTimeout = setTimeout(() => {
        if (currentChatUser.isGroup) {
            sendTypingIndicator(null, currentChatUser.id, false);
        } else {
            sendTypingIndicator(currentChatUser.id, null, false);
        }
    }, 2000);
}

// ===== UNREAD BADGES =====

// Update unread badge
function updateUnreadBadge(id, count) {
    const badge = document.getElementById(`unread-${id}`);
    if (badge) {
        if (count > 0) {
            badge.textContent = count;
            badge.style.display = 'block';
        } else {
            badge.style.display = 'none';
        }
    }
}

// Increment unread badge
function incrementUnreadBadge(id, isGroup = false) {
    const badgeId = isGroup ? `unread-group-${id}` : `unread-${id}`;
    const badge = document.getElementById(badgeId);

    if (badge) {
        const current = parseInt(badge.textContent) || 0;
        badge.textContent = current + 1;
        badge.style.display = 'block';
    }
}

// ===== EVENT LISTENERS =====

// Setup event listeners
function setupEventListeners() {
    // Message input: Enter to send
    const input = document.getElementById('messageInput');
    input.addEventListener('keydown', handleMessageInputKeydown);

    // Auto resize textarea
    input.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = (this.scrollHeight) + 'px';
    });
    // Infinite scroll
    const messagesContainer = document.getElementById('messagesContainer');
    if (messagesContainer) {
        messagesContainer.addEventListener('scroll', handleChatScroll);
    }
}

// Handle scroll để load more messages
function handleChatScroll(event) {
    const container = event.target;
    const scrollTop = container.scrollTop;

    // Nếu scroll lên gần đầu (100px) và còn messages để load
    if (scrollTop < 100 &&
        !isLoadingMessages &&
        hasMoreMessages &&
        currentChatUser) {

        console.log('🔄 Loading more messages...');
        loadChatHistory(currentChatUser.id, currentChatUser.isGroup, true);
    }
}


// ===== TABS =====

// Switch between Friends/Groups tabs
function switchTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');

    // Show/hide lists
    if (tabName === 'friends') {
        document.getElementById('friendsList').style.display = 'block';
        document.getElementById('groupsList').style.display = 'none';
    } else {
        document.getElementById('friendsList').style.display = 'none';
        document.getElementById('groupsList').style.display = 'block';
    }
}

// ===== SEARCH =====

// Filter users by search input
function filterUsers() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();

    document.querySelectorAll('.user-item').forEach(item => {
        const name = item.querySelector('.user-item-name').textContent.toLowerCase();
        item.style.display = name.includes(searchTerm) ? 'flex' : 'none';
    });
}

// ===== DEBUG & TESTING =====

// Test realtime functions - gõ testRealtime() trong console
window.testRealtime = function () {
    console.log('🧪 TESTING REALTIME FUNCTIONS...');

    // Kiểm tra WebSocket
    console.log('🔌 WebSocket state:', socket?.readyState,
        '(0=CONNECTING, 1=OPEN, 2=CLOSING, 3=CLOSED)');

    // Kiểm tra current user
    const currentUser = getCurrentUser();
    console.log('👤 Current user:', currentUser);

    // Kiểm tra functions
    console.log('📡 WebSocket functions:', {
        initWebSocket: typeof window.initWebSocket,
        sendPrivateMessage: typeof window.sendPrivateMessage,
        sendGroupMessage: typeof window.sendGroupMessage,
        onPrivateMessageReceived: typeof onPrivateMessageReceived,
        onGroupMessageReceived: typeof onGroupMessageReceived
    });

    // Kiểm tra data
    console.log('📊 Data:', {
        users: users.length,
        groups: groups.length,
        currentChat: currentChatUser,
        messages: Object.keys(messages).length
    });

    // Test send message
    if (users.length > 0 && currentUser) {
        const otherUser = users.find(u => u.id !== currentUser.id);
        if (otherUser) {
            console.log('💡 Test command: sendPrivateMessage(' + otherUser.id + ', "Test message")');
        }
    }

    console.log('✅ Test completed');
};

// Handle Enter key in message input
function handleMessageInputKeydown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();

        const input = document.getElementById('messageInput');
        const content = input.value;

        console.log('↵ Enter pressed');
        console.log('📝 Raw input value:', `"${input.value}"`);
        console.log('✂️ Trimmed content:', `"${content.trim()}"`);
        console.log('🔢 Raw length:', input.value.length);
        console.log('🔢 Trimmed length:', content.trim().length);

        sendMessage();
    }
}

// Thêm vào renderMessages hoặc prependMessages
function showLoadingIndicator(show) {
    let loader = document.getElementById('messagesLoading');

    if (show && !loader) {
        loader = document.createElement('div');
        loader.id = 'messagesLoading';
        loader.className = 'loading-indicator';
        loader.innerHTML = '<div class="loading-spinner"></div><span>Đang tải tin nhắn cũ...</span>';

        const container = document.getElementById('messagesContainer');
        container.insertBefore(loader, container.firstChild);
    } else if (!show && loader) {
        loader.remove();
    }
}