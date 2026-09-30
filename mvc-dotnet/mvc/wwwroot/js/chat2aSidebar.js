// Right-hand Chat2a sidebar on the map page. Streams to /Chat2a/Messages and, after
// every completed turn, calls weatherMap.refreshCities so cities the agent added or
// deleted show up on the map.
(() => {
  const toggle = document.getElementById('chatToggleButton');
  const sidebar = document.getElementById('chat2a-sidebar');
  if (!toggle || !sidebar) return;

  const closeButton = document.getElementById('chat2a-sidebar-close');
  const messagesEl = document.getElementById('chat2a-sidebar-messages');
  const form = document.getElementById('chat2a-sidebar-form');
  const input = document.getElementById('chat2a-sidebar-input');
  const sendButton = document.getElementById('chat2a-sidebar-send');
  const MESSAGE_ROLES = ['user', 'assistant', 'tool', 'error', 'blocked'];

  const history = [];
  let sessionId = null;
  let isSending = false;

  function setOpen(open) {
    sidebar.hidden = !open;
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    if (open) {
      messagesEl.scrollTop = messagesEl.scrollHeight;
      input.focus();
    }
  }

  toggle.addEventListener('click', () => setOpen(sidebar.hidden));
  closeButton.addEventListener('click', () => setOpen(false));

  function toolDetails(entry) {
    const sections = [];
    if (entry.toolArguments) sections.push(`Arguments\n${entry.toolArguments}`);
    if (entry.toolResult) sections.push(`Result\n${entry.toolResult}`);
    if (sections.length === 0) return entry.running ? 'Waiting for tool output…' : '';
    return sections.join('\n\n');
  }

  function render() {
    messagesEl.innerHTML = '';
    if (history.length === 0) {
      const empty = document.createElement('p');
      empty.className = 'chat-sidebar-empty';
      empty.textContent = 'Ask about the weather, or ask to add or remove a city on your map.';
      messagesEl.appendChild(empty);
    }
    history.forEach((entry) => {
      const item = document.createElement('div');
      const role = MESSAGE_ROLES.includes(entry.role) ? entry.role : 'assistant';
      item.className = `chat-message ${role}`;
      if (role === 'assistant' && !entry.streaming && window.safeGfmMarkdown) {
        item.classList.add('chat-markdown');
        item.innerHTML = window.safeGfmMarkdown.render(entry.content);
      } else {
        item.textContent = entry.content;
      }
      if (role === 'tool') {
        const details = toolDetails(entry);
        if (details) item.title = details;
      }
      messagesEl.appendChild(item);
    });
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function setSending(sending) {
    isSending = sending;
    input.disabled = sending;
    sendButton.disabled = sending;
    sendButton.textContent = sending ? 'Sending…' : 'Send';
  }

  function refreshMapCities() {
    if (!window.weatherMap || typeof window.weatherMap.refreshCities !== 'function') return;
    window.weatherMap.refreshCities().catch((error) => console.error(error));
  }

  function handleEvent(payload, state) {
    if (payload.type === 'session' && payload.sessionId) {
      sessionId = payload.sessionId;
    } else if (payload.type === 'token' && payload.text) {
      state.text += payload.text;
      if (!state.entry) {
        state.entry = { role: 'assistant', content: '', streaming: true };
        history.push(state.entry);
      }
      state.entry.content = state.text;
    } else if (payload.type === 'tool_start' && payload.toolName) {
      history.push({
        role: 'tool',
        content: `Running ${payload.toolName} …`,
        toolName: payload.toolName,
        toolArguments: payload.toolArguments,
        running: true,
      });
    } else if (payload.type === 'tool_end' && payload.toolName) {
      for (let index = history.length - 1; index >= 0; index -= 1) {
        const entry = history[index];
        if (entry.role === 'tool' && entry.running && entry.toolName === payload.toolName) {
          entry.running = false;
          entry.content = `Ran ${payload.toolName} …`;
          entry.toolArguments = payload.toolArguments || entry.toolArguments;
          entry.toolResult = payload.toolResult;
          break;
        }
      }
    } else if ((payload.type === 'error' || payload.type === 'blocked') && payload.errorMessage) {
      history.push({ role: payload.type, content: payload.errorMessage });
    } else if (payload.type === 'done' && state.entry) {
      state.entry.streaming = false;
    } else {
      return;
    }
    render();
  }

  async function streamChat(message) {
    const response = await fetch('/Chat2a/Messages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId, message }),
    });

    if (!response.ok || !response.body) {
      throw new Error(`Chat request failed (${response.status})`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    const state = { text: '', entry: null };
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const parts = buffer.split('\n\n');
        buffer = parts.pop() ?? '';

        for (const part of parts) {
          const line = part.trim();
          if (!line.startsWith('data:')) continue;
          handleEvent(JSON.parse(line.slice(5).trim()), state);
        }
      }
    } finally {
      if (state.entry) state.entry.streaming = false;
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
    setSending(true);
    history.push({ role: 'user', content: message });
    render();

    try {
      await streamChat(message);
    } catch (error) {
      history.push({ role: 'error', content: error.message || 'Chat failed.' });
    } finally {
      setSending(false);
      render();
      input.focus();
      refreshMapCities();
    }
  });
})();
