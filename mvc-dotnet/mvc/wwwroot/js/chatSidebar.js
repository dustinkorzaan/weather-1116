(() => {
  const button = document.getElementById('chatSidebarButton');
  const sidebar = document.getElementById('chat2a-sidebar');
  const closeButton = document.getElementById('chat2a-sidebar-close');
  const messagesEl = document.getElementById('chat2a-sidebar-messages');
  const form = document.getElementById('chat2a-sidebar-form');
  const input = document.getElementById('chat2a-sidebar-input');
  const sendButton = document.getElementById('chat2a-sidebar-send');

  if (!button || !sidebar || !closeButton || !messagesEl || !form || !input || !sendButton) {
    return;
  }

  const MESSAGE_ROLES = ['user', 'assistant', 'tool', 'error'];

  // The sidebar keeps its own Chat2a session, independent of the /chat-clients Chat2a tab.
  let sessionId = null;
  let isSending = false;

  function setOpen(open) {
    sidebar.hidden = !open;
    button.setAttribute('aria-expanded', open ? 'true' : 'false');
    if (open) {
      input.focus();
    }
  }

  button.addEventListener('click', () => setOpen(sidebar.hidden));
  closeButton.addEventListener('click', () => {
    setOpen(false);
    button.focus();
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && !sidebar.hidden) {
      setOpen(false);
    }
  });

  function scrollToBottom() {
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  // Plain text only: assistant markdown is not rendered in the sidebar.
  function addMessage(role, content) {
    const item = document.createElement('div');
    item.className = `chat-message ${MESSAGE_ROLES.includes(role) ? role : 'assistant'}`;
    item.textContent = content;
    messagesEl.appendChild(item);
    scrollToBottom();
    return item;
  }

  function findLastRunningTool(toolName) {
    const items = messagesEl.querySelectorAll('.chat-message.tool[data-running="true"]');
    for (let index = items.length - 1; index >= 0; index -= 1) {
      if (items[index].dataset.toolName === toolName) {
        return items[index];
      }
    }

    return null;
  }

  function updateSendingControls() {
    sendButton.disabled = isSending;
    sendButton.textContent = isSending ? 'Sending…' : 'Send';
    input.disabled = isSending;
  }

  // Re-read /User after every completion so the map's pins match the agent's changes.
  // weatherMap.js only loads on Home; elsewhere a plain GET keeps one refetch per send.
  function refreshCities() {
    if (window.weatherMap && typeof window.weatherMap.refreshCities === 'function') {
      window.weatherMap.refreshCities().catch(() => {});
    } else {
      fetch('/User', { headers: { Accept: 'application/json' } }).catch(() => {});
    }
  }

  async function streamChat(message) {
    const response = await fetch('/Chat2a/messages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId, message }),
    });

    if (!response.ok || !response.body) {
      throw new Error(`Chat request failed (${response.status})`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let assistantEl = null;
    let assistantText = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const parts = buffer.split('\n\n');
      buffer = parts.pop() ?? '';

      for (const part of parts) {
        const line = part.trim();
        if (!line.startsWith('data:')) continue;
        const payload = JSON.parse(line.slice(5).trim());

        if (payload.type === 'session' && payload.sessionId) {
          sessionId = payload.sessionId;
        } else if (payload.type === 'token' && payload.text) {
          assistantText += payload.text;
          if (!assistantEl) {
            assistantEl = addMessage('assistant', assistantText);
          } else {
            assistantEl.textContent = assistantText;
            scrollToBottom();
          }
        } else if (payload.type === 'tool_start' && payload.toolName) {
          const toolEl = addMessage('tool', `Running ${payload.toolName} …`);
          toolEl.dataset.toolName = payload.toolName;
          toolEl.dataset.running = 'true';
        } else if (payload.type === 'tool_end' && payload.toolName) {
          const pending = findLastRunningTool(payload.toolName);
          if (pending) {
            pending.dataset.running = 'false';
            pending.textContent = `Ran ${payload.toolName} …`;
          }
        } else if (payload.type === 'error' && payload.errorMessage) {
          addMessage('error', payload.errorMessage);
        } else if (payload.type === 'done') {
          scrollToBottom();
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
    if (!message || isSending) return;

    input.value = '';
    isSending = true;
    updateSendingControls();
    addMessage('user', message);

    try {
      await streamChat(message);
    } catch (error) {
      addMessage('error', error.message || 'Chat failed.');
    } finally {
      isSending = false;
      updateSendingControls();
      refreshCities();
      input.focus();
    }
  });
})();
