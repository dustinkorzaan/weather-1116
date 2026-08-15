(() => {
  const tabs = document.querySelectorAll('.chat-tab');
  const descriptions = document.querySelectorAll('.chat-tab-description');
  const messagesEl = document.getElementById('chat-messages');
  const form = document.getElementById('chat-form');
  const input = document.getElementById('chat-input');
  const sendButton = document.getElementById('chat-send');

  const TAB_ACTIVE_CLASSES = ['border-blue-700', 'bg-blue-700', 'text-white'];
  const TAB_INACTIVE_CLASSES = ['border-gray-300', 'bg-white', 'text-gray-800'];
  const MESSAGE_CLASSES = {
    user: 'self-end max-w-[85%] whitespace-pre-wrap rounded-2xl bg-blue-600 px-3 py-2 text-white',
    assistant: 'self-start max-w-[85%] whitespace-pre-wrap rounded-2xl border bg-gray-100 px-3 py-2 text-gray-900',
    tool: 'self-center max-w-[85%] rounded-full bg-amber-100 px-3 py-1 text-sm text-amber-900',
    error: 'w-full rounded-md bg-red-100 px-3 py-2 text-red-800',
  };

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

  const sendingTabs = {
    Chat1a: false,
    Chat1b: false,
    Chat2a: false,
    Chat2b: false,
  };

  function updateSendingControls() {
    const isSending = sendingTabs[activeTab];
    sendButton.disabled = isSending;
    sendButton.textContent = isSending ? 'Sending…' : 'Send';
    input.disabled = isSending;
  }

  function setActiveTab(tabId) {
    activeTab = tabId;
    tabs.forEach((tab) => {
      const isActive = tab.dataset.chatTab === tabId;
      tab.classList.remove(...(isActive ? TAB_INACTIVE_CLASSES : TAB_ACTIVE_CLASSES));
      tab.classList.add(...(isActive ? TAB_ACTIVE_CLASSES : TAB_INACTIVE_CLASSES));
      tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });
    descriptions.forEach((desc) => {
      desc.classList.toggle('hidden', desc.dataset.chatDescription !== tabId);
    });
    renderMessages();
    updateSendingControls();
  }

  function renderMessages() {
    messagesEl.innerHTML = '';
    const history = window.chatHistory[activeTab] ?? [];
    history.forEach((entry) => renderEntry(entry));
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function renderEntry(entry) {
    const item = document.createElement('div');
    item.className = MESSAGE_CLASSES[entry.role] ?? MESSAGE_CLASSES.assistant;
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

  input.addEventListener('keydown', (event) => {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing && event.keyCode !== 229) {
      event.preventDefault();
      form.requestSubmit();
    }
  });

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message || sendingTabs[activeTab]) return;

    // The active tab can change while the stream is in flight.
    const tabId = activeTab;

    input.value = '';
    sendingTabs[tabId] = true;
    updateSendingControls();

    addEntry(tabId, { role: 'user', content: message });

    try {
      await streamChat(tabId, message);
    } catch (error) {
      addEntry(tabId, { role: 'error', content: error.message || 'Chat failed.' });
    } finally {
      sendingTabs[tabId] = false;
      updateSendingControls();
      input.focus();
    }
  });
})();
