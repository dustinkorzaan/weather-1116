(() => {
  const tabs = document.querySelectorAll('.chat-tab');
  const descriptions = document.querySelectorAll('.chat-tab-description');
  const messagesEl = document.getElementById('chat-messages');
  const form = document.getElementById('chat-form');
  const input = document.getElementById('chat-input');
  const sendButton = document.getElementById('chat-send');

  let activeTab = 'Chat1a';
  const sessions = {
    Chat1a: null,
    Chat1b: null,
    Chat2a: null,
    Chat2b: null,
  };

  function setActiveTab(tabId) {
    activeTab = tabId;
    tabs.forEach((tab) => {
      const isActive = tab.dataset.chatTab === tabId;
      tab.classList.toggle('active', isActive);
      tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });
    descriptions.forEach((desc) => {
      desc.classList.toggle('active', desc.dataset.chatDescription === tabId);
    });
    renderMessages();
  }

  function renderMessages() {
    messagesEl.innerHTML = '';
    const history = window.chatHistory?.[activeTab] ?? [];
    history.forEach((entry) => appendMessage(entry.role, entry.content, entry.toolStatus));
  }

  function appendMessage(role, content, toolStatus) {
    const item = document.createElement('div');
    item.className = `chat-message chat-message-${role}`;
    if (toolStatus) {
      item.textContent = toolStatus;
    } else {
      item.textContent = content;
    }
    messagesEl.appendChild(item);
    messagesEl.scrollTop = messagesEl.scrollHeight;
    return item;
  }

  window.chatHistory = window.chatHistory || {
    Chat1a: [],
    Chat1b: [],
    Chat2a: [],
    Chat2b: [],
  };

  tabs.forEach((tab) => {
    tab.addEventListener('click', () => setActiveTab(tab.dataset.chatTab));
  });

  async function streamChat(message) {
    const endpoint = `/${activeTab}/Messages`;
    const response = await fetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId: sessions[activeTab], message }),
    });

    if (!response.ok || !response.body) {
      throw new Error(`Chat request failed (${response.status})`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let assistantItem = null;
    let assistantText = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const parts = buffer.split('\n\n');
      buffer = parts.pop() ?? '';

      for (const part of parts) {
        const line = part.trim();
        if (!line.startsWith('data:')) continue;
        const payload = JSON.parse(line.slice(5).trim());

        if (payload.type === 'session' && payload.sessionId) {
          sessions[activeTab] = payload.sessionId;
        } else if (payload.type === 'token' && payload.text) {
          if (!assistantItem) {
            assistantItem = appendMessage('assistant', '');
          }
          assistantText += payload.text;
          assistantItem.textContent = assistantText;
          messagesEl.scrollTop = messagesEl.scrollHeight;
        } else if (payload.type === 'tool_start' && payload.toolName) {
          appendMessage('tool', '', `Running ${payload.toolName}…`);
        } else if (payload.type === 'error' && payload.errorMessage) {
          appendMessage('error', payload.errorMessage);
        }
      }
    }

    if (assistantText) {
      window.chatHistory[activeTab].push({ role: 'assistant', content: assistantText });
    }
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message) return;

    input.value = '';
    sendButton.disabled = true;

    window.chatHistory[activeTab].push({ role: 'user', content: message });
    appendMessage('user', message);

    try {
      await streamChat(message);
    } catch (error) {
      appendMessage('error', error.message || 'Chat failed.');
    } finally {
      sendButton.disabled = false;
      input.focus();
    }
  });
})();
