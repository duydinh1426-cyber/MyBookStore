/**
 * chatbox.js  –  Floating AI Chatbox Widget
<<<<<<< HEAD
 * Lịch sử trò chuyện được lưu vào localStorage,
 * giữ nguyên khi refresh hay chuyển trang.
=======
 * Gọi API:  POST /api/chat
 * Body:     { history: [{role, text}], message: string }
 * Response: { reply: string }
>>>>>>> f84b213 ( new chatbox)
 */

(function () {
    const API_URL = 'https://localhost:7204/api/chat'; // ← đổi port nếu cần
<<<<<<< HEAD
    const STORAGE_KEY = 'bs_chat_history';   // key lưu history (API)
    const STORAGE_UI = 'bs_chat_ui';        // key lưu tin nhắn hiển thị
    const MAX_HISTORY = 40;                  // tối đa 20 lượt (40 entries)

    // ── LocalStorage helpers ──────────────────────────────────────
    function saveHistory(history) {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(history)); } catch { }
    }
    function loadHistory() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY)) || []; } catch { return []; }
    }
    function saveUIMessages(uiMessages) {
        try { localStorage.setItem(STORAGE_UI, JSON.stringify(uiMessages)); } catch { }
    }
    function loadUIMessages() {
        try { return JSON.parse(localStorage.getItem(STORAGE_UI)) || []; } catch { return []; }
    }
    function clearStorage() {
        localStorage.removeItem(STORAGE_KEY);
        localStorage.removeItem(STORAGE_UI);
    }
