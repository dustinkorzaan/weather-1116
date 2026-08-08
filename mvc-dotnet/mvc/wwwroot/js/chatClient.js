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

  window.chatHistory = window.chatHistory || {
    Chat1a: [],
    Chat1b: [],
    Chat2a: [],
    Chat2b: [],
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
    const history = window.chatHistory[activeTab] ?? [];
    history.forEach((entry) => renderEntry(entry));
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function renderEntry(entry) {
    const item = document.createElement('div');
    item.className = `chat-message chat-message-${entry.role}`;
    item.textContent = entry.content;
    messagesEl.appendChild(item);
    return item;
  }

  // History is the single source of truth so tab switches never lose messages.
  function addEntry(tabId, entry) {
    window.chatHistory[tabId].push(entry);
    if (tabId === activeTab) {
      renderEntry(entry);
      messagesEl.scrollTop = messagesEl.scrollHeight;
    }
    return entry;
  }

  function updateEntry(tabId, entry, content) {
    entry.content = content;
    if (tabId === activeTab) {
      renderMessages();
    }
  }

  tabs.forEach((tab) => {
    tab.addEventListener('click', () => setActiveTab(tab.dataset.chatTab));
  });

  function findLastRunningTool(history, toolName) {
    for (let index = history.length - 1; index >= 0; index -= 1) {
      const entry = history[index];
      if (entry.role === 'tool' && entry.running && entry.toolName === toolName) {
        return entry;
      }
    }

    return null;
  }

  async function streamChat(tabId, message) {
    const response = await fetch(`/${tabId}/Messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId: sessions[tabId], message }),
    });

    if (!response.ok || !response.body) {
      throw new Error(`Chat request failed (${response.status})`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let assistantEntry = null;
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
          sessions[tabId] = payload.sessionId;
        } else if (payload.type === 'token' && payload.text) {
          assistantText += payload.text;
          if (!assistantEntry) {
            assistantEntry = addEntry(tabId, { role: 'assistant', content: assistantText });
          } else {
            updateEntry(tabId, assistantEntry, assistantText);
          }
        } else if (payload.type === 'tool_start' && payload.toolName) {
          addEntry(tabId, {
            role: 'tool',
            content: `Running ${payload.toolName}…`,
            toolName: payload.toolName,
            running: true,
          });
        } else if (payload.type === 'tool_end' && payload.toolName) {
          const history = window.chatHistory[tabId];
          const pending = findLastRunningTool(history, payload.toolName);
          if (pending) {
            pending.running = false;
            updateEntry(tabId, pending, `Ran ${payload.toolName}`);
          }
        } else if (payload.type === 'error' && payload.errorMessage) {
          addEntry(tabId, { role: 'error', content: payload.errorMessage });
        }
      }
    }
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message) return;

    // The active tab can change while the stream is in flight.
    const tabId = activeTab;

    input.value = '';
    sendButton.disabled = true;

    addEntry(tabId, { role: 'user', content: message });

    try {
      await streamChat(tabId, message);
    } catch (error) {
      addEntry(tabId, { role: 'error', content: error.message || 'Chat failed.' });
    } finally {
      sendButton.disabled = false;
      input.focus();
    }
  });
})();
