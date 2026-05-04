/**
 * chatbox.js  –  Floating AI Chatbox Widget
 * Gọi API:  POST /api/chat
 * Body:     { history: [{role, text}], message: string }
 * Response: { reply: string }
 */

(function () {
    const API_URL = 'https://localhost:7204/api/chat'; // ← đổi port nếu cần

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
                <button id="bs-chat-close" title="Đóng">✕</button>
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

        // ── State ─────────────────────────────────────────────────────
        const history = [];
        let isLoading = false;

        // ── DOM refs ──────────────────────────────────────────────────
        const btn = document.getElementById('bs-chat-btn');
        const box = document.getElementById('bs-chat-box');
        const closeBtn = document.getElementById('bs-chat-close');
        const messages = document.getElementById('bs-chat-messages');
        const input = document.getElementById('bs-chat-input');
        const send = document.getElementById('bs-chat-send');

        // ── Toggle open/close ─────────────────────────────────────────
        function openChat() {
            box.classList.add('open');
            input.focus();
            if (messages.children.length === 0)
                addBotMessage('Xin chào! Tôi có thể giúp gì cho bạn hôm nay? 😊');
        }
        function closeChat() { box.classList.remove('open'); }

        btn.addEventListener('click', () =>
            box.classList.contains('open') ? closeChat() : openChat());
        closeBtn.addEventListener('click', closeChat);

        // ── Add message ───────────────────────────────────────────────
        function addMessage(text, role) {
            const el = document.createElement('div');
            el.className = `bs-msg ${role === 'user' ? 'user' : 'bot'}`;
            el.textContent = text;
            messages.appendChild(el);
            messages.scrollTop = messages.scrollHeight;
            return el;
        }
        function addBotMessage(text) { return addMessage(text, 'bot'); }
        function addUserMessage(text) { return addMessage(text, 'user'); }

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

                history.push({ role: 'user', text });
                history.push({ role: 'model', text: data.reply });
                if (history.length > 40) history.splice(0, history.length - 40);

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

    // Đảm bảo DOM đã sẵn sàng trước khi chạy
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();