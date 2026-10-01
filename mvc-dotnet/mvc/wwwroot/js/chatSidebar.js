(() => {
  const button = document.getElementById('chatSidebarButton');
  const sidebar = document.getElementById('chat5a-sidebar');
  const closeButton = document.getElementById('chat5a-sidebar-close');
  const messagesEl = document.getElementById('chat5a-sidebar-messages');
  const form = document.getElementById('chat5a-sidebar-form');
  const input = document.getElementById('chat5a-sidebar-input');
  const sendButton = document.getElementById('chat5a-sidebar-send');
  const gateOptionsEl = document.getElementById('chat5a-sidebar-gate-options');
  const gateCheckboxes = gateOptionsEl
    ? Array.from(gateOptionsEl.querySelectorAll('input[data-sidebar-gate]'))
    : [];

  if (!button || !sidebar || !closeButton || !messagesEl || !form || !input || !sendButton || !window.chatRender) {
    return;
  }

  // The sidebar keeps its own Chat5a session and gate checkboxes, independent of the /chat-clients Chat5a tab.
  let sessionId = null;
  let isSending = false;
  const history = [];

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

  // Rendered by chatRender.js, the same code as the /chat-clients panel.
  function renderMessages() {
    messagesEl.replaceChildren();
    history.forEach((entry) => window.chatRender.renderEntry(entry, messagesEl));
    scrollToBottom();
  }

  function addEntry(entry) {
    history.push(entry);
    window.chatRender.renderEntry(entry, messagesEl);
    scrollToBottom();
    return entry;
  }

  function updateEntry(entry, content) {
    entry.content = content;
    renderMessages();
  }

  function findLastRunningTool(toolName) {
    for (let index = history.length - 1; index >= 0; index -= 1) {
      const entry = history[index];
      if (entry.role === 'tool' && entry.running && entry.toolName === toolName) {
        return entry;
      }
    }

    return null;
  }

  function updateSendingControls() {
    sendButton.disabled = isSending;
    sendButton.textContent = isSending ? 'Sending…' : 'Send';
    input.disabled = isSending;
  }

  // Reads the checkbox for a gate; Code Input and LLM Output default off in the sidebar, the rest on.
  function isGateEnabled(gate, fallback) {
    const checkbox = gateCheckboxes.find((item) => item.dataset.sidebarGate === gate);
    return checkbox ? checkbox.checked : fallback;
  }

  async function streamChat(message) {
    const response = await fetch('/Chat5a/messages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        sessionId,
        message,
        enableMaxLengthGate: isGateEnabled('maxLength', true),
        enableRuleInputGate: isGateEnabled('ruleInput', false),
        enableLlmInputGate: isGateEnabled('llmInput', true),
        enableSystemPromptGuard: isGateEnabled('systemPrompt', true),
        enableLlmOutputGate: isGateEnabled('llmOutput', false),
      }),
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
      if (done) {
        if (assistantEntry) {
          assistantEntry.streaming = false;
          renderMessages();
        }
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
          if (!assistantEntry) {
            assistantEntry = addEntry({ role: 'assistant', content: assistantText, streaming: true });
          } else {
            updateEntry(assistantEntry, assistantText);
          }
        } else if (payload.type === 'tool_start' && payload.toolName) {
          addEntry({
            role: 'tool',
            content: `Running ${payload.toolName} …`,
            toolName: payload.toolName,
            toolArguments: payload.toolArguments,
            running: true,
          });
        } else if (payload.type === 'tool_end' && payload.toolName) {
          const pending = findLastRunningTool(payload.toolName);
          if (pending) {
            pending.running = false;
            pending.toolArguments = payload.toolArguments || pending.toolArguments;
            pending.toolResult = payload.toolResult;
            updateEntry(pending, `Ran ${payload.toolName} …`);
          }
        } else if (payload.type === 'blocked' && payload.errorMessage) {
          addEntry({ role: 'blocked', content: payload.errorMessage });
        } else if (payload.type === 'error' && payload.errorMessage) {
          addEntry({ role: 'error', content: payload.errorMessage });
        } else if (payload.type === 'done') {
          if (assistantEntry) {
            assistantEntry.streaming = false;
            assistantEntry.usage = payload.usage || null;
            renderMessages();
          }
          scrollToBottom();
        }
      }
    }
  }

  window.chatRender.attachToolHover(messagesEl);

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
    addEntry({ role: 'user', content: message });

    try {
      await streamChat(message);
    } catch (error) {
      addEntry({ role: 'error', content: error.message || 'Chat failed.' });
    } finally {
      isSending = false;
      updateSendingControls();
      // Re-read /User after every completion through the map's own path so its pins match the agent's changes.
      try {
        if (window.weatherMap && typeof window.weatherMap.refreshCities === 'function') {
          Promise.resolve(window.weatherMap.refreshCities()).catch(() => {});
        }
      } catch {
        // A failed map refresh must not break the chat.
      }
      // Focus only after updateSendingControls() re-enabled the textarea; a disabled one can't take focus.
      input.focus();
    }
  });
})();