=======
>>>>>>> f84b213 ( new chatbox)

    function init() {
        // ── Inject HTML ───────────────────────────────────────────────
        document.body.insertAdjacentHTML('beforeend', `
        <button id="bs-chat-btn" title="Chat với AI">
            <i class="bi bi-stars"></i>
        </button>
        <div id="bs-chat-box" role="dialog" aria-label="Chatbox AI">
            <div id="bs-chat-header">
                <div class="avatar"><i class="bi bi-robot"></i></div>
                <div class="info">
                    <strong>Trợ lý AI BookStore</strong>
                    <span>Luôn sẵn sàng hỗ trợ bạn ✨</span>
                </div>
<<<<<<< HEAD
                <div style="display:flex;gap:6px;align-items:center">
                    <button id="bs-chat-clear" title="Xóa lịch sử" style="background:none;border:none;color:rgba(255,255,255,.7);cursor:pointer;font-size:.8rem;padding:2px 6px;border-radius:6px;" onmouseover="this.style.background='rgba(255,255,255,.15)'" onmouseout="this.style.background='none'">
                        <i class="bi bi-trash3"></i>
                    </button>
                    <button id="bs-chat-close" title="Đóng">✕</button>
                </div>
=======
                <button id="bs-chat-close" title="Đóng">✕</button>
>>>>>>> f84b213 ( new chatbox)
            </div>
            <div id="bs-chat-messages"></div>
            <div id="bs-chat-footer">
                <textarea id="bs-chat-input" rows="1"
                    placeholder="Nhập tin nhắn..." maxlength="2000"></textarea>
                <button id="bs-chat-send" title="Gửi">
                    <i class="bi bi-send-fill"></i>
                </button>
            </div>
        </div>
        `);

<<<<<<< HEAD
        // ── State — khôi phục từ localStorage ────────────────────────
        const history = loadHistory();
=======
        // ── State ─────────────────────────────────────────────────────
        const history = [];
>>>>>>> f84b213 ( new chatbox)
        let isLoading = false;

        // ── DOM refs ──────────────────────────────────────────────────
        const btn = document.getElementById('bs-chat-btn');
        const box = document.getElementById('bs-chat-box');
        const closeBtn = document.getElementById('bs-chat-close');
<<<<<<< HEAD
        const clearBtn = document.getElementById('bs-chat-clear');
=======
>>>>>>> f84b213 ( new chatbox)
        const messages = document.getElementById('bs-chat-messages');
        const input = document.getElementById('bs-chat-input');
        const send = document.getElementById('bs-chat-send');

<<<<<<< HEAD
        // ── Render lại tin nhắn cũ từ localStorage ────────────────────
        function restoreUI() {
            const saved = loadUIMessages();
            if (saved.length === 0) {
                addBotMessage('Xin chào! Tôi có thể giúp gì cho bạn hôm nay? 😊', false);
                return;
            }
            saved.forEach(m => addMessage(m.text, m.role, false)); // false = không lưu lại
            messages.scrollTop = messages.scrollHeight;
        }

        // ── Khôi phục UI ngay khi load trang ─────────────────────────
        restoreUI();

=======
>>>>>>> f84b213 ( new chatbox)
        // ── Toggle open/close ─────────────────────────────────────────
        function openChat() {
            box.classList.add('open');
            input.focus();
<<<<<<< HEAD
            messages.scrollTop = messages.scrollHeight;
=======
            if (messages.children.length === 0)
                addBotMessage('Xin chào! Tôi có thể giúp gì cho bạn hôm nay? 😊');
>>>>>>> f84b213 ( new chatbox)
        }
        function closeChat() { box.classList.remove('open'); }

        btn.addEventListener('click', () =>
            box.classList.contains('open') ? closeChat() : openChat());
        closeBtn.addEventListener('click', closeChat);

<<<<<<< HEAD
        // ── Xóa lịch sử ──────────────────────────────────────────────
        clearBtn.addEventListener('click', () => {
            if (!confirm('Xóa toàn bộ lịch sử trò chuyện?')) return;
            clearStorage();
            history.length = 0;
            messages.innerHTML = '';
            addBotMessage('Lịch sử đã được xóa. Tôi có thể giúp gì cho bạn? 😊', true);
        });

        // ── Add message ───────────────────────────────────────────────
        function addMessage(text, role, persist = true) {
=======
        // ── Add message ───────────────────────────────────────────────
        function addMessage(text, role) {
>>>>>>> f84b213 ( new chatbox)
            const el = document.createElement('div');
            el.className = `bs-msg ${role === 'user' ? 'user' : 'bot'}`;
            el.textContent = text;
            messages.appendChild(el);
            messages.scrollTop = messages.scrollHeight;
<<<<<<< HEAD

            // Lưu UI message vào localStorage
            if (persist) {
                const saved = loadUIMessages();
                saved.push({ role, text });
                // Giữ tối đa 60 tin nhắn hiển thị
                if (saved.length > 60) saved.splice(0, saved.length - 60);
                saveUIMessages(saved);
            }
            return el;
        }
        function addBotMessage(text, persist = true) { return addMessage(text, 'bot', persist); }
        function addUserMessage(text, persist = true) { return addMessage(text, 'user', persist); }
=======
            return el;
        }
        function addBotMessage(text) { return addMessage(text, 'bot'); }
        function addUserMessage(text) { return addMessage(text, 'user'); }
>>>>>>> f84b213 ( new chatbox)

        function showTyping() {
            const el = document.createElement('div');
            el.className = 'bs-msg bot typing';
            el.id = 'bs-typing';
            el.innerHTML = '<span></span><span></span><span></span>';
            messages.appendChild(el);
            messages.scrollTop = messages.scrollHeight;
        }
        function removeTyping() {
            const el = document.getElementById('bs-typing');
            if (el) el.remove();
        }

        // ── Send message ──────────────────────────────────────────────
        async function sendMessage() {
            const text = input.value.trim();
            if (!text || isLoading) return;

            isLoading = true;
            send.disabled = true;
            input.value = '';
            autoResizeInput();
            addUserMessage(text);
            showTyping();

            try {
                const res = await fetch(API_URL, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ history, message: text })
                });

                const data = await res.json();
                removeTyping();

                if (!res.ok) throw new Error(data.message || 'Lỗi không xác định');

<<<<<<< HEAD
                // Cập nhật history + lưu vào localStorage
                history.push({ role: 'user', text });
                history.push({ role: 'model', text: data.reply });
                if (history.length > MAX_HISTORY) history.splice(0, history.length - MAX_HISTORY);
                saveHistory(history);
=======
                history.push({ role: 'user', text });
                history.push({ role: 'model', text: data.reply });
                if (history.length > 40) history.splice(0, history.length - 40);
>>>>>>> f84b213 ( new chatbox)

                addBotMessage(data.reply);
            } catch (err) {
                removeTyping();
                addBotMessage('⚠️ Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau.');
                console.error('[Chatbox]', err);
            } finally {
                isLoading = false;
                send.disabled = false;
                input.focus();
            }
        }

        // ── Auto-resize textarea ──────────────────────────────────────
        function autoResizeInput() {
            input.style.height = 'auto';
            input.style.height = Math.min(input.scrollHeight, 80) + 'px';
        }

        input.addEventListener('input', autoResizeInput);
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
        send.addEventListener('click', sendMessage);
    }

<<<<<<< HEAD
=======
    // Đảm bảo DOM đã sẵn sàng trước khi chạy
>>>>>>> f84b213 ( new chatbox)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();